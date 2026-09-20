namespace RealEstateCrm.Contracts.Financial;

public sealed record MoneyV1(decimal Amount, string Currency);

public static class Currencies
{
    public const string ARS = "ARS";
    public const string USD = "USD";
    public static readonly IReadOnlyCollection<string> All = new[] { ARS, USD };
    public static bool IsValid(string currency) => All.Contains(currency, StringComparer.Ordinal);
}
