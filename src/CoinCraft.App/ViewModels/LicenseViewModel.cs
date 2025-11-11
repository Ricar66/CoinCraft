using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using CoinCraft.Services.Licensing;

namespace CoinCraft.App.ViewModels
{
    public sealed class LicenseViewModel : INotifyPropertyChanged
    {
        private readonly ILicensingService _licensing;
        private readonly ILicenseApiClient _api;
        private string _licenseKey = string.Empty;
        private string _status = "Status desconhecido";
        private string _installs = "-";
        private bool _busy;

        public event PropertyChangedEventHandler? PropertyChanged;

        public LicenseViewModel(ILicensingService licensing, ILicenseApiClient api)
        {
            _licensing = licensing;
            _api = api;
            ActivateCommand = new AsyncCommand(ActivateAsync, () => !_busy);
            PurchaseCommand = new AsyncCommand(PurchaseAsync, () => !_busy);
            RefreshUi();
        }

        public string LicenseKey
        {
            get => _licenseKey;
            set { _licenseKey = value; OnPropertyChanged(); }
        }

        public string StatusText
        {
            get => _status;
            private set { _status = value; OnPropertyChanged(); }
        }

        public string RemainingInstallsText
        {
            get => _installs;
            private set { _installs = value; OnPropertyChanged(); }
        }

        public ICommand ActivateCommand { get; }
        public ICommand PurchaseCommand { get; }

        private async Task ActivateAsync()
        {
            _busy = true; ((AsyncCommand)ActivateCommand).RaiseCanExecuteChanged(); ((AsyncCommand)PurchaseCommand).RaiseCanExecuteChanged();
            var res = await _licensing.EnsureLicensedAsync(async () => LicenseKey);
            StatusText = res.IsValid ? "Licença ativa" : ($"Falha: {res.Message}");
            RemainingInstallsText = _licensing.CurrentLicense?.RemainingInstallations.ToString() ?? "-";
            _busy = false; ((AsyncCommand)ActivateCommand).RaiseCanExecuteChanged(); ((AsyncCommand)PurchaseCommand).RaiseCanExecuteChanged();
        }

        private async Task PurchaseAsync()
        {
            _busy = true; ((AsyncCommand)ActivateCommand).RaiseCanExecuteChanged(); ((AsyncCommand)PurchaseCommand).RaiseCanExecuteChanged();
            // Em um app real, use a conta do usuário logado
            var license = await _api.PurchaseLicenseAsync("current-user");
            if (license != null)
            {
                LicenseKey = license.LicenseKey;
                RemainingInstallsText = license.RemainingInstallations.ToString();
                StatusText = "Licença adquirida. Clique em Ativar.";
            }
            else
            {
                StatusText = "Falha na compra";
            }
            _busy = false; ((AsyncCommand)ActivateCommand).RaiseCanExecuteChanged(); ((AsyncCommand)PurchaseCommand).RaiseCanExecuteChanged();
        }

        private void RefreshUi()
        {
            StatusText = _licensing.CurrentState switch
            {
                LicenseState.Active => "Licença ativa",
                LicenseState.Inactive => "Licença inativa",
                LicenseState.Revoked => "Licença revogada",
                LicenseState.Expired => "Licença expirada",
                _ => "Status desconhecido"
            };
            RemainingInstallsText = _licensing.CurrentLicense?.RemainingInstallations.ToString() ?? "-";
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private sealed class AsyncCommand : ICommand
        {
            private readonly Func<Task> _execute;
            private readonly Func<bool> _canExecute;
            public AsyncCommand(Func<Task> execute, Func<bool> canExecute)
            { _execute = execute; _canExecute = canExecute; }
            public bool CanExecute(object? parameter) => _canExecute();
            public event EventHandler? CanExecuteChanged;
            public async void Execute(object? parameter) => await _execute();
            public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}