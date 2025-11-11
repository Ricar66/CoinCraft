using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using CoinCraft.App.ViewModels;
using CoinCraft.Domain;

namespace CoinCraft.App.Views;

public partial class TransactionEditWindow : Window
{
    private readonly TransactionsViewModel _vm;
    private readonly TransactionItem? _editing;
    public Transaction? ResultTransaction { get; private set; }

    public TransactionEditWindow(TransactionsViewModel vm, TransactionItem? editing = null)
    {
        InitializeComponent();
        _vm = vm;
        _editing = editing;

        // Bind sources
        TipoCombo.ItemsSource = Enum.GetValues(typeof(TransactionType));
        ContaCombo.ItemsSource = _vm.Accounts;
        CategoriaCombo.ItemsSource = _vm.Categories;
        ContaDestinoCombo.ItemsSource = _vm.Accounts;

        // Prefill
        if (_editing is null)
        {
            DataPicker.SelectedDate = DateTime.Today;
            TipoCombo.SelectedItem = TransactionType.Despesa;
        }
        else
        {
            DataPicker.SelectedDate = _editing.Data;
            TipoCombo.SelectedItem = _editing.Tipo;
            ValorBox.Text = _editing.Valor.ToString();
            ContaCombo.SelectedValue = _editing.AccountId;
            CategoriaCombo.SelectedValue = _editing.CategoryId;
            DescricaoBox.Text = _editing.Descricao ?? string.Empty;
            ContaDestinoCombo.SelectedValue = _editing.OpostoAccountId;
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        var data = DataPicker.SelectedDate ?? DateTime.Today;
        var tipo = (TransactionType)(TipoCombo.SelectedItem ?? TransactionType.Despesa);
        if (!decimal.TryParse(ValorBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var valor) || valor <= 0)
        {
            MessageBox.Show("Informe um valor válido (maior que zero).", "Dados obrigatórios", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (ContaCombo.SelectedValue is null)
        {
            MessageBox.Show("Selecione a conta.", "Dados obrigatórios", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var accountId = (int)ContaCombo.SelectedValue;
        int? opostoId = (int?)ContaDestinoCombo.SelectedValue;
        if (tipo == TransactionType.Transferencia)
        {
            if (!opostoId.HasValue)
            {
                MessageBox.Show("Selecione a conta destino para transferência.", "Dados obrigatórios", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (opostoId.Value == accountId)
            {
                MessageBox.Show("Conta origem e destino não podem ser a mesma.", "Dados inválidos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        var tx = new Transaction
        {
            Data = data,
            Tipo = tipo,
            Valor = valor,
            AccountId = accountId,
            CategoryId = (int?)CategoriaCombo.SelectedValue,
            Descricao = string.IsNullOrWhiteSpace(DescricaoBox.Text) ? null : DescricaoBox.Text.Trim(),
            OpostoAccountId = opostoId
        };

        ResultTransaction = tx;
        DialogResult = true;
        Close();
    }
}