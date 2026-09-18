using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using PartyService.Application.Parties;

namespace PartyService.Api.Parties;

/// <summary>
/// Body de alta y edición de Empresa/Contacto. Solo <c>displayName</c> es obligatorio (captura
/// mínima, alineada con crm-web: desvío explícito de la task, que pedía además un dato de
/// contacto). El email, si viene, debe tener formato válido; una cadena vacía cuenta como ausente.
/// </summary>
public sealed record PartyDataRequest(
    [Required, MaxLength(200)] string DisplayName,
    [MaxLength(200)] string? LegalName,
    [MaxLength(120)] string? GivenNames,
    [MaxLength(120)] string? FamilyNames,
    [MaxLength(40)] string? TaxIdentifier,
    [MaxLength(40)] string? IdentityDocument,
    [MaxLength(254)] string? Email,
    [MaxLength(40)] string? Phone,
    [MaxLength(300)] string? Address,
    [MaxLength(120)] string? Industry,
    [MaxLength(2000)] string? Notes,
    [MaxLength(80)] string? OriginCode) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(Email) && !MailAddress.TryCreate(Email.Trim(), out _))
        {
            yield return new ValidationResult("El email no tiene un formato válido.", new[] { nameof(Email) });
        }
    }

    public PartyDataInput ToInput() =>
        new(DisplayName, LegalName, GivenNames, FamilyNames, TaxIdentifier, IdentityDocument, Email, Phone, Address, Industry, Notes, OriginCode);
}

/// <summary><c>relationshipType</c> es opcional y por defecto <c>CONTACT_OF</c>.</summary>
public sealed record RelateContactRequest(
    [Required] Guid CompanyId,
    [MaxLength(20)] string? RelationshipType);

public sealed record ChangeCommercialStatusRequest([Required, MaxLength(20)] string CommercialStatus);

public sealed record AssignResponsibleRequest([Required] Guid ResponsibleUserId);
