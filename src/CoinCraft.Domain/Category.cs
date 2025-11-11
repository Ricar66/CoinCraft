namespace CoinCraft.Domain;

public sealed class Category
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? CorHex { get; set; }
    public string? Icone { get; set; }
    public int? ParentCategoryId { get; set; }
    public decimal? LimiteMensal { get; set; }
}
