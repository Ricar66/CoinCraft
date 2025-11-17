using System.Net.Http;
using System.Windows;
using System.Windows.Input;
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

        public void OnMinimizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }


        public void OnToggleMaximizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

        public void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

        public void OnHeaderMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }
    }
}