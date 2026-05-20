using System.IO.Compression;
using System.Text;
using TradeCopilot.Application.Services.Imports;
using TradeCopilot.Domain;

namespace TradeCopilot.Tests;

public sealed class BoursobankTransactionImportStrategyTests
{
    [Fact]
    public async Task Parses_boursobank_spreadsheet_export_with_cash_deposit_and_buy()
    {
        await using var stream = CreateBoursobankSpreadsheetExport();

        var result = await new BoursobankTransactionImportStrategy().ParseAsync(stream);

        Assert.Equal(2, result.RowsRead);
        Assert.Empty(result.Warnings);
        Assert.Equal(2, result.Transactions.Count);

        var deposit = Assert.Single(result.Transactions, transaction => transaction.Type == TransactionType.Deposit);
        Assert.Equal(new DateOnly(2026, 5, 15), deposit.Date);
        Assert.Equal(100m, deposit.UnitPrice);
        Assert.Null(deposit.Asset);

        var buy = Assert.Single(result.Transactions, transaction => transaction.Type == TransactionType.Buy);
        Assert.Equal(new DateOnly(2026, 5, 15), buy.Date);
        Assert.Equal(45m, buy.Quantity);
        Assert.Equal(6.62m, buy.UnitPrice);
        Assert.Equal(0.23m, decimal.Round(buy.Fees, 2));
        Assert.Equal("IE0002XZSHO1", buy.Asset?.Isin);
        Assert.Equal(AssetType.Etf, buy.Asset?.Type);
        Assert.False(string.IsNullOrWhiteSpace(buy.ExternalId));
    }

    private static MemoryStream CreateBoursobankSpreadsheetExport()
    {
        string[] sharedStrings =
        [
            "Date operation",
            "Date valeur",
            "Operation",
            "Valeur",
            "Code ISIN",
            "Montant",
            "Quantite",
            "Cours",
            "ALIMENTATION CB ORD/PEA",
            "ACHAT COMPTANT ETR",
            "ISHS VI-ISMWSPE EO",
            "IE0002XZSHO1"
        ];

        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(
                archive,
                "xl/sharedStrings.xml",
                $"""
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <sst xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" count="{sharedStrings.Length}" uniqueCount="{sharedStrings.Length}">
                {string.Join(Environment.NewLine, sharedStrings.Select(value => $"<si><t>{value}</t></si>"))}
                </sst>
                """);

            WriteEntry(
                archive,
                "xl/worksheets/sheet1.xml",
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                  <sheetData>
                    <row r="1">
                      <c r="A1" t="s"><v>0</v></c><c r="B1" t="s"><v>1</v></c><c r="C1" t="s"><v>2</v></c><c r="D1" t="s"><v>3</v></c>
                      <c r="E1" t="s"><v>4</v></c><c r="F1" t="s"><v>5</v></c><c r="G1" t="s"><v>6</v></c><c r="H1" t="s"><v>7</v></c>
                    </row>
                    <row r="2">
                      <c r="A2"><v>46157</v></c><c r="B2"><v>46157</v></c><c r="C2" t="s"><v>8</v></c>
                      <c r="F2"><v>100</v></c><c r="G2"><v>0</v></c><c r="H2"><v>0</v></c>
                    </row>
                    <row r="3">
                      <c r="A3"><v>46157</v></c><c r="B3"><v>46161</v></c><c r="C3" t="s"><v>9</v></c><c r="D3" t="s"><v>10</v></c>
                      <c r="E3" t="s"><v>11</v></c><c r="F3"><v>-298.13</v></c><c r="G3"><v>45</v></c><c r="H3"><v>6.62</v></c>
                    </row>
                  </sheetData>
                </worksheet>
                """);
        }

        stream.Position = 0;
        return stream;
    }

    private static void WriteEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }
}
