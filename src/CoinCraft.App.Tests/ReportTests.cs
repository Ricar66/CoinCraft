using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CoinCraft.Domain;
using CoinCraft.Infrastructure;
using CoinCraft.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoinCraft.App.Tests;

public class ReportTests : IDisposable
{
    private readonly string _dbPath;
    private readonly Func<CoinCraftDbContext> _contextFactory;

    public ReportTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"coincraft_report_test_{Guid.NewGuid()}.db");
        _contextFactory = () =>
        {
            var options = new DbContextOptionsBuilder<CoinCraftDbContext>()
                .UseSqlite($"Data Source={_dbPath}")
                .Options;
            return new CoinCraftDbContext(options);
        };

        using var db = _contextFactory();
        db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { }
        }
    }

    [Fact]
    public void GetNetWorthHistory_ShouldCalculateCorrectly()
    {
        // Arrange
        using (var db = _contextFactory())
        {
            var acc = new Account { Nome = "Acc1", SaldoInicial = 1000 };
            db.Accounts.Add(acc);
            db.SaveChanges();

            // Month 1: +500
            db.Transactions.Add(new Transaction
            {
                AccountId = acc.Id,
                Tipo = TransactionType.Receita,
                Valor = 500,
                Data = DateTime.Today.AddMonths(-2)
            });

            // Month 2: -200
            db.Transactions.Add(new Transaction
            {
                AccountId = acc.Id,
                Tipo = TransactionType.Despesa,
                Valor = 200,
                Data = DateTime.Today.AddMonths(-1)
            });

            db.SaveChanges();
        }

        var service = new ReportService(_contextFactory);

        // Act
        var history = service.GetNetWorthHistory(3);

        // Assert
        // History should have 3 points.
        // Point 0 (2 months ago): Initial 1000 + 500 = 1500
        // Point 1 (1 month ago): 1500 - 200 = 1300
        // Point 2 (Current): 1300 (no change this month)
        
        Assert.Equal(3, history.Count);
        
        // Order is usually oldest to newest or newest to oldest?
        // ReportService code: "points.Add(new NetWorthPoint...)" inside loop "for (int i = months - 1; i >= 0; i--)"
        // i=2 (2 months ago) -> added first.
        // So list is ordered oldest to newest.
        
        var p1 = history[0]; // 2 months ago
        var p2 = history[1]; // 1 month ago
        var p3 = history[2]; // current

        Assert.Equal(1500, p1.NetWorth);
        Assert.Equal(1300, p2.NetWorth);
        Assert.Equal(1300, p3.NetWorth);
    }
}
