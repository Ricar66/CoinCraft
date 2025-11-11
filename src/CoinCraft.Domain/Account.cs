namespace CoinCraft.Domain;

public enum AccountType { Carteira, ContaCorrente, CartaoCredito, Poupanca }

public sealed class Account
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public AccountType Tipo { get; set; } = AccountType.ContaCorrente;
    public decimal SaldoInicial { get; set; }
    public bool Ativa { get; set; } = true;
    public string? CorHex { get; set; }
    public string? Icone { get; set; }
}
