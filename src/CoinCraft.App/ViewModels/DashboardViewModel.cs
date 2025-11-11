using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CoinCraft.Domain;
using CoinCraft.Infrastructure;
using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace CoinCraft.App.ViewModels;

public sealed class CategorySlice
{
    public string Name { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string ColorHex { get; set; } = "#888888";
}

public sealed class DashboardViewModel : ObservableObject
{
    public DateTime? FilterFrom { get; set; }
    public DateTime? FilterTo { get; set; }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(CanApply));
            }
        }
    }

    public bool CanApply => !_isLoading;

    private decimal _totalReceitas;
    public decimal TotalReceitas
    {
        get => _totalReceitas;
        private set => SetProperty(ref _totalReceitas, value);
    }

    private decimal _totalDespesas;
    public decimal TotalDespesas
    {
        get => _totalDespesas;
        private set => SetProperty(ref _totalDespesas, value);
    }

    private ObservableCollection<CategorySlice> _despesasPorCategoria = new();
    public ObservableCollection<CategorySlice> DespesasPorCategoria
    {
        get => _despesasPorCategoria;
        private set => SetProperty(ref _despesasPorCategoria, value);
    }

    public sealed class AccountBalanceItem
    {
        public string AccountName { get; set; } = string.Empty;
        public decimal Balance { get; set; }
    }

    private ObservableCollection<AccountBalanceItem> _accountBalances = new();
    public ObservableCollection<AccountBalanceItem> AccountBalances
    {
        get => _accountBalances;
        private set => SetProperty(ref _accountBalances, value);
    }

    public sealed class GoalProgressItem
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal Limit { get; set; }
        public decimal Spent { get; set; }
        public decimal Percent => Limit <= 0 ? 0 : Math.Round(Spent / Limit, 4);
    }

    private ObservableCollection<GoalProgressItem> _goalsProgress = new();
    public ObservableCollection<GoalProgressItem> GoalsProgress
    {
        get => _goalsProgress;
        private set => SetProperty(ref _goalsProgress, value);
    }

    public async Task LoadAsync()
    {
        using var db = new CoinCraftDbContext();
        var today = DateTime.Today;
        var firstDay = new DateTime(today.Year, today.Month, 1);
        var nextMonth = firstDay.AddMonths(1);

        var from = FilterFrom ?? firstDay;
        var toExclusive = FilterTo.HasValue ? FilterTo.Value.AddDays(1) : nextMonth;
        var txs = await db.Transactions
            .Where(t => t.Data >= from && t.Data < toExclusive)
            .ToListAsync();

        TotalReceitas = txs.Where(t => t.Tipo == TransactionType.Receita).Sum(t => t.Valor);
        TotalDespesas = txs.Where(t => t.Tipo == TransactionType.Despesa).Sum(t => t.Valor);

        var categories = await db.Categories.ToDictionaryAsync(c => c.Id, c => c);
        var porCat = txs.Where(t => t.Tipo == TransactionType.Despesa && t.CategoryId.HasValue)
            .GroupBy(t => t.CategoryId!.Value)
            .Select(g => new CategorySlice
            {
                Name = categories.TryGetValue(g.Key, out var cat) ? cat.Nome : $"Cat {g.Key}",
                Total = g.Sum(x => x.Valor),
                ColorHex = categories.TryGetValue(g.Key, out var cat2) && !string.IsNullOrWhiteSpace(cat2.CorHex) ? cat2.CorHex! : "#888888"
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        DespesasPorCategoria = new ObservableCollection<CategorySlice>(porCat);

        // Saldos por conta
        var accounts = await db.Accounts.ToListAsync();
        var balances = new List<AccountBalanceItem>();
        foreach (var acc in accounts)
        {
            var receitas = txs.Where(t => t.Tipo == TransactionType.Receita && t.AccountId == acc.Id).Sum(t => t.Valor);
            var despesas = txs.Where(t => t.Tipo == TransactionType.Despesa && t.AccountId == acc.Id).Sum(t => t.Valor);
            var transfOut = txs.Where(t => t.Tipo == TransactionType.Transferencia && t.AccountId == acc.Id).Sum(t => t.Valor);
            var transfIn = txs.Where(t => t.Tipo == TransactionType.Transferencia && t.OpostoAccountId == acc.Id).Sum(t => t.Valor);
            var saldo = acc.SaldoInicial + receitas - despesas - transfOut + transfIn;
            balances.Add(new AccountBalanceItem { AccountName = acc.Nome, Balance = saldo });
        }
        AccountBalances = new ObservableCollection<AccountBalanceItem>(balances.OrderByDescending(b => b.Balance));

        // Metas do mês (usa FilterFrom para ano/mês)
        var baseMonth = new DateTime((FilterFrom ?? firstDay).Year, (FilterFrom ?? firstDay).Month, 1);
        var goals = await db.Goals.Where(g => g.Ano == baseMonth.Year && g.Mes == baseMonth.Month).ToListAsync();
        var goalsVm = new List<GoalProgressItem>();
        foreach (var g in goals)
        {
            var spent = txs.Where(t => t.Tipo == TransactionType.Despesa && t.CategoryId == g.CategoryId).Sum(t => t.Valor);
            var name = categories.TryGetValue(g.CategoryId, out var cat) ? cat.Nome : $"Cat {g.CategoryId}";
            goalsVm.Add(new GoalProgressItem { CategoryName = name, Limit = g.LimiteMensal, Spent = spent });
        }
        GoalsProgress = new ObservableCollection<GoalProgressItem>(goalsVm.OrderByDescending(x => x.Percent));
    }

    private ObservableCollection<TransactionItem> _recentTransactions = new();
    public ObservableCollection<TransactionItem> RecentTransactions
    {
        get => _recentTransactions;
        set => SetProperty(ref _recentTransactions, value);
    }
}