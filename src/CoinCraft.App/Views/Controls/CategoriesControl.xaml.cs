using System.Windows;
using System.Windows.Controls;
using CoinCraft.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CoinCraft.App.Views.Controls
{
    public partial class CategoriesControl : UserControl
    {
        private readonly CategoriesViewModel _vm = App.Services!.GetRequiredService<CategoriesViewModel>();

        public CategoriesControl()
        {
            InitializeComponent();
            DataContext = _vm;
            Loaded += async (_, __) =>
            {
                await _vm.LoadAsync();
                StatusText.Text = _vm.StatusMessage;
            };
        }

        

        private void OnAddClick(object sender, RoutedEventArgs e)
        {
            var cat = new CoinCraft.Domain.Category { Nome = "Nova Categoria", CorHex = "#009688" };
            var edit = new CategoryEditWindow(cat);
            var ok = edit.ShowDialog();
            if (ok == true)
            {
                _ = AddAndReloadAsync(cat);
            }
        }

        private async Task AddAndReloadAsync(CoinCraft.Domain.Category cat)
        {
            await _vm.AddAsync(cat);
            await _vm.LoadAsync();
            StatusText.Text = _vm.StatusMessage;
        }

        private void OnEditClick(object sender, RoutedEventArgs e)
        {
            if (_vm.Selected is null) return;
            var edit = new CategoryEditWindow(_vm.Selected);
            var ok = edit.ShowDialog();
            if (ok == true)
            {
                _ = UpdateAndReloadAsync(_vm.Selected);
            }
        }

        private async Task UpdateAndReloadAsync(CoinCraft.Domain.Category cat)
        {
            await _vm.UpdateAsync(cat);
            await _vm.LoadAsync();
            StatusText.Text = _vm.StatusMessage;
        }

        private async void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            if (_vm.Selected is null) return;
            var result = MessageBox.Show("Excluir a categoria selecionada?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                await _vm.DeleteAsync(_vm.Selected.Id);
                await _vm.LoadAsync();
                StatusText.Text = _vm.StatusMessage;
            }
        }
    }
}