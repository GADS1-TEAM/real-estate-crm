using System.ComponentModel.DataAnnotations;

namespace AccessService.Api.Users;

public sealed record CreateUserRequest(
    [Required] Guid KeycloakSubject,
    [Required, MaxLength(200)] string DisplayName,
    [Required, EmailAddress, MaxLength(200)] string Email);

public sealed record UpdateUserRequest(
    [Required, MaxLength(200)] string DisplayName,
    [Required, EmailAddress, MaxLength(200)] string Email);

public sealed record AssignRoleRequest([Required] string RoleCode);
