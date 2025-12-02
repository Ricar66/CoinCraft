using System;
using System.Threading.Tasks;
using System.Windows;

namespace CoinCraft.Infrastructure;

public static class DispatcherHelper
{
    // Permite substituir a implementação em testes
    public static Action<Func<Task>> InvokeAsyncImpl { get; set; } = async (action) => 
    {
        if (Application.Current?.Dispatcher != null)
        {
            await Application.Current.Dispatcher.InvokeAsync(action);
        }
        else
        {
            // Fallback para execução direta (úteis em testes ou fora do contexto UI)
            await action();
        }
    };

    public static void InvokeAsync(Func<Task> action)
    {
        InvokeAsyncImpl(action);
    }
}
