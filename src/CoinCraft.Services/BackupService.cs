using System.IO.Compression;

namespace CoinCraft.Services;

public sealed class BackupService
{
    public string CreateBackup(string destinationFolder)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dataDir = Path.Combine(appData, "CoinCraft");
        var dbPath = Path.Combine(dataDir, "coincraft.db");
        var configPath = Path.Combine(dataDir, "config.json");
        Directory.CreateDirectory(destinationFolder);
        var file = Path.Combine(destinationFolder, $"coincraft_{DateTime.Now:yyyyMMdd_HHmmss}.coincraft");

        using var zip = ZipFile.Open(file, ZipArchiveMode.Create);
        if (File.Exists(dbPath)) zip.CreateEntryFromFile(dbPath, "coincraft.db");
        if (File.Exists(configPath)) zip.CreateEntryFromFile(configPath, "config.json");

        return file;
    }

    public void RestoreBackup(string backupFile)
    {
        if (!File.Exists(backupFile)) throw new FileNotFoundException("Arquivo de backup não encontrado", backupFile);

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dataDir = Path.Combine(appData, "CoinCraft");
        Directory.CreateDirectory(dataDir);

        var tempDir = Path.Combine(Path.GetTempPath(), "CoinCraftRestore_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            ZipFile.ExtractToDirectory(backupFile, tempDir);
            var srcDb = Path.Combine(tempDir, "coincraft.db");
            var srcCfg = Path.Combine(tempDir, "config.json");
            var dstDb = Path.Combine(dataDir, "coincraft.db");
            var dstCfg = Path.Combine(dataDir, "config.json");

            if (File.Exists(srcDb))
            {
                if (File.Exists(dstDb)) { try { File.Delete(dstDb); } catch { } }
                File.Copy(srcDb, dstDb, overwrite: true);
            }
            if (File.Exists(srcCfg))
            {
                if (File.Exists(dstCfg)) { try { File.Delete(dstCfg); } catch { } }
                File.Copy(srcCfg, dstCfg, overwrite: true);
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }
}
