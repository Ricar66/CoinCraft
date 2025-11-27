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
            var vm = new LicenseViewModel(licensingService);
            DataContext = vm;
        }
    }
}
