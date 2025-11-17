using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;

namespace CoinCraft.Services
{
    public sealed class UpdateInfo
    {
        public string Version { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Sha256 { get; set; } = string.Empty;
        public bool Mandatory { get; set; }
    }

    public sealed class UpdateService
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;

        public UpdateService(HttpClient http, string baseUrl)
        {
            _http = http;
            _baseUrl = baseUrl.TrimEnd('/');
        }

        public static string GetCurrentVersion()
        {
            var v = Assembly.GetEntryAssembly()?.GetName().Version;
            return v?.ToString() ?? "0.0.0.0";
        }

        public async Task<UpdateInfo?> CheckForUpdatesAsync(string currentVersion)
        {
            try
            {
                var url = $"{_baseUrl}/api/updates/latest?version={Uri.EscapeDataString(currentVersion)}";
                return await _http.GetFromJsonAsync<UpdateInfo>(url);
            }
            catch { return null; }
        }
    }
}