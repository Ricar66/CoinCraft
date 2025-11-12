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
    private string? _attachmentPath;
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
            AttachmentNameText.Text = "Nenhum arquivo selecionado";
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
            _attachmentPath = _editing.AttachmentPath;
            AttachmentNameText.Text = string.IsNullOrEmpty(_editing.AttachmentPath)
                ? "Nenhum arquivo selecionado"
                : System.IO.Path.GetFileName(_editing.AttachmentPath);
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
            OpostoAccountId = opostoId,
            AttachmentPath = _attachmentPath
        };

        ResultTransaction = tx;
        DialogResult = true;
        Close();
    }

    private void OnSelectAttachmentClick(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Selecionar comprovante",
            Filter = "Imagens e PDF|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.pdf|Todos os arquivos|*.*",
            Multiselect = false
        };

        if (dlg.ShowDialog() == true)
        {
            try
            {
                var targetPath = CopyAttachmentToAppData(dlg.FileName);
                _attachmentPath = targetPath;
                AttachmentNameText.Text = System.IO.Path.GetFileName(targetPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Falha ao anexar arquivo: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void OnClearAttachmentClick(object sender, RoutedEventArgs e)
    {
        _attachmentPath = null;
        AttachmentNameText.Text = "Nenhum arquivo selecionado";
    }

    private static string CopyAttachmentToAppData(string sourceFile)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = System.IO.Path.Combine(appData, "CoinCraft", "attachments");
        System.IO.Directory.CreateDirectory(dir);

        var ext = System.IO.Path.GetExtension(sourceFile);
        var name = $"{Guid.NewGuid():N}{ext}";
        var dest = System.IO.Path.Combine(dir, name);
        System.IO.File.Copy(sourceFile, dest, overwrite: false);
        return dest;
    }
}