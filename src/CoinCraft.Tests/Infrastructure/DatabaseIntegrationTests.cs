using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CoinCraft.Domain;
using CoinCraft.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoinCraft.Tests.Infrastructure;

public class DatabaseIntegrationTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbContextOptions<CoinCraftDbContext> _options;

    public DatabaseIntegrationTests()
    {
        _dbPath = Path.GetTempFileName();
        _options = new DbContextOptionsBuilder<CoinCraftDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        using var context = new CoinCraftDbContext(_options);
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task CanAddAndRetrieveTransaction()
    {
        // Arrange
        using var context = new CoinCraftDbContext(_options);
        var account = new Account { Nome = "Test Account", SaldoInicial = 1000 };
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var tx = new Transaction
        {
            Descricao = "Test Transaction",
            Valor = 50,
            Tipo = TransactionType.Despesa,
            Data = DateTime.Now,
            AccountId = account.Id
        };
        context.Transactions.Add(tx);
        await context.SaveChangesAsync();

        // Act
        using var context2 = new CoinCraftDbContext(_options);
        var savedTx = await context2.Transactions.FirstOrDefaultAsync(t => t.Id == tx.Id);

        // Assert
        savedTx.Should().NotBeNull();
        savedTx!.Descricao.Should().Be("Test Transaction");
        savedTx.AccountId.Should().Be(account.Id);
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { }
        }
    }
}