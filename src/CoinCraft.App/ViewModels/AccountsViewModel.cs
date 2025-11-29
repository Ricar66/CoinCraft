using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CoinCraft.Domain;
using CoinCraft.Infrastructure;
using CoinCraft.Services;
using System.Windows;

namespace CoinCraft.App.ViewModels;

public sealed class AccountsViewModel : ObservableObject
{
    private readonly LogService _log;
    public ObservableCollection<string> AvailableColors { get; } = new()
    {
        "#4CAF50", "#FFC107", "#FF7043", "#42A5F5", "#66BB6A",
        "#AB47BC", "#EC407A", "#26C6DA", "#8D6E63", "#78909C"
    };
    private string? _statusMessage;
    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    private ObservableCollection<Account> _accounts = new();
    public ObservableCollection<Account> Accounts
    {
        get => _accounts;
        private set => SetProperty(ref _accounts, value);
    }

    private Account? _selectedAccount;
    public Account? SelectedAccount
    {
        get => _selectedAccount;
        set => SetProperty(ref _selectedAccount, value);
    }

    public AccountsViewModel(LogService log)
    {
        _log = log;
    }

    public async Task LoadAsync()
    {
        using var db = new CoinCraftDbContext();
        var list = await Task.Run(() => db.Accounts.OrderBy(a => a.Nome).ToList());
        Accounts = new ObservableCollection<Account>(list);
        StatusMessage = $"{Accounts.Count} contas carregadas.";
    }

    public async Task AddAsync(Account account)
    {
        try
        {
            using var db = new CoinCraftDbContext();
            db.Accounts.Add(account);
            await db.SaveChangesAsync();
            StatusMessage = "Conta adicionada com sucesso.";
        }
        catch (Exception ex)
        {
            _log.Error($"Falha ao adicionar conta: {ex.Message}");
            MessageBox.Show(ex.Message, "Erro ao salvar conta", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public async Task UpdateAsync(Account updated)
    {
        try
        {
            using var db = new CoinCraftDbContext();
            var entity = await db.Accounts.FindAsync(updated.Id);
            if (entity is null) return;

            entity.Nome = updated.Nome;
            entity.Tipo = updated.Tipo;
            entity.SaldoInicial = updated.SaldoInicial;
            entity.Ativa = updated.Ativa;
            entity.CorHex = updated.CorHex;
            entity.Icone = updated.Icone;

            await db.SaveChangesAsync();
            StatusMessage = "Conta atualizada com sucesso.";
        }
        catch (Exception ex)
        {
            _log.Error($"Falha ao atualizar conta: {ex.Message}");
            MessageBox.Show(ex.Message, "Erro ao atualizar conta", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public async Task DeleteAsync(Account account)
    {
        try
        {
            using var db = new CoinCraftDbContext();
            var entity = await db.Accounts.FindAsync(account.Id);
            if (entity is null) return;
            db.Accounts.Remove(entity);
            await db.SaveChangesAsync();
            StatusMessage = "Conta excluída com sucesso.";
        }
        catch (Exception ex)
        {
            _log.Error($"Falha ao excluir conta: {ex.Message}");
            MessageBox.Show(ex.Message, "Erro ao excluir conta", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
