using System.Text.Json;
using CoinCraft.Services;

namespace CoinCraft.App.ViewModels;

public sealed class SettingsViewModel
{
    private readonly ConfigService _config;
    private readonly LogService _log;

    public string Tema { get; set; } = "claro"; // claro/escuro
    public string Moeda { get; set; } = "BRL";  // código da moeda
    public string TelaInicial { get; set; } = "dashboard"; // dashboard/lancamentos

    public SettingsViewModel(ConfigService config, LogService log)
    {
        _config = config;
        _log = log;
        Load();
    }

    public void Load()
    {
        try
        {
            var json = _config.LoadOrDefault();
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            var root = doc.RootElement;
            if (root.TryGetProperty("tema", out var tema)) Tema = tema.GetString() ?? Tema;
            if (root.TryGetProperty("moeda", out var moeda)) Moeda = moeda.GetString() ?? Moeda;
            if (root.TryGetProperty("tela_inicial", out var tela)) TelaInicial = tela.GetString() ?? TelaInicial;
        }
        catch (System.Exception ex)
        {
            _log.Error($"Falha ao carregar configurações: {ex.Message}");
        }
    }

    public void Save()
    {
        try
        {
            var obj = new
            {
                tema = Tema,
                moeda = Moeda,
                tela_inicial = TelaInicial
            };
            var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
            _config.Save(json);
        }
        catch (System.Exception ex)
        {
            _log.Error($"Falha ao salvar configurações: {ex.Message}");
        }
    }
}