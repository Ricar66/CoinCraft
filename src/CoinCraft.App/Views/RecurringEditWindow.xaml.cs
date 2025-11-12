using System;
using System.Globalization;
using System.Windows;
using CoinCraft.App.ViewModels;
using CoinCraft.Domain;

namespace CoinCraft.App.Views;

public partial class RecurringEditWindow : Window
{
    private readonly RecurringViewModel _vm;
    private readonly RecurringTransaction? _editing;
    public RecurringTransaction? ResultRecurring { get; private set; }

    public RecurringEditWindow(RecurringViewModel vm, RecurringTransaction? editing = null)
    {
        InitializeComponent();
        _vm = vm;
        _editing = editing;

        FrequenciaCombo.ItemsSource = Enum.GetValues(typeof(RecurrenceFrequency));
        TipoCombo.ItemsSource = Enum.GetValues(typeof(TransactionType));
        ContaCombo.ItemsSource = _vm.Accounts;
        CategoriaCombo.ItemsSource = _vm.Categories;
        ContaDestinoCombo.ItemsSource = _vm.Accounts;

        if (_editing is null)
        {
            InicioPicker.SelectedDate = DateTime.Today;
            TipoCombo.SelectedItem = TransactionType.Despesa;
        }
        else
        {
            NomeBox.Text = _editing.Nome;
            FrequenciaCombo.SelectedItem = _editing.Frequencia;
            InicioPicker.SelectedDate = _editing.StartDate;
            FimPicker.SelectedDate = _editing.EndDate;
            DiaSemanaBox.Text = _editing.DiaDaSemana?.ToString() ?? string.Empty;
            DiaMesBox.Text = _editing.DiaDoMes?.ToString() ?? string.Empty;
            AutoCheck.IsChecked = _editing.AutoLancamento;
            TipoCombo.SelectedItem = _editing.Tipo;
            ValorBox.Text = _editing.Valor.ToString(CultureInfo.CurrentCulture);
            ContaCombo.SelectedValue = _editing.AccountId;
            CategoriaCombo.SelectedValue = _editing.CategoryId;
            DescricaoBox.Text = _editing.Descricao ?? string.Empty;
            ContaDestinoCombo.SelectedValue = _editing.OpostoAccountId;
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        var nome = NomeBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(nome))
        {
            MessageBox.Show("Informe o nome.", "Dados obrigatórios", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var freq = (RecurrenceFrequency)(FrequenciaCombo.SelectedItem ?? RecurrenceFrequency.Mensal);
        var inicio = InicioPicker.SelectedDate ?? DateTime.Today;
        DateTime? fim = FimPicker.SelectedDate;
        int? diaSemana = null;
        if (int.TryParse(DiaSemanaBox.Text, out var ds)) diaSemana = ds;
        int? diaMes = null;
        if (int.TryParse(DiaMesBox.Text, out var dm)) diaMes = dm;
        var auto = AutoCheck.IsChecked == true;
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

        var r = new RecurringTransaction
        {
            Nome = nome!,
            Frequencia = freq,
            StartDate = inicio,
            EndDate = fim,
            DiaDaSemana = diaSemana,
            DiaDoMes = diaMes,
            AutoLancamento = auto,
            NextRunDate = inicio,
            Tipo = tipo,
            Valor = valor,
            AccountId = accountId,
            CategoryId = (int?)CategoriaCombo.SelectedValue,
            Descricao = string.IsNullOrWhiteSpace(DescricaoBox.Text) ? null : DescricaoBox.Text.Trim(),
            OpostoAccountId = opostoId
        };

        ResultRecurring = r;
        DialogResult = true;
        Close();
    }
}