using System;
using System.Windows;
using CoinCraft.Services.Licensing;

namespace CoinCraft.App.Views
{
    public partial class EmailPromptWindow : Window
    {
        public string Email { get; private set; } = string.Empty;
        public bool IsVerified { get; private set; } = false;
        public string? LicenseKey { get; private set; }

        public EmailPromptWindow()
        {
            InitializeComponent();
        }

        private async void VerifyButton_Click(object sender, RoutedEventArgs e)
        {
            var email = EmailTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                StatusText.Text = "Por favor, digite um e-mail.";
                return;
            }

            StatusText.Text = "Verificando...";
            StatusText.Foreground = System.Windows.Media.Brushes.Black;
            IsEnabled = false;

            try
            {
                var hardwareId = HardwareHelper.ComputeHardwareId();
                var service = new LicenseService();
                var result = await service.VerifyAsync(email, hardwareId);

                if (result != null && result.Licensed)
                {
                    IsVerified = true;
                    Email = email;
                    LicenseKey = result.LicenseKey;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    StatusText.Text = "Licença não encontrada ou inválida para este hardware.";
                    StatusText.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "Erro de conexão: " + ex.Message;
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
            finally
            {
                IsEnabled = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
