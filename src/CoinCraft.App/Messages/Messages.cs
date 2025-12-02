using CommunityToolkit.Mvvm.Messaging.Messages;

namespace CoinCraft.App.Messages;

/// <summary>
/// Mensagem enviada quando transações são adicionadas, alteradas ou removidas.
/// </summary>
public class TransactionsChangedMessage : ValueChangedMessage<string>
{
    public TransactionsChangedMessage(string changeType) : base(changeType)
    {
    }
}

/// <summary>
/// Mensagem enviada quando contas são adicionadas, alteradas ou removidas.
/// </summary>
public class AccountsChangedMessage : ValueChangedMessage<string>
{
    public AccountsChangedMessage(string changeType) : base(changeType)
    {
    }
}

/// <summary>
/// Mensagem enviada quando categorias são adicionadas, alteradas ou removidas.
/// </summary>
public class CategoriesChangedMessage : ValueChangedMessage<string>
{
    public CategoriesChangedMessage(string changeType) : base(changeType)
    {
    }
}

/// <summary>
/// Mensagem enviada quando metas (goals) são alteradas.
/// </summary>
public class GoalsChangedMessage : ValueChangedMessage<string>
{
    public GoalsChangedMessage(string changeType) : base(changeType)
    {
    }
}

/// <summary>
/// Mensagem enviada quando transações recorrentes são alteradas.
/// </summary>
public class RecurringTransactionsChangedMessage : ValueChangedMessage<string>
{
    public RecurringTransactionsChangedMessage(string changeType) : base(changeType)
    {
    }
}
