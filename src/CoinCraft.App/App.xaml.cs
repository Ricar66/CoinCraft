using System.Windows;
using System.IO;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using CoinCraft.Infrastructure;
using CoinCraft.Services;
using CoinCraft.Services.Licensing;
using System.Net.Http;

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text.Json;
using System.Diagnostics;
using System.Threading;

namespace CoinCraft.App;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }
    private static Mutex? _singleInstanceMutex;
    protected override async void OnStartup(StartupEventArgs e)
    {
        this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        try
        {
            _singleInstanceMutex = new Mutex(true, "CoinCraft.App.Singleton", out bool createdNew);
            if (!createdNew)
            {
                MessageBox.Show("O CoinCraft já está em execução.", "Instância única", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }
        }
        catch { }
        // Aplicar migrations no início da aplicação para garantir schema
        try
        {
            var log = new LogService();

            // Logar o caminho do banco para confirmar onde está sendo criado
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dataDir = Path.Combine(appData, "CoinCraft");
            Directory.CreateDirectory(dataDir);
            var dbPath = Path.Combine(dataDir, "coincraft.db");
            log.Info($"Inicializando banco SQLite em: {dbPath}");

            using var db = new CoinCraftDbContext();
            // Checar histórico de migrações
            try
            {
                using var conn0 = db.Database.GetDbConnection();
                conn0.Open();
                using var cmd0 = conn0.CreateCommand();
                cmd0.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory'";
                var historyExists = Convert.ToInt32(cmd0.ExecuteScalar()) > 0;
                log.Info($"__EFMigrationsHistory existe? {historyExists}");
            }
            catch (Exception ex0)
            {
                log.Info($"Falha ao verificar __EFMigrationsHistory: {ex0.Message}");
            }

            db.Database.Migrate();
            log.Info("Migrations aplicadas com sucesso.");

            // Sanidade: garantir coluna AttachmentPath caso alguma base antiga não tenha aplicado a migration
            try
            {
                using var conn = db.Database.GetDbConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA table_info('Transactions')";
                using var reader = cmd.ExecuteReader();
                bool hasAttachment = false;
                while (reader.Read())
                {
                    var colName = reader.GetString(1); // name
                    if (string.Equals(colName, "AttachmentPath", StringComparison.OrdinalIgnoreCase))
                    {
                        hasAttachment = true;
                        break;
                    }
                }
                if (!hasAttachment)
                {
                    log.Info("AttachmentPath ausente em Transactions — aplicando ALTER TABLE.");
                    using var alter = conn.CreateCommand();
                    alter.CommandText = "ALTER TABLE Transactions ADD COLUMN AttachmentPath TEXT";
                    alter.ExecuteNonQuery();
                    log.Info("Coluna AttachmentPath criada via ALTER TABLE.");
                }
            }
            catch (Exception exSanity)
            {
                log.Error($"Falha na checagem/correção de AttachmentPath: {exSanity.Message}");
            }

            // PRAGMAs de SQLite para estabilidade e desempenho
            try
            {
                db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON;");
                db.Database.ExecuteSqlRaw("PRAGMA journal_mode = WAL;");
                db.Database.ExecuteSqlRaw("PRAGMA synchronous = NORMAL;");
                log.Info("PRAGMAs aplicadas.");
            }
            catch (Exception exPragma)
            {
                log.Info($"Falha ao aplicar PRAGMAs: {exPragma.Message}");
            }

            // Garantir tabela RecurringTransactions caso base antiga não a possua
            try
            {
                using var conn = db.Database.GetDbConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='RecurringTransactions'";
                var exists = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                if (!exists)
                {
                    log.Info("Tabela RecurringTransactions ausente — criando via DDL.");
                    var ddl = @"
CREATE TABLE IF NOT EXISTS RecurringTransactions (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  Nome TEXT NOT NULL,
  Frequencia INTEGER NOT NULL,
  StartDate TEXT NOT NULL,
  EndDate TEXT NULL,
  DiaDaSemana INTEGER NULL,
  DiaDoMes INTEGER NULL,
  AutoLancamento INTEGER NOT NULL,
  NextRunDate TEXT NOT NULL,
  Tipo INTEGER NOT NULL,
  Valor TEXT NOT NULL,
  AccountId INTEGER NOT NULL,
  CategoryId INTEGER NULL,
  Descricao TEXT NULL,
  OpostoAccountId INTEGER NULL,
  FOREIGN KEY (AccountId) REFERENCES Accounts(Id) ON DELETE CASCADE,
  FOREIGN KEY (OpostoAccountId) REFERENCES Accounts(Id),
  FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
);

CREATE INDEX IF NOT EXISTS IX_RecurringTransactions_NextRunDate ON RecurringTransactions(NextRunDate);
CREATE INDEX IF NOT EXISTS IX_RecurringTransactions_Frequencia_AccountId ON RecurringTransactions(Frequencia, AccountId);
CREATE INDEX IF NOT EXISTS IX_RecurringTransactions_AccountId ON RecurringTransactions(AccountId);
CREATE INDEX IF NOT EXISTS IX_RecurringTransactions_CategoryId ON RecurringTransactions(CategoryId);
CREATE INDEX IF NOT EXISTS IX_RecurringTransactions_OpostoAccountId ON RecurringTransactions(OpostoAccountId);
";
                    db.Database.ExecuteSqlRaw(ddl);
                    log.Info("Tabela RecurringTransactions criada.");
                }
            }
            catch (Exception exRec)
            {
                log.Error($"Falha na checagem/correção de RecurringTransactions: {exRec.Message}");
            }

            // Verificar se as tabelas principais existem; se não, fazer EnsureCreated
            try
            {
                using var conn = db.Database.GetDbConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('Accounts','Categories','Transactions','Goals','UserSettings')";
                var count = Convert.ToInt32(cmd.ExecuteScalar());
                if (count < 5)
                {
                    log.Info($"Tabelas principais ausentes (encontradas {count}). Executando EnsureCreated.");
                    db.Database.EnsureCreated();
                    // Recontar após EnsureCreated
                    using var cmd2 = conn.CreateCommand();
                    cmd2.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('Accounts','Categories','Transactions','Goals','UserSettings')";
                    var count2 = Convert.ToInt32(cmd2.ExecuteScalar());
                    log.Info($"Após EnsureCreated, tabelas encontradas: {count2}.");
                    if (count2 < 5)
                    {
                        log.Info("Reforçando criação do schema via CREATE TABLE IF NOT EXISTS.");
                        var ddl = @"
CREATE TABLE IF NOT EXISTS Accounts (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  Nome TEXT NOT NULL,
  Tipo INTEGER NOT NULL,
  SaldoInicial TEXT NOT NULL,
  Ativa INTEGER NOT NULL,
  CorHex TEXT NULL,
  Icone TEXT NULL
);

CREATE TABLE IF NOT EXISTS Categories (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  Nome TEXT NOT NULL,
  CorHex TEXT NULL,
  Icone TEXT NULL,
  ParentCategoryId INTEGER NULL,
  LimiteMensal TEXT NULL,
  FOREIGN KEY (ParentCategoryId) REFERENCES Categories(Id)
);

CREATE TABLE IF NOT EXISTS Goals (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  CategoryId INTEGER NOT NULL,
  LimiteMensal TEXT NOT NULL,
  Ano INTEGER NOT NULL,
  Mes INTEGER NOT NULL,
  FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS UserSettings (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  Chave TEXT NOT NULL,
  Valor TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Transactions (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  Data TEXT NOT NULL,
  Tipo INTEGER NOT NULL,
  Valor TEXT NOT NULL,
  AccountId INTEGER NOT NULL,
  CategoryId INTEGER NULL,
  Descricao TEXT NULL,
  OpostoAccountId INTEGER NULL,
  FOREIGN KEY (AccountId) REFERENCES Accounts(Id) ON DELETE CASCADE,
  FOREIGN KEY (OpostoAccountId) REFERENCES Accounts(Id),
  FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
);

CREATE INDEX IF NOT EXISTS IX_Categories_ParentCategoryId ON Categories(ParentCategoryId);
CREATE INDEX IF NOT EXISTS IX_Goals_CategoryId ON Goals(CategoryId);
CREATE INDEX IF NOT EXISTS IX_Transactions_AccountId ON Transactions(AccountId);
CREATE INDEX IF NOT EXISTS IX_Transactions_CategoryId ON Transactions(CategoryId);
CREATE INDEX IF NOT EXISTS IX_Transactions_Data ON Transactions(Data);
CREATE INDEX IF NOT EXISTS IX_Transactions_OpostoAccountId ON Transactions(OpostoAccountId);
CREATE INDEX IF NOT EXISTS IX_Transactions_Tipo_AccountId ON Transactions(Tipo, AccountId);
";
                        db.Database.ExecuteSqlRaw(ddl);
                        using var cmd3 = conn.CreateCommand();
                        cmd3.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name";
                        using var rdr2 = cmd3.ExecuteReader();
                        var names2 = new List<string>();
                        while (rdr2.Read()) names2.Add(rdr2.GetString(0));
                        log.Info($"Após DDL, tabelas: {string.Join(", ", names2)}");
                    }
                    // Listar tabelas para diagnóstico
                    using var listCmd = conn.CreateCommand();
                    listCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name";
                    using var rdr = listCmd.ExecuteReader();
                    var names = new List<string>();
                    while (rdr.Read()) names.Add(rdr.GetString(0));
                    log.Info($"Tabelas: {string.Join(", ", names)}");
                }
                else
                {
                    log.Info($"Tabelas principais já existem (encontradas {count}).");
                    // Listar tabelas para diagnóstico
                    using var listCmd = conn.CreateCommand();
                    listCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name";
                    using var rdr = listCmd.ExecuteReader();
                    var names = new List<string>();
                    while (rdr.Read()) names.Add(rdr.GetString(0));
                    log.Info($"Tabelas: {string.Join(", ", names)}");
                }
            }
            catch (Exception ex)
            {
                log.Info($"Falha ao verificar/criar tabelas: {ex.Message}");
            }

            // Views e seed básicos alinhados com o schema do EF
            try
            {
                var sql = @"
CREATE VIEW IF NOT EXISTS vw_saldo_por_conta AS
WITH mov AS (
  SELECT 
    a.Id AS AccountId,
    SUM(
      CASE
        WHEN t.Tipo = 0 THEN +t.Valor -- Receita
        WHEN t.Tipo = 1 THEN -t.Valor -- Despesa
        WHEN t.Tipo = 2 AND t.AccountId = a.Id THEN -t.Valor -- Transferência (origem)
        WHEN t.Tipo = 2 AND t.OpostoAccountId = a.Id THEN +t.Valor -- Transferência (destino)
        ELSE 0
      END
    ) AS Delta
  FROM Accounts a
  LEFT JOIN Transactions t
    ON t.AccountId = a.Id OR t.OpostoAccountId = a.Id
  GROUP BY a.Id
)
SELECT 
  a.Id,
  a.Nome,
  a.Tipo,
  a.SaldoInicial,
  COALESCE(m.Delta,0) AS Delta,
  (a.SaldoInicial + COALESCE(m.Delta,0)) AS SaldoAtual
FROM Accounts a
LEFT JOIN mov m ON m.AccountId = a.Id;

CREATE VIEW IF NOT EXISTS vw_totais_mensais AS
SELECT 
  substr(Data,1,7) AS AnoMes,
  SUM(CASE WHEN Tipo=0 THEN Valor ELSE 0 END) AS TotalReceitas,
  SUM(CASE WHEN Tipo=1 THEN Valor ELSE 0 END) AS TotalDespesas
FROM Transactions
WHERE Tipo IN (0,1)
GROUP BY substr(Data,1,7)
ORDER BY AnoMes;

CREATE VIEW IF NOT EXISTS vw_despesas_por_categoria_mes AS
SELECT 
  substr(t.Data,1,7) AS AnoMes,
  c.Id AS CategoryId,
  c.Nome AS Categoria,
  SUM(CASE WHEN t.Tipo=1 THEN t.Valor ELSE 0 END) AS TotalDespesas
FROM Transactions t
LEFT JOIN Categories c ON c.Id = t.CategoryId
WHERE t.Tipo=1
GROUP BY substr(t.Data,1,7), c.Id, c.Nome
ORDER BY AnoMes, TotalDespesas DESC;

CREATE TABLE IF NOT EXISTS Meta (
  Chave TEXT PRIMARY KEY,
  Valor TEXT NOT NULL
);
INSERT OR IGNORE INTO Meta (Chave, Valor) VALUES ('schema_version', 'ef_InitialCreate');

INSERT INTO Accounts (Nome, Tipo, SaldoInicial, Ativa, CorHex)
SELECT 'Conta Corrente', 1, 1000, 1, '#4CAF50'
WHERE NOT EXISTS (SELECT 1 FROM Accounts WHERE Nome='Conta Corrente');

INSERT INTO Accounts (Nome, Tipo, SaldoInicial, Ativa, CorHex)
SELECT 'Carteira', 0, 150, 1, '#FFC107'
WHERE NOT EXISTS (SELECT 1 FROM Accounts WHERE Nome='Carteira');

INSERT INTO Categories (Nome, CorHex)
SELECT 'Alimentação', '#FF7043'
WHERE NOT EXISTS (SELECT 1 FROM Categories WHERE Nome='Alimentação');

INSERT INTO Categories (Nome, CorHex)
SELECT 'Transporte', '#42A5F5'
WHERE NOT EXISTS (SELECT 1 FROM Categories WHERE Nome='Transporte');

INSERT INTO Categories (Nome, CorHex)
SELECT 'Salário', '#66BB6A'
WHERE NOT EXISTS (SELECT 1 FROM Categories WHERE Nome='Salário');

-- Seed de transações exemplo (evita duplicação por descrição)
INSERT INTO Transactions (Data, Tipo, Valor, AccountId, CategoryId, Descricao)
SELECT date('now','-20 days'), 0, 5000,
       (SELECT Id FROM Accounts WHERE Nome='Conta Corrente'),
       (SELECT Id FROM Categories WHERE Nome='Salário'),
       'Seed: Salário'
WHERE NOT EXISTS (SELECT 1 FROM Transactions WHERE Descricao='Seed: Salário');

INSERT INTO Transactions (Data, Tipo, Valor, AccountId, CategoryId, Descricao)
SELECT date('now','-18 days'), 1, 120,
       (SELECT Id FROM Accounts WHERE Nome='Carteira'),
       (SELECT Id FROM Categories WHERE Nome='Alimentação'),
       'Seed: Mercado'
WHERE NOT EXISTS (SELECT 1 FROM Transactions WHERE Descricao='Seed: Mercado');

INSERT INTO Transactions (Data, Tipo, Valor, AccountId, OpostoAccountId, Descricao)
SELECT date('now','-15 days'), 2, 200,
       (SELECT Id FROM Accounts WHERE Nome='Conta Corrente'),
       (SELECT Id FROM Accounts WHERE Nome='Carteira'),
       'Seed: Transferência CC -> Carteira'
WHERE NOT EXISTS (SELECT 1 FROM Transactions WHERE Descricao='Seed: Transferência CC -> Carteira');
";

                db.Database.ExecuteSqlRaw(sql);
                log.Info("Views e seeds principais aplicados.");

                // Seed de configurações do usuário
                var userSettingsSql = @"
INSERT OR IGNORE INTO UserSettings (Chave, Valor) VALUES ('tema', 'claro');
INSERT OR IGNORE INTO UserSettings (Chave, Valor) VALUES ('moeda', 'BRL');
INSERT OR IGNORE INTO UserSettings (Chave, Valor) VALUES ('tela_inicial', 'dashboard');
";
                db.Database.ExecuteSqlRaw(userSettingsSql);
                log.Info("UserSettings seed aplicado.");
            }
            catch (Exception exSql)
            {
                log.Info($"Falha ao criar views/seed: {exSql.Message}");
            }

            // Verificação rápida via EF: contagens das tabelas
            try
            {
                var accCount = db.Accounts.Count();
                var catCount = db.Categories.Count();
                var txCount = db.Transactions.Count();
                log.Info($"Verificação EF: Accounts={accCount}, Categories={catCount}, Transactions={txCount}");
            }
            catch (Exception exCnt)
            {
                var log2 = new LogService();
                log2.Error($"Falha ao contar registros via EF: {exCnt.Message}");
            }
        }
        catch (Exception ex)
        {
            var log = new LogService();
            log.Error($"Falha ao aplicar migrations no startup: {ex.Message}");
            MessageBox.Show(ex.Message, "Erro ao inicializar banco de dados", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // Tratar exceções não capturadas para evitar fechamento abrupto
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        // ShutdownMode já definido manualmente na primeira linha

        // Licenciamento temporariamente desativado: não bloquear startup por licença
        // (Reativar removendo este bloco comentado e a mensagem de log abaixo)
        // var skipLic = Environment.GetEnvironmentVariable("COINCRAFT_SKIP_LICENSE");
        // if (skipLic != "1")
        // {
        //     var httpClient = new HttpClient();
        //     var apiClient = new LicenseApiClient(httpClient, "https://licensing.example.com"); // TODO: mover para configuração
        //     var licensing = new LicensingService(apiClient);
        //
        //     var validRes = licensing.ValidateExistingAsync().GetAwaiter().GetResult();
        //     if (!validRes.IsValid)
        //     {
        //         var licWin = new CoinCraft.App.Views.LicenseWindow(licensing, apiClient);
        //         var ok = licWin.ShowDialog();
        //         if (licensing.CurrentState != LicenseState.Active)
        //         {
        //             MessageBox.Show(validRes.Message ?? "Licença inválida ou não fornecida.", "Licença necessária", MessageBoxButton.OK, MessageBoxImage.Warning);
        //             Shutdown();
        //             return;
        //         }
        //     }
        // }
        new LogService().Info("Licenciamento desativado temporariamente: app liberado sem validação.");

        // Configurar Injeção de Dependência
        try
        {
            var sc = new ServiceCollection();
            sc.AddDbContext<CoinCraftDbContext>();

            // Serviços centrais
            sc.AddSingleton<LogService>();
            sc.AddSingleton<BackupService>();
            sc.AddSingleton<RecurringService>();
            sc.AddSingleton<AlertService>();
            sc.AddSingleton<AttachmentService>();
            sc.AddSingleton<ConfigService>();
            sc.AddSingleton<HttpClient>();
            sc.AddTransient<ReportService>();

            // Licenciamento (mantido desativado no fluxo de UI por ora)
            sc.AddSingleton<CoinCraft.Services.Licensing.ILicenseApiClient>(sp =>
                new CoinCraft.Services.Licensing.LicenseApiClient(
                    sp.GetRequiredService<HttpClient>(),
                    "https://licensing.example.com")); // TODO: mover para configuração
            sc.AddSingleton<CoinCraft.Services.Licensing.ILicensingService, CoinCraft.Services.Licensing.LicensingService>();

            // ViewModels
            sc.AddTransient<CoinCraft.App.ViewModels.DashboardViewModel>();
            sc.AddTransient<CoinCraft.App.ViewModels.TransactionsViewModel>();
            sc.AddTransient<CoinCraft.App.ViewModels.RecurringViewModel>();
            sc.AddTransient<CoinCraft.App.ViewModels.AccountsViewModel>();
            sc.AddTransient<CoinCraft.App.ViewModels.CategoriesViewModel>();
            sc.AddTransient<CoinCraft.App.ViewModels.ImportViewModel>();
            sc.AddTransient<CoinCraft.App.ViewModels.SettingsViewModel>();

            Services = sc.BuildServiceProvider();
            new LogService().Info("DI inicializada.");
        }
        catch (Exception exDi)
        {
            var logDi = new LogService();
            logDi.Error($"Falha ao inicializar DI: {exDi.Message}");
        }

        var licensing = Services!.GetRequiredService<CoinCraft.Services.Licensing.ILicensingService>();
        var validRes = await licensing.ValidateExistingAsync();
        if (!validRes.IsValid)
        {
            var licWin = new CoinCraft.App.Views.LicenseWindow(licensing);
            var owner = Application.Current.MainWindow;
            if (owner != null && !ReferenceEquals(owner, licWin)) licWin.Owner = owner;
            var ok = licWin.ShowDialog();
            var activated = ok.HasValue && ok.Value && licensing.CurrentState == LicenseState.Active;
            if (!activated)
            {
                Shutdown();
                return;
            }
        }
        OpenDashboard();

        // Aplicar tema e tela inicial conforme configurações do usuário
        try
        {
            var settingsVm = Services!.GetRequiredService<CoinCraft.App.ViewModels.SettingsViewModel>();
            // Ajuste de tema
            var bg = settingsVm.Tema.Equals("escuro", StringComparison.OrdinalIgnoreCase) ? System.Windows.Media.Color.FromRgb(30, 30, 30) : System.Windows.Media.Colors.White;
            var fg = settingsVm.Tema.Equals("escuro", StringComparison.OrdinalIgnoreCase) ? System.Windows.Media.Colors.White : System.Windows.Media.Colors.Black;
            Application.Current.Resources["AppBackgroundBrush"] = new System.Windows.Media.SolidColorBrush(bg);
            Application.Current.Resources["AppForegroundBrush"] = new System.Windows.Media.SolidColorBrush(fg);

            // Tela inicial: se for lançamentos, abre janela após o Dashboard principal
            var initial = settingsVm.TelaInicial?.ToLowerInvariant();
            if (initial == "lancamentos" && Application.Current.MainWindow is Window owner)
            {
                var tx = new CoinCraft.App.Views.TransactionsWindow { Owner = owner };
                tx.Show();
            }
        }
        catch (Exception exInit)
        {
            new LogService().Info($"Configurações iniciais não aplicadas: {exInit.Message}");
        }

        base.OnStartup(e);
    }

    private void OpenDashboard()
    {
        var dashboard = new CoinCraft.App.Views.DashboardWindow();
        Application.Current.MainWindow = dashboard;
        dashboard.Show();
        dashboard.Activate();
        Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
    }

    private void OpenLicenseWindow(CoinCraft.Services.Licensing.ILicensingService licensing)
    {
        try
        {
            var licWin = new CoinCraft.App.Views.LicenseWindow(licensing);
            var owner = Application.Current.MainWindow;
            if (owner != null && !ReferenceEquals(owner, licWin))
                licWin.Owner = owner;
            var ok = licWin.ShowDialog();
            var activated = ok.HasValue && ok.Value && licensing.CurrentState == LicenseState.Active;
            if (activated)
            {
                OpenDashboard();
            }
            else
            {
                Shutdown();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro ao abrir licença", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var log = new LogService();
        log.Error($"Unhandled: {e.Exception}");
        MessageBox.Show(e.Exception.Message, "Erro inesperado", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
