using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoinCraft.Services.Licensing;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;

namespace CoinCraft.App.ViewModels
{
    public partial class ActivationMethodViewModel : ObservableObject
    {
        private readonly ILicensingService _licensingService;
        private readonly HttpClient _httpClient;
        private const string ApiUrl = "https://codecraftgenz.com.br/api/licenses/activate-by-email";

        // IsEmailMode is no longer needed as logic is direct, but keeping for compatibility if View still binds (removed in XAML)
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
                var hardwareId = MachineIdProvider.ComputeFingerprint();
                var payload = new { email = Email, hardwareId = hardwareId };

                var response = await _httpClient.PostAsJsonAsync(ApiUrl, payload);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ActivationResponse>();
                    if (result != null && !string.IsNullOrEmpty(result.LicenseKey))
                    {
                        var licenseResult = await _licensingService.EnsureLicensedAsync(() => Task.FromResult<string?>(result.LicenseKey));
                        
                        if (licenseResult.IsValid)
                        {
                            StatusMessage = "Ativado com sucesso!";
                            foreach (Window window in Application.Current.Windows)
                            {
                                if (window is Views.ActivationMethodWindow activationWindow)
                                {
                                    activationWindow.DialogResult = true;
                                    activationWindow.Close();
                                }
                            }
                        }
                        else
                        {
                            StatusMessage = "Chave recebida, mas inválida localmente.";
                        }
                    }
                    else
                    {
                        StatusMessage = "Resposta inválida do servidor.";
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Conflict) // 409
                {
                    StatusMessage = string.Empty; // Limpa mensagem de status anterior
                    MessageBox.Show("Identificamos que este computador já tem uma licença! Por favor, use a opção manual e cole sua chave antiga.", "Licença Existente", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    StatusMessage = !string.IsNullOrWhiteSpace(errorContent) ? errorContent : $"Erro no servidor: {response.StatusCode}";
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

        private class ActivationResponse
        {
            public string LicenseKey { get; set; } = string.Empty;
        }
    }
}
