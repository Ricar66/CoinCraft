using System.Windows;
using CoinCraft.App.ViewModels;

namespace CoinCraft.App.Views
{
    public partial class ActivationMethodWindow : Window
    {
        public ActivationMethodWindow(ActivationMethodViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
