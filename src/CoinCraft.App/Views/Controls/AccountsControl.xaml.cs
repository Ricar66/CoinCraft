using System.Windows;
using System.Windows.Controls;
using CoinCraft.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CoinCraft.App.Views.Controls
{
    public partial class AccountsControl : UserControl
    {
        private readonly AccountsViewModel _vm = App.Services!.GetRequiredService<AccountsViewModel>();

        public AccountsControl()
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
            var acc = new CoinCraft.Domain.Account { Nome = "Nova Conta", Tipo = CoinCraft.Domain.AccountType.Carteira, Ativa = true };
            var edit = new AccountEditWindow(acc);
            var ok = edit.ShowDialog();
            if (ok == true)
            {
                _ = AddAndReloadAsync(acc);
            }
        }

        private async Task AddAndReloadAsync(CoinCraft.Domain.Account acc)
        {
            await _vm.AddAsync(acc);
            await _vm.LoadAsync();
            StatusText.Text = _vm.StatusMessage;
        }

        private void OnEditClick(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedAccount is null) return;
            var edit = new AccountEditWindow(_vm.SelectedAccount);
            var ok = edit.ShowDialog();
            if (ok == true)
            {
                _ = UpdateAndReloadAsync(_vm.SelectedAccount);
            }
        }

        private async Task UpdateAndReloadAsync(CoinCraft.Domain.Account acc)
        {
            await _vm.UpdateAsync(acc);
            await _vm.LoadAsync();
            StatusText.Text = _vm.StatusMessage;
        }

        private async void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedAccount is null) return;
            var result = MessageBox.Show("Excluir a conta selecionada?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                await _vm.DeleteAsync(_vm.SelectedAccount);
                await _vm.LoadAsync();
                StatusText.Text = _vm.StatusMessage;
            }
        }
    }
}