using System.ComponentModel.DataAnnotations;

namespace PlatformConfigService.Api.Catalogs;

public sealed record CreateCatalogEntryRequest(
    [Required, MaxLength(60)] string CatalogType,
    [Required, MaxLength(80)] string Code,
    [Required, MaxLength(200)] string Label,
    int? Order,
    [MaxLength(20)] string? PipelineKind,
    [MaxLength(20)] string? SemanticState);

public sealed record UpdateCatalogEntryRequest(
    [Required, MaxLength(200)] string Label,
    int? Order);
