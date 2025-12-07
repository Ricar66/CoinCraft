using System.Windows;
using CoinCraft.App.ViewModels;
using CoinCraft.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace CoinCraft.App.Views;

public partial class CategoriesWindow : Window
{
    private readonly CategoriesViewModel _vm = App.Services!.GetRequiredService<CategoriesViewModel>();
    public CategoriesWindow()
    {
        InitializeComponent();
        DataContext = _vm;
        Loaded += async (_, __) => await _vm.LoadAsync();
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e) => await _vm.LoadAsync();

    private async void OnAddClick(object sender, RoutedEventArgs e)
    {
        var cat = new Category();
        var editor = new CategoryEditWindow(cat) { Owner = this };
        if (editor.ShowDialog() == true)
        {
            await _vm.AddAsync(cat);
            await _vm.LoadAsync();
        }
    }

    private async void OnEditClick(object sender, RoutedEventArgs e)
    {
        if (_vm.Selected is null) return;
        var copy = new Category
        {
            Id = _vm.Selected.Id,
            Nome = _vm.Selected.Nome,
            CorHex = _vm.Selected.CorHex,
            Icone = _vm.Selected.Icone,
            ParentCategoryId = _vm.Selected.ParentCategoryId,
            LimiteMensal = _vm.Selected.LimiteMensal
        };
        var editor = new CategoryEditWindow(copy) { Owner = this };
        if (editor.ShowDialog() == true)
        {
            await _vm.UpdateAsync(copy);
            await _vm.LoadAsync();
        }
    }

    private async void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (_vm.Selected is null) return;
        var r = MessageBox.Show($"Excluir a categoria '{_vm.Selected.Nome}'?", "Confirmar exclusão", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (r == MessageBoxResult.Yes)
        {
            await _vm.DeleteAsync(_vm.Selected.Id);
            await _vm.LoadAsync();
        }
    }

    // Navigation
    private void OnGoDashboard(object sender, RoutedEventArgs e) { Close(); }
    private void OnGoTransactions(object sender, RoutedEventArgs e) { App.ShowSingle(() => new TransactionsWindow { Owner = Owner }); Close(); }
    private void OnGoAccounts(object sender, RoutedEventArgs e) { App.ShowSingle(() => new AccountsWindow { Owner = Owner }); Close(); }
    private void OnGoCategories(object sender, RoutedEventArgs e) { /* Already here */ }
    private void OnGoRecurring(object sender, RoutedEventArgs e) { App.ShowSingle(() => new RecurringWindow { Owner = Owner }); Close(); }
    private void OnGoImport(object sender, RoutedEventArgs e) { App.ShowSingle(() => new ImportWindow { Owner = Owner }); Close(); }
    private void OnGoSettings(object sender, RoutedEventArgs e) 
    { 
        var vm = App.Services!.GetRequiredService<CoinCraft.App.ViewModels.SettingsViewModel>();
        App.ShowSingle(() => new SettingsWindow(vm) { Owner = Owner }); 
        Close(); 
    }
}
