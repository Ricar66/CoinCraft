using System.Windows;
using System.Windows.Controls;
using CoinCraft.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CoinCraft.App.Views.Controls
{
    public partial class TransactionsControl : UserControl
    {
        private readonly TransactionsViewModel _vm = App.Services!.GetRequiredService<TransactionsViewModel>();

        public TransactionsControl()
        {
            InitializeComponent();
            DataContext = _vm;
            Loaded += async (_, __) => await _vm.LoadAsync();
            Loaded += (_, __) =>
            {
                AccountFilter.ItemsSource = _vm.Accounts;
                CategoryFilter.ItemsSource = _vm.Categories;
                StatusText.Text = _vm.StatusMessage;
            };
        }

        

        private async void OnApplyFiltersClick(object sender, RoutedEventArgs e)
        {
            _vm.FilterFrom = FromPicker.SelectedDate;
            _vm.FilterTo = ToPicker.SelectedDate;
            _vm.FilterAccountId = (int?)AccountFilter.SelectedValue;
            _vm.FilterCategoryId = (int?)CategoryFilter.SelectedValue;
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
            var w = new TransactionEditWindow(_vm);
            w.ShowDialog();
        }

        private void OnEditClick(object sender, RoutedEventArgs e)
        {
            if (_vm.Selected is null) return;
            var w = new TransactionEditWindow(_vm, _vm.Selected);
            w.ShowDialog();
        }

        private async void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            if (_vm.Selected is null) return;
            var result = MessageBox.Show("Excluir o lançamento selecionado?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                await _vm.DeleteAsync(_vm.Selected.Id);
                await _vm.LoadAsync();
                StatusText.Text = _vm.StatusMessage;
            }
        }
    }
}