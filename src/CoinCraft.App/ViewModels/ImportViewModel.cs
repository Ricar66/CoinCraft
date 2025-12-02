using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CoinCraft.Infrastructure;
using CoinCraft.Domain;
using CoinCraft.Services;
using CommunityToolkit.Mvvm.Messaging;
using CoinCraft.App.Messages;

namespace CoinCraft.App.ViewModels;

public sealed class ImportRow
{
    public DateTime? Data { get; set; }
    public string? Descricao { get; set; }
    public decimal? Valor { get; set; }
    public TransactionType? Tipo { get; set; }
    public string? AccountName { get; set; }
    public string? CategoryName { get; set; }
}

public sealed class ImportViewModel : ObservableObject
{
    private readonly Func<CoinCraftDbContext> _contextFactory;
    private readonly ImportService _service;

    public ImportViewModel(Func<CoinCraftDbContext>? contextFactory = null)
    {
        _contextFactory = contextFactory ?? (() => new CoinCraftDbContext());
        _service = new ImportService(_contextFactory);

        // Ouvintes para atualização de lookups
        WeakReferenceMessenger.Default.Register<AccountsChangedMessage>(this, (r, m) =>
        {
            DispatcherHelper.InvokeAsync(async () => await LoadLookupsAsync());
        });

        WeakReferenceMessenger.Default.Register<CategoriesChangedMessage>(this, (r, m) =>
        {
            DispatcherHelper.InvokeAsync(async () => await LoadLookupsAsync());
        });
    }

    public ObservableCollection<ImportRow> Items { get; } = new();
    public string? StatusMessage { get; private set; }
    public string? FilePath { get; private set; }
    public System.Collections.Generic.List<string> HeaderColumns { get; private set; } = new();

    public System.Collections.Generic.List<Account> Accounts { get; private set; } = new();
    public System.Collections.Generic.List<Category> Categories { get; private set; } = new();

    public async Task LoadLookupsAsync()
    {
        using var db = _contextFactory();
        Accounts = await Task.Run(() => db.Accounts.OrderBy(a => a.Nome).ToList());
        Categories = await Task.Run(() => db.Categories.OrderBy(c => c.Nome).ToList());
    }

    public void LoadFile(string path)
    {
        FilePath = path;
        Items.Clear();

        if (path.EndsWith(".ofx", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".qfx", StringComparison.OrdinalIgnoreCase))
        {
            var importedOfx = _service.ParseOfx(path);
            foreach (var it in importedOfx)
            {
                Items.Add(new ImportRow
                {
                    Data = it.Data,
                    Descricao = it.Descricao,
                    Valor = it.Valor,
                    Tipo = it.Tipo
                });
            }
            HeaderColumns = new();
            StatusMessage = $"Carregado {Items.Count} linha(s) de OFX/QFX";
            OnPropertyChanged(nameof(StatusMessage));
            return;
        }

        HeaderColumns = _service.ReadCsvHeader(path);
        var imported = _service.ParseCsv(path);
        foreach (var it in imported)
        {
            Items.Add(new ImportRow
            {
                Data = it.Data,
                Descricao = it.Descricao,
                Valor = it.Valor,
                Tipo = it.Tipo,
                AccountName = it.AccountName,
                CategoryName = it.CategoryName
            });
        }
        StatusMessage = $"Carregado {Items.Count} linha(s) do arquivo";
        OnPropertyChanged(nameof(StatusMessage));
    }

    public void LoadFileWithMapping(string path, System.Collections.Generic.Dictionary<string,int> map)
    {
        FilePath = path;
        Items.Clear();
        var imported = _service.ParseCsvWithMap(path, map);
        foreach (var it in imported)
        {
            Items.Add(new ImportRow
            {
                Data = it.Data,
                Descricao = it.Descricao,
                Valor = it.Valor,
                Tipo = it.Tipo,
                AccountName = it.AccountName,
                CategoryName = it.CategoryName
            });
        }
        StatusMessage = $"Carregado {Items.Count} linha(s) com mapeamento aplicado";
        OnPropertyChanged(nameof(StatusMessage));
    }

    public int Import(int? defaultAccountId, int? defaultCategoryId, TransactionType? defaultTipo)
    {
        var list = Items.Select(i => new ImportService.ImportedTransaction
        {
            Data = i.Data,
            Descricao = i.Descricao,
            Valor = i.Valor,
            Tipo = i.Tipo,
            AccountName = i.AccountName,
            CategoryName = i.CategoryName
        }).ToList();

        var saved = _service.ApplyImport(list, defaultAccountId, defaultCategoryId, defaultTipo);
        StatusMessage = $"Importados {saved} lançamento(s)";
        OnPropertyChanged(nameof(StatusMessage));
        if (saved > 0)
        {
            WeakReferenceMessenger.Default.Send(new TransactionsChangedMessage("Import"));
        }
        return saved;
    }
}