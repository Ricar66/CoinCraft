using System.Windows;
using System.Globalization;
using CoinCraft.Domain;
using CoinCraft.Infrastructure;

namespace CoinCraft.App.Views;

public partial class CategoryEditWindow : Window
{
    private readonly Category _cat;
    public CategoryEditWindow(Category cat)
    {
        InitializeComponent();
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
}