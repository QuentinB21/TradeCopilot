using Microsoft.EntityFrameworkCore;
using TradeCopilot.Application.Abstractions;
using TradeCopilot.Domain;
using TradeCopilot.Infrastructure;
using TradeCopilot.Infrastructure.Persistence;

namespace TradeCopilot.Tests;

public sealed class EfInvestmentRepositoryTenantTests
{
    [Fact]
    public async Task Repository_only_returns_current_user_data()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var userA = new TestCurrentUserContext("user-a");
        var userB = new TestCurrentUserContext("user-b");

        await using (var dbContext = CreateDbContext(databaseName))
        {
            await new EfInvestmentRepository(dbContext, userA).AddPortfolioAsync(new Portfolio
            {
                Name = "PEA A",
                Broker = "Broker",
                BaseCurrency = "EUR",
                Type = PortfolioType.Pea
            });

            await new EfInvestmentRepository(dbContext, userB).AddPortfolioAsync(new Portfolio
            {
                Name = "PEA B",
                Broker = "Broker",
                BaseCurrency = "EUR",
                Type = PortfolioType.Pea
            });
        }

        await using (var dbContext = CreateDbContext(databaseName))
        {
            var portfolios = await new EfInvestmentRepository(dbContext, userA).GetPortfoliosAsync();

            var portfolio = Assert.Single(portfolios);
            Assert.Equal("PEA A", portfolio.Name);
            Assert.Equal("user-a", portfolio.OwnerUserId);
        }
    }

    [Fact]
    public async Task Repository_rejects_cross_user_references()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        Guid userBPortfolioId;

        await using (var dbContext = CreateDbContext(databaseName))
        {
            var portfolio = new Portfolio
            {
                Name = "PEA B",
                Broker = "Broker",
                BaseCurrency = "EUR",
                Type = PortfolioType.Pea
            };
            await new EfInvestmentRepository(dbContext, new TestCurrentUserContext("user-b")).AddPortfolioAsync(portfolio);
            userBPortfolioId = portfolio.Id;
        }

        await using (var dbContext = CreateDbContext(databaseName))
        {
            var repository = new EfInvestmentRepository(dbContext, new TestCurrentUserContext("user-a"));

            await Assert.ThrowsAsync<ArgumentException>(() => repository.AddTransactionAsync(new Transaction
            {
                PortfolioId = userBPortfolioId,
                Type = TransactionType.Deposit,
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                Quantity = 1m,
                UnitPrice = 10m,
                Fees = 0m,
                Currency = "EUR"
            }));
        }
    }

    private static TradeCopilotDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<TradeCopilotDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new TradeCopilotDbContext(options);
    }

    private sealed class TestCurrentUserContext(string userId) : ICurrentUserContext
    {
        public string UserId { get; } = userId;
    }
}
