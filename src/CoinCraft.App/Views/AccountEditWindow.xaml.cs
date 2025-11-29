using System;
using System.Globalization;
using System.Windows;
using CoinCraft.Domain;

namespace CoinCraft.App.Views;

public partial class AccountEditWindow : Window
{
    private readonly Account _account;

    public AccountEditWindow(Account account)
    {
        InitializeComponent();
        _account = account;
        DataContext = new CoinCraft.App.ViewModels.AccountsViewModel(new CoinCraft.Services.LogService());

        // Bind enum values
        TipoCombo.ItemsSource = Enum.GetValues(typeof(AccountType));

        // Prefill
        NomeBox.Text = _account.Nome;
        TipoCombo.SelectedItem = _account.Tipo;
        SaldoBox.Text = _account.SaldoInicial.ToString();
        AtivaCheck.IsChecked = _account.Ativa;
        CorBox.Text = _account.CorHex ?? string.Empty;
        IconeBox.Text = _account.Icone ?? string.Empty;
    }

    private void OnColorSelected(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        var color = (sender as System.Windows.Controls.ListBox)?.SelectedItem as string;
        if (!string.IsNullOrWhiteSpace(color))
        {
            _account.CorHex = color;
            CorBox.Text = color;
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        var nome = NomeBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(nome))
        {
            MessageBox.Show("Informe um nome para a conta.", "Dados obrigatórios", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(SaldoBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var saldo))
        {
            MessageBox.Show("Saldo inicial inválido.", "Dados obrigatórios", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _account.Nome = nome;
        _account.Tipo = (AccountType)(TipoCombo.SelectedItem ?? AccountType.ContaCorrente);
        _account.SaldoInicial = saldo;
        _account.Ativa = AtivaCheck.IsChecked == true;
        _account.CorHex = string.IsNullOrWhiteSpace(CorBox.Text) ? null : CorBox.Text.Trim();
        _account.Icone = string.IsNullOrWhiteSpace(IconeBox.Text) ? null : IconeBox.Text.Trim();

        DialogResult = true;
        Close();
    }
}
