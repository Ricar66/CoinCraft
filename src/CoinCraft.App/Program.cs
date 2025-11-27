using System;
using System.IO;
using System.Windows;

namespace CoinCraft.App
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            try
            {
                var app = new CoinCraft.App.App();
                app.InitializeComponent();
                app.Run();
            }
            catch (Exception ex)
            {
                var errorMessage = $"Erro Fatal no CoinCraft:\n\n{ex.Message}\n\nDetalhes:\n{ex.StackTrace}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\n\nInner Exception:\n{ex.InnerException.Message}\n\n{ex.InnerException.StackTrace}";
                }
                try
                {
                    var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    File.WriteAllText(Path.Combine(desktopPath, "CoinCraft_Erro_Fatal.txt"), errorMessage);
                }
                catch { }
                MessageBox.Show(errorMessage, "CoinCraft - Falha Crítica", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
