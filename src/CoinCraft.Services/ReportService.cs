using System.Text;
using CoinCraft.Infrastructure;
using CoinCraft.Domain;
using QuestPDF.Fluent;

namespace CoinCraft.Services;

public sealed class ReportService
{
    public string ExportTransactionsCsv(string destinationFolder, DateTime? from = null, DateTime? to = null, int? accountId = null, int? categoryId = null)
    {
        Directory.CreateDirectory(destinationFolder);
        var file = Path.Combine(destinationFolder, $"transactions_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

        using var db = new CoinCraftDbContext();
        var query = db.Transactions.AsQueryable();
        if (from.HasValue) query = query.Where(t => t.Data >= from.Value);
        if (to.HasValue) query = query.Where(t => t.Data <= to.Value);
        if (accountId.HasValue) query = query.Where(t => t.AccountId == accountId.Value);
        if (categoryId.HasValue) query = query.Where(t => t.CategoryId == categoryId.Value);

        var accounts = db.Accounts.ToDictionary(a => a.Id, a => a.Nome);
        var categories = db.Categories.ToDictionary(c => c.Id, c => c.Nome);

        var sb = new StringBuilder();
        sb.AppendLine("Data;Tipo;Valor;Conta;Categoria;Descricao;ContaDestino");
        foreach (var t in query.OrderByDescending(t => t.Data))
        {
            var conta = accounts.TryGetValue(t.AccountId, out var an) ? an : $"#{t.AccountId}";
            var categoria = t.CategoryId.HasValue && categories.TryGetValue(t.CategoryId.Value, out var cn) ? cn : string.Empty;
            var destino = t.OpostoAccountId.HasValue && accounts.TryGetValue(t.OpostoAccountId.Value, out var dn) ? dn : string.Empty;
            var descricao = (t.Descricao ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
            sb.AppendLine($"{t.Data:yyyy-MM-dd};{t.Tipo};{t.Valor};{conta};{categoria};{descricao};{destino}");
        }

        // Escrever com BOM para melhor compat com Excel
        var utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        File.WriteAllText(file, sb.ToString(), utf8WithBom);
        return file;
    }

    public string ExportTransactionsPdf(string destinationFolder, DateTime? from = null, DateTime? to = null, int? accountId = null, int? categoryId = null)
    {
        // Implementação usa QuestPDF; pacote referenciado no csproj
        Directory.CreateDirectory(destinationFolder);
        var file = Path.Combine(destinationFolder, $"transactions_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

        using var db = new CoinCraftDbContext();
        var query = db.Transactions.AsQueryable();
        if (from.HasValue) query = query.Where(t => t.Data >= from.Value);
        if (to.HasValue) query = query.Where(t => t.Data <= to.Value);
        if (accountId.HasValue) query = query.Where(t => t.AccountId == accountId.Value);
        if (categoryId.HasValue) query = query.Where(t => t.CategoryId == categoryId.Value);
        var list = query.OrderByDescending(t => t.Data).ToList();

        var accounts = db.Accounts.ToDictionary(a => a.Id, a => a.Nome);
        var categories = db.Categories.ToDictionary(c => c.Id, c => c.Nome);

        var totalReceitas = list.Where(t => t.Tipo == TransactionType.Receita).Sum(t => t.Valor);
        var totalDespesas = list.Where(t => t.Tipo == TransactionType.Despesa).Sum(t => t.Valor);

        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        var doc = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(QuestPDF.Helpers.PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(10));
                page.Header().Text("Relatório de Lançamentos").SemiBold().FontSize(16);
                page.Content().Column(col =>
                {
                    col.Item().Text($"Período: {(from.HasValue ? from.Value.ToString("yyyy-MM-dd") : "-")} até {(to.HasValue ? to.Value.ToString("yyyy-MM-dd") : "-")}");
                    col.Item().Text($"Receitas: {totalReceitas:C} — Despesas: {totalDespesas:C}");
                    col.Item().LineHorizontal(0.5f);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(1); // Data
                            cols.RelativeColumn(1); // Tipo
                            cols.RelativeColumn(1); // Valor
                            cols.RelativeColumn(2); // Conta
                            cols.RelativeColumn(2); // Categoria
                            cols.RelativeColumn(3); // Descrição
                        });
                        table.Header(header =>
                        {
                            header.Cell().Text("Data").SemiBold();
                            header.Cell().Text("Tipo").SemiBold();
                            header.Cell().Text("Valor").SemiBold();
                            header.Cell().Text("Conta").SemiBold();
                            header.Cell().Text("Categoria").SemiBold();
                            header.Cell().Text("Descrição").SemiBold();
                        });
                        foreach (var t in list)
                        {
                            var conta = accounts.TryGetValue(t.AccountId, out var an) ? an : $"#{t.AccountId}";
                            var categoria = t.CategoryId.HasValue && categories.TryGetValue(t.CategoryId.Value, out var cn) ? cn : string.Empty;
                            table.Cell().Text(t.Data.ToString("yyyy-MM-dd"));
                            table.Cell().Text(t.Tipo.ToString());
                            table.Cell().Text(t.Valor.ToString());
                            table.Cell().Text(conta);
                            table.Cell().Text(categoria);
                            table.Cell().Text(t.Descricao ?? string.Empty);
                        }
                    });
                });
                page.Footer().AlignRight().Text($"Gerado em {DateTime.Now:yyyy-MM-dd HH:mm}");
            });
        });

        doc.GeneratePdf(file);
        return file;
    }

    public string ExportSummaryByCategoryCsv(string destinationFolder, DateTime? from = null, DateTime? to = null)
    {
        Directory.CreateDirectory(destinationFolder);
        var file = Path.Combine(destinationFolder, $"summary_categories_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        using var db = new CoinCraftDbContext();
        var query = db.Transactions.AsQueryable();
        if (from.HasValue) query = query.Where(t => t.Data >= from.Value);
        if (to.HasValue) query = query.Where(t => t.Data <= to.Value);
        var categories = db.Categories.ToDictionary(c => c.Id, c => c);
        var despesas = query.Where(t => t.Tipo == TransactionType.Despesa && t.CategoryId.HasValue)
            .GroupBy(t => t.CategoryId!.Value)
            .Select(g => new { CategoryId = g.Key, Total = g.Sum(x => x.Valor) })
            .OrderByDescending(x => x.Total)
            .ToList();
        var sb = new StringBuilder();
        sb.AppendLine("Categoria;Total;CorHex");
        foreach (var d in despesas)
        {
            var cat = categories.TryGetValue(d.CategoryId, out var c) ? c : new Category { Nome = $"Cat {d.CategoryId}" };
            sb.AppendLine($"{cat.Nome};{d.Total};{(cat.CorHex ?? string.Empty)}");
        }
        File.WriteAllText(file, sb.ToString(), new UTF8Encoding(true));
        return file;
    }

    public string ExportSummaryByAccountCsv(string destinationFolder, DateTime? from = null, DateTime? to = null)
    {
        Directory.CreateDirectory(destinationFolder);
        var file = Path.Combine(destinationFolder, $"summary_accounts_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        using var db = new CoinCraftDbContext();
        var query = db.Transactions.AsQueryable();
        if (from.HasValue) query = query.Where(t => t.Data >= from.Value);
        if (to.HasValue) query = query.Where(t => t.Data <= to.Value);
        var accounts = db.Accounts.ToDictionary(a => a.Id, a => a);
        var receitas = query.Where(t => t.Tipo == TransactionType.Receita)
            .GroupBy(t => t.AccountId).Select(g => new { AccountId = g.Key, Total = g.Sum(x => x.Valor) }).ToList();
        var despesas = query.Where(t => t.Tipo == TransactionType.Despesa)
            .GroupBy(t => t.AccountId).Select(g => new { AccountId = g.Key, Total = g.Sum(x => x.Valor) }).ToList();
        var sb = new StringBuilder();
        sb.AppendLine("Conta;Receitas;Despesas");
        var accIds = receitas.Select(r => r.AccountId).Union(despesas.Select(d => d.AccountId)).Distinct();
        foreach (var id in accIds)
        {
            var conta = accounts.TryGetValue(id, out var a) ? a.Nome : $"Conta {id}";
            var r = receitas.FirstOrDefault(x => x.AccountId == id)?.Total ?? 0m;
            var d = despesas.FirstOrDefault(x => x.AccountId == id)?.Total ?? 0m;
            sb.AppendLine($"{conta};{r};{d}");
        }
        File.WriteAllText(file, sb.ToString(), new UTF8Encoding(true));
        return file;
    }
}
