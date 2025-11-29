using System;
using System.IO;
using System.Windows;

namespace CoinCraft.Admin
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            try
            {
                var app = new App();
                app.InitializeComponent();
                app.Run(new MainWindow());
            }
            catch (Exception ex)
            {
                var msg = $"Falha ao iniciar CoinCraft.Admin:\n\n{ex}";
                try
                {
                    var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    File.WriteAllText(Path.Combine(desktop, "CoinCraft.Admin_error.txt"), msg);
                }
                catch { }
                MessageBox.Show(msg, "CoinCraft Admin - Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
