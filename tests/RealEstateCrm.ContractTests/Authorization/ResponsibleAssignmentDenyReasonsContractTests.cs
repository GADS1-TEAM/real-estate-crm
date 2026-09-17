using RealEstateCrm.Contracts.Authorization;

namespace RealEstateCrm.ContractTests.Authorization;

public class ResponsibleAssignmentDenyReasonsContractTests
{
    [Fact]
    public void PermissionNotGranted_shares_the_same_literal_as_DenyReasons()
    {
        Assert.Equal(DenyReasons.PermissionNotGranted, ResponsibleAssignmentDenyReasons.PermissionNotGranted);
    }

    [Theory]
    [InlineData(nameof(ResponsibleAssignmentDenyReasons.ResponsibleUserNotFound), "responsible_user_not_found")]
    [InlineData(nameof(ResponsibleAssignmentDenyReasons.ResponsibleUserInactive), "responsible_user_inactive")]
    public void Declared_reason_has_the_expected_literal_value(string constantName, string expectedValue)
    {
        var field = typeof(ResponsibleAssignmentDenyReasons).GetField(constantName);

        Assert.NotNull(field);
        Assert.Equal(expectedValue, field!.GetRawConstantValue());
    }
}
