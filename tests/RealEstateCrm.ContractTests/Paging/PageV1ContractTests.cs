using System.Text.Json;
using RealEstateCrm.Contracts.Paging;
using RealEstateCrm.Contracts.Serialization;

namespace RealEstateCrm.ContractTests.Paging;

public class PageV1ContractTests
{
    [Theory]
    [InlineData(10, 3, 4)]
    [InlineData(10, 10, 1)]
    [InlineData(0, 10, 0)]
    [InlineData(11, 10, 2)]
    public void TotalPages_is_computed_from_total_count_and_page_size(int totalCount, int pageSize, int expectedTotalPages)
    {
        var page = new PageV1<string>(Items: [], Page: 1, PageSize: pageSize, TotalCount: totalCount);

        Assert.Equal(expectedTotalPages, page.TotalPages);
    }

    [Fact]
    public void Serialized_shape_exposes_all_required_fields_as_camelCase()
    {
        var page = new PageV1<string>(["a", "b"], Page: 1, PageSize: 10, TotalCount: 2);

        var json = JsonSerializer.Serialize(page, RealEstateCrmJsonDefaults.Options);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        foreach (var expectedField in new[] { "items", "page", "pageSize", "totalCount" })
        {
            Assert.True(root.TryGetProperty(expectedField, out _), $"Falta el campo '{expectedField}' en PageV1 serializado.");
        }
    }
}
