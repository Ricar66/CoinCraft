using System.Windows;
using CoinCraft.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CoinCraft.App.Views;

public partial class ManualWindow : Window
{
    private readonly ManualViewModel _vm = App.Services!.GetRequiredService<ManualViewModel>();

    public ManualWindow()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
