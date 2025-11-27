using System.Windows;
using CoinCraft.Services.Licensing;
using CoinCraft.App.ViewModels;

namespace CoinCraft.App.Views
{
    public partial class LicenseWindow : Window
    {
        public LicenseWindow(ILicensingService licensingService)
        {
            InitializeComponent();
            this.DataContext = new LicenseViewModel(licensingService);
        }
    }
}
