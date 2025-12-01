using System;
using System.Threading;
using Xunit;
using CoinCraft.App.Views;
using CoinCraft.App.ViewModels;
using CoinCraft.Services.Licensing;
using Moq;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CoinCraft.Tests.UI
{
    public class WindowSmokeTests
    {
        private void RunOnSta(Action action)
        {
            Exception? captured = null;
            var t = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { captured = ex; }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
            if (captured != null) throw captured;
        }

        [Fact]
        public void AllWindows_ShouldInstantiate_OnStaThread()
        {
            RunOnSta(() =>
            {
                var api = new Mock<ILicenseApiClient>();
                api.Setup(x => x.ValidateLicenseAsync(It.IsAny<string>(), It.IsAny<string>()))
                   .ReturnsAsync(new LicenseValidationResult { IsValid = true });
                api.Setup(x => x.RegisterInstallationAsync(It.IsAny<string>(), It.IsAny<string>()))
                   .ReturnsAsync(true);
                var licensing = new LicensingService(api.Object);

                void Try(string name, Func<object> ctor)
                {
                    try { Assert.NotNull(ctor()); }
                    catch (Exception ex) { throw new Exception($"Falha ao instanciar {name}: {ex.Message}", ex); }
                }

                var vmActivation = new ActivationMethodViewModel(licensing, new HttpClient());
                Try("ActivationMethodWindow", () => new ActivationMethodWindow(vmActivation));

                Try("LicenseWindow", () => new LicenseWindow(licensing));
                var sc = new ServiceCollection();
                sc.AddSingleton<DashboardViewModel>();
                sc.AddSingleton<CoinCraft.App.ViewModels.TransactionsViewModel>();
                sc.AddSingleton<CoinCraft.App.ViewModels.AccountsViewModel>();
                sc.AddSingleton<CoinCraft.App.ViewModels.CategoriesViewModel>();
                sc.AddSingleton<CoinCraft.App.ViewModels.RecurringViewModel>();
                sc.AddSingleton<CoinCraft.App.ViewModels.ImportViewModel>();
                sc.AddSingleton<CoinCraft.App.ViewModels.SettingsViewModel>(sp => new CoinCraft.App.ViewModels.SettingsViewModel(new CoinCraft.Services.ConfigService(), new CoinCraft.Services.LogService()));
                sc.AddSingleton<CoinCraft.Services.LogService>();
                var provider = sc.BuildServiceProvider();
                var prop = typeof(CoinCraft.App.App).GetProperty("Services", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                prop!.SetValue(null, provider);
                Try("DashboardWindow", () => new DashboardWindow());
                Try("TransactionsWindow", () => new TransactionsWindow());
                Try("AccountsWindow", () => new AccountsWindow());
                Try("CategoriesWindow", () => new CategoriesWindow());
                Try("RecurringWindow", () => new RecurringWindow());
                Try("ImportWindow", () => new ImportWindow());

                var settingsVm = new CoinCraft.App.ViewModels.SettingsViewModel(new CoinCraft.Services.ConfigService(), new CoinCraft.Services.LogService());
                Try("SettingsWindow", () => new SettingsWindow(settingsVm));

                var account = new CoinCraft.Domain.Account { Nome = "Teste", Tipo = CoinCraft.Domain.AccountType.ContaCorrente, SaldoInicial = 0m, Ativa = true };
                Try("AccountEditWindow", () => new AccountEditWindow(account));

                var cat = new CoinCraft.Domain.Category { Nome = "Categoria" };
                Try("CategoryEditWindow", () => new CategoryEditWindow(cat));

                var txVm = new CoinCraft.App.ViewModels.TransactionsViewModel(new CoinCraft.Services.LogService());
                Try("TransactionEditWindow", () => new TransactionEditWindow(txVm, null));

                var recVm = new CoinCraft.App.ViewModels.RecurringViewModel();
                Try("RecurringEditWindow", () => new RecurringEditWindow(recVm, null));
            });
        }
    }
}
