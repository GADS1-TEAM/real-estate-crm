using System.Text.RegularExpressions;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Observability;

/// <summary>
/// Redacta valores de campos sensibles antes de que lleguen a un log (OPS-005). Busca pares
/// <c>clave: valor</c> / <c>clave=valor</c> cuya clave sea una de <see cref="SensitiveKeyNames"/>
/// (password, token, secret, documento/DNI/CUIT, texto de actividad) y reemplaza el valor por
/// <see cref="RedactedPlaceholder"/>, sin tocar el resto del mensaje ni correlationId/actorId.
/// </summary>
/// <remarks>
/// Detección basada en nombre de clave, no en contenido libre: un texto sensible que no venga
/// acompañado de una de estas claves no se redacta. Cada servicio de dominio que llegue a
/// loguear datos de negocio debe usar estos nombres de campo (o filtrar antes de loguear); no
/// hay aún ningún aggregate real que lo ejercite (ver IMPLEMENTATION_REPORT-V2-FND-003.md).
/// </remarks>
public static partial class SensitiveDataRedactor
{
    public const string RedactedPlaceholder = "[REDACTED]";

    private static readonly string[] SensitiveKeyNames =
    [
        "password", "pwd", "token", "accesstoken", "refreshtoken", "secret", "clientsecret",
        "documento", "dni", "cuit", "cuil", "activitytext", "texto", "textoactividad",
    ];

    [GeneratedRegex(
        """(?i)("?(?:password|pwd|token|accesstoken|refreshtoken|secret|clientsecret|documento|dni|cuit|cuil|activitytext|texto|textoactividad)"?\s*[:=]\s*)("(?:[^"\\]|\\.)*"|'[^']*'|[^,\s}\]]+)""")]
    private static partial Regex SensitiveKeyValuePattern();

    public static string Redact(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input ?? string.Empty;
        }

        return SensitiveKeyValuePattern().Replace(input, match => match.Groups[1].Value + RedactedPlaceholder);
    }

    /// <summary>True si el nombre de clave (sin distinguir mayúsculas) se considera sensible.</summary>
    public static bool IsSensitiveKey(string key)
        => Array.Exists(SensitiveKeyNames, k => string.Equals(k, key.Replace("_", string.Empty), StringComparison.OrdinalIgnoreCase));
}
