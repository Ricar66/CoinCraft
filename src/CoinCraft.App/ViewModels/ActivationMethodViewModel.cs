using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoinCraft.Services.Licensing;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace CoinCraft.App.ViewModels
{
    public partial class ActivationMethodViewModel : ObservableObject
    {
        private readonly ILicensingService _licensingService;
        private readonly HttpClient _httpClient;
        
        // Mantido para compatibilidade, embora não usado diretamente aqui
        private const string ApiUrl = "https://codecraftgenz.com.br/";

        [ObservableProperty]
        private bool _isEmailMode = true;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        public ActivationMethodViewModel(ILicensingService licensingService, HttpClient httpClient)
        {
            _licensingService = licensingService;
            _httpClient = httpClient;
        }

        [RelayCommand]
        private void SwitchToEmailMode()
        {
            IsEmailMode = true;
            StatusMessage = string.Empty;
        }

        [RelayCommand]
        private void SwitchToOfflineMode()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window is Views.ActivationMethodWindow activationWindow)
                {
                    activationWindow.Tag = "Offline";
                    activationWindow.Close();
                }
            }
        }

        [RelayCommand]
        private async Task ValidateEmail()
        {
            if (string.IsNullOrWhiteSpace(Email) || !Email.Contains("@"))
            {
                StatusMessage = "Por favor, informe um e-mail válido.";
                return;
            }

            IsLoading = true;
            StatusMessage = "Verificando...";

            try
            {
                var hwId = HardwareHelper.GetHardwareId();
                var svc = new LicenseService();
                
                // 1. Tenta verificar diretamente
                var verifyResult = await svc.VerifyLicenseAsync(Email, hwId);
                
                if (verifyResult.Success)
                {
                    await FinishActivation(verifyResult.LicenseKey);
                    return;
                }

                // 2. Se falhar por limite (LICENSE_LIMIT), tentamos o fluxo de Claim
                if (verifyResult.Code == "LICENSE_LIMIT")
                {
                    StatusMessage = "Limite atingido. Tentando ativar nova licença...";
                    var claimedKey = await svc.ClaimByEmailAsync(Email, hwId);
                    
                    if (!string.IsNullOrEmpty(claimedKey))
                    {
                        // Se conseguiu reivindicar, verificamos novamente para confirmar
                        var retry = await svc.VerifyLicenseAsync(Email, hwId);
                        if (retry.Success)
                        {
                            await FinishActivation(claimedKey);
                            return;
                        }
                    }
                    else
                    {
                        StatusMessage = "Limite de ativações atingido ou compra não encontrada. Use 'Tenho uma chave' ou compre uma nova licença.";
                    }
                }
                else
                {
                    StatusMessage = verifyResult.Message ?? "Licença não encontrada ou inválida.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erro de conexão: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task FinishActivation(string? licenseKey)
        {
            // Salvar e-mail localmente
            try
            {
                var emailFile = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CoinCraft", "license.dat");
                var dir = System.IO.Path.GetDirectoryName(emailFile)!;
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                System.IO.File.WriteAllText(emailFile, Email);
            }
            catch { }

            // Salvar chave de licença se disponível (usando o sistema seguro existente)
            if (!string.IsNullOrEmpty(licenseKey))
            {
                var hwId = HardwareHelper.GetHardwareId();
                var record = new InstallationRecord
                {
                    LicenseKey = licenseKey,
                    MachineFingerprint = hwId,
                    InstalledAtIso8601 = DateTimeOffset.UtcNow.ToString("O"),
                    Notes = $"Activated via email: {Email}"
                };
                LicensingStorage.Save(record);
                
                // Tenta revalidar o serviço principal para atualizar o estado da aplicação
                await _licensingService.ValidateExistingAsync();
            }

            StatusMessage = "Licença válida neste dispositivo.";
            await Task.Delay(1000); // Breve pausa para o usuário ler

            foreach (Window window in Application.Current.Windows)
            {
                if (window is Views.ActivationMethodWindow activationWindow)
                {
                    activationWindow.Tag = "EmailSuccess";
                    activationWindow.DialogResult = true;
                    activationWindow.Close();
                }
            }
        }
    }
}