using System;
using System.IO;
using System.Threading.Tasks;
using CoinCraft.App.ViewModels;
using CoinCraft.Domain;
using CoinCraft.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoinCraft.Tests.ViewModels;

public class DashboardViewModelTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbContextOptions<CoinCraftDbContext> _options;

    public DashboardViewModelTests()
    {
        _dbPath = Path.GetTempFileName();
        _options = new DbContextOptionsBuilder<CoinCraftDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        using var context = new CoinCraftDbContext(_options);
        context.Database.EnsureCreated();
    }

    private CoinCraftDbContext CreateContext() => new CoinCraftDbContext(_options);

    [Fact]
    public async Task LoadAsync_ShouldCalculateTotalsCorrectly()
    {
        // Arrange
        using (var context = CreateContext())
        {
            context.Transactions.AddRange(
                new Transaction { Valor = 100, Tipo = TransactionType.Receita, Data = DateTime.Today },
                new Transaction { Valor = 50, Tipo = TransactionType.Despesa, Data = DateTime.Today }
            );
            await context.SaveChangesAsync();
        }

        var vm = new DashboardViewModel(null, CreateContext);

        // Act
        await vm.LoadAsync();

        // Assert
        vm.TotalReceitas.Should().Be(100);
        vm.TotalDespesas.Should().Be(50);
        vm.TotalSaldo.Should().Be(50);
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { }
        }
    }
}