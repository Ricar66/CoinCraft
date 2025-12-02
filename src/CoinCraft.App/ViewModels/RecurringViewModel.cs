using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CoinCraft.Domain;
using CoinCraft.Infrastructure;
using System.Linq;
using System;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using CoinCraft.App.Messages;

namespace CoinCraft.App.ViewModels;

public sealed class RecurringViewModel : ObservableObject
{
    private readonly Func<CoinCraftDbContext> _contextFactory;

    public RecurringViewModel(Func<CoinCraftDbContext>? contextFactory = null)
    {
        _contextFactory = contextFactory ?? (() => new CoinCraftDbContext());

        // Ouvintes para atualização de lookups
        WeakReferenceMessenger.Default.Register<AccountsChangedMessage>(this, (r, m) =>
        {
            DispatcherHelper.InvokeAsync(async () => await LoadAsync());
        });

        WeakReferenceMessenger.Default.Register<CategoriesChangedMessage>(this, (r, m) =>
        {
            DispatcherHelper.InvokeAsync(async () => await LoadAsync());
        });
    }

    public ObservableCollection<RecurringTransaction> Items { get; private set; } = new();
    public string? StatusMessage { get; private set; }
    public RecurringTransaction? Selected { get; set; }
    public System.Collections.Generic.List<Account> Accounts { get; private set; } = new();
    public System.Collections.Generic.List<Category> Categories { get; private set; } = new();

    // Filtros
    public int? FilterAccountId { get; set; }
    public int? FilterCategoryId { get; set; }
    public RecurrenceFrequency? FilterFrequency { get; set; }
    public DateTime? FilterFrom { get; set; }
    public DateTime? FilterTo { get; set; }
    public string? FilterNome { get; set; }

    public async Task LoadAsync()
    {
        using var db = _contextFactory();
        Accounts = db.Accounts.OrderBy(a => a.Nome).ToList();
        Categories = db.Categories.OrderBy(c => c.Nome).ToList();

        var query = db.RecurringTransactions.AsQueryable();
        if (FilterAccountId.HasValue)
            query = query.Where(r => r.AccountId == FilterAccountId.Value);
        if (FilterCategoryId.HasValue)
            query = query.Where(r => r.CategoryId == FilterCategoryId.Value);
        if (FilterFrequency.HasValue)
            query = query.Where(r => r.Frequencia == FilterFrequency.Value);
        if (FilterFrom.HasValue)
            query = query.Where(r => r.NextRunDate >= FilterFrom.Value);
        if (FilterTo.HasValue)
            query = query.Where(r => r.NextRunDate <= FilterTo.Value);
        if (!string.IsNullOrWhiteSpace(FilterNome))
            query = query.Where(r => r.Nome.Contains(FilterNome));

        var items = query.OrderBy(r => r.NextRunDate).ToList();
        Items = new ObservableCollection<RecurringTransaction>(items);

        var parts = new System.Collections.Generic.List<string>();
        if (FilterAccountId.HasValue)
        {
            var accName = Accounts.FirstOrDefault(a => a.Id == FilterAccountId.Value)?.Nome ?? $"#{FilterAccountId.Value}";
            parts.Add($"Conta: {accName}");
        }
        if (FilterCategoryId.HasValue)
        {
            var catName = Categories.FirstOrDefault(c => c.Id == FilterCategoryId.Value)?.Nome ?? $"#{FilterCategoryId.Value}";
            parts.Add($"Categoria: {catName}");
        }
        if (FilterFrequency.HasValue)
        {
            parts.Add($"Frequência: {FilterFrequency.Value}");
        }
        if (!string.IsNullOrWhiteSpace(FilterNome))
        {
            parts.Add($"Nome contém: '{FilterNome}'");
        }
        if (FilterFrom.HasValue || FilterTo.HasValue)
        {
            var fromTxt = FilterFrom.HasValue ? FilterFrom.Value.ToString("d") : "…";
            var toTxt = FilterTo.HasValue ? FilterTo.Value.ToString("d") : "…";
            parts.Add($"Próxima execução: {fromTxt}–{toTxt}");
        }
        StatusMessage = parts.Count == 0
            ? $"{Items.Count} recorrentes"
            : $"{Items.Count} recorrentes — {string.Join(", ", parts)}";

        OnPropertyChanged(nameof(Items));
        OnPropertyChanged(nameof(StatusMessage));
    }

    public async Task AddAsync(RecurringTransaction r)
    {
        using var db = _contextFactory();
        if (r.NextRunDate < DateTime.Today) r.NextRunDate = DateTime.Today;
        db.RecurringTransactions.Add(r);
        await db.SaveChangesAsync();
        StatusMessage = "Recorrente adicionado.";
        WeakReferenceMessenger.Default.Send(new RecurringTransactionsChangedMessage("Add"));
    }

    public async Task UpdateAsync(RecurringTransaction r)
    {
        using var db = _contextFactory();
        var entity = await db.RecurringTransactions.FindAsync(r.Id);
        if (entity is null) return;
        entity.Nome = r.Nome;
        entity.Frequencia = r.Frequencia;
        entity.StartDate = r.StartDate;
        entity.EndDate = r.EndDate;
        entity.DiaDaSemana = r.DiaDaSemana;
        entity.DiaDoMes = r.DiaDoMes;
        entity.AutoLancamento = r.AutoLancamento;
        entity.NextRunDate = r.NextRunDate;
        entity.Tipo = r.Tipo;
        entity.Valor = r.Valor;
        entity.AccountId = r.AccountId;
        entity.CategoryId = r.CategoryId;
        entity.Descricao = r.Descricao;
        entity.OpostoAccountId = r.OpostoAccountId;
        await db.SaveChangesAsync();
        StatusMessage = "Recorrente atualizado.";
        WeakReferenceMessenger.Default.Send(new RecurringTransactionsChangedMessage("Update"));
    }

    public async Task DeleteAsync(int id)
    {
        using var db = _contextFactory();
        var entity = await db.RecurringTransactions.FindAsync(id);
        if (entity is null) return;
        db.RecurringTransactions.Remove(entity);
        await db.SaveChangesAsync();
        StatusMessage = "Recorrente excluído.";
        WeakReferenceMessenger.Default.Send(new RecurringTransactionsChangedMessage("Delete"));
    }
}