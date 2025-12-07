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
using System.Linq;

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

        // Removido prompt direto de e-mail para manter a tela antiga com duas opções

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
        //     var services = new ServiceCollection();
        //     ConfigureServices(services);
        //     Services = services.BuildServiceProvider();
        //
        //     // Configurar o licenciamento
        //     var licenseService = Services.GetRequiredService<CoinCraft.Services.Licensing.LicenseService>();
        //     var licensing = new CoinCraft.Services.Licensing.LicensingService(licenseService);
        //     
        //     // Validar licença existente
        //     var validRes = licensing.ValidateExistingAsync().GetAwaiter().GetResult();
        //
        //     if (!validRes.IsValid)
        //     {
        //         // Usar o LicenseService injetado em vez de criar HttpClient manualmente
        //         var httpClient = Services.GetRequiredService<HttpClient>();
        //         var vm = new CoinCraft.App.ViewModels.ActivationMethodViewModel(licensing, httpClient);
        //         var activationWin = new CoinCraft.App.Views.ActivationMethodWindow(vm);
        //         var dialogResult = activationWin.ShowDialog();
        //         
        //         if (dialogResult == true)
        //         {
        //             if (Equals(activationWin.Tag, "EmailSuccess"))
        //             {
        //                  // Sucesso
        //             }
        //             else if (licensing.CurrentState != LicenseState.Active)
        //             {
        //                 MessageBox.Show(validRes.Message ?? "Licença inválida ou não fornecida.", "Licença necessária", MessageBoxButton.OK, MessageBoxImage.Warning);
        //                 Shutdown();
        //                 return;
        //             }
        //         }
        //         else if (Equals(activationWin.Tag, "Offline"))
        //         {
        //              // Fluxo offline mantido se necessário
        //         }
        //         else
        //         {
        //             Shutdown();
        //             return;
        //         }
        //     }
        //
        //     var mainWindow = Services.GetRequiredService<MainWindow>();
        //     mainWindow.Show();
        // }
        
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        // Configurar o licenciamento
        var licenseService = Services.GetRequiredService<CoinCraft.Services.Licensing.LicenseService>();
        var licensing = new CoinCraft.Services.Licensing.LicensingService(licenseService);
        
        // Validar licença existente
        var validRes = await licensing.ValidateExistingAsync();

        if (!validRes.IsValid)
        {
            // Usar o LicenseService injetado em vez de criar HttpClient manualmente
            var httpClient = Services.GetRequiredService<HttpClient>();
            var vm = new CoinCraft.App.ViewModels.ActivationMethodViewModel(licensing, httpClient);
            var activationWin = new CoinCraft.App.Views.ActivationMethodWindow(vm);
            var dialogResult = activationWin.ShowDialog();
            
            if (dialogResult == true)
            {
                if (Equals(activationWin.Tag, "EmailSuccess"))
                {
                     // Sucesso
                }
                else if (licensing.CurrentState != LicenseState.Active)
                {
                    MessageBox.Show(validRes.Message ?? "Licença inválida ou não fornecida.", "Licença necessária", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Shutdown();
                    return;
                }
            }
            else if (Equals(activationWin.Tag, "Offline"))
            {
                 var licWin = new CoinCraft.App.Views.LicenseWindow(licensing);
                 var ok = licWin.ShowDialog();
                 if (licensing.CurrentState != LicenseState.Active)
                 {
                     MessageBox.Show(validRes.Message ?? "Licença inválida ou não fornecida.", "Licença necessária", MessageBoxButton.OK, MessageBoxImage.Warning);
                     Shutdown();
                     return;
                 }
            }
            else
            {
                Shutdown();
                return;
            }
        }

        var settingsVm = Services!.GetRequiredService<CoinCraft.App.ViewModels.SettingsViewModel>();
        var initial = settingsVm.TelaInicial?.ToLowerInvariant();
        Window mainWindow;
        if (initial == "lancamentos")
        {
            mainWindow = new CoinCraft.App.Views.TransactionsWindow();
        }
        else
        {
            mainWindow = Services.GetRequiredService<CoinCraft.App.Views.DashboardWindow>();
        }
        Application.Current.MainWindow = mainWindow;
        Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
        mainWindow.Show();
        new LogService().Info("Aplicação iniciada com sucesso.");

        // Aplicar tema e tela inicial conforme configurações do usuário
        try
        {
            // Já carregado acima para decidir a tela inicial
            // Ajuste de tema
            var bg = settingsVm.Tema.Equals("escuro", StringComparison.OrdinalIgnoreCase) ? System.Windows.Media.Color.FromRgb(30, 30, 30) : System.Windows.Media.Colors.White;
            var fg = settingsVm.Tema.Equals("escuro", StringComparison.OrdinalIgnoreCase) ? System.Windows.Media.Colors.White : System.Windows.Media.Colors.Black;
            Application.Current.Resources["AppBackgroundBrush"] = new System.Windows.Media.SolidColorBrush(bg);
            Application.Current.Resources["AppForegroundBrush"] = new System.Windows.Media.SolidColorBrush(fg);

            // A tela inicial já foi escolhida antes de mostrar a MainWindow.
        }
        catch (Exception exInit)
        {
            new LogService().Info($"Configurações iniciais não aplicadas: {exInit.Message}");
        }

        // Removido código antigo comentado e desnecessário
    }

    private void OpenDashboard()
    {
        var settingsVm = Services!.GetRequiredService<CoinCraft.App.ViewModels.SettingsViewModel>();
        var initial = settingsVm.TelaInicial?.ToLowerInvariant();
        Window w = initial == "lancamentos"
            ? new CoinCraft.App.Views.TransactionsWindow()
            : new CoinCraft.App.Views.DashboardWindow();
        Application.Current.MainWindow = w;
        w.Show();
        w.Activate();
        Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<CoinCraftDbContext>();

        // Serviços centrais
        services.AddSingleton<LogService>();
        services.AddSingleton<BackupService>();
        services.AddSingleton<RecurringService>();
        services.AddSingleton<AlertService>();
        services.AddSingleton<AttachmentService>();
        services.AddSingleton<ConfigService>();
        services.AddSingleton<HttpClient>();
        services.AddTransient<ReportService>();

        // Context Factory para ViewModels que criam instâncias de curta duração
        services.AddSingleton<Func<CoinCraftDbContext>>(sp => () => new CoinCraftDbContext());

        // Licenciamento
        services.AddSingleton<CoinCraft.Services.Licensing.LicenseService>();
        services.AddSingleton<CoinCraft.Services.Licensing.ILicensingService, CoinCraft.Services.Licensing.LicensingService>();

        // ViewModels
        services.AddTransient<CoinCraft.App.ViewModels.DashboardViewModel>();
        services.AddTransient<CoinCraft.App.ViewModels.TransactionsViewModel>();
        services.AddTransient<CoinCraft.App.ViewModels.RecurringViewModel>();
        services.AddTransient<CoinCraft.App.ViewModels.AccountsViewModel>();
        services.AddTransient<CoinCraft.App.ViewModels.CategoriesViewModel>();
        services.AddTransient<CoinCraft.App.ViewModels.ImportViewModel>();
        services.AddTransient<CoinCraft.App.ViewModels.GoalsViewModel>();
        services.AddTransient<CoinCraft.App.ViewModels.SettingsViewModel>();
        services.AddTransient<CoinCraft.App.ViewModels.ManualViewModel>();
        services.AddTransient<CoinCraft.App.ViewModels.ActivationMethodViewModel>();
        
        // Views
        services.AddTransient<CoinCraft.App.Views.DashboardWindow>();
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

    private string? PromptEmail()
    {
        var win = new Window
        {
            Title = "Email",
            Width = 360,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ResizeMode = ResizeMode.NoResize,
            Background = System.Windows.Media.Brushes.White
        };
        var grid = new System.Windows.Controls.Grid { Margin = new Thickness(16) };
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
        var label = new System.Windows.Controls.TextBlock { Text = "Informe seu e-mail", Margin = new Thickness(0, 0, 0, 8) };
        var box = new System.Windows.Controls.TextBox { Margin = new Thickness(0, 0, 0, 12) };
        var panel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var okBtn = new System.Windows.Controls.Button { Content = "OK", Width = 80, Margin = new Thickness(0, 0, 8, 0) };
        var cancelBtn = new System.Windows.Controls.Button { Content = "Cancelar", Width = 80 };
        okBtn.Click += (_, __) => { win.DialogResult = true; win.Close(); };
        cancelBtn.Click += (_, __) => { win.DialogResult = false; win.Close(); };
        panel.Children.Add(okBtn);
        panel.Children.Add(cancelBtn);
        System.Windows.Controls.Grid.SetRow(label, 0);
        System.Windows.Controls.Grid.SetRow(box, 1);
        System.Windows.Controls.Grid.SetRow(panel, 2);
        grid.Children.Add(label);
        grid.Children.Add(box);
        grid.Children.Add(panel);
        win.Content = grid;
        var r = win.ShowDialog();
        if (r == true) return box.Text.Trim();
        return null;
    }

    public static void ShowSingle<T>(Func<T> factory) where T : Window
    {
        var existing = Application.Current.Windows.OfType<T>().FirstOrDefault();
        if (existing != null)
        {
            if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }
        var w = factory();
        if (Application.Current.MainWindow != null && !ReferenceEquals(Application.Current.MainWindow, w))
            w.Owner = Application.Current.MainWindow;
        w.Show();
    }
}
