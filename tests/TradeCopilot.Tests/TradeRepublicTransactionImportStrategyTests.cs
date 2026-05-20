using System.Text;
using TradeCopilot.Application.Services.Imports;
using TradeCopilot.Domain;

namespace TradeCopilot.Tests;

public sealed class TradeRepublicTransactionImportStrategyTests
{
    [Fact]
    public async Task Parses_trade_republic_buy_deposit_dividend_and_skips_migration()
    {
        const string csv = """
            "date","category","type","asset_class","name","symbol","shares","price","amount","fee","tax","currency","description","transaction_id"
            "2026-01-02","TRADING","BUY","FUND","S&P 500 Information Tech USD (Acc)","IE00B3WJKG14","1.3804520000","36.2200000000","-50.00","","","EUR","Savings plan execution","buy-id"
            "2026-01-03","CASH","CUSTOMER_INPAYMENT","","","","","","505.000000","-5.00","","EUR","Card Top up","deposit-id"
            "2026-01-04","CASH","DIVIDEND","STOCK","Microsoft","US5949181045","0.2559090000","","0.200000","","-0.10","EUR","Cash Dividend","dividend-id"
            "2026-01-05","DELIVERY","MIGRATION","STOCK","Microsoft","US5949181045","0.2559090000","376.25","","","","EUR","MIGRATION","migration-id"
            """;
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await new TradeRepublicTransactionImportStrategy().ParseAsync(stream);

        Assert.Equal(4, result.RowsRead);
        Assert.Equal(3, result.Transactions.Count);
        Assert.Contains(result.Warnings, warning => warning.Code == "SkippedMigration");

        var buy = Assert.Single(result.Transactions, transaction => transaction.ExternalId == "buy-id");
        Assert.Equal(TransactionType.Buy, buy.Type);
        Assert.Equal(1.3804520000m, buy.Quantity);
        Assert.Equal(36.2200000000m, buy.UnitPrice);
        Assert.Equal(AssetType.Etf, buy.Asset?.Type);
        Assert.Equal("IE00B3WJKG14", buy.Asset?.Isin);

        var deposit = Assert.Single(result.Transactions, transaction => transaction.ExternalId == "deposit-id");
        Assert.Equal(TransactionType.Deposit, deposit.Type);
        Assert.Equal(500m, deposit.UnitPrice);

        var dividend = Assert.Single(result.Transactions, transaction => transaction.ExternalId == "dividend-id");
        Assert.Equal(TransactionType.Dividend, dividend.Type);
        Assert.Equal(0.10m, dividend.UnitPrice);
    }
}
