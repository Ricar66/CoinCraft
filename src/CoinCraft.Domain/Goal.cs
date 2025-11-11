namespace CoinCraft.Domain;

public sealed class Goal
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public decimal LimiteMensal { get; set; }
    public int Ano { get; set; }
    public int Mes { get; set; }
}
