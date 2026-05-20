using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Microsoft.VisualBasic.FileIO;
using TradeCopilot.Application.Contracts.Imports;
using TradeCopilot.Domain;

namespace TradeCopilot.Application.Services.Imports;

public sealed class BoursobankTransactionImportStrategy : ITransactionImportStrategy
{
    private const string ProviderPrefix = "BOURSO";

    private static readonly string[] RequiredHeaders =
    [
        "Date operation",
        "Date valeur",
        "Operation",
        "Valeur",
        "Code ISIN",
        "Montant",
        "Quantite",
        "Cours"
    ];

    public TransactionImportProvider Provider => TransactionImportProvider.Boursobank;

    public async Task<TransactionImportParseResult> ParseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        await using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        return LooksLikeOpenXmlSpreadsheet(buffer)
            ? ParseSpreadsheet(buffer, cancellationToken)
            : ParseDelimitedCsv(buffer, cancellationToken);
    }

    private static TransactionImportParseResult ParseSpreadsheet(Stream stream, CancellationToken cancellationToken)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var sharedStrings = ReadSharedStrings(archive);
        var worksheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml")
            ?? archive.Entries.FirstOrDefault(entry => entry.FullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase)
                && entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));

        if (worksheetEntry is null)
        {
            return InvalidFile("Aucune feuille Excel lisible n'a ete trouvee dans l'export Boursobank.");
        }

        XNamespace spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        using var worksheetStream = worksheetEntry.Open();
        var worksheet = XDocument.Load(worksheetStream);
        var parsedRows = worksheet
            .Descendants(spreadsheet + "row")
            .Select(row => ReadSpreadsheetRow(row, spreadsheet, sharedStrings))
            .Where(row => row.Cells.Count > 0)
            .ToList();

        if (parsedRows.Count == 0)
        {
            return EmptyFile();
        }

        var headerRow = parsedRows[0];
        var headerByColumn = headerRow.Cells.ToDictionary(
            cell => cell.Key,
            cell => NormalizeHeader(cell.Value),
            StringComparer.OrdinalIgnoreCase);

        var missingHeaders = MissingHeaders(headerByColumn.Values);
        if (missingHeaders.Count > 0)
        {
            return InvalidHeader(missingHeaders);
        }

        var rows = parsedRows
            .Skip(1)
            .Select(row => new BoursobankRow(
                row.RowNumber,
                row.Cells
                    .Where(cell => headerByColumn.ContainsKey(cell.Key))
                    .ToDictionary(cell => headerByColumn[cell.Key], cell => cell.Value, StringComparer.OrdinalIgnoreCase)))
            .Where(row => !row.IsEmpty)
            .ToList();

        return MapRows(rows, cancellationToken);
    }

    private static TransactionImportParseResult ParseDelimitedCsv(Stream stream, CancellationToken cancellationToken)
    {
        stream.Position = 0;
        using var parser = new TextFieldParser(stream, Encoding.UTF8, detectEncoding: true)
        {
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = false
        };
        parser.SetDelimiters(";");

        if (parser.EndOfData)
        {
            return EmptyFile();
        }

        var headers = parser.ReadFields() ?? [];
        var headerIndex = headers
            .Select((header, index) => new { Header = NormalizeHeader(header), Index = index })
            .ToDictionary(item => item.Header, item => item.Index, StringComparer.OrdinalIgnoreCase);

        var missingHeaders = MissingHeaders(headerIndex.Keys);
        if (missingHeaders.Count > 0)
        {
            return InvalidHeader(missingHeaders);
        }

        var rows = new List<BoursobankRow>();
        var rowsRead = 0;
        while (!parser.EndOfData)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rowNumber = rowsRead + 2;
            var fields = parser.ReadFields() ?? [];
            rowsRead++;

            var values = headerIndex.ToDictionary(
                item => item.Key,
                item => item.Value < fields.Length ? fields[item.Value] : string.Empty,
                StringComparer.OrdinalIgnoreCase);

            var row = new BoursobankRow(rowNumber, values);
            if (!row.IsEmpty)
            {
                rows.Add(row);
            }
        }

        return MapRows(rows, cancellationToken);
    }

    private static TransactionImportParseResult MapRows(IReadOnlyCollection<BoursobankRow> rows, CancellationToken cancellationToken)
    {
        var transactions = new List<ImportedTransactionCandidate>();
        var warnings = new List<ImportWarningDto>();

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var transaction = MapRow(row, warnings);
                if (transaction is not null)
                {
                    transactions.Add(transaction);
                }
            }
            catch (Exception exception) when (exception is FormatException or ArgumentException or OverflowException)
            {
                warnings.Add(new ImportWarningDto(
                    row.RowNumber,
                    "InvalidRow",
                    $"Ligne Boursobank ignoree : {exception.Message}",
                    "Corrigez la ligne dans le fichier exporte ou saisissez l'operation manuellement si elle impacte votre portefeuille."));
            }
        }

        return new TransactionImportParseResult(rows.Count, transactions, warnings);
    }

    private static ImportedTransactionCandidate? MapRow(BoursobankRow row, List<ImportWarningDto> warnings)
    {
        var operation = Required(row["Operation"], "Operation");
        var normalizedOperation = NormalizeOperation(operation);
        var amount = ParseDecimal(Required(row["Montant"], "Montant"))
            ?? throw new FormatException("Le champ Montant est obligatoire.");
        var quantity = ParseDecimal(row["Quantite"]) ?? 0m;
        var unitPrice = ParseDecimal(row["Cours"]) ?? 0m;
        var date = ParseDate(Required(row["Date operation"], "Date operation"));
        var currency = "EUR";
        var externalId = BuildExternalId(row);
        var asset = CreateAssetCandidate(row, currency);
        var comment = BuildComment(operation, row["Valeur"], externalId);

        if (normalizedOperation.Contains("ALIMENTATION", StringComparison.Ordinal)
            || normalizedOperation.Contains("VIREMENT", StringComparison.Ordinal)
            || normalizedOperation.Contains("VERSEMENT", StringComparison.Ordinal)
            || normalizedOperation.Contains("RETRAIT", StringComparison.Ordinal))
        {
            return new ImportedTransactionCandidate(
                row.RowNumber,
                externalId,
                amount >= 0m ? TransactionType.Deposit : TransactionType.Withdrawal,
                date,
                1m,
                Math.Abs(amount),
                0m,
                currency,
                comment,
                null);
        }

        if (normalizedOperation.Contains("ACHAT", StringComparison.Ordinal))
        {
            return CreateTrade(row, externalId, TransactionType.Buy, date, Math.Abs(amount), Math.Abs(quantity), unitPrice, currency, comment, asset);
        }

        if (normalizedOperation.Contains("VENTE", StringComparison.Ordinal))
        {
            return CreateTrade(row, externalId, TransactionType.Sell, date, Math.Abs(amount), Math.Abs(quantity), unitPrice, currency, comment, asset);
        }

        if (normalizedOperation.Contains("DIVIDENDE", StringComparison.Ordinal)
            || normalizedOperation.Contains("COUPON", StringComparison.Ordinal))
        {
            return new ImportedTransactionCandidate(row.RowNumber, externalId, TransactionType.Dividend, date, 1m, Math.Abs(amount), 0m, currency, comment, asset);
        }

        if (normalizedOperation.Contains("FRAIS", StringComparison.Ordinal))
        {
            return new ImportedTransactionCandidate(row.RowNumber, externalId, TransactionType.Fee, date, 1m, Math.Abs(amount), 0m, currency, comment, asset);
        }

        warnings.Add(new ImportWarningDto(
            row.RowNumber,
            "UnsupportedOperation",
            $"Operation Boursobank non supportee : {operation}.",
            "Cette operation n'a pas encore de mapping fiable. Verifiez son libelle dans l'export et saisissez-la manuellement si elle change votre cash, vos quantites ou votre PRU."));
        return null;
    }

    private static ImportedTransactionCandidate CreateTrade(
        BoursobankRow row,
        string externalId,
        TransactionType type,
        DateOnly date,
        decimal absoluteAmount,
        decimal quantity,
        decimal unitPrice,
        string currency,
        string comment,
        ImportedAssetCandidate? asset)
    {
        if (asset is null)
        {
            throw new FormatException("Une operation d'achat ou vente doit contenir une valeur et un code ISIN.");
        }

        if (quantity <= 0m)
        {
            throw new FormatException("La quantite est vide ou nulle.");
        }

        if (unitPrice <= 0m)
        {
            unitPrice = absoluteAmount / quantity;
        }

        var grossAmount = quantity * unitPrice;
        var fees = type == TransactionType.Buy
            ? Math.Max(0m, absoluteAmount - grossAmount)
            : Math.Max(0m, grossAmount - absoluteAmount);

        return new ImportedTransactionCandidate(row.RowNumber, externalId, type, date, quantity, unitPrice, fees, currency, comment, asset);
    }

    private static ImportedAssetCandidate? CreateAssetCandidate(BoursobankRow row, string currency)
    {
        var name = row["Valeur"].Trim();
        var isin = row["Code ISIN"].Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(isin))
        {
            return null;
        }

        return new ImportedAssetCandidate(name, isin, LooksLikeIsin(isin) ? isin : null, InferAssetType(name), currency);
    }

    private static AssetType InferAssetType(string name)
    {
        var normalizedName = NormalizeOperation(name);
        return normalizedName.Contains("ETF", StringComparison.Ordinal)
            || normalizedName.Contains("ISH", StringComparison.Ordinal)
            || normalizedName.Contains("AMUNDI", StringComparison.Ordinal)
            || normalizedName.Contains("LYXOR", StringComparison.Ordinal)
            || normalizedName.Contains("SPDR", StringComparison.Ordinal)
            || normalizedName.Contains("MSCI", StringComparison.Ordinal)
                ? AssetType.Etf
                : AssetType.Stock;
    }

    private static bool LooksLikeOpenXmlSpreadsheet(Stream stream)
    {
        stream.Position = 0;
        Span<byte> signature = stackalloc byte[2];
        var bytesRead = stream.Read(signature);
        stream.Position = 0;
        return bytesRead == 2 && signature[0] == 0x50 && signature[1] == 0x4B;
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
        {
            return [];
        }

        XNamespace spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        using var stream = entry.Open();
        var document = XDocument.Load(stream);
        return document
            .Descendants(spreadsheet + "si")
            .Select(item => string.Concat(item.Descendants(spreadsheet + "t").Select(text => text.Value)))
            .ToList();
    }

    private static SpreadsheetRow ReadSpreadsheetRow(XElement row, XNamespace spreadsheet, IReadOnlyList<string> sharedStrings)
    {
        var rowNumber = int.TryParse(row.Attribute("r")?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedRowNumber)
            ? parsedRowNumber
            : 0;

        var cells = row
            .Elements(spreadsheet + "c")
            .Select(cell => new
            {
                Column = GetColumnReference(cell.Attribute("r")?.Value),
                Value = ReadCellValue(cell, spreadsheet, sharedStrings)
            })
            .Where(cell => !string.IsNullOrWhiteSpace(cell.Column))
            .ToDictionary(cell => cell.Column, cell => cell.Value, StringComparer.OrdinalIgnoreCase);

        return new SpreadsheetRow(rowNumber, cells);
    }

    private static string ReadCellValue(XElement cell, XNamespace spreadsheet, IReadOnlyList<string> sharedStrings)
    {
        var rawValue = cell.Element(spreadsheet + "v")?.Value ?? string.Empty;
        if (cell.Attribute("t")?.Value == "s"
            && int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sharedStringIndex)
            && sharedStringIndex >= 0
            && sharedStringIndex < sharedStrings.Count)
        {
            return sharedStrings[sharedStringIndex];
        }

        return rawValue;
    }

    private static string GetColumnReference(string? cellReference)
    {
        if (string.IsNullOrWhiteSpace(cellReference))
        {
            return string.Empty;
        }

        var letters = cellReference.TakeWhile(char.IsLetter).ToArray();
        return new string(letters);
    }

    private static DateOnly ParseDate(string value)
    {
        var trimmed = value.Trim();
        if (double.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out var serialDate))
        {
            return DateOnly.FromDateTime(DateTime.FromOADate(serialDate));
        }

        string[] formats = ["yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy"];
        if (DateOnly.TryParseExact(trimmed, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        if (DateOnly.TryParse(trimmed, CultureInfo.GetCultureInfo("fr-FR"), DateTimeStyles.None, out date))
        {
            return date;
        }

        throw new FormatException($"La date '{value}' n'est pas reconnue.");
    }

    private static decimal? ParseDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim().Replace(" ", string.Empty);
        if (decimal.TryParse(trimmed, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var invariantValue))
        {
            return invariantValue;
        }

        if (decimal.TryParse(trimmed, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo("fr-FR"), out var frenchValue))
        {
            return frenchValue;
        }

        throw new FormatException($"Le nombre '{value}' n'est pas reconnu.");
    }

    private static string BuildExternalId(BoursobankRow row)
    {
        var stableContent = string.Join(
            "|",
            RequiredHeaders.Select(header => (row[header] ?? string.Empty).Trim().ToUpperInvariant()));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(stableContent));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string BuildComment(string operation, string value, string externalId)
    {
        var label = string.IsNullOrWhiteSpace(value)
            ? $"Import Boursobank {operation}"
            : $"Import Boursobank {operation} - {value.Trim()}";

        return Truncate($"{label} [{ProviderPrefix}:{externalId[..12]}]", 800);
    }

    private static List<string> MissingHeaders(IEnumerable<string> headers)
    {
        var headerSet = headers.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return RequiredHeaders.Where(header => !headerSet.Contains(header)).ToList();
    }

    private static TransactionImportParseResult EmptyFile() =>
        new(0, [], [new ImportWarningDto(null, "EmptyFile", "Le fichier Boursobank est vide.", "Selectionnez un export Boursobank non vide.")]);

    private static TransactionImportParseResult InvalidHeader(IReadOnlyCollection<string> missingHeaders) =>
        new(0, [], [new ImportWarningDto(
            null,
            "InvalidHeader",
            $"Colonnes Boursobank manquantes : {string.Join(", ", missingHeaders)}.",
            "Verifiez que la provenance choisie est bien Boursobank et que le fichier contient l'onglet Operations exporte depuis la banque.")]);

    private static TransactionImportParseResult InvalidFile(string message) =>
        new(0, [], [new ImportWarningDto(null, "InvalidFile", message, "Regénérez l'export depuis Boursobank puis relancez l'import.")]);

    private static string NormalizeHeader(string value) =>
        RemoveDiacritics(value).Trim();

    private static string NormalizeOperation(string value) =>
        RemoveDiacritics(value).Trim().ToUpperInvariant();

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string Required(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new FormatException($"Le champ {fieldName} est obligatoire.");
        }

        return value.Trim();
    }

    private static bool LooksLikeIsin(string value) =>
        value.Length == 12 &&
        value.Take(2).All(char.IsLetter) &&
        value.Skip(2).Take(9).All(char.IsLetterOrDigit) &&
        char.IsDigit(value[^1]);

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private sealed record SpreadsheetRow(int RowNumber, Dictionary<string, string> Cells);

    private sealed class BoursobankRow(int rowNumber, Dictionary<string, string> values)
    {
        public int RowNumber { get; } = rowNumber;

        public bool IsEmpty => values.Values.All(string.IsNullOrWhiteSpace);

        public string this[string header] =>
            values.TryGetValue(NormalizeHeader(header), out var value)
                ? value
                : string.Empty;
    }
}
