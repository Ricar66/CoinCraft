using System;
using System.Linq;
using CoinCraft.Infrastructure;
using CoinCraft.Domain;

namespace CoinCraft.Services;

public sealed class RecurringService
{
    private readonly LogService _log = new();

    public int ProcessDueRecurringTransactions(bool createSuggestionsOnly = false)
    {
        try
        {
            using var db = new CoinCraftDbContext();
            var today = DateTime.Today;
            var due = db.RecurringTransactions
                .Where(r => r.NextRunDate <= today && (r.EndDate == null || r.EndDate >= today))
                .ToList();

            int created = 0;
            foreach (var r in due)
            {
                if (!createSuggestionsOnly && r.AutoLancamento)
                {
                    var tx = new Transaction
                    {
                        Data = r.NextRunDate,
                        Tipo = r.Tipo,
                        Valor = r.Valor,
                        AccountId = r.AccountId,
                        CategoryId = r.CategoryId,
                        Descricao = r.Descricao,
                        OpostoAccountId = r.OpostoAccountId
                    };
                    db.Transactions.Add(tx);
                    created++;
                }
                // Atualiza próxima execução
                r.NextRunDate = CalculateNextRunDate(r);
            }

            db.SaveChanges();
            _log.Info($"Recorrentes processados: {due.Count}, lançamentos criados: {created}");
            return created;
        }
        catch (Exception ex)
        {
            _log.Error($"Falha ao processar recorrentes: {ex.Message}");
            return 0;
        }
    }

    public static DateTime CalculateNextRunDate(RecurringTransaction r)
    {
        var d = r.NextRunDate;
        return r.Frequencia switch
        {
            RecurrenceFrequency.Diario => d.AddDays(1),
            RecurrenceFrequency.Semanal => d.AddDays(7),
            RecurrenceFrequency.Mensal => AddMonthsSafe(d, 1, r.DiaDoMes),
            RecurrenceFrequency.Anual => AddYearsSafe(d, 1),
            _ => d.AddMonths(1)
        };
    }

    private static DateTime AddMonthsSafe(DateTime date, int months, int? dayOfMonth)
    {
        var target = new DateTime(date.Year, date.Month, Math.Min(date.Day, DateTime.DaysInMonth(date.Year, date.Month)));
        var next = target.AddMonths(months);
        if (dayOfMonth.HasValue)
        {
            var dom = Math.Clamp(dayOfMonth.Value, 1, DateTime.DaysInMonth(next.Year, next.Month));
            return new DateTime(next.Year, next.Month, dom);
        }
        return next;
    }

    private static DateTime AddYearsSafe(DateTime date, int years)
    {
        var dom = Math.Min(date.Day, DateTime.DaysInMonth(date.Year + years, date.Month));
        return new DateTime(date.Year + years, date.Month, dom);
    }
}