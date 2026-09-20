using RealEstateCrm.Contracts.Financial;
using SupplyService.Domain.Aggregates;

namespace SupplyService.Application;

public static class MoneyMapper
{
    public static MoneyV1? ToContract(this Money? money) =>
        money == null ? null : new MoneyV1(money.Amount, money.Currency);

    public static Money? ToDomain(this MoneyV1? money) =>
        money == null ? null : new Money(money.Amount, money.Currency);
}
