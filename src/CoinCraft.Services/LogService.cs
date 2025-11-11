namespace CoinCraft.Services;

public sealed class LogService
{
    private readonly string _logDir;
    public LogService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _logDir = Path.Combine(appData, "CoinCraft", "logs");
        Directory.CreateDirectory(_logDir);
    }
    public void Info(string msg) => Write("INFO", msg);
    public void Error(string msg) => Write("ERROR", msg);

    private void Write(string level, string msg)
    {
        var file = Path.Combine(_logDir, $"{DateTime.Today:yyyy-MM-dd}.log");
        File.AppendAllText(file, $"[{DateTime.Now:HH:mm:ss}] [{level}] {msg}{Environment.NewLine}");
    }
}
