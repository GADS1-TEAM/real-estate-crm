using System.Text.Json;
using RealEstateCrm.Contracts.Financial;
using RealEstateCrm.Contracts.Serialization;

namespace RealEstateCrm.ContractTests.Financial;

public class MoneyV1ContractTests
{
    [Fact]
    public void Serialization_Uses_CamelCase()
    {
        var money = new MoneyV1(1234.56m, Currencies.USD);
        var json = JsonSerializer.Serialize(money, RealEstateCrmJsonDefaults.Options);
        
        Assert.Contains("\"amount\":1234.56", json);
        Assert.Contains("\"currency\":\"USD\"", json);
    }
}
