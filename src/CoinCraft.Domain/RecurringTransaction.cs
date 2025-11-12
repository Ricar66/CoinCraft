using System;

namespace CoinCraft.Domain;

public enum RecurrenceFrequency
{
    Diario = 1,
    Semanal = 2,
    Mensal = 3,
    Anual = 4
}

public sealed class RecurringTransaction
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public RecurrenceFrequency Frequencia { get; set; } = RecurrenceFrequency.Mensal;
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime? EndDate { get; set; }
    public int? DiaDaSemana { get; set; } // 0..6 (Dom..Sáb) quando Semanal
    public int? DiaDoMes { get; set; } // 1..31 quando Mensal
    public bool AutoLancamento { get; set; } = false; // true: lança sem pedir
    public DateTime NextRunDate { get; set; } = DateTime.Today; // próxima execução

    // Template da transação
    public TransactionType Tipo { get; set; }
    public decimal Valor { get; set; }
    public int AccountId { get; set; }
    public int? CategoryId { get; set; }
    public string? Descricao { get; set; }
    public int? OpostoAccountId { get; set; }
}