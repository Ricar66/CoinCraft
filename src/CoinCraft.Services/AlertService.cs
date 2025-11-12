using System;
using System.Collections.Generic;
using System.Linq;
using CoinCraft.Infrastructure;
using CoinCraft.Domain;

namespace CoinCraft.Services;

public sealed class AlertService
{
    public sealed class Alert
    {
        public string Kind { get; set; } = string.Empty; // e.g., "Recurring", "Budget"
        public string Message { get; set; } = string.Empty;
    }

    // Retorna alertas: recorrentes próximos e orçamento próximo/estourado
    public List<Alert> GetAlerts(decimal thresholdPercent = 0.9m, int daysAhead = 3)
    {
        var alerts = new List<Alert>();
        var today = DateTime.Today;
        var ahead = today.AddDays(Math.Max(0, daysAhead));

        using var db = new CoinCraftDbContext();

        // Recorrentes próximos
        var upcoming = db.RecurringTransactions
            .Where(r => r.NextRunDate >= today && r.NextRunDate <= ahead && (r.EndDate == null || r.EndDate >= today))
            .OrderBy(r => r.NextRunDate)
            .ToList();
        foreach (var r in upcoming)
        {
            var days = (r.NextRunDate - today).Days;
            var msg = days == 0
                ? $"Lembrete: '{r.Nome}' vence hoje"
                : $"Lembrete: '{r.Nome}' vence em {days} dia(s)";
            alerts.Add(new Alert { Kind = "Recurring", Message = msg });
        }

        // Orçamento do mês (Goals + LimiteMensal de Category)
        var firstDay = new DateTime(today.Year, today.Month, 1);
        var nextMonth = firstDay.AddMonths(1);

        var goals = db.Goals.Where(g => g.Ano == firstDay.Year && g.Mes == firstDay.Month)
            .ToList();
        var categories = db.Categories.ToDictionary(c => c.Id, c => c);

        var limits = new Dictionary<int, decimal>();
        foreach (var g in goals)
        {
            limits[g.CategoryId] = g.LimiteMensal;
        }
        // Complementa com LimiteMensal na Category quando não houver Goal explícito
        foreach (var kvp in categories)
        {
            var cat = kvp.Value;
            if (!limits.ContainsKey(cat.Id) && cat.LimiteMensal.HasValue && cat.LimiteMensal.Value > 0)
            {
                limits[cat.Id] = cat.LimiteMensal.Value;
            }
        }

        if (limits.Count > 0)
        {
            var despesasMes = db.Transactions
                .Where(t => t.Tipo == TransactionType.Despesa && t.CategoryId.HasValue && t.Data >= firstDay && t.Data < nextMonth)
                .GroupBy(t => t.CategoryId!.Value)
                .Select(g => new { CategoryId = g.Key, Spent = g.Sum(x => x.Valor) })
                .ToList();

            var spentMap = despesasMes.ToDictionary(x => x.CategoryId, x => x.Spent);
            foreach (var (catId, limit) in limits)
            {
                var spent = spentMap.TryGetValue(catId, out var s) ? s : 0m;
                var name = categories.TryGetValue(catId, out var c) ? c.Nome : $"Categoria {catId}";
                if (spent >= limit)
                {
                    alerts.Add(new Alert
                    {
                        Kind = "Budget",
                        Message = $"Alerta: Orçamento de '{name}' foi ultrapassado (R$ {spent} de R$ {limit})."
                    });
                }
                else if (limit > 0 && spent >= limit * thresholdPercent)
                {
                    var pct = (int)Math.Round((spent / limit) * 100);
                    alerts.Add(new Alert
                    {
                        Kind = "Budget",
                        Message = $"Alerta: Você já gastou {pct}% do orçamento de '{name}'."
                    });
                }
            }
        }

        return alerts;
    }
}