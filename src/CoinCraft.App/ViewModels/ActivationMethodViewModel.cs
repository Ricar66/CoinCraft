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
        private readonly LicenseService _apiClient;
        
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

        public ActivationMethodViewModel(ILicensingService licensingService, HttpClient httpClient, LicenseService? apiClient = null)
        {
            _licensingService = licensingService;
            _httpClient = httpClient;
            _apiClient = apiClient ?? new LicenseService();
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
                
                // 1. Tenta verificar diretamente usando o novo serviço simplificado
                var success = await _apiClient.VerificarLicenca(Email, hwId);
                
                if (success)
                {
                    await FinishActivation(null); // Key não é mais retornada no modelo simples
                    return;
                }
                else
                {
                    StatusMessage = "Licença não encontrada ou limite atingido.";
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
            // Salvar informações de licença (Email e Opcionalmente a Key)
            var hwId = HardwareHelper.GetHardwareId();
            var record = new InstallationRecord
            {
                LicenseKey = licenseKey ?? string.Empty,
                Email = Email,
                MachineFingerprint = hwId,
                InstalledAtIso8601 = DateTimeOffset.UtcNow.ToString("O"),
                Notes = $"Activated via email: {Email}"
            };
            
            try 
            {
                LicensingStorage.Save(record);
                
                // Tenta revalidar o serviço principal para atualizar o estado da aplicação
                await _licensingService.ValidateExistingAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erro ao salvar licença: {ex.Message}";
                return;
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