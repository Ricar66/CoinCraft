using System.Net.Http;
using System.Windows;
using CoinCraft.App.ViewModels;
using CoinCraft.Services.Licensing;

namespace CoinCraft.App.Views
{
    public partial class LicenseWindow : Window
    {
        public LicenseWindow(ILicensingService licensingService, ILicenseApiClient apiClient)
        {
            InitializeComponent();
            var vm = new LicenseViewModel(licensingService, apiClient);
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.StatusText) && vm.StatusText.Contains("Licença ativa"))
                {
                    DialogResult = true;
                    Close();
                }
            };
            DataContext = vm;
        }
    }
}