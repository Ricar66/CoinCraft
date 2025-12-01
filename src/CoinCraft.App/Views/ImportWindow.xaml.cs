using System.Windows;
using Microsoft.Win32;
using CoinCraft.App.ViewModels;
using CoinCraft.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace CoinCraft.App.Views;

public partial class ImportWindow : Window
{
    private readonly ImportViewModel _vm = App.Services!.GetRequiredService<ImportViewModel>();

    public ImportWindow()
    {
        InitializeComponent();
        DataContext = _vm;
        Loaded += async (_, __) =>
        {
            await _vm.LoadLookupsAsync();
            DefaultAccountCombo.ItemsSource = _vm.Accounts;
            DefaultCategoryCombo.ItemsSource = _vm.Categories;
            DefaultTipoCombo.ItemsSource = System.Enum.GetValues(typeof(TransactionType));
        };
    }

    private void OnSelectFile(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Arquivos suportados (*.csv;*.ofx;*.qfx)|*.csv;*.ofx;*.qfx|CSV (*.csv)|*.csv|OFX/QFX (*.ofx;*.qfx)|*.ofx;*.qfx|Todos os arquivos (*.*)|*.*",
            Title = "Selecionar arquivo para importação"
        };
        if (dlg.ShowDialog(this) == true)
        {
            FilePathText.Text = dlg.FileName;
            _vm.LoadFile(dlg.FileName);
            PreviewGrid.ItemsSource = _vm.Items;
            StatusText.Text = _vm.StatusMessage;

            // Mostrar mapeamento se for CSV
            if (dlg.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                MappingGroup.Visibility = Visibility.Visible;
                MapDataCombo.ItemsSource = _vm.HeaderColumns;
                MapDescricaoCombo.ItemsSource = _vm.HeaderColumns;
                MapValorCombo.ItemsSource = _vm.HeaderColumns;
                MapTipoCombo.ItemsSource = _vm.HeaderColumns;
                MapContaCombo.ItemsSource = _vm.HeaderColumns;
                MapCategoriaCombo.ItemsSource = _vm.HeaderColumns;
            }
            else
            {
                MappingGroup.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void OnImportClick(object sender, RoutedEventArgs e)
    {
        var defaultAccId = DefaultAccountCombo.SelectedValue as int?;
        var defaultCatId = DefaultCategoryCombo.SelectedValue as int?;
        var defaultTipo = DefaultTipoCombo.SelectedItem is TransactionType t ? t : (TransactionType?)null;

        var saved = _vm.Import(defaultAccId, defaultCatId, defaultTipo);
        StatusText.Text = _vm.StatusMessage;
        MessageBox.Show(this, $"Importação concluída: {saved} lançamento(s)", "Importar", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnApplyMappingClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_vm.FilePath) || !_vm.FilePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return;

        // Construir mapa: chave -> índice escolhido
        var map = new Dictionary<string, int>();
        int idxOf(object? sel) => sel is string s ? _vm.HeaderColumns.IndexOf(s) : -1;

        void add(string key, object? sel)
        {
            var idx = idxOf(sel);
            if (idx >= 0) map[key] = idx;
        }
        add("data", MapDataCombo.SelectedItem);
        add("descricao", MapDescricaoCombo.SelectedItem);
        add("valor", MapValorCombo.SelectedItem);
        add("tipo", MapTipoCombo.SelectedItem);
        add("conta", MapContaCombo.SelectedItem);
        add("categoria", MapCategoriaCombo.SelectedItem);

        _vm.LoadFileWithMapping(_vm.FilePath!, map);
        PreviewGrid.ItemsSource = _vm.Items;
        StatusText.Text = _vm.StatusMessage;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    // Navigation
    private void OnGoDashboard(object sender, RoutedEventArgs e) { Close(); }
    private void OnGoTransactions(object sender, RoutedEventArgs e) { new TransactionsWindow { Owner = Owner }.Show(); Close(); }
    private void OnGoAccounts(object sender, RoutedEventArgs e) { new AccountsWindow { Owner = Owner }.Show(); Close(); }
    private void OnGoCategories(object sender, RoutedEventArgs e) { new CategoriesWindow { Owner = Owner }.Show(); Close(); }
    private void OnGoRecurring(object sender, RoutedEventArgs e) { new RecurringWindow { Owner = Owner }.Show(); Close(); }
    private void OnGoImport(object sender, RoutedEventArgs e) { /* Already here */ }
    private void OnGoSettings(object sender, RoutedEventArgs e) 
    { 
        var vm = App.Services!.GetRequiredService<CoinCraft.App.ViewModels.SettingsViewModel>();
        new SettingsWindow(vm) { Owner = Owner }.Show(); 
        Close(); 
    }
}