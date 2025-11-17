using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace CoinCraft.App.Views.Controls
{
    public partial class SettingsControl : UserControl
    {
        private readonly CoinCraft.App.ViewModels.SettingsViewModel _vm = App.Services!.GetRequiredService<CoinCraft.App.ViewModels.SettingsViewModel>();

        public SettingsControl()
        {
            InitializeComponent();
            DataContext = _vm;
            Loaded += (_, __) =>
            {
                // Preencher combos com valores atuais
                TemaCombo.SelectedValue = _vm.Tema;
                MoedaCombo.SelectedValue = _vm.Moeda;
                TelaInicialCombo.SelectedValue = _vm.TelaInicial;
            };
        }

        

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            _vm.Tema = (TemaCombo.SelectedValue as string) ?? _vm.Tema;
            _vm.Moeda = (MoedaCombo.SelectedValue as string) ?? _vm.Moeda;
            _vm.TelaInicial = (TelaInicialCombo.SelectedValue as string) ?? _vm.TelaInicial;
            _vm.Save();

            var dark = string.Equals(_vm.Tema, "escuro", System.StringComparison.OrdinalIgnoreCase);
            var bg = dark ? System.Windows.Media.Color.FromRgb(30, 30, 30) : System.Windows.Media.Colors.White;
            var fg = dark ? System.Windows.Media.Colors.White : System.Windows.Media.Colors.Black;
            Application.Current.Resources["AppBackgroundBrush"] = new System.Windows.Media.SolidColorBrush(bg);
            Application.Current.Resources["AppForegroundBrush"] = new System.Windows.Media.SolidColorBrush(fg);
            MessageBox.Show("Configurações salvas.", "Configurações", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnApplyOfflineUpdateClick(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Pacotes de atualização (*.zip)|*.zip|Todos os arquivos (*.*)|*.*",
                Title = "Selecionar pacote de atualização offline"
            };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var key = CoinCraft.Services.PublicKey.Xml;
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        MessageBox.Show("Chave pública não configurada.", "Atualização", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    var upd = new CoinCraft.Services.OfflineUpdateService(key);
                    if (!upd.VerifyPackage(dlg.FileName, out var ver))
                    {
                        MessageBox.Show("Pacote inválido ou assinatura incorreta.", "Atualização", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    var exe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                    var installDir = System.IO.Path.GetDirectoryName(exe!);
                    var script = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", "Apply-OfflineUpdate.ps1");
                    if (!System.IO.File.Exists(script))
                    {
                        MessageBox.Show("Script de atualização não encontrado.", "Atualização", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "powershell",
                        Arguments = $"-ExecutionPolicy Bypass -File \"{script}\" -PackagePath \"{dlg.FileName}\" -InstallDir \"{installDir}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    System.Diagnostics.Process.Start(psi);
                    MessageBox.Show($"Atualização aplicada. O app será fechado.", "Atualização", MessageBoxButton.OK, MessageBoxImage.Information);
                    Application.Current.Shutdown();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.Message, "Erro na atualização", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}