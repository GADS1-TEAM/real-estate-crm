using RealEstateCrm.Contracts.Catalogs;

namespace RealEstateCrm.ContractTests.Catalogs;

/// <summary>
/// Congela los literales de <see cref="CatalogTypes"/>/<see cref="PipelineKinds"/>/
/// <see cref="SemanticStates"/>: son el contrato que V2-CAT-001 publica y que V2-PTY-001 (y
/// luego pipe/demand/property-service) van a consumir vía <c>ICatalogReaderPort</c> (D7).
/// </summary>
public class CatalogTypesContractTests
{
    [Theory]
    [InlineData(nameof(CatalogTypes.CommercialStage), "CommercialStage")]
    [InlineData(nameof(CatalogTypes.ActivityType), "ActivityType")]
    [InlineData(nameof(CatalogTypes.CommercialOrigin), "CommercialOrigin")]
    [InlineData(nameof(CatalogTypes.LossReason), "LossReason")]
    [InlineData(nameof(CatalogTypes.OperationType), "OperationType")]
    [InlineData(nameof(CatalogTypes.PropertyType), "PropertyType")]
    public void Declared_catalogType_has_the_expected_literal_value(string constantName, string expectedValue)
    {
        var field = typeof(CatalogTypes).GetField(constantName);

        Assert.NotNull(field);
        Assert.Equal(expectedValue, field!.GetRawConstantValue());
    }

    [Fact]
    public void Exposes_exactly_the_six_mandatory_catalog_types()
    {
        Assert.Equal(6, CatalogTypes.All.Count);
        Assert.Equal(CatalogTypes.All.Count, CatalogTypes.All.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void IsValid_rejects_an_unknown_catalogType()
    {
        Assert.False(CatalogTypes.IsValid("NotACatalog"));
        Assert.True(CatalogTypes.IsValid(CatalogTypes.CommercialStage));
    }

    [Theory]
    [InlineData(nameof(PipelineKinds.Demand), "DEMAND")]
    [InlineData(nameof(PipelineKinds.Supply), "SUPPLY")]
    public void Declared_pipelineKind_has_the_expected_literal_value(string constantName, string expectedValue)
    {
        var field = typeof(PipelineKinds).GetField(constantName);

        Assert.NotNull(field);
        Assert.Equal(expectedValue, field!.GetRawConstantValue());
    }

    [Theory]
    [InlineData(nameof(SemanticStates.Open), "OPEN")]
    [InlineData(nameof(SemanticStates.Won), "WON")]
    [InlineData(nameof(SemanticStates.Lost), "LOST")]
    public void Declared_semanticState_has_the_expected_literal_value(string constantName, string expectedValue)
    {
        var field = typeof(SemanticStates).GetField(constantName);

        Assert.NotNull(field);
        Assert.Equal(expectedValue, field!.GetRawConstantValue());
    }
}
