using Microsoft.EntityFrameworkCore;
using CoinCraft.Domain;

namespace CoinCraft.Infrastructure;

public sealed class CoinCraftDbContext : DbContext
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<UserSetting> UserSettings => Set<UserSetting>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dataDir = Path.Combine(appData, "CoinCraft");
        Directory.CreateDirectory(dataDir);
        var dbPath = Path.Combine(dataDir, "coincraft.db");

        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>().Property(x => x.Nome).HasMaxLength(80);
        modelBuilder.Entity<Category>().Property(x => x.Nome).HasMaxLength(80);
        modelBuilder.Entity<Transaction>().Property(x => x.Valor).HasPrecision(18,2);

        // Índices úteis
        modelBuilder.Entity<Transaction>().HasIndex(x => x.Data);
        modelBuilder.Entity<Transaction>().HasIndex(x => new { x.Tipo, x.AccountId });
    }
}
