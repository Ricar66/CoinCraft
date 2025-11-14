using System;
using System.IO;
using System.Linq;
using CoinCraft.Infrastructure;

namespace CoinCraft.Services;

public sealed class AttachmentService
{
    public string GetAttachmentDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "CoinCraft", "attachments");
        Directory.CreateDirectory(dir);
        return dir;
    }

    public (int deletedCount, long freedBytes) CleanupOrphans()
    {
        var dir = GetAttachmentDirectory();
        if (!Directory.Exists(dir)) return (0, 0);

        using var db = new CoinCraftDbContext();
        var referenced = db.Transactions
            .Where(t => t.AttachmentPath != null && t.AttachmentPath != "")
            .Select(t => t.AttachmentPath!)
            .ToList();
        var refSet = referenced.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var files = Directory.GetFiles(dir);
        int deleted = 0;
        long freed = 0;
        foreach (var f in files)
        {
            if (!refSet.Contains(f))
            {
                try
                {
                    var info = new FileInfo(f);
                    freed += info.Length;
                    File.Delete(f);
                    deleted++;
                }
                catch
                {
                    // ignore individual file errors
                }
            }
        }
        return (deleted, freed);
    }
}