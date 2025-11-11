namespace CoinCraft.Domain;

public sealed class UserSetting
{
    public int Id { get; set; }
    public string Chave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
}
