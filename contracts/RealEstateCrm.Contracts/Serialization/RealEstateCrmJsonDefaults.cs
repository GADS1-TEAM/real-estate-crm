using System.Text.Json;
using System.Text.Json.Serialization;

namespace RealEstateCrm.Contracts.Serialization;

/// <summary>
/// Opciones de serialización JSON compartidas por todos los contratos V2 (API y eventos).
/// </summary>
/// <remarks>
/// Fijar estas opciones en un único lugar evita que dos servicios serialicen el mismo
/// contrato de forma distinta (camelCase vs PascalCase, enums como número vs string).
/// </remarks>
public static class RealEstateCrmJsonDefaults
{
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
        };

        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        return options;
    }
}
