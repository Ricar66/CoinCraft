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

        // Inicializar recursos de tema com valores padrão para evitar StaticResourceExtension exception
        // antes que o banco de dados seja lido.
        Application.Current.Resources["AppBackgroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
        Application.Current.Resources["AppForegroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
        Application.Current.Resources["BoolToVis"] = new System.Windows.Controls.BooleanToVisibilityConverter();

        try
        {
            string? foundKeyPath = null;
            string baseDir = AppContext.BaseDirectory;
            var candidates = new[]
            {
                System.IO.Path.Combine(baseDir, "public.xml"),
                System.IO.Path.Combine(baseDir, "public.pem"),
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CoinCraft", "public.xml"),
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CoinCraft", "public.pem"),
                // Dev machine known location
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive - Tracbel", "Área de Trabalho", "CoinCraft", "publish_final", "public.xml"),
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive - Tracbel", "Área de Trabalho", "CoinCraft", "publish_final", "public.pem")
            };
            foreach (var c in candidates)
            {
                if (System.IO.File.Exists(c)) { foundKeyPath = c; break; }
            }
            if (!string.IsNullOrWhiteSpace(foundKeyPath))
            {
                if (foundKeyPath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                    Environment.SetEnvironmentVariable("COINCRAFT_PUBLICKEY_XML_PATH", foundKeyPath);
                else
                    Environment.SetEnvironmentVariable("COINCRAFT_PUBLICKEY_PEM_PATH", foundKeyPath);
                Environment.SetEnvironmentVariable("COINCRAFT_ALLOW_OFFLINE", "1");
                new LogService().Info($"Chave pública para offline localizada em: {foundKeyPath}");
            }
            else
            {
                new LogService().Info("Chave pública não encontrada nos caminhos padrão; offline pode falhar.");
            }
            LogResources();
        }
        catch { }

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
            var dbInit = new DatabaseInitializer();
            dbInit.Initialize();
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

            // Context Factory para ViewModels que criam instâncias de curta duração
            sc.AddSingleton<Func<CoinCraftDbContext>>(sp => () => new CoinCraftDbContext());

            // Licenciamento (mantido desativado no fluxo de UI por ora)
            sc.AddSingleton<CoinCraft.Services.Licensing.ILicenseApiClient>(sp =>
                new CoinCraft.Services.Licensing.LicenseApiClient(
                    sp.GetRequiredService<HttpClient>(),
                    "https://codecraftgenz.com.br/"));
            sc.AddSingleton<CoinCraft.Services.Licensing.ILicensingService, CoinCraft.Services.Licensing.LicensingService>();

            // ViewModels
            sc.AddTransient<CoinCraft.App.ViewModels.DashboardViewModel>();
            sc.AddTransient<CoinCraft.App.ViewModels.TransactionsViewModel>();
            sc.AddTransient<CoinCraft.App.ViewModels.RecurringViewModel>();
            sc.AddTransient<CoinCraft.App.ViewModels.AccountsViewModel>();
            sc.AddTransient<CoinCraft.App.ViewModels.CategoriesViewModel>();
            sc.AddTransient<CoinCraft.App.ViewModels.ImportViewModel>();
            sc.AddTransient<CoinCraft.App.ViewModels.GoalsViewModel>();
            sc.AddTransient<CoinCraft.App.ViewModels.SettingsViewModel>();
            sc.AddTransient<CoinCraft.App.ViewModels.ManualViewModel>();

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
            // Fluxo de ativação: Escolha de método
            var httpClient = Services!.GetRequiredService<HttpClient>();
            var activationVm = new CoinCraft.App.ViewModels.ActivationMethodViewModel(licensing, httpClient);
            var activationWin = new CoinCraft.App.Views.ActivationMethodWindow(activationVm);
            
            var actResult = activationWin.ShowDialog();

            // 1. Se ativou via email (DialogResult=true) e estado Active
            if (actResult == true && licensing.CurrentState == LicenseState.Active)
            {
                // Segue o fluxo normal para abrir Dashboard
            }
            // 2. Se usuário pediu modo Offline (Tag="Offline")
            else if (activationWin.Tag?.ToString() == "Offline")
            {
                var licWin = new CoinCraft.App.Views.LicenseWindow(licensing);
                var owner = Application.Current.MainWindow;
                // Define owner apenas se houver alguma janela principal visível que não seja ela mesma
                if (owner != null && !ReferenceEquals(owner, licWin) && owner.IsVisible) 
                    licWin.Owner = owner;
                
                var ok = licWin.ShowDialog();
                var activated = ok.HasValue && ok.Value && licensing.CurrentState == LicenseState.Active;
                if (!activated)
                {
                    Shutdown();
                    return;
                }
            }
            // 3. Cancelou ou fechou
            else
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
        string details;
        if (e.Exception is System.Windows.Markup.XamlParseException xe)
        {
            details = $"XamlParse: {xe.Message} | Inner: {xe.InnerException?.GetType().Name}: {xe.InnerException?.Message} | Line: {xe.LineNumber} | Uri: {xe.BaseUri}";
        }
        else
        {
            details = e.Exception.ToString();
        }
        log.Error(details);
        MessageBox.Show(details, "Erro inesperado", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private static void LogResources()
    {
        var log = new LogService();
        var keys = new[]
        {
            "AppBackgroundBrush","AppForegroundBrush","PrimaryBrush","PrimaryDarkBrush","AccentBrush","SuccessBrush","DangerBrush","WarningBrush",
            "TextPrimaryBrush","TextSecondaryBrush","CardBackgroundBrush","BorderBrush","HeaderTextStyle","SectionHeaderStyle","PrimaryButtonStyle","SecondaryButtonStyle","MenuButtonStyle","BoolToVis"
        };
        foreach (var k in keys)
        {
            var present = Application.Current.Resources.Contains(k);
            log.Info($"Resource {k}: {(present ? "OK" : "MISSING")}");
        }
    }
}
