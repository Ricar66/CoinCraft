namespace CoinCraft.Domain;

public enum TransactionType { Receita, Despesa, Transferencia }

public sealed class Transaction
{
    public int Id { get; set; }
    public DateTime Data { get; set; } = DateTime.Today;
    public TransactionType Tipo { get; set; }
    public decimal Valor { get; set; }
    public int AccountId { get; set; }
    public int? CategoryId { get; set; }
    public string? Descricao { get; set; }
    public int? OpostoAccountId { get; set; } // Para Transferência (destino)
    public string? AttachmentPath { get; set; } // Caminho do comprovante (imagem/PDF)
}
