using System.Windows;
using System.IO;
using CoinCraft.App.ViewModels;
using CoinCraft.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace CoinCraft.App.Views;

public partial class TransactionsWindow : Window
{
    private readonly TransactionsViewModel _vm = App.Services!.GetRequiredService<TransactionsViewModel>();

    public TransactionsWindow()
    {
        InitializeComponent();
        DataContext = _vm;
        Loaded += async (_, __) => await _vm.LoadAsync();
        Loaded += (_, __) =>
        {
            AccountFilter.ItemsSource = _vm.Accounts;
            CategoryFilter.ItemsSource = _vm.Categories;
        };
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await _vm.LoadAsync();
    }

    private async void OnAddClick(object sender, RoutedEventArgs e)
    {
        if (_vm.Accounts is null || _vm.Accounts.Count == 0)
        {
            MessageBox.Show("Nenhuma conta encontrada. Cadastre uma conta primeiro.", "Pré-requisito", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var editor = new TransactionEditWindow(_vm) { Owner = this };
        if (editor.ShowDialog() == true)
        {
            var tx = editor.ResultTransaction;
            if (tx is not null)
            {
                await _vm.AddAsync(tx);
                await _vm.LoadAsync();
            }
        }
    }

    private async void OnEditClick(object sender, RoutedEventArgs e)
    {
        if (_vm.Selected is null) return;
        var editor = new TransactionEditWindow(_vm, _vm.Selected) { Owner = this };
        if (editor.ShowDialog() == true)
        {
            var tx = editor.ResultTransaction;
            if (tx is not null)
            {
                tx.Id = _vm.Selected.Id;
                await _vm.UpdateAsync(tx);
                await _vm.LoadAsync();
            }
        }
    }

    private async void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (_vm.Selected is null) return;
        var result = MessageBox.Show($"Excluir lançamento de {_vm.Selected.Data:d} no valor de {_vm.Selected.Valor:C}?",
            "Confirmar exclusão", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
        {
            await _vm.DeleteAsync(_vm.Selected.Id);
            await _vm.LoadAsync();
        }
    }

    private async void OnApplyFilters(object sender, RoutedEventArgs e)
    {
        _vm.FilterFrom = FromPicker.SelectedDate;
        _vm.FilterTo = ToPicker.SelectedDate;
        _vm.FilterAccountId = (int?)AccountFilter.SelectedValue;
        _vm.FilterCategoryId = (int?)CategoryFilter.SelectedValue;
        await _vm.LoadAsync();
    }

    private void OnExportCsvClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var dest = Path.Combine(docs, "CoinCraftExports");
            var report = new CoinCraft.Services.ReportService();
            var path = report.ExportTransactionsCsv(dest, _vm.FilterFrom, _vm.FilterTo, _vm.FilterAccountId, _vm.FilterCategoryId);
            MessageBox.Show($"CSV exportado em:\n{path}", "Exportação CSV", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro na exportação CSV", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnExportPdfClick(object sender, RoutedEventArgs e)
    {
        // Not implemented yet or removed from UI
    }

    // Navigation
    private void OnGoDashboard(object sender, RoutedEventArgs e) { Close(); }
    private void OnGoTransactions(object sender, RoutedEventArgs e) { /* Already here */ }
    private void OnGoAccounts(object sender, RoutedEventArgs e) { new AccountsWindow { Owner = Owner }.Show(); Close(); }
    private void OnGoCategories(object sender, RoutedEventArgs e) { new CategoriesWindow { Owner = Owner }.Show(); Close(); }
    private void OnGoRecurring(object sender, RoutedEventArgs e) { new RecurringWindow { Owner = Owner }.Show(); Close(); }
    private void OnGoImport(object sender, RoutedEventArgs e) { new ImportWindow { Owner = Owner }.Show(); Close(); }
    private void OnGoSettings(object sender, RoutedEventArgs e) 
    { 
        var vm = App.Services!.GetRequiredService<CoinCraft.App.ViewModels.SettingsViewModel>();
        new SettingsWindow(vm) { Owner = Owner }.Show(); 
        Close(); 
    }
}