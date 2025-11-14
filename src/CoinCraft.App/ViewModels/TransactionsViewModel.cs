using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CoinCraft.Domain;
using CoinCraft.Infrastructure;
using CoinCraft.Services;
using System.Windows;

namespace CoinCraft.App.ViewModels;

public sealed class TransactionItem
{
    public int Id { get; set; }
    public DateTime Data { get; set; }
    public TransactionType Tipo { get; set; }
    public decimal Valor { get; set; }
    public int AccountId { get; set; }
    public int? CategoryId { get; set; }
    public string? Descricao { get; set; }
    public int? OpostoAccountId { get; set; }
    public string? AttachmentPath { get; set; }

    public string AccountName { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
}

public sealed class TransactionsViewModel : ObservableObject
{
    private readonly LogService _log;
    private string? _statusMessage;
    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    private ObservableCollection<TransactionItem> _transactions = new();
    public ObservableCollection<TransactionItem> Transactions
    {
        get => _transactions;
        private set => SetProperty(ref _transactions, value);
    }

    private TransactionItem? _selected;
    public TransactionItem? Selected
    {
        get => _selected;
        set => SetProperty(ref _selected, value);
    }

    public List<Account> Accounts { get; private set; } = new();
    public List<Category> Categories { get; private set; } = new();

    public DateTime? FilterFrom { get; set; }
    public DateTime? FilterTo { get; set; }
    public int? FilterAccountId { get; set; }
    public int? FilterCategoryId { get; set; }

    public TransactionsViewModel(LogService log)
    {
        _log = log;
    }

    public async Task LoadAsync()
    {
        using var db = new CoinCraftDbContext();
        var accounts = await Task.Run(() => db.Accounts.OrderBy(a => a.Nome).ToList());
        var categories = await Task.Run(() => db.Categories.OrderBy(c => c.Nome).ToList());
        Accounts = accounts;
        Categories = categories;

        var accMap = accounts.ToDictionary(a => a.Id, a => a.Nome);
        var catMap = categories.ToDictionary(c => c.Id, c => c.Nome);

        var q = db.Transactions.AsQueryable();
        if (FilterFrom.HasValue) q = q.Where(t => t.Data >= FilterFrom.Value);
        if (FilterTo.HasValue) q = q.Where(t => t.Data <= FilterTo.Value);
        if (FilterAccountId.HasValue) q = q.Where(t => t.AccountId == FilterAccountId.Value);
        if (FilterCategoryId.HasValue) q = q.Where(t => t.CategoryId == FilterCategoryId.Value);

        var items = await Task.Run(() => q
            .OrderByDescending(t => t.Data)
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
                AttachmentPath = t.AttachmentPath,
                AccountName = accMap.ContainsKey(t.AccountId) ? accMap[t.AccountId] : $"#{t.AccountId}",
                CategoryName = t.CategoryId.HasValue && catMap.ContainsKey(t.CategoryId.Value) ? catMap[t.CategoryId.Value] : null
            }).ToList());

        Transactions = new ObservableCollection<TransactionItem>(items);
        var parts = new List<string>();
        if (FilterAccountId.HasValue)
        {
            var accName = accMap.TryGetValue(FilterAccountId.Value, out var aName) ? aName : $"#{FilterAccountId.Value}";
            parts.Add($"Conta: {accName}");
        }
        if (FilterCategoryId.HasValue)
        {
            var catName = catMap.TryGetValue(FilterCategoryId.Value, out var cName) ? cName : $"#{FilterCategoryId.Value}";
            parts.Add($"Categoria: {catName}");
        }
        if (FilterFrom.HasValue || FilterTo.HasValue)
        {
            var fromTxt = FilterFrom.HasValue ? FilterFrom.Value.ToString("d") : "…";
            var toTxt = FilterTo.HasValue ? FilterTo.Value.ToString("d") : "…";
            parts.Add($"Período: {fromTxt}–{toTxt}");
        }
        StatusMessage = parts.Count == 0
            ? $"{Transactions.Count} lançamentos carregados."
            : $"{Transactions.Count} lançamentos carregados — {string.Join(", ", parts)}";
    }

    public async Task AddAsync(Transaction tx)
    {
        try
        {
            using var db = new CoinCraftDbContext();
            db.Transactions.Add(tx);
            await db.SaveChangesAsync();
            StatusMessage = "Lançamento adicionado com sucesso.";
        }
        catch (Exception ex)
        {
            _log.Error($"Falha ao adicionar lançamento: {ex.Message}");
            MessageBox.Show(ex.Message, "Erro ao salvar lançamento", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public async Task UpdateAsync(Transaction tx)
    {
        try
        {
            using var db = new CoinCraftDbContext();
            var entity = await db.Transactions.FindAsync(tx.Id);
            if (entity is null) return;
            // Limpa anexo antigo se foi removido ou substituído
            try
            {
                var oldPath = entity.AttachmentPath;
                var newPath = tx.AttachmentPath;
                if (!string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(oldPath) && System.IO.File.Exists(oldPath))
                {
                    System.IO.File.Delete(oldPath);
                }
            }
            catch (Exception fileEx)
            {
                _log.Error($"Falha ao remover anexo antigo do lançamento #{tx.Id}: {fileEx.Message}");
            }
            entity.Data = tx.Data;
            entity.Tipo = tx.Tipo;
            entity.Valor = tx.Valor;
            entity.AccountId = tx.AccountId;
            entity.CategoryId = tx.CategoryId;
            entity.Descricao = tx.Descricao;
            entity.OpostoAccountId = tx.OpostoAccountId;
            entity.AttachmentPath = tx.AttachmentPath;
            await db.SaveChangesAsync();
            StatusMessage = "Lançamento atualizado com sucesso.";
        }
        catch (Exception ex)
        {
            _log.Error($"Falha ao atualizar lançamento: {ex.Message}");
            MessageBox.Show(ex.Message, "Erro ao atualizar lançamento", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            using var db = new CoinCraftDbContext();
            var entity = await db.Transactions.FindAsync(id);
            if (entity is null) return;
            try
            {
                var path = entity.AttachmentPath;
                if (!string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }
            catch (Exception fileEx)
            {
                _log.Error($"Falha ao remover anexo do lançamento #{id}: {fileEx.Message}");
            }
            db.Transactions.Remove(entity);
            await db.SaveChangesAsync();
            StatusMessage = "Lançamento excluído com sucesso.";
        }
        catch (Exception ex)
        {
            _log.Error($"Falha ao excluir lançamento: {ex.Message}");
            MessageBox.Show(ex.Message, "Erro ao excluir lançamento", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}