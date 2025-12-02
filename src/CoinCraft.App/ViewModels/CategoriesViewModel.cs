using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CoinCraft.Domain;
using CoinCraft.Infrastructure;
using CoinCraft.Services;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using CoinCraft.App.Messages;

namespace CoinCraft.App.ViewModels;

public sealed class CategoriesViewModel : ObservableObject
{
    private readonly LogService _log;
    private readonly Func<CoinCraftDbContext> _contextFactory;

    public ObservableCollection<string> AvailableColors { get; } = new()
    {
        "#FF7043", "#42A5F5", "#66BB6A", "#AB47BC", "#EC407A",
        "#26C6DA", "#FFA726", "#8D6E63", "#78909C", "#D4E157",
        "#5C6BC0", "#EF5350", "#26A69A", "#7E57C2", "#FFCA28"
    };
    private string? _statusMessage;
    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    private ObservableCollection<Category> _categories = new();
    public ObservableCollection<Category> Categories
    {
        get => _categories;
        private set => SetProperty(ref _categories, value);
    }

    private Category? _selected;
    public Category? Selected
    {
        get => _selected;
        set => SetProperty(ref _selected, value);
    }

    public CategoriesViewModel(LogService log, Func<CoinCraftDbContext>? contextFactory = null)
    {
        _log = log;
        _contextFactory = contextFactory ?? (() => new CoinCraftDbContext());
    }

    public async Task LoadAsync()
    {
        using var db = _contextFactory();
        var list = await Task.Run(() => db.Categories.OrderBy(c => c.Nome).ToList());
        Categories = new ObservableCollection<Category>(list);
        StatusMessage = $"{Categories.Count} categorias carregadas.";
    }

    public async Task AddAsync(Category cat)
    {
        try
        {
            using var db = _contextFactory();
            db.Categories.Add(cat);
            await db.SaveChangesAsync();
            StatusMessage = "Categoria adicionada com sucesso.";
            WeakReferenceMessenger.Default.Send(new CategoriesChangedMessage("Add"));
        }
        catch (Exception ex)
        {
            _log.Error($"Falha ao adicionar categoria: {ex.Message}");
            MessageBox.Show(ex.Message, "Erro ao salvar categoria", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public async Task UpdateAsync(Category updated)
    {
        try
        {
            using var db = _contextFactory();
            var entity = await db.Categories.FindAsync(updated.Id);
            if (entity is null) return;
            entity.Nome = updated.Nome;
            entity.CorHex = updated.CorHex;
            entity.Icone = updated.Icone;
            entity.ParentCategoryId = updated.ParentCategoryId;
            entity.LimiteMensal = updated.LimiteMensal;
            await db.SaveChangesAsync();
            StatusMessage = "Categoria atualizada com sucesso.";
            WeakReferenceMessenger.Default.Send(new CategoriesChangedMessage("Update"));
        }
        catch (Exception ex)
        {
            _log.Error($"Falha ao atualizar categoria: {ex.Message}");
            MessageBox.Show(ex.Message, "Erro ao atualizar categoria", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            using var db = _contextFactory();
            var entity = await db.Categories.FindAsync(id);
            if (entity is null) return;
            db.Categories.Remove(entity);
            await db.SaveChangesAsync();
            StatusMessage = "Categoria excluída com sucesso.";
            WeakReferenceMessenger.Default.Send(new CategoriesChangedMessage("Delete"));
        }
        catch (Exception ex)
        {
            _log.Error($"Falha ao excluir categoria: {ex.Message}");
            MessageBox.Show(ex.Message, "Erro ao excluir categoria", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
