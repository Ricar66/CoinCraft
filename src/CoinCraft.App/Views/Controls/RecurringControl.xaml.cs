using System.Windows;
using System.Windows.Controls;
using CoinCraft.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CoinCraft.App.Views.Controls
{
    public partial class RecurringControl : UserControl
    {
        private readonly RecurringViewModel _vm = App.Services!.GetRequiredService<RecurringViewModel>();

        public RecurringControl()
        {
            InitializeComponent();
            DataContext = _vm;
            Loaded += async (_, __) =>
            {
                await _vm.LoadAsync();
                AccountFilter.ItemsSource = _vm.Accounts;
                CategoryFilter.ItemsSource = _vm.Categories;
                StatusText.Text = _vm.StatusMessage;
            };
        }

        

        private async void OnApplyFiltersClick(object sender, RoutedEventArgs e)
        {
            _vm.FilterAccountId = (int?)AccountFilter.SelectedValue;
            _vm.FilterCategoryId = (int?)CategoryFilter.SelectedValue;
            _vm.FilterNome = string.IsNullOrWhiteSpace(NameFilter.Text) ? null : NameFilter.Text.Trim();
            _vm.FilterFrom = FromPicker.SelectedDate;
            _vm.FilterTo = ToPicker.SelectedDate;
            // Frequency
            if (FrequencyFilter.SelectedItem is ComboBoxItem cb)
            {
                var content = cb.Content?.ToString();
                _vm.FilterFrequency = content switch
                {
                    "Diario" => CoinCraft.Domain.RecurrenceFrequency.Diario,
                    "Semanal" => CoinCraft.Domain.RecurrenceFrequency.Semanal,
                    "Mensal" => CoinCraft.Domain.RecurrenceFrequency.Mensal,
                    "Anual" => CoinCraft.Domain.RecurrenceFrequency.Anual,
                    _ => null
                };
            }
            else _vm.FilterFrequency = null;

            await _vm.LoadAsync();
            StatusText.Text = _vm.StatusMessage;
        }

        private async void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            await _vm.LoadAsync();
            StatusText.Text = _vm.StatusMessage;
        }

        private void OnAddClick(object sender, RoutedEventArgs e)
        {
            var vm = _vm;
            var edit = new RecurringEditWindow(vm);
            var ok = edit.ShowDialog();
            if (ok == true)
            {
                _ = ReloadAfterEditAsync();
            }
        }

        private void OnEditClick(object sender, RoutedEventArgs e)
        {
            if (_vm.Selected is null) return;
            var edit = new RecurringEditWindow(_vm, _vm.Selected);
            var ok = edit.ShowDialog();
            if (ok == true)
            {
                _ = ReloadAfterEditAsync();
            }
        }

        private async void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            if (_vm.Selected is null) return;
            var result = MessageBox.Show("Excluir o recorrente selecionado?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                await _vm.DeleteAsync(_vm.Selected.Id);
                await _vm.LoadAsync();
                StatusText.Text = _vm.StatusMessage;
            }
        }

        private async Task ReloadAfterEditAsync()
        {
            await _vm.LoadAsync();
            StatusText.Text = _vm.StatusMessage;
        }
    }
}