using System.Windows;
using CoinCraft.App.ViewModels;
using CoinCraft.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace CoinCraft.App.Views;

public partial class AccountsWindow : Window
{
    private readonly AccountsViewModel _vm = App.Services!.GetRequiredService<AccountsViewModel>();

    public AccountsWindow()
    {
        InitializeComponent();
        DataContext = _vm;
        Loaded += async (_, __) => await _vm.LoadAsync();
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await _vm.LoadAsync();
    }

    // Navigation
    private void OnGoDashboard(object sender, RoutedEventArgs e) { Close(); }
    private void OnGoTransactions(object sender, RoutedEventArgs e) { new TransactionsWindow { Owner = Owner }.Show(); Close(); }
    private void OnGoAccounts(object sender, RoutedEventArgs e) { /* Already here */ }
    private void OnGoCategories(object sender, RoutedEventArgs e) { new CategoriesWindow { Owner = Owner }.Show(); Close(); }
    private void OnGoRecurring(object sender, RoutedEventArgs e) { new RecurringWindow { Owner = Owner }.Show(); Close(); }
    private void OnGoImport(object sender, RoutedEventArgs e) { new ImportWindow { Owner = Owner }.Show(); Close(); }
    private void OnGoSettings(object sender, RoutedEventArgs e) 
    { 
        var vm = App.Services!.GetRequiredService<CoinCraft.App.ViewModels.SettingsViewModel>();
        new SettingsWindow(vm) { Owner = Owner }.Show(); 
        Close(); 
    }

    private async void OnAddClick(object sender, RoutedEventArgs e)
    {
        var account = new Account { Ativa = true };
        var editor = new AccountEditWindow(account) { Owner = this };
        if (editor.ShowDialog() == true)
        {
            await _vm.AddAsync(account);
            await _vm.LoadAsync();
        }
    }

    private async void OnEditClick(object sender, RoutedEventArgs e)
    {
        if (_vm.SelectedAccount is null) return;

        // Clone simples para edição
        var copy = new Account
        {
            Id = _vm.SelectedAccount.Id,
            Nome = _vm.SelectedAccount.Nome,
            Tipo = _vm.SelectedAccount.Tipo,
            SaldoInicial = _vm.SelectedAccount.SaldoInicial,
            Ativa = _vm.SelectedAccount.Ativa,
            CorHex = _vm.SelectedAccount.CorHex,
            Icone = _vm.SelectedAccount.Icone
        };

        var editor = new AccountEditWindow(copy) { Owner = this };
        if (editor.ShowDialog() == true)
        {
            await _vm.UpdateAsync(copy);
            await _vm.LoadAsync();
        }
    }

    private async void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (_vm.SelectedAccount is null) return;

        var result = MessageBox.Show(
            $"Excluir a conta '{_vm.SelectedAccount.Nome}'?",
            "Confirmar exclusão",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );

        if (result == MessageBoxResult.Yes)
        {
            await _vm.DeleteAsync(_vm.SelectedAccount);
            await _vm.LoadAsync();
        }
    }
}