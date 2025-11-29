using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace CoinCraft.Admin
{
    public partial class MainWindow : Window
    {
        private const string SMTP_HOST = "smtp.gmail.com";
        private const int SMTP_PORT = 587;
        private const bool SMTP_SSL = true;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnGenerateAndSendClick(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Gerando licença...";
            var name = NameText.Text?.Trim() ?? string.Empty;
            var email = EmailText.Text?.Trim() ?? string.Empty;
            var hwid = HardwareText.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(hwid)) { StatusText.Text = "Informe o Hardware ID"; return; }
            try
            {
                var privatePath = Environment.GetEnvironmentVariable("COINCRAFT_PRIVATE_XML_PATH");
                if (string.IsNullOrWhiteSpace(privatePath)) privatePath = Path.Combine(AppContext.BaseDirectory, "private.xml");
                if (!File.Exists(privatePath)) { StatusText.Text = "private.xml não encontrado"; return; }
                var keyText = File.ReadAllText(privatePath);
                var data = Encoding.UTF8.GetBytes(hwid);
                byte[] sig;
                if (keyText.Contains("<RSAKeyValue>"))
                {
                    using var rsa = new RSACryptoServiceProvider();
                    rsa.FromXmlString(keyText);
                    sig = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                }
                else if (keyText.Contains("BEGIN EC") || keyText.Contains("BEGIN PRIVATE KEY") || keyText.Contains("BEGIN EC PRIVATE KEY"))
                {
                    try
                    {
                        using var ecdsa = ECDsa.Create();
                        ecdsa.ImportFromPem(keyText);
                        sig = ecdsa.SignData(data, HashAlgorithmName.SHA256);
                    }
                    catch
                    {
                        using var rsa = RSA.Create();
                        rsa.ImportFromPem(keyText);
                        sig = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    }
                }
                else
                {
                    using var rsa = RSA.Create();
                    rsa.ImportFromPem(keyText);
                    sig = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                }
                var license = Convert.ToBase64String(sig);
                LicenseOutput.Text = license;

                if (string.IsNullOrWhiteSpace(email)) { StatusText.Text = "Licença gerada. Informe o e-mail para enviar."; return; }
                var senderEmail = (SenderEmailText.Text?.Trim() ?? string.Empty);
                var senderPass = SenderAppPasswordBox.Password?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(senderEmail)) senderEmail = Environment.GetEnvironmentVariable("COINCRAFT_ADMIN_EMAIL") ?? string.Empty;
                if (string.IsNullOrWhiteSpace(senderPass)) senderPass = Environment.GetEnvironmentVariable("COINCRAFT_ADMIN_APP_PASSWORD") ?? string.Empty;
                senderPass = senderPass.Replace(" ", string.Empty);
                var host = Environment.GetEnvironmentVariable("COINCRAFT_ADMIN_SMTP_HOST") ?? SMTP_HOST;
                var portStr = Environment.GetEnvironmentVariable("COINCRAFT_ADMIN_SMTP_PORT");
                var sslStr = Environment.GetEnvironmentVariable("COINCRAFT_ADMIN_SMTP_SSL");
                int port = SMTP_PORT; if (!string.IsNullOrWhiteSpace(portStr) && int.TryParse(portStr, out var p)) port = p;
                bool ssl = SMTP_SSL; if (!string.IsNullOrWhiteSpace(sslStr) && bool.TryParse(sslStr, out var s)) ssl = s;
                if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(senderPass)) { StatusText.Text = "Configure COINCRAFT_ADMIN_EMAIL e COINCRAFT_ADMIN_APP_PASSWORD"; return; }
                try { _ = new MailAddress(senderEmail); _ = new MailAddress(email); }
                catch { StatusText.Text = "E-mail inválido (remetente ou destinatário)"; return; }
                StatusText.Text = "Enviando e-mail...";
                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = ssl,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(senderEmail, senderPass)
                };
                using var message = new MailMessage
                {
                    From = new MailAddress(senderEmail, "CoinCraft Admin"),
                    Subject = "Sua licença CoinCraft",
                    Body = $"<html><body style=\"font-family:Segoe UI\"><p>Olá {WebUtility.HtmlEncode(name)},</p><p>Obrigado por comprar o CoinCraft!</p><p>Esta é sua chave de ativação definitiva:</p><pre style=\"background:#f4f4f4;padding:12px;border-radius:8px\">{WebUtility.HtmlEncode(license)}</pre><p>Qualquer dúvida, responda este e-mail.</p><p>Atenciosamente,<br/>CoinCraft</p></body></html>",
                    IsBodyHtml = true
                };
                message.To.Add(new MailAddress(email));
                client.Send(message);
                StatusText.Text = "Sucesso";
            }
            catch (Exception ex)
            {
                StatusText.Text = ex.Message;
            }
        }
    }
}
