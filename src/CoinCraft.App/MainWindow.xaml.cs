using System.Diagnostics;
using System.IO;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using CoinCraft.Infrastructure;
using CoinCraft.Domain;
using CoinCraft.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Timers;
using System.Linq;

namespace CoinCraft.App;

public partial class MainWindow : Window
{
    private readonly BackupService _backup;
    private readonly LogService _log;
    private readonly RecurringService _recurring;
    private readonly ReportService _report;
    private readonly AlertService _alerts;
    private readonly AttachmentService _attachments;
    private System.Timers.Timer? _recurringTimer;

    public MainWindow()
    {
        InitializeComponent();
        _backup = App.Services!.GetRequiredService<BackupService>();
        _log = App.Services!.GetRequiredService<LogService>();
        _recurring = App.Services!.GetRequiredService<RecurringService>();
        _report = App.Services!.GetRequiredService<ReportService>();
        _alerts = App.Services!.GetRequiredService<AlertService>();
        _attachments = App.Services!.GetRequiredService<AttachmentService>();
        _log.Info("App iniciado.");
        // Migrations e criação de banco já foram executadas no startup (App.xaml.cs)
        // Evitar rodar migrations novamente aqui para não travar a UI.

        // Processa recorrentes ao iniciar e agenda verificação leve periódica
        Loaded += (_, __) =>
        {
            try
            {
                var created = _recurring.ProcessDueRecurringTransactions(createSuggestionsOnly: false);
                if (created > 0)
                {
                    _log.Info($"{created} lançamentos recorrentes criados.");
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Recorrentes init falhou: {ex.Message}");
            }

            _recurringTimer = new System.Timers.Timer(60 * 60 * 1000); // 1h
            _recurringTimer.Elapsed += (_, __) =>
            {
                try { _recurring.ProcessDueRecurringTransactions(createSuggestionsOnly: false); }
                catch (Exception ex) { _log.Error($"Recurring timer error: {ex.Message}"); }
            };
            _recurringTimer.AutoReset = true;
            _recurringTimer.Start();

            // Atualiza estado do sino de alertas
            try
            {
                var alerts = _alerts.GetAlerts();
                if (alerts.Count > 0)
                {
                    AlertsButton.Content = $"🔔 {alerts.Count} alerta(s)";
                    AlertsButton.Background = System.Windows.Media.Brushes.IndianRed;
                    AlertsButton.Foreground = System.Windows.Media.Brushes.White;
                    AlertsButton.ToolTip = string.Join("\n", alerts.Select(a => a.Message));
                }
                else
                {
                    AlertsButton.Content = "🔔 Nenhum alerta";
                    AlertsButton.Background = System.Windows.Media.Brushes.Transparent;
                    AlertsButton.ClearValue(System.Windows.Controls.Button.ForegroundProperty);
                    AlertsButton.ToolTip = "Nenhum alerta";
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Falha ao carregar alertas: {ex.Message}");
            }
        };
    }

    private void OnExitClick(object sender, RoutedEventArgs e) => Close();

    private void OnOpenDataFolder(object sender, RoutedEventArgs e)
    {
        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CoinCraft"
        );
        if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
        Process.Start(new ProcessStartInfo { FileName = dataDir, UseShellExecute = true });
    }

    private void OnBackupClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var path = _backup.CreateBackup(Path.Combine(docs, "CoinCraftBackups"));
            MessageBox.Show($"Backup criado em:\n{path}", "Backup", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro ao criar backup", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnDashboardClick(object sender, RoutedEventArgs e)
    {
        var win = new Views.DashboardWindow { Owner = this };
        win.ShowDialog();
    }
    private void OnLancamentosClick(object sender, RoutedEventArgs e)
    {
        var win = new Views.TransactionsWindow { Owner = this };
        win.ShowDialog();
    }
    private void OnRecorrentesClick(object sender, RoutedEventArgs e)
    {
        var win = new Views.RecurringWindow { Owner = this };
        win.ShowDialog();
    }
    private void OnContasClick(object sender, RoutedEventArgs e)
    {
        var win = new Views.AccountsWindow { Owner = this };
        win.ShowDialog();
    }
    private void OnCategoriasClick(object sender, RoutedEventArgs e)
    {
        var win = new Views.CategoriesWindow { Owner = this };
        win.ShowDialog();
    }

    private void OnImportClick(object sender, RoutedEventArgs e)
    {
        var win = new Views.ImportWindow { Owner = this };
        win.ShowDialog();
    }

    private void OnAlertsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var alerts = _alerts.GetAlerts();
            if (alerts.Count == 0)
            {
                MessageBox.Show("Nenhum alerta no momento.", "Alertas", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var text = string.Join("\n", alerts.Select(a => $"• {a.Message}"));
            MessageBox.Show(text, "Alertas", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro ao carregar alertas", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var vm = App.Services!.GetRequiredService<CoinCraft.App.ViewModels.SettingsViewModel>();
            var win = new Views.SettingsWindow(vm) { Owner = this };
            win.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro ao abrir Configurações", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnExportCsvClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var dest = Path.Combine(docs, "CoinCraftExports");
            var path = _report.ExportTransactionsCsv(dest);
            MessageBox.Show($"CSV exportado em:\n{path}", "Exportação CSV", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro na exportação CSV", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnExportPdfClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var dest = Path.Combine(docs, "CoinCraftExports");
            var path = _report.ExportTransactionsPdf(dest);
            MessageBox.Show($"PDF exportado em:\n{path}", "Exportação PDF", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro na exportação PDF", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnSeedClick(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new CoinCraftDbContext();
            // Garante que o schema está criado e atualizado
            db.Database.Migrate();
            if (!db.Accounts.Any())
            {
                db.Accounts.Add(new Account { Nome = "Conta Corrente", Tipo = AccountType.ContaCorrente, SaldoInicial = 1000 });
                db.Accounts.Add(new Account { Nome = "Carteira", Tipo = AccountType.Carteira, SaldoInicial = 150 });
            }
            if (!db.Categories.Any())
            {
                db.Categories.Add(new Category { Nome = "Alimentação", CorHex = "#FF7043" });
                db.Categories.Add(new Category { Nome = "Transporte", CorHex = "#42A5F5" });
                db.Categories.Add(new Category { Nome = "Salário", CorHex = "#66BB6A" });
            }
            db.SaveChanges();

            MessageBox.Show("Dados de exemplo criados (Contas e Categorias).", "Seed", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _log.Error($"Seed falhou: {ex.Message}");
            MessageBox.Show(ex.Message, "Erro ao criar dados de exemplo", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void OnExportSummaryCategoryClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var dest = Path.Combine(docs, "CoinCraftExports");
            var path = _report.ExportSummaryByCategoryCsv(dest);
            MessageBox.Show($"Resumo por Categoria exportado em:\n{path}", "Exportação CSV", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro na exportação de resumo por categoria", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnExportSummaryAccountClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var dest = Path.Combine(docs, "CoinCraftExports");
            var path = _report.ExportSummaryByAccountCsv(dest);
            MessageBox.Show($"Resumo por Conta exportado em:\n{path}", "Exportação CSV", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro na exportação de resumo por conta", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnMainWindowKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.N:
                    OnLancamentosClick(sender, e);
                    e.Handled = true;
                    break;
                case System.Windows.Input.Key.E:
                    OnExportCsvClick(sender, e);
                    e.Handled = true;
                    break;
                case System.Windows.Input.Key.B:
                    OnBackupClick(sender, e);
                    e.Handled = true;
                    break;
            }
        }
    }

    private void OnRestoreClick(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Selecionar arquivo de backup",
            Filter = "CoinCraft Backup (*.coincraft)|*.coincraft|Todos os arquivos (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                _backup.RestoreBackup(dlg.FileName);
                var ask = MessageBox.Show(
                    "Backup restaurado com sucesso. Deseja reiniciar o aplicativo agora?",
                    "Restaurar Backup",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );
                if (ask == MessageBoxResult.Yes)
                {
                    try
                    {
                        var exe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                        if (!string.IsNullOrEmpty(exe))
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = exe!,
                                UseShellExecute = true
                            });
                        }
                    }
                    catch { }
                    Application.Current.Shutdown();
                }
                else
                {
                    MessageBox.Show("Você pode reiniciar mais tarde para aplicar o restore.", "Restaurar Backup", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Erro ao restaurar backup", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void OnCleanupAttachmentsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var (deleted, bytes) = _attachments.CleanupOrphans();
            var mb = bytes / (1024.0 * 1024.0);
            MessageBox.Show($"Removidos {deleted} arquivo(s) órfão(s). Espaço liberado: {mb:F2} MB.", "Limpeza de anexos", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro na limpeza de anexos", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
