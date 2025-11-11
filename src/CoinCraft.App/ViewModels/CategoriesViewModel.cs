using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CoinCraft.Domain;
using CoinCraft.Infrastructure;
using CoinCraft.Services;
using System.Windows;

namespace CoinCraft.App.ViewModels;

public sealed class CategoriesViewModel : ObservableObject
{
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

    public async Task LoadAsync()
    {
        using var db = new CoinCraftDbContext();
        var list = await Task.Run(() => db.Categories.OrderBy(c => c.Nome).ToList());
        Categories = new ObservableCollection<Category>(list);
        StatusMessage = $"{Categories.Count} categorias carregadas.";
    }

    public async Task AddAsync(Category cat)
    {
        try
        {
            using var db = new CoinCraftDbContext();
            db.Categories.Add(cat);
            await db.SaveChangesAsync();
            StatusMessage = "Categoria adicionada com sucesso.";
        }
        catch (Exception ex)
        {
            new LogService().Error($"Falha ao adicionar categoria: {ex.Message}");
            MessageBox.Show(ex.Message, "Erro ao salvar categoria", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public async Task UpdateAsync(Category updated)
    {
        try
        {
            using var db = new CoinCraftDbContext();
            var entity = await db.Categories.FindAsync(updated.Id);
            if (entity is null) return;
            entity.Nome = updated.Nome;
            entity.CorHex = updated.CorHex;
            entity.Icone = updated.Icone;
            entity.ParentCategoryId = updated.ParentCategoryId;
            entity.LimiteMensal = updated.LimiteMensal;
            await db.SaveChangesAsync();
            StatusMessage = "Categoria atualizada com sucesso.";
        }
        catch (Exception ex)
        {
            new LogService().Error($"Falha ao atualizar categoria: {ex.Message}");
            MessageBox.Show(ex.Message, "Erro ao atualizar categoria", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            using var db = new CoinCraftDbContext();
            var entity = await db.Categories.FindAsync(id);
            if (entity is null) return;
            db.Categories.Remove(entity);
            await db.SaveChangesAsync();
            StatusMessage = "Categoria excluída com sucesso.";
        }
        catch (Exception ex)
        {
            new LogService().Error($"Falha ao excluir categoria: {ex.Message}");
            MessageBox.Show(ex.Message, "Erro ao excluir categoria", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}