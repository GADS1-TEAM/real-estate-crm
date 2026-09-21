using System.Text.Json;
using RealEstateCrm.Contracts.Paging;
using RealEstateCrm.Contracts.Serialization;

namespace RealEstateCrm.ContractTests.Paging;

public class PagedResultContractTests
{
    [Fact]
    public void Serialized_shape_exposes_all_required_fields_as_camelCase()
    {
        var page = new PagedResult<string>(["a", "b"], 1, 10, 2, false);

        var json = JsonSerializer.Serialize(page, RealEstateCrmJsonDefaults.Options);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        foreach (var expectedField in new[] { "items", "page", "pageSize", "total", "hasNext" })
        {
            Assert.True(root.TryGetProperty(expectedField, out _), $"Falta el campo '{expectedField}' en PagedResult serializado.");
        }
    }
}
