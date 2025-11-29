using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoinCraft.Services.Licensing;
using System.Threading.Tasks;
using System.Windows;

namespace CoinCraft.App.ViewModels
{
    public partial class LicenseViewModel : ObservableObject
    {
        private readonly ILicensingService _licensingService;

        [ObservableProperty]
        private string _hardwareId = string.Empty;

        [ObservableProperty]
        private string _licenseKey = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private string _currentStatus = string.Empty;

        [ObservableProperty]
        private string _remainingInstalls = string.Empty;

        [ObservableProperty]
        private string _transferTarget = string.Empty;

        public LicenseViewModel(ILicensingService licensingService)
        {
            _licensingService = licensingService;

            HardwareId = _licensingService.CurrentFingerprint;
            if (string.IsNullOrEmpty(HardwareId))
            {
                HardwareId = MachineIdProvider.ComputeFingerprint();
                if (string.IsNullOrEmpty(HardwareId)) HardwareId = "HWID-GENERICO-FALLBACK";
            }

            CurrentStatus = _licensingService.CurrentState.ToString();
            RemainingInstalls = _licensingService.CurrentLicense?.RemainingInstallations.ToString() ?? "-";
        }

        [RelayCommand]
        private void CopyId()
        {
            if (!string.IsNullOrEmpty(HardwareId))
            {
                Clipboard.SetText(HardwareId);
                StatusMessage = "ID copiado para a área de transferência!";
            }
        }

        [RelayCommand]
        private async Task Activate()
        {
            if (string.IsNullOrWhiteSpace(LicenseKey))
            {
                StatusMessage = "Por favor, cole a licença gerada no site.";
                return;
            }
            LicenseKey = LicenseKey.Trim();

            var result = await _licensingService.EnsureLicensedAsync(() => Task.FromResult<string?>(LicenseKey));
            if (result.IsValid)
            {
                MessageBox.Show("Licença ativada com sucesso! Reiniciando o aplicativo...", "Sucesso");
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is Views.LicenseWindow)
                    {
                        window.DialogResult = true;
                        window.Close();
                    }
                }
                try
                {
                    var exe = System.Environment.ProcessPath;
                    if (!string.IsNullOrEmpty(exe))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(exe) { UseShellExecute = true });
                        System.Windows.Application.Current.Shutdown();
                        return;
                    }
                }
                catch { }
                CurrentStatus = _licensingService.CurrentState.ToString();
                RemainingInstalls = _licensingService.CurrentLicense?.RemainingInstallations.ToString() ?? "-";
            }
            else
            {
                StatusMessage = "Licença inválida para este computador.";
            }
        }

        [RelayCommand]
        private async Task Transfer()
        {
            if (string.IsNullOrWhiteSpace(TransferTarget))
            {
                StatusMessage = "Informe o fingerprint de destino para transferir.";
                return;
            }
            var ok = await _licensingService.TransferAsync(TransferTarget);
            StatusMessage = ok ? "Licença transferida." : "Falha ao transferir licença.";
        }
    }
}
