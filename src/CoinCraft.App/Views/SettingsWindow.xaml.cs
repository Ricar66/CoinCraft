using System.Windows;
using System.Windows.Media;
using CoinCraft.App.ViewModels;

namespace CoinCraft.App.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _vm;
    public SettingsWindow(SettingsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = _vm;
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        _vm.Save();
        // Aplicar tema imediatamente após salvar
        var dark = string.Equals(_vm.Tema, "escuro", System.StringComparison.OrdinalIgnoreCase);
        var bg = dark ? Color.FromRgb(30, 30, 30) : Colors.White;
        var fg = dark ? Colors.White : Colors.Black;
        Application.Current.Resources["AppBackgroundBrush"] = new SolidColorBrush(bg);
        Application.Current.Resources["AppForegroundBrush"] = new SolidColorBrush(fg);
        MessageBox.Show("Configurações salvas.", "Configurações", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}