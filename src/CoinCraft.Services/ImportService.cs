using System.Globalization;
using System.Text;
using CoinCraft.Infrastructure;
using CoinCraft.Domain;

namespace CoinCraft.Services;

public sealed class ImportService
{
    private readonly Func<CoinCraftDbContext> _contextFactory;

    public ImportService(Func<CoinCraftDbContext>? contextFactory = null)
    {
        _contextFactory = contextFactory ?? (() => new CoinCraftDbContext());
    }

    public sealed class ImportedTransaction
    {
        public DateTime? Data { get; set; }
        public string? Descricao { get; set; }
        public decimal? Valor { get; set; }
        public TransactionType? Tipo { get; set; }
        public string? AccountName { get; set; }
        public string? CategoryName { get; set; }
    }

    public sealed class ParseOptions
    {
        public char Delimiter { get; set; } = ';';
        public CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;
    }

    // Parse CSV expecting header names: Data, Descricao, Valor, Tipo, Conta, Categoria
    public List<ImportedTransaction> ParseCsv(string filePath, ParseOptions? options = null)
    {
        options ??= new ParseOptions();
        var lines = File.ReadAllLines(filePath, Encoding.UTF8)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();
        if (lines.Count == 0) return new List<ImportedTransaction>();

        var header = SplitCsvLine(lines[0], options.Delimiter);
        var map = BuildHeaderMap(header);
        var list = new List<ImportedTransaction>();
        for (int i = 1; i < lines.Count; i++)
        {
            var cols = SplitCsvLine(lines[i], options.Delimiter);
            var item = new ImportedTransaction
            {
                Data = TryParseDate(Get(cols, map, "data"), options.Culture),
                Descricao = Get(cols, map, "descricao"),
                Valor = TryParseDecimal(Get(cols, map, "valor"), options.Culture),
                Tipo = TryParseTipo(Get(cols, map, "tipo")),
                AccountName = Get(cols, map, "conta"),
                CategoryName = Get(cols, map, "categoria")
            };
            list.Add(item);
        }
        return list;
    }

    // Parse CSV utilizando mapeamento manual: key -> índice da coluna
    // keys válidas: data, descricao, valor, tipo, conta, categoria
    public List<ImportedTransaction> ParseCsvWithMap(string filePath, Dictionary<string,int> map, ParseOptions? options = null)
    {
        options ??= new ParseOptions();
        var lines = File.ReadAllLines(filePath, Encoding.UTF8)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();
        if (lines.Count <= 1) return new List<ImportedTransaction>();

        var list = new List<ImportedTransaction>();
        for (int i = 1; i < lines.Count; i++)
        {
            var cols = SplitCsvLine(lines[i], options.Delimiter);
            string? get(string key)
            {
                if (map.TryGetValue(key, out var idx) && idx >= 0 && idx < cols.Count)
                    return cols[idx];
                return null;
            }
            var item = new ImportedTransaction
            {
                Data = TryParseDate(get("data"), options.Culture),
                Descricao = get("descricao"),
                Valor = TryParseDecimal(get("valor"), options.Culture),
                Tipo = TryParseTipo(get("tipo")),
                AccountName = get("conta"),
                CategoryName = get("categoria")
            };
            list.Add(item);
        }
        return list;
    }

    public List<string> ReadCsvHeader(string filePath, char delimiter = ';')
    {
        using var sr = new StreamReader(filePath, Encoding.UTF8, true);
        var first = sr.ReadLine();
        if (string.IsNullOrEmpty(first)) return new List<string>();
        return SplitCsvLine(first!, delimiter);
    }

    // OFX/QFX parser básico (STMTTRN): Data, Valor, Nome
    public List<ImportedTransaction> ParseOfx(string filePath)
    {
        var text = File.ReadAllText(filePath, Encoding.UTF8);
        var items = new List<ImportedTransaction>();
        int pos = 0;
        while (true)
        {
            var start = text.IndexOf("<STMTTRN>", pos, StringComparison.OrdinalIgnoreCase);
            if (start < 0) break;
            var end = text.IndexOf("</STMTTRN>", start, StringComparison.OrdinalIgnoreCase);
            if (end < 0) end = text.Length;
            var block = text.Substring(start, end - start);

            var dt = ExtractTag(block, "DTPOSTED") ?? ExtractTag(block, "DTUSER");
            var amt = ExtractTag(block, "TRNAMT");
            var name = ExtractTag(block, "NAME") ?? ExtractTag(block, "MEMO");
            var trnType = ExtractTag(block, "TRNTYPE");

            var date = TryParseOfxDate(dt);
            var valor = TryParseDecimal(amt, CultureInfo.InvariantCulture);
            TransactionType? tipo = null;
            if (!string.IsNullOrEmpty(trnType))
            {
                var t = trnType.Trim().ToUpperInvariant();
                tipo = t switch
                {
                    "CREDIT" => TransactionType.Receita,
                    "DEBIT" => TransactionType.Despesa,
                    _ => null
                };
            }
            if (tipo is null && valor is not null)
            {
                tipo = valor.Value >= 0 ? TransactionType.Receita : TransactionType.Despesa;
                if (valor.Value < 0) valor = Math.Abs(valor.Value);
            }

            items.Add(new ImportedTransaction
            {
                Data = date,
                Valor = valor,
                Descricao = name,
                Tipo = tipo
            });
            pos = end + 1;
        }
        return items;
    }

    public int ApplyImport(List<ImportedTransaction> items, int? defaultAccountId, int? defaultCategoryId, TransactionType? defaultTipo)
    {
        using var db = _contextFactory();

        var accounts = db.Accounts.ToList();
        var categories = db.Categories.ToList();
        int saved = 0;

        foreach (var it in items)
        {
            if (it.Data is null || it.Valor is null)
                continue; // mínimos necessários

            var tipo = it.Tipo ?? defaultTipo ?? TransactionType.Despesa;

            var accId = MatchByName(accounts.Select(a => (a.Id, a.Nome)), it.AccountName) ?? defaultAccountId;
            var catId = MatchByName(categories.Select(c => (c.Id, c.Nome)), it.CategoryName) ?? defaultCategoryId;

            if (accId is null)
                continue; // sem conta não dá para lançar

            db.Transactions.Add(new Transaction
            {
                Data = it.Data!.Value,
                Valor = it.Valor!.Value,
                Tipo = tipo,
                AccountId = accId.Value,
                CategoryId = catId,
                Descricao = it.Descricao
            });
            saved++;
        }
        db.SaveChanges();
        return saved;
    }

    private static int? MatchByName(IEnumerable<(int Id, string Nome)> items, string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var norm = Normalize(name);
        foreach (var (Id, Nome) in items)
        {
            if (Normalize(Nome) == norm) return Id;
        }
        return null;
    }

    private static string Normalize(string s)
    {
        return new string(s.ToLowerInvariant().Where(ch => char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch)).ToArray()).Trim();
    }

    private static string? Get(List<string> cols, Dictionary<string, int> map, string key)
    {
        if (map.TryGetValue(key, out var idx) && idx >= 0 && idx < cols.Count)
            return cols[idx];
        return null;
    }

    private static Dictionary<string, int> BuildHeaderMap(List<string> header)
    {
        var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < header.Count; i++)
        {
            var h = header[i].Trim();
            var key = h.ToLowerInvariant();
            key = key switch
            {
                "data" or "date" or "transaction_date" => "data",
                "descricao" or "description" or "memo" => "descricao",
                "valor" or "amount" or "value" => "valor",
                "tipo" or "type" => "tipo",
                "conta" or "account" => "conta",
                "categoria" or "category" => "categoria",
                _ => key
            };
            if (!dict.ContainsKey(key)) dict[key] = i;
        }
        return dict;
    }

    private static DateTime? TryParseDate(string? s, CultureInfo culture)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        // try ISO first
        if (DateTime.TryParse(s, culture, DateTimeStyles.AssumeLocal, out var dt))
            return dt.Date;
        return null;
    }

    private static decimal? TryParseDecimal(string? s, CultureInfo culture)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (decimal.TryParse(s, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, culture, out var v))
            return v;
        return null;
    }

    private static TransactionType? TryParseTipo(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var t = s.Trim().ToLowerInvariant();
        return t switch
        {
            "receita" or "income" => TransactionType.Receita,
            "despesa" or "expense" => TransactionType.Despesa,
            "transferencia" or "transfer" => TransactionType.Transferencia,
            _ => null
        };
    }

    // Basic CSV splitter with quotes support
    private static List<string> SplitCsvLine(string line, char delimiter)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++; // skip escaped quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == delimiter && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }
        result.Add(sb.ToString());
        return result;
    }

    private static string? ExtractTag(string text, string tag)
    {
        // Supports both SGML OFX (<TAG>value) and XML (<TAG>value</TAG>)
        var open = "<" + tag + ">";
        var idx = text.IndexOf(open, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        idx += open.Length;
        int endIdx = text.IndexOf('<', idx);
        if (endIdx < 0) endIdx = text.Length;
        var value = text.Substring(idx, endIdx - idx).Trim();
        if (string.IsNullOrEmpty(value))
        {
            // Try XML close tag pattern
            var close = "</" + tag + ">";
            endIdx = text.IndexOf(close, idx, StringComparison.OrdinalIgnoreCase);
            if (endIdx > idx)
            {
                value = text.Substring(idx, endIdx - idx).Trim();
            }
        }
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static DateTime? TryParseOfxDate(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        // OFX dates like 20240115, 20240115T123000, 20240115[+/-tz]
        s = s.Trim();
        // Strip timezone info if present
        int tz = s.IndexOf('[');
        if (tz > 0) s = s.Substring(0, tz);
        s = s.Replace("T", "");

        string[] formats = { "yyyyMMdd", "yyyyMMddHHmmss", "yyyyMMddHHmm" };
        if (DateTime.TryParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
            return dt.Date;
        return null;
    }
}