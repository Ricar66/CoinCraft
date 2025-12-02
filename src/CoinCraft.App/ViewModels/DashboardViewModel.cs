using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CoinCraft.Domain;
using CoinCraft.Infrastructure;
using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using CoinCraft.Services;
using Microsoft.Win32;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CoinCraft.App.Messages;

namespace CoinCraft.App.ViewModels;

public sealed class CategorySlice
{
    public string Name { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string ColorHex { get; set; } = "#888888";
}

public sealed partial class DashboardViewModel : ObservableObject
    {
        private readonly ReportService? _reportService;
        private readonly ExportService _exportService = new();
        private readonly Func<CoinCraftDbContext> _contextFactory;

    public DashboardViewModel(ReportService? reportService = null, Func<CoinCraftDbContext>? contextFactory = null)
    {
        _reportService = reportService;
        _contextFactory = contextFactory ?? (() => new CoinCraftDbContext());

        // Registrar ouvintes de mensagens para atualização em tempo real
        WeakReferenceMessenger.Default.Register<TransactionsChangedMessage>(this, (r, m) => 
        {
            DispatcherHelper.InvokeAsync(async () => await LoadAsync());
        });

        WeakReferenceMessenger.Default.Register<AccountsChangedMessage>(this, (r, m) => 
        {
            DispatcherHelper.InvokeAsync(async () => await LoadAsync());
        });

        WeakReferenceMessenger.Default.Register<CategoriesChangedMessage>(this, (r, m) => 
        {
            DispatcherHelper.InvokeAsync(async () => await LoadAsync());
        });

        WeakReferenceMessenger.Default.Register<GoalsChangedMessage>(this, (r, m) => 
        {
            DispatcherHelper.InvokeAsync(async () => await LoadAsync());
        });

        WeakReferenceMessenger.Default.Register<RecurringTransactionsChangedMessage>(this, (r, m) => 
        {
            DispatcherHelper.InvokeAsync(async () => await LoadAsync());
        });
    }
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
        private set
        {
            if (SetProperty(ref _totalReceitas, value))
            {
                OnPropertyChanged(nameof(TotalSaldo));
            }
        }
    }

    private decimal _totalDespesas;
    public decimal TotalDespesas
    {
        get => _totalDespesas;
        private set
        {
            if (SetProperty(ref _totalDespesas, value))
            {
                OnPropertyChanged(nameof(TotalSaldo));
            }
        }
    }

    public decimal TotalSaldo => TotalReceitas - TotalDespesas;

    private ObservableCollection<CategorySlice> _despesasPorCategoria = new();
    public ObservableCollection<CategorySlice> DespesasPorCategoria
    {
        get => _despesasPorCategoria;
        private set => SetProperty(ref _despesasPorCategoria, value);
    }

    private IEnumerable<ISeries> _pieSeries = Array.Empty<ISeries>();
    public IEnumerable<ISeries> PieSeries
    {
        get => _pieSeries;
        private set => SetProperty(ref _pieSeries, value);
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
        if (IsLoading) return;
        IsLoading = true;
        try
        {
            using var db = _contextFactory();
            var today = DateTime.Today;
            var firstDay = new DateTime(today.Year, today.Month, 1);
            var nextMonth = firstDay.AddMonths(1);

            var from = FilterFrom ?? firstDay;
            var toExclusive = FilterTo.HasValue ? FilterTo.Value.AddDays(1) : nextMonth;

            // Otimização: Calcular totais diretamente no banco (SQLite requer cast para double no Sum)
            var totalRecDouble = await db.Transactions
                .Where(t => t.Data >= from && t.Data < toExclusive && t.Tipo == TransactionType.Receita)
                .SumAsync(t => (double)t.Valor);
            TotalReceitas = (decimal)totalRecDouble;

            var totalDespDouble = await db.Transactions
                .Where(t => t.Data >= from && t.Data < toExclusive && t.Tipo == TransactionType.Despesa)
                .SumAsync(t => (double)t.Valor);
            TotalDespesas = (decimal)totalDespDouble;

            // Atualiza série de comparação Receitas vs Despesas
            UpdateComparisonSeries();

            // Despesas por Categoria (Pizza)
            var catExpensesList = await db.Transactions
                .Where(t => t.Data >= from && t.Data < toExclusive && t.Tipo == TransactionType.Despesa && t.CategoryId != null)
                .GroupBy(t => t.CategoryId!.Value)
                .Select(g => new { CatId = g.Key, Total = g.Sum(t => (double)t.Valor) })
                .OrderByDescending(x => x.Total)
                .ToListAsync();

            var categories = await db.Categories.ToDictionaryAsync(c => c.Id, c => c);
            
            var porCat = catExpensesList.Select(x => new CategorySlice
            {
                Name = categories.TryGetValue(x.CatId, out var cat) ? cat.Nome : $"Cat {x.CatId}",
                Total = (decimal)x.Total,
                ColorHex = categories.TryGetValue(x.CatId, out var cat2) && !string.IsNullOrWhiteSpace(cat2.CorHex) ? cat2.CorHex! : "#888888"
            }).ToList();

            DespesasPorCategoria = new ObservableCollection<CategorySlice>(porCat);

            // Atualiza séries do gráfico de pizza
            var pie = porCat.Select(s => new PieSeries<double>
            {
                Name = s.Name,
                Values = new[] { (double)s.Total },
                Fill = new SolidColorPaint(SKColor.Parse(s.ColorHex))
            }).Cast<ISeries>().ToArray();
            PieSeries = pie;

            // Saldos por conta (Otimizado)
            // Recupera somatórios por conta no período
            var accountSums = await db.Transactions
                .Where(t => t.Data >= from && t.Data < toExclusive)
                .GroupBy(t => t.AccountId)
                .Select(g => new
                {
                    AccountId = g.Key,
                    Receitas = g.Where(t => t.Tipo == TransactionType.Receita).Sum(t => (double)t.Valor),
                    Despesas = g.Where(t => t.Tipo == TransactionType.Despesa).Sum(t => (double)t.Valor),
                    TransfOut = g.Where(t => t.Tipo == TransactionType.Transferencia).Sum(t => (double)t.Valor)
                })
                .ToDictionaryAsync(k => k.AccountId);

            // Recupera transferências de entrada (baseado em OpostoAccountId)
            var transfInSums = await db.Transactions
                .Where(t => t.Data >= from && t.Data < toExclusive && t.Tipo == TransactionType.Transferencia && t.OpostoAccountId != null)
                .GroupBy(t => t.OpostoAccountId!.Value)
                .Select(g => new { AccountId = g.Key, Total = g.Sum(t => (double)t.Valor) })
                .ToDictionaryAsync(k => k.AccountId);

            var accounts = await db.Accounts.ToListAsync();
            var balances = new List<AccountBalanceItem>();
            foreach (var acc in accounts)
            {
                var sums = accountSums.TryGetValue(acc.Id, out var s) ? s : null;
                var rec = (decimal)(sums?.Receitas ?? 0);
                var desp = (decimal)(sums?.Despesas ?? 0);
                var tOut = (decimal)(sums?.TransfOut ?? 0);
                var tIn = (decimal)(transfInSums.TryGetValue(acc.Id, out var ti) ? ti.Total : 0);
                
                var saldo = acc.SaldoInicial + rec - desp - tOut + tIn;
                balances.Add(new AccountBalanceItem { AccountName = acc.Nome, Balance = saldo });
            }
            AccountBalances = new ObservableCollection<AccountBalanceItem>(balances.OrderByDescending(b => b.Balance));

            // Metas do mês (usa FilterFrom para ano/mês)
            var baseMonth = new DateTime((FilterFrom ?? firstDay).Year, (FilterFrom ?? firstDay).Month, 1);
            var goals = await db.Goals.Where(g => g.Ano == baseMonth.Year && g.Mes == baseMonth.Month).ToListAsync();
            
            // Prepara dicionário de gastos por categoria para verificar metas
            var expenseByCatDict = catExpensesList.ToDictionary(x => x.CatId, x => x.Total);

            var goalsVm = new List<GoalProgressItem>();
            if (goals.Count > 0)
            {
                foreach (var g in goals)
                {
                    var spent = expenseByCatDict.TryGetValue(g.CategoryId, out var val) ? val : 0;
                    var name = categories.TryGetValue(g.CategoryId, out var cat) ? cat.Nome : $"Cat {g.CategoryId}";
                    goalsVm.Add(new GoalProgressItem { CategoryName = name, Limit = g.LimiteMensal, Spent = (decimal)spent });
                }
            }
            else
            {
                var catLimits = await db.Categories.Where(c => c.LimiteMensal != null && c.LimiteMensal > 0).ToListAsync();
                foreach (var c in catLimits)
                {
                    var spent = expenseByCatDict.TryGetValue(c.Id, out var val) ? val : 0;
                    goalsVm.Add(new GoalProgressItem { CategoryName = c.Nome, Limit = c.LimiteMensal!.Value, Spent = (decimal)spent });
                }
            }
            GoalsProgress = new ObservableCollection<GoalProgressItem>(goalsVm.OrderByDescending(x => x.Percent));

            // Atualiza histórico de patrimônio
            UpdateNetWorthSeries(12);
            
            await LoadRecentAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private ObservableCollection<TransactionItem> _recentTransactions = new();
    public ObservableCollection<TransactionItem> RecentTransactions
    {
        get => _recentTransactions;
        set => SetProperty(ref _recentTransactions, value);
    }

    private IEnumerable<ISeries> _netWorthSeries = Array.Empty<ISeries>();
    public IEnumerable<ISeries> NetWorthSeries
    {
        get => _netWorthSeries;
        private set => SetProperty(ref _netWorthSeries, value);
    }

    private Axis[] _netWorthXAxis = System.Array.Empty<Axis>();
    public Axis[] NetWorthXAxis
    {
        get => _netWorthXAxis;
        private set => SetProperty(ref _netWorthXAxis, value);
    }

    public void UpdateNetWorthSeries(int months)
    {
        List<ReportService.NetWorthPoint> points;
        // Usa o serviço injetado ou cria uma instância temporária para garantir a lógica otimizada
        var reportService = _reportService ?? new ReportService();
        points = reportService.GetNetWorthHistory(months);

        if (points is null || points.Count == 0)
        {
            NetWorthSeries = Array.Empty<ISeries>();
            NetWorthXAxis = Array.Empty<Axis>();
            return;
        }

        var values = points.Select(p => (double)p.NetWorth).ToArray();
        var labels = points.Select(p => $"{p.Month:00}/{p.Year % 100}").ToArray();

        NetWorthSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Values = values,
                Name = "Patrimônio",
                Fill = new SolidColorPaint(SKColors.SkyBlue)
            }
        };
        NetWorthXAxis = new[]
        {
            new Axis
            {
                Labels = labels,
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                TextSize = 12
            }
        };
    }

    private IEnumerable<ISeries> _comparisonSeries = Array.Empty<ISeries>();
    public IEnumerable<ISeries> ComparisonSeries
    {
        get => _comparisonSeries;
        private set => SetProperty(ref _comparisonSeries, value);
    }

    private Axis[] _comparisonYAxis = System.Array.Empty<Axis>();
    public Axis[] ComparisonYAxis
    {
        get => _comparisonYAxis;
        private set => SetProperty(ref _comparisonYAxis, value);
    }

    private void UpdateComparisonSeries()
    {
        var values = new double[] { (double)TotalReceitas, (double)TotalDespesas };
        // Séries horizontais (RowSeries)
        ComparisonSeries = new ISeries[]
        {
            new RowSeries<double>
            {
                Values = new double[]{ values[0] },
                Name = "Receitas",
                Fill = new SolidColorPaint(SKColors.LightGreen),
                MaxBarWidth = 40
            },
            new RowSeries<double>
            {
                Values = new double[]{ values[1] },
                Name = "Despesas",
                Fill = new SolidColorPaint(SKColors.IndianRed),
                MaxBarWidth = 40
            }
        };
        ComparisonYAxis = new[]
        {
            new Axis
            {
                Labels = new[] { "Receitas", "Despesas" },
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                TextSize = 12
            }
        };
    }

    public async Task LoadRecentAsync()
    {
        using var db = _contextFactory();
        var from = FilterFrom ?? DateTime.Today.AddDays(-30);
        var to = FilterTo ?? DateTime.Today;
        var accounts = await db.Accounts.ToDictionaryAsync(a => a.Id, a => a.Nome);
        var categories = await db.Categories.ToDictionaryAsync(c => c.Id, c => c.Nome);
        var recent = await db.Transactions
            .Where(t => t.Data >= from && t.Data <= to)
            .OrderByDescending(t => t.Data)
            .Take(10)
            .Select(t => new TransactionItem
            {
                Id = t.Id,
                Data = t.Data,
                Tipo = t.Tipo,
                Valor = t.Valor,
                AccountId = t.AccountId,
                CategoryId = t.CategoryId,
                Descricao = t.Descricao,
                OpostoAccountId = t.OpostoAccountId,
                AccountName = accounts.ContainsKey(t.AccountId) ? accounts[t.AccountId] : $"#{t.AccountId}",
                CategoryName = t.CategoryId.HasValue && categories.ContainsKey(t.CategoryId.Value) ? categories[t.CategoryId.Value] : null
            })
            .ToListAsync();

        RecentTransactions = new ObservableCollection<TransactionItem>(recent);
    }

    [RelayCommand]
    private void ExportReport()
    {
        var dlg = new SaveFileDialog
        {
            Title = "Exportar Relatório",
            Filter = "CSV|*.csv",
            FileName = $"relatorio_{DateTime.Now:yyyyMMdd_HHmm}.csv"
        };
        if (dlg.ShowDialog() == true)
        {
            _exportService.ExportToCsv(RecentTransactions, dlg.FileName);
        }
    }
}
