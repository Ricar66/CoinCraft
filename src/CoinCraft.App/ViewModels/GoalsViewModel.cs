using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CoinCraft.Domain;
using CoinCraft.Infrastructure;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using CoinCraft.App.Messages;
using System;
using Microsoft.EntityFrameworkCore;
using CoinCraft.Services;

namespace CoinCraft.App.ViewModels;

public sealed class GoalsViewModel : ObservableObject
{
    private readonly LogService _log;
    private readonly Func<CoinCraftDbContext> _contextFactory;

    public GoalsViewModel(LogService log, Func<CoinCraftDbContext>? contextFactory = null)
    {
        _log = log;
        _contextFactory = contextFactory ?? (() => new CoinCraftDbContext());

        // Ouvintes para atualização de lookups
        WeakReferenceMessenger.Default.Register<CategoriesChangedMessage>(this, (r, m) =>
        {
            DispatcherHelper.InvokeAsync(async () => await LoadCategoriesAsync());
        });
    }

    public ObservableCollection<Goal> Items { get; private set; } = new();
    public ObservableCollection<Category> Categories { get; private set; } = new();

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public async Task LoadAsync()
    {
        await LoadCategoriesAsync();
        await LoadGoalsAsync();
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            using var db = _contextFactory();
            var list = await db.Categories.OrderBy(c => c.Nome).ToListAsync();
            Categories.Clear();
            foreach (var item in list) Categories.Add(item);
        }
        catch (Exception ex)
        {
            _log.Error($"Erro ao carregar categorias em GoalsViewModel: {ex.Message}");
        }
    }

    private async Task LoadGoalsAsync()
    {
        try
        {
            using var db = _contextFactory();
            var list = await db.Goals.ToListAsync();
            Items.Clear();
            foreach (var item in list) Items.Add(item);
            StatusMessage = $"{Items.Count} metas carregadas.";
        }
        catch (Exception ex)
        {
            _log.Error($"Erro ao carregar metas: {ex.Message}");
            StatusMessage = "Erro ao carregar metas.";
        }
    }

    public async Task AddAsync(Goal goal)
    {
        try
        {
            using var db = _contextFactory();
            // Verifica se já existe meta para essa categoria no mesmo mês/ano
            var exists = await db.Goals.AnyAsync(g => g.CategoryId == goal.CategoryId && g.Ano == goal.Ano && g.Mes == goal.Mes);
            if (exists)
            {
                StatusMessage = "Já existe uma meta para esta categoria neste período.";
                return;
            }

            db.Goals.Add(goal);
            await db.SaveChangesAsync();
            StatusMessage = "Meta adicionada com sucesso.";
            WeakReferenceMessenger.Default.Send(new GoalsChangedMessage("Add"));
            await LoadGoalsAsync();
        }
        catch (Exception ex)
        {
            _log.Error($"Falha ao adicionar meta: {ex.Message}");
            StatusMessage = $"Erro ao salvar meta: {ex.Message}";
        }
    }

    public async Task UpdateAsync(Goal updated)
    {
        try
        {
            using var db = _contextFactory();
            var entity = await db.Goals.FindAsync(updated.Id);
            if (entity is null) return;

            entity.CategoryId = updated.CategoryId;
            entity.LimiteMensal = updated.LimiteMensal;
            entity.Ano = updated.Ano;
            entity.Mes = updated.Mes;

            await db.SaveChangesAsync();
            StatusMessage = "Meta atualizada com sucesso.";
            WeakReferenceMessenger.Default.Send(new GoalsChangedMessage("Update"));
            await LoadGoalsAsync();
        }
        catch (Exception ex)
        {
            _log.Error($"Falha ao atualizar meta: {ex.Message}");
            StatusMessage = $"Erro ao atualizar meta: {ex.Message}";
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            using var db = _contextFactory();
            var entity = await db.Goals.FindAsync(id);
            if (entity is null) return;
            db.Goals.Remove(entity);
            await db.SaveChangesAsync();
            StatusMessage = "Meta excluída com sucesso.";
            WeakReferenceMessenger.Default.Send(new GoalsChangedMessage("Delete"));
            await LoadGoalsAsync();
        }
        catch (Exception ex)
        {
            _log.Error($"Falha ao excluir meta: {ex.Message}");
            StatusMessage = $"Erro ao excluir meta: {ex.Message}";
        }
    }
}
