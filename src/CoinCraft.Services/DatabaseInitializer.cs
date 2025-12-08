using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using CoinCraft.Infrastructure;

namespace CoinCraft.Services
{
    public sealed class DatabaseInitializer
    {
        private readonly LogService _log;

        public DatabaseInitializer()
        {
            _log = new LogService();
        }

        public void Initialize()
        {
            try
            {
                // Logar o caminho do banco para confirmar onde está sendo criado
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var dataDir = Path.Combine(appData, "CoinCraft");
                Directory.CreateDirectory(dataDir);
                var dbPath = Path.Combine(dataDir, "coincraft.db");
                _log.Info($"Inicializando banco SQLite em: {dbPath}");

                using var db = new CoinCraftDbContext();
                
                CheckMigrationHistory(db);

                db.Database.Migrate();
                _log.Info("Migrations aplicadas com sucesso.");

                ApplyPragmas(db);
                EnsureRecurringTransactionsTable(db);
                EnsureCoreTables(db);
                EnsureSchemaIntegrity(db);
                ApplyViewsAndSeed(db);
                VerifyDatabase(db);
            }
            catch (Exception ex)
            {
                _log.Error($"Falha ao aplicar migrations no startup: {ex.Message}");
                throw; // Re-throw to be caught by the caller (App.xaml.cs) which might show MessageBox
            }
        }

        private void CheckMigrationHistory(CoinCraftDbContext db)
        {
            try
            {
                using var conn0 = db.Database.GetDbConnection();
                conn0.Open();
                using var cmd0 = conn0.CreateCommand();
                cmd0.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory'";
                var historyExists = Convert.ToInt32(cmd0.ExecuteScalar()) > 0;
                _log.Info($"__EFMigrationsHistory existe? {historyExists}");
            }
            catch (Exception ex0)
            {
                _log.Info($"Falha ao verificar __EFMigrationsHistory: {ex0.Message}");
            }
        }

        private void EnsureSchemaIntegrity(CoinCraftDbContext db)
        {
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
                    _log.Info("AttachmentPath ausente em Transactions — aplicando ALTER TABLE.");
                    using var alter = conn.CreateCommand();
                    alter.CommandText = "ALTER TABLE Transactions ADD COLUMN AttachmentPath TEXT";
                    alter.ExecuteNonQuery();
                    _log.Info("Coluna AttachmentPath criada via ALTER TABLE.");
                }
            }
            catch (Exception exSanity)
            {
                _log.Error($"Falha na checagem/correção de AttachmentPath: {exSanity.Message}");
            }
        }

        private void ApplyPragmas(CoinCraftDbContext db)
        {
            try
            {
                db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON;");
                db.Database.ExecuteSqlRaw("PRAGMA journal_mode = WAL;");
                db.Database.ExecuteSqlRaw("PRAGMA synchronous = NORMAL;");
                _log.Info("PRAGMAs aplicadas.");
            }
            catch (Exception exPragma)
            {
                _log.Info($"Falha ao aplicar PRAGMAs: {exPragma.Message}");
            }
        }

        private void EnsureRecurringTransactionsTable(CoinCraftDbContext db)
        {
            try
            {
                using var conn = db.Database.GetDbConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='RecurringTransactions'";
                var exists = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                if (!exists)
                {
                    _log.Info("Tabela RecurringTransactions ausente — criando via DDL.");
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
                    _log.Info("Tabela RecurringTransactions criada.");
                }
            }
            catch (Exception exRec)
            {
                _log.Error($"Falha na checagem/correção de RecurringTransactions: {exRec.Message}");
            }
        }

        private void EnsureCoreTables(CoinCraftDbContext db)
        {
            try
            {
                using var conn = db.Database.GetDbConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('Accounts','Categories','Transactions','Goals','UserSettings')";
                var count = Convert.ToInt32(cmd.ExecuteScalar());
                if (count < 5)
                {
                    _log.Info($"Tabelas principais ausentes (encontradas {count}). Executando EnsureCreated.");
                    db.Database.EnsureCreated();
                    // Recontar após EnsureCreated
                    using var cmd2 = conn.CreateCommand();
                    cmd2.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('Accounts','Categories','Transactions','Goals','UserSettings')";
                    var count2 = Convert.ToInt32(cmd2.ExecuteScalar());
                    _log.Info($"Após EnsureCreated, tabelas encontradas: {count2}.");
                    if (count2 < 5)
                    {
                        _log.Info("Reforçando criação do schema via CREATE TABLE IF NOT EXISTS.");
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
  AttachmentPath TEXT NULL,
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
                        _log.Info($"Após DDL, tabelas: {string.Join(", ", names2)}");
                    }
                    // Listar tabelas para diagnóstico
                    using var listCmd = conn.CreateCommand();
                    listCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name";
                    using var rdr = listCmd.ExecuteReader();
                    var names = new List<string>();
                    while (rdr.Read()) names.Add(rdr.GetString(0));
                    _log.Info($"Tabelas: {string.Join(", ", names)}");
                }
                else
                {
                    _log.Info($"Tabelas principais já existem (encontradas {count}).");
                    // Listar tabelas para diagnóstico
                    using var listCmd = conn.CreateCommand();
                    listCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name";
                    using var rdr = listCmd.ExecuteReader();
                    var names = new List<string>();
                    while (rdr.Read()) names.Add(rdr.GetString(0));
                    _log.Info($"Tabelas: {string.Join(", ", names)}");
                }
            }
            catch (Exception ex)
            {
                _log.Info($"Falha ao verificar/criar tabelas: {ex.Message}");
            }
        }

        private void ApplyViewsAndSeed(CoinCraftDbContext db)
        {
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
                _log.Info("Views e seeds principais aplicados.");

                // Seed de configurações do usuário
                var userSettingsSql = @"
INSERT OR IGNORE INTO UserSettings (Chave, Valor) VALUES ('tema', 'claro');
INSERT OR IGNORE INTO UserSettings (Chave, Valor) VALUES ('moeda', 'BRL');
INSERT OR IGNORE INTO UserSettings (Chave, Valor) VALUES ('tela_inicial', 'dashboard');
";
                db.Database.ExecuteSqlRaw(userSettingsSql);
                _log.Info("UserSettings seed aplicado.");
            }
            catch (Exception exSql)
            {
                _log.Info($"Falha ao criar views/seed: {exSql.Message}");
            }
        }

        private void VerifyDatabase(CoinCraftDbContext db)
        {
            // Verificação rápida via EF: contagens das tabelas
            try
            {
                var accCount = db.Accounts.Count();
                var catCount = db.Categories.Count();
                var txCount = db.Transactions.Count();
                _log.Info($"Verificação EF: Accounts={accCount}, Categories={catCount}, Transactions={txCount}");
            }
            catch (Exception exCnt)
            {
                _log.Error($"Falha ao contar registros via EF: {exCnt.Message}");
            }
        }
    }
}
