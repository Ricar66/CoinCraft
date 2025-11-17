using System.Windows;
using System.Globalization;
using CoinCraft.Domain;
using CoinCraft.Infrastructure;
using System.Windows.Input;

namespace CoinCraft.App.Views;

public partial class CategoryEditWindow : Window
{
    private readonly Category _cat;
    public CategoryEditWindow(Category cat)
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Erro ao carregar janela", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
        _cat = cat;

        using var db = new CoinCraftDbContext();
        var all = db.Categories.OrderBy(c => c.Nome).ToList();
        ParentCombo.ItemsSource = all;

        NomeBox.Text = _cat.Nome;
        CorBox.Text = _cat.CorHex ?? string.Empty;
        IconeBox.Text = _cat.Icone ?? string.Empty;
        ParentCombo.SelectedValue = _cat.ParentCategoryId;
        LimiteBox.Text = _cat.LimiteMensal?.ToString() ?? string.Empty;
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        var nome = NomeBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(nome))
        {
            MessageBox.Show("Informe um nome para a categoria.", "Dados obrigatórios", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _cat.Nome = nome;
        _cat.CorHex = string.IsNullOrWhiteSpace(CorBox.Text) ? null : CorBox.Text.Trim();
        _cat.Icone = string.IsNullOrWhiteSpace(IconeBox.Text) ? null : IconeBox.Text.Trim();
        _cat.ParentCategoryId = (int?)ParentCombo.SelectedValue;
        _cat.LimiteMensal = decimal.TryParse(LimiteBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var lim) ? lim : null;

        DialogResult = true;
        Close();
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void OnToggleMaximizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
    private void OnHeaderMouseDown(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
}