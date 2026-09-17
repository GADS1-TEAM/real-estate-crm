using System.Text.Json;
using RealEstateCrm.Contracts.Authorization;
using RealEstateCrm.Contracts.Serialization;

namespace RealEstateCrm.ContractTests.Authorization;

public class AuthorizationDecisionContractTests
{
    [Fact]
    public void Allow_has_no_reason_code()
    {
        var decision = AuthorizationDecision.Allow();

        Assert.True(decision.Allowed);
        Assert.Null(decision.ReasonCode);
    }

    [Fact]
    public void Deny_carries_the_reason_code()
    {
        var decision = AuthorizationDecision.Deny(DenyReasons.PermissionNotGranted);

        Assert.False(decision.Allowed);
        Assert.Equal(DenyReasons.PermissionNotGranted, decision.ReasonCode);
    }

    [Fact]
    public void Allow_round_trips_as_camel_case_json_without_reason_code()
    {
        var json = JsonSerializer.Serialize(AuthorizationDecision.Allow(), RealEstateCrmJsonDefaults.Options);
        using var document = JsonDocument.Parse(json);

        Assert.True(document.RootElement.GetProperty("allowed").GetBoolean());
        Assert.False(document.RootElement.TryGetProperty("reasonCode", out _));

        var roundTripped = JsonSerializer.Deserialize<AuthorizationDecision>(json, RealEstateCrmJsonDefaults.Options);
        Assert.Equal(AuthorizationDecision.Allow(), roundTripped);
    }

    [Theory]
    [InlineData(DenyReasons.PermissionNotGranted)]
    [InlineData(DenyReasons.UserInactive)]
    [InlineData(DenyReasons.UserPending)]
    public void Deny_round_trips_with_the_declared_reason_code(string reasonCode)
    {
        var original = AuthorizationDecision.Deny(reasonCode);

        var json = JsonSerializer.Serialize(original, RealEstateCrmJsonDefaults.Options);
        var roundTripped = JsonSerializer.Deserialize<AuthorizationDecision>(json, RealEstateCrmJsonDefaults.Options);

        Assert.Equal(original, roundTripped);
    }
}
