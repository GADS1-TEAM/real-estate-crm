using RealEstateCrm.Contracts.Catalogs;

namespace PlatformConfigService.Infrastructure.Seeding;

/// <summary>Una entrada del seed reproducible de desarrollo (D9, CAT-006).</summary>
public sealed record DevSeedCatalogEntry(string CatalogType, string Code, string Label, int? Order, string? PipelineKind, string? SemanticState);

/// <summary>
/// Defaults inmobiliarios obligatorios literales de V2-CAT-001 (etapas DEMAND/SUPPLY, tipos de
/// actividad, orígenes, motivos de pérdida) más OperationType/PropertyType del plan maestro §5
/// ("Estados y catálogos"). Los códigos (<c>Code</c>) no están dados literalmente por la task: se
/// derivan en SCREAMING_SNAKE_CASE, sin acentos, de cada etiqueta en español.
/// </summary>
public static class DevSeedCatalogEntries
{
    public static readonly IReadOnlyList<DevSeedCatalogEntry> All = BuildAll();

    private static IReadOnlyList<DevSeedCatalogEntry> BuildAll()
    {
        var entries = new List<DevSeedCatalogEntry>();

        // CommercialStage — DEMAND (CAT-001, orden 1..8). "Operación concretada" = WON, "Operación perdida" = LOST.
        var demandStages = new (string Code, string Label, string SemanticState)[]
        {
            ("CONSULTA_RECIBIDA", "Consulta recibida", SemanticStates.Open),
            ("NECESIDAD_RELEVADA", "Necesidad relevada", SemanticStates.Open),
            ("PROPIEDADES_SELECCIONADAS", "Propiedades seleccionadas", SemanticStates.Open),
            ("VISITA_REALIZADA", "Visita realizada", SemanticStates.Open),
            ("NEGOCIACION", "Negociación", SemanticStates.Open),
            ("RESERVA", "Reserva", SemanticStates.Open),
            ("OPERACION_CONCRETADA", "Operación concretada", SemanticStates.Won),
            ("OPERACION_PERDIDA", "Operación perdida", SemanticStates.Lost),
        };

        for (var i = 0; i < demandStages.Length; i++)
        {
            var (code, label, semanticState) = demandStages[i];
            entries.Add(new DevSeedCatalogEntry(CatalogTypes.CommercialStage, code, label, i + 1, PipelineKinds.Demand, semanticState));
        }

        // CommercialStage — SUPPLY (CAT-001, orden 1..6). "Captación concretada" = WON, "Captación perdida" = LOST.
        var supplyStages = new (string Code, string Label, string SemanticState)[]
        {
            ("CONTACTO_PROPIETARIO", "Contacto propietario", SemanticStates.Open),
            ("TASACION", "Tasación", SemanticStates.Open),
            ("MANDATO", "Mandato", SemanticStates.Open),
            ("PUBLICACION", "Publicación", SemanticStates.Open),
            ("CAPTACION_CONCRETADA", "Captación concretada", SemanticStates.Won),
            ("CAPTACION_PERDIDA", "Captación perdida", SemanticStates.Lost),
        };

        for (var i = 0; i < supplyStages.Length; i++)
        {
            var (code, label, semanticState) = supplyStages[i];
            entries.Add(new DevSeedCatalogEntry(CatalogTypes.CommercialStage, code, label, i + 1, PipelineKinds.Supply, semanticState));
        }

        // ActivityType (CAT-002).
        AddSimple(entries, CatalogTypes.ActivityType, new (string, string)[]
        {
            ("LLAMADA", "Llamada"),
            ("CORREO_ELECTRONICO", "Correo electrónico"),
            ("MENSAJE", "Mensaje"),
            ("REUNION_PRESENCIAL", "Reunión presencial"),
            ("REUNION_VIRTUAL", "Reunión virtual"),
            ("DEMOSTRACION", "Demostración"),
            ("ENVIO_PROPUESTA", "Envío de propuesta"),
            ("NOTA_INTERNA", "Nota interna"),
            ("OTRO", "Otro"),
        });

        // CommercialOrigin (CAT-003). "Portal inmobiliario" es un origen manual: no implica integración con portales.
        AddSimple(entries, CatalogTypes.CommercialOrigin, new (string, string)[]
        {
            ("SITIO_WEB", "Sitio web"),
            ("REDES_SOCIALES", "Redes sociales"),
            ("PUBLICIDAD", "Publicidad"),
            ("RECOMENDACION", "Recomendación"),
            ("EVENTO", "Evento"),
            ("PROSPECCION", "Prospección"),
            ("CLIENTE_EXISTENTE", "Cliente existente"),
            ("PORTAL_INMOBILIARIO", "Portal inmobiliario"),
        });

        // LossReason (CAT-004).
        AddSimple(entries, CatalogTypes.LossReason, new (string, string)[]
        {
            ("PRECIO", "Precio"),
            ("FALTA_PRESUPUESTO", "Falta de presupuesto"),
            ("COMPETIDOR", "Competidor"),
            ("INMUEBLE_INADECUADO", "Inmueble inadecuado"),
            ("FALTA_RESPUESTA", "Falta de respuesta"),
            ("DECISION_POSTERGADA", "Decisión postergada"),
            ("INMUEBLE_NO_DISPONIBLE", "Inmueble no disponible"),
            ("FINANCIACION", "Financiación"),
            ("OTRO", "Otro"),
        });

        // OperationType (CAT-005, plan maestro §5: "compraventa, alquiler residencial, alquiler
        // comercial, alquiler temporario y las modalidades inmobiliarias que se usen en la demo").
        AddSimple(entries, CatalogTypes.OperationType, new (string, string)[]
        {
            ("COMPRAVENTA", "Compraventa"),
            ("ALQUILER_RESIDENCIAL", "Alquiler residencial"),
            ("ALQUILER_COMERCIAL", "Alquiler comercial"),
            ("ALQUILER_TEMPORARIO", "Alquiler temporario"),
        });

        // PropertyType (CAT-005, plan maestro §5: "casa, departamento, PH, local, oficina,
        // terreno, campo, cochera y otros tipos necesarios para distinguir lo urbano de lo rural").
        AddSimple(entries, CatalogTypes.PropertyType, new (string, string)[]
        {
            ("CASA", "Casa"),
            ("DEPARTAMENTO", "Departamento"),
            ("PH", "PH"),
            ("LOCAL", "Local"),
            ("OFICINA", "Oficina"),
            ("TERRENO", "Terreno"),
            ("CAMPO", "Campo"),
            ("COCHERA", "Cochera"),
        });

        return entries;
    }

    private static void AddSimple(List<DevSeedCatalogEntry> entries, string catalogType, (string Code, string Label)[] items)
    {
        foreach (var (code, label) in items)
        {
            entries.Add(new DevSeedCatalogEntry(catalogType, code, label, Order: null, PipelineKind: null, SemanticState: null));
        }
    }
}
