using System.Diagnostics;
using System.IO;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using CoinCraft.Infrastructure;
using CoinCraft.Domain;
using CoinCraft.Services;
using System.Timers;

namespace CoinCraft.App;

public partial class MainWindow : Window
{
    private readonly BackupService _backup = new();
    private readonly LogService _log = new();
    private readonly RecurringService _recurring = new();
    private System.Timers.Timer? _recurringTimer;

    public MainWindow()
    {
        InitializeComponent();
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

    private void OnExportCsvClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var dest = Path.Combine(docs, "CoinCraftExports");
            var report = new ReportService();
            var path = report.ExportTransactionsCsv(dest);
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
            var report = new ReportService();
            var path = report.ExportTransactionsPdf(dest);
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
            var report = new ReportService();
            var path = report.ExportSummaryByCategoryCsv(dest);
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
            var report = new ReportService();
            var path = report.ExportSummaryByAccountCsv(dest);
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
}
