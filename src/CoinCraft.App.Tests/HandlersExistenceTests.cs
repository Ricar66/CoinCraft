using System.Reflection;
using Xunit;
using CoinCraft.App.Views;

namespace CoinCraft.App.Tests;

public class HandlersExistenceTests
{
    private static MethodInfo? GetNonPublicInstanceMethod<T>(string name)
        => typeof(T).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);

    [Fact]
    public void AccountsWindow_ShouldHaveAddEditDeleteRefreshHandlers()
    {
        Assert.NotNull(GetNonPublicInstanceMethod<AccountsWindow>("OnAddClick"));
        Assert.NotNull(GetNonPublicInstanceMethod<AccountsWindow>("OnEditClick"));
        Assert.NotNull(GetNonPublicInstanceMethod<AccountsWindow>("OnDeleteClick"));
        Assert.NotNull(GetNonPublicInstanceMethod<AccountsWindow>("OnRefreshClick"));
    }

    [Fact]
    public void CategoriesWindow_ShouldHaveAddEditDeleteRefreshHandlers()
    {
        Assert.NotNull(GetNonPublicInstanceMethod<CategoriesWindow>("OnAddClick"));
        Assert.NotNull(GetNonPublicInstanceMethod<CategoriesWindow>("OnEditClick"));
        Assert.NotNull(GetNonPublicInstanceMethod<CategoriesWindow>("OnDeleteClick"));
        Assert.NotNull(GetNonPublicInstanceMethod<CategoriesWindow>("OnRefreshClick"));
    }

    [Fact]
    public void TransactionsWindow_ShouldHaveAddEditDeleteRefreshHandlers()
    {
        Assert.NotNull(GetNonPublicInstanceMethod<TransactionsWindow>("OnAddClick"));
        Assert.NotNull(GetNonPublicInstanceMethod<TransactionsWindow>("OnEditClick"));
        Assert.NotNull(GetNonPublicInstanceMethod<TransactionsWindow>("OnDeleteClick"));
        Assert.NotNull(GetNonPublicInstanceMethod<TransactionsWindow>("OnRefreshClick"));
    }

    [Fact]
    public void EditWindows_ShouldHaveSaveHandlers()
    {
        Assert.NotNull(GetNonPublicInstanceMethod<AccountEditWindow>("OnSaveClick"));
        Assert.NotNull(GetNonPublicInstanceMethod<CategoryEditWindow>("OnSaveClick"));
        Assert.NotNull(GetNonPublicInstanceMethod<TransactionEditWindow>("OnSaveClick"));
    }
}