using System.Globalization;
using System.Text;
using Microsoft.VisualBasic.FileIO;
using TradeCopilot.Application.Contracts.Imports;
using TradeCopilot.Domain;

namespace TradeCopilot.Application.Services.Imports;

public sealed class TradeRepublicTransactionImportStrategy : ITransactionImportStrategy
{
    private static readonly string[] RequiredHeaders =
    [
        "date",
        "category",
        "type",
        "asset_class",
        "name",
        "symbol",
        "shares",
        "price",
        "amount",
        "fee",
        "tax",
        "currency",
        "description",
        "transaction_id"
    ];

    public TransactionImportProvider Provider => TransactionImportProvider.TradeRepublic;

    public Task<TransactionImportParseResult> ParseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using var parser = new TextFieldParser(stream, Encoding.UTF8, detectEncoding: true)
        {
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = false
        };
        parser.SetDelimiters(",");

        if (parser.EndOfData)
        {
            return Task.FromResult(new TransactionImportParseResult(
                0,
                [],
                [new ImportWarningDto(null, "EmptyFile", "Le fichier CSV est vide.", "Selectionnez un export CSV Trade Republic non vide.")]));
        }

        var headers = parser.ReadFields() ?? [];
        var headerIndex = headers
            .Select((header, index) => new { Header = header, Index = index })
            .ToDictionary(item => item.Header, item => item.Index, StringComparer.OrdinalIgnoreCase);

        var missingHeaders = RequiredHeaders.Where(header => !headerIndex.ContainsKey(header)).ToList();
        if (missingHeaders.Count > 0)
        {
            return Task.FromResult(new TransactionImportParseResult(
                0,
                [],
                [new ImportWarningDto(
                    null,
                    "InvalidHeader",
                    $"Colonnes Trade Republic manquantes : {string.Join(", ", missingHeaders)}.",
                    "Verifiez que la provenance choisie correspond bien au fichier importe.")]));
        }

        var rowsRead = 0;
        var transactions = new List<ImportedTransactionCandidate>();
        var warnings = new List<ImportWarningDto>();

        while (!parser.EndOfData)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rowNumber = rowsRead + 2;
            var fields = parser.ReadFields() ?? [];
            rowsRead++;

            try
            {
                var row = new TradeRepublicRow(headerIndex, fields);
                var transaction = MapRow(row, rowNumber, warnings);
                if (transaction is not null)
                {
                    transactions.Add(transaction);
                }
            }
            catch (Exception exception) when (exception is FormatException or ArgumentException or IndexOutOfRangeException)
            {
                warnings.Add(new ImportWarningDto(
                    rowNumber,
                    "InvalidRow",
                    $"Ligne ignoree : {exception.Message}",
                    "Corrigez la ligne dans le CSV ou saisissez l'operation manuellement si elle est importante."));
            }
        }

        return Task.FromResult(new TransactionImportParseResult(rowsRead, transactions, warnings));
    }

    private static ImportedTransactionCandidate? MapRow(TradeRepublicRow row, int rowNumber, List<ImportWarningDto> warnings)
    {
        var transactionId = Required(row["transaction_id"], "transaction_id");
        var date = DateOnly.ParseExact(Required(row["date"], "date"), "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var type = row["type"].Trim().ToUpperInvariant();
        var category = row["category"].Trim().ToUpperInvariant();
        var amount = ParseDecimal(row["amount"]) ?? 0m;
        var fee = ParseDecimal(row["fee"]) ?? 0m;
        var tax = ParseDecimal(row["tax"]) ?? 0m;
        var netCash = amount + fee + tax;
        var currency = (string.IsNullOrWhiteSpace(row["currency"]) ? "EUR" : row["currency"]).Trim().ToUpperInvariant();
        var asset = CreateAssetCandidate(row, currency);
        var comment = BuildComment(row, category, type, transactionId);

        return type switch
        {
            "BUY" => CreateTrade(row, rowNumber, transactionId, TransactionType.Buy, date, currency, comment, asset),
            "SELL" => CreateTrade(row, rowNumber, transactionId, TransactionType.Sell, date, currency, comment, asset),
            "DIVIDEND" => new ImportedTransactionCandidate(rowNumber, transactionId, TransactionType.Dividend, date, 1m, netCash, 0m, currency, comment, asset),
            "CUSTOMER_INPAYMENT" => new ImportedTransactionCandidate(rowNumber, transactionId, netCash >= 0m ? TransactionType.Deposit : TransactionType.Withdrawal, date, 1m, Math.Abs(netCash), 0m, currency, comment, null),
            "INTEREST_PAYMENT" => new ImportedTransactionCandidate(rowNumber, transactionId, netCash >= 0m ? TransactionType.Deposit : TransactionType.Fee, date, 1m, Math.Abs(netCash), 0m, currency, comment, null),
            "SPLIT" => CreateSplitAdjustment(row, rowNumber, transactionId, date, currency, comment, asset),
            "MIGRATION" => Skip(
                rowNumber,
                "SkippedMigration",
                "Ligne technique de migration Trade Republic ignoree.",
                "Trade Republic exporte parfois une sortie puis une entree du meme actif lors d'une migration interne. Les importer creerait de faux achats/ventes et fausserait le PRU. Aucune action n'est necessaire si vos quantites finales sont coherentes apres l'import.",
                warnings),
            _ => Skip(
                rowNumber,
                "UnsupportedType",
                $"Type Trade Republic non supporte : {type}.",
                "Cette operation n'a pas encore de mapping fiable dans TradeCopilot. Verifiez son libelle dans le CSV et saisissez-la manuellement si elle impacte votre portefeuille.",
                warnings)
        };
    }

    private static ImportedTransactionCandidate? CreateTrade(
        TradeRepublicRow row,
        int rowNumber,
        string transactionId,
        TransactionType type,
        DateOnly date,
        string currency,
        string comment,
        ImportedAssetCandidate? asset)
    {
        if (asset is null)
        {
            throw new FormatException("Une operation d'achat ou vente doit contenir un actif.");
        }

        var quantity = Math.Abs(ParseDecimal(row["shares"]) ?? 0m);
        if (quantity <= 0m)
        {
            throw new FormatException("La quantite est vide ou nulle.");
        }

        var unitPrice = ParseDecimal(row["price"]);
        if (unitPrice is null or <= 0m)
        {
            var amount = Math.Abs(ParseDecimal(row["amount"]) ?? 0m);
            unitPrice = amount > 0m ? amount / quantity : 0m;
        }

        var fees = Math.Abs(ParseDecimal(row["fee"]) ?? 0m) + Math.Abs(ParseDecimal(row["tax"]) ?? 0m);
        return new ImportedTransactionCandidate(rowNumber, transactionId, type, date, quantity, unitPrice.Value, fees, currency, comment, asset);
    }

    private static ImportedTransactionCandidate? CreateSplitAdjustment(
        TradeRepublicRow row,
        int rowNumber,
        string transactionId,
        DateOnly date,
        string currency,
        string comment,
        ImportedAssetCandidate? asset)
    {
        if (asset is null)
        {
            throw new FormatException("Un split doit contenir un actif.");
        }

        var quantityDelta = ParseDecimal(row["shares"]) ?? 0m;
        if (quantityDelta == 0m)
        {
            return null;
        }

        var transactionType = quantityDelta > 0m ? TransactionType.Buy : TransactionType.Sell;
        return new ImportedTransactionCandidate(rowNumber, transactionId, transactionType, date, Math.Abs(quantityDelta), 0m, 0m, currency, comment, asset);
    }

    private static ImportedTransactionCandidate? Skip(
        int rowNumber,
        string code,
        string message,
        string recommendation,
        List<ImportWarningDto> warnings)
    {
        warnings.Add(new ImportWarningDto(rowNumber, code, message, recommendation));
        return null;
    }

    private static ImportedAssetCandidate? CreateAssetCandidate(TradeRepublicRow row, string currency)
    {
        var name = row["name"].Trim();
        var symbol = row["symbol"].Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(symbol))
        {
            return null;
        }

        var type = row["asset_class"].Trim().ToUpperInvariant() switch
        {
            "FUND" => AssetType.Etf,
            "STOCK" => AssetType.Stock,
            "CRYPTO" => AssetType.Crypto,
            _ => AssetType.Other
        };

        var isin = LooksLikeIsin(symbol) ? symbol : null;
        return new ImportedAssetCandidate(name, symbol, isin, type, currency);
    }

    private static string BuildComment(TradeRepublicRow row, string category, string type, string transactionId)
    {
        var description = row["description"].Trim();
        var comment = string.IsNullOrWhiteSpace(description)
            ? $"Import Trade Republic {category}/{type}"
            : $"Import Trade Republic {category}/{type} - {description}";

        return Truncate($"{comment} [TR:{transactionId}]", 800);
    }

    private static bool LooksLikeIsin(string value) =>
        value.Length == 12 &&
        value.Take(2).All(char.IsLetter) &&
        value.Skip(2).Take(9).All(char.IsLetterOrDigit) &&
        char.IsDigit(value[^1]);

    private static string Required(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new FormatException($"Le champ {fieldName} est obligatoire.");
        }

        return value.Trim();
    }

    private static decimal? ParseDecimal(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : decimal.Parse(value.Trim(), NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private sealed class TradeRepublicRow(Dictionary<string, int> headerIndex, string[] fields)
    {
        public string this[string header] =>
            headerIndex.TryGetValue(header, out var index) && index < fields.Length
                ? fields[index]
                : string.Empty;
    }
}
