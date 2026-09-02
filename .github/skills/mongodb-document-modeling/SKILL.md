---
name: mongodb-document-modeling
description: |
  Activa cuando se diseña cómo se guarda un aggregate en MongoDB en este CRM: forma
  del documento, qué se embebe y qué se referencia, índices, unicidad, crecimiento,
  historial y evolución del esquema. Triggers: "colección", "collection", "documento",
  "esquema", "schema", "modelar en Mongo", "cómo guardo", "embeber", "embed",
  "referencia", "reference", "subdocumento", "array embebido", "documento gigante",
  "límite de tamaño", "16MB", "índice", "index", "índice compuesto", "índice
  parcial", "partialFilterExpression", "índice único", "unique index", "2dsphere",
  "$text", "búsqueda por zona", "geo", "concurrencia optimista", "optimistic
  concurrency", "version", "lost update", "atomicidad", "transacción",
  "$lookup", "join", "desnormalizar", "historial", "versionado de documento",
  "schemaVersion", "migración de documentos", "tenantId en el índice",
  "crecimiento sin límite", "paginación de subdocumentos".
  Garantiza que el documento siga el límite del aggregate, que ningún array crezca
  sin cota, que todo índice empiece por tenantId, que la unicidad sea por tenant y
  que UNKNOWN no se degrade a false o cero. NO activar para: la API del driver de
  .NET, mensajería, capa REST ni reglas de negocio puras.
---

# MongoDB Document Modeling

## Objetivo

La forma de un documento decide qué se puede escribir de manera atómica, qué crece
sin control y qué consultas van a ser baratas. En este proyecto hay dos modos
típicos de fallar:

1. **Modelar Mongo como SQL:** normalizar todo, referenciar por ID y resolver cada
   lectura con `$lookup`. Se pierden las ventajas del modelo documental y se gana
   latencia.
2. **Modelar Mongo como un cajón:** embeber todo "porque se lee junto". Aparecen
   documentos que crecen sin cota, contención de escritura y, eventualmente, el
   techo duro de 16 MiB.

Esta skill define el criterio del proyecto para elegir entre las dos, y las reglas
de índices y concurrencia que van con esa elección.

Fuentes: [`ARCHITECTURE.md`](../../../ARCHITECTURE.md) §7,
[`README.md`](../../../README.md) §1.4, §1.5 y §5.1,
[`POC_TECH_DECISIONS.md`](../../../docs/implementation/POC_TECH_DECISIONS.md).

## Cuándo activar

- Se define la colección y la forma del documento de un aggregate.
- Se decide si una parte del aggregate va embebida o referenciada.
- Se agrega un índice o se diagnostica una consulta lenta.
- Se necesita unicidad (DNI, CUIT, email) dentro de un tenant.
- Se implementa búsqueda por zona, texto o mapa.
- Hay riesgo de escrituras concurrentes sobre el mismo aggregate.
- Un documento existente cambia de forma y hay datos ya guardados.

## Cuándo NO activar

- API concreta del driver: `MongoClient`, serializers, `CancellationToken`,
  proyecciones (ver `mongodb-dotnet-driver`, pendiente).
- Reglas de negocio e invariantes (ver `ddd-hexagonal-architecture`).
- Proyecciones y read models (ver `cqrs-read-models-projections`, pendiente).
- Mensajería, outbox y consumidores.

## Decisiones del proyecto

| Tema | Decisión |
|---|---|
| Instancia | Una sola instancia MongoDB Community en POC, compartida por todos los servicios. |
| Ownership | Cada colección tiene **un único servicio owner**. Compartir instancia no autoriza acceso cruzado. |
| Nombres | Prefijo por contexto: `party_parties`, `property_properties`, `supply_listings`, `demand_requirements`. |
| Unidad | Una colección por **raíz de agregado**, no por clase. |
| Tenancy | `tenantId` en todo documento de negocio y como **primer campo** de todo índice compuesto. |
| Búsqueda | MongoDB nativo en V1: índices, `$text`, `2dsphere`. Sin motor dedicado. |
| Historial | Sin Event Sourcing global: estado actual + historia de negocio significativa + audit trail técnico. |
| Binarios | Nunca en el documento. Van detrás de `IObjectStorage`; el documento guarda la referencia. |

### Límites duros de MongoDB

Verificados contra la documentación oficial:

| Límite | Valor |
|---|---|
| Tamaño máximo de documento BSON | **16 MiB** |
| Niveles de anidamiento | 100 |
| Índices por colección | 64 |
| Campos en un índice compuesto | 32 |

El límite de 16 MiB **no es un objetivo de diseño**: un documento que se acerca a
ese orden ya venía mal desde mucho antes.

## Estado actual vs target

- **Estado:** no hay colecciones creadas. `W1-PTY-01` y `W1-PRP-01` son las
  primeras que materializan documentos reales.
- **Target:** cada repositorio crea sus índices al arranque de forma idempotente,
  todo filtro incluye `tenantId`, y ningún documento tiene arrays de crecimiento
  ilimitado.

## Reglas obligatorias

### MUST

- **MUST** hacer que el documento coincida con el límite del aggregate definido por
  `ddd-hexagonal-architecture`. La forma del documento no redefine el dominio.
- **MUST** incluir `tenantId` en todo documento de negocio y **como primer campo**
  de todo índice compuesto, para que sirva a cualquier consulta scopeada por tenant.
- **MUST** incluir `tenantId` en el filtro de toda lectura y escritura, incluso
  cuando el `_id` ya es único.
- **MUST** llevar un campo `version` (entero) por aggregate y actualizar con
  concurrencia optimista: filtrar por `version` esperada e incrementarla.
- **MUST** resolver una operación de negocio con **una sola escritura de documento**.
  La atomicidad de MongoDB es por documento.
- **MUST** embeber únicamente cuando se cumplen las cuatro condiciones: forma parte
  de la misma invariante, no tiene ciclo de vida propio, el volumen está acotado y
  se lee o escribe habitualmente junto.
- **MUST** referenciar cuando se da cualquiera de estas: ciclo de vida propio,
  crecimiento sin cota razonable, necesidad de concurrencia independiente, otro
  aggregate lo referencia, u ownership o permisos distintos.
- **MUST** hacer la unicidad **por tenant**: índice compuesto que empieza por
  `tenantId`, con `partialFilterExpression` cuando el campo es opcional.
- **MUST** crear los índices al arranque del servicio de forma idempotente, no a
  mano en un entorno.
- **MUST** guardar un `schemaVersion` en documentos cuya forma se espera que
  evolucione, y tolerar en lectura las versiones anteriores.
- **MUST** representar lo desconocido de forma explícita y distinguible de un valor
  confirmado.

### MUST NOT

- **MUST NOT** acceder a una colección cuyo owner es otro servicio, ni siquiera para
  leer, ni siquiera con `$lookup`.
- **MUST NOT** embeber una colección que crece con el uso: interacciones, pagos,
  visitas, mensajes, versiones de documentos, historial de matches.
- **MUST NOT** embeber toda la jerarquía de un edificio o un campo en un documento.
  Cada `Property` es un aggregate independiente que referencia a su parent.
- **MUST NOT** crear un índice que no empiece por `tenantId` para consultas de
  negocio.
- **MUST NOT** crear un índice único global sobre un dato de negocio: colisiona
  entre tenants.
- **MUST NOT** guardar `false`, `0` o `[]` para representar un dato desconocido.
- **MUST NOT** usar transacciones multi-documento como mecanismo por defecto para
  compensar un límite de aggregate mal elegido.
- **MUST NOT** guardar binarios, PII innecesaria ni secretos dentro del documento.
- **MUST NOT** escribir en la colección de un read model desde lógica de negocio: se
  alimenta por proyección y es reconstruible.

## Recomendaciones

### SHOULD

- **SHOULD** desnormalizar de forma deliberada y acotada dentro del mismo servicio
  cuando evita una lectura extra frecuente (por ejemplo, el nombre para mostrar de
  una Party dentro de un documento del mismo contexto), documentando quién lo
  refresca.
- **SHOULD** diseñar los índices compuestos con el orden igualdad → orden →
  rango, después de `tenantId`.
- **SHOULD** usar `partialFilterExpression` para índices sobre campos opcionales o
  sobre subconjuntos activos: ahorra tamaño y evita unicidad indeseada sobre nulos.
- **SHOULD** usar `2dsphere` para ubicación y `$text` para búsqueda textual, en
  línea con la decisión de no incorporar un motor dedicado en V1.
- **SHOULD** mover la historia significativa a su propia colección
  (`<contexto>_<aggregate>_history`) en vez de acumularla dentro del aggregate.
- **SHOULD** acotar explícitamente todo array embebido y documentar la cota
  esperada.

### SHOULD NOT

- **SHOULD NOT** replicar en un documento datos cuyo owner es otro servicio salvo
  como snapshot inmutable, con `occurredAt`, entendido como copia y no como verdad.
- **SHOULD NOT** crear un índice por cada consulta nueva sin revisar si un compuesto
  existente ya la cubre por prefijo.
- **SHOULD NOT** usar `ObjectId` como identificador público de un aggregate: los IDs
  públicos se definen en los contratos compartidos (`FND-003`).

## Anti-patrones prohibidos

### 1. Embeber la jerarquía completa

```csharp
// ❌ Todo el edificio en un documento: crece sin cota, contención de escritura,
//    y una unidad no puede tener lifecycle ni Listing propio.
{
  "_id": "building-1",
  "tenantId": "t-1",
  "units": [ { "unitId": "u-1", "listings": [ /* ... */ ] }, /* x 400 */ ]
}
```

```csharp
// ✅ Cada Property es aggregate propio y referencia al parent.
{ "_id": "prop-b1",  "tenantId": "t-1", "kind": "BUILDING", "parentPropertyId": null }
{ "_id": "prop-u1",  "tenantId": "t-1", "kind": "UNIT",     "parentPropertyId": "prop-b1" }
```

### 2. Array de crecimiento ilimitado

```csharp
// ❌ Las interacciones crecen para siempre dentro de la Party.
{ "_id": "party-1", "tenantId": "t-1", "interactions": [ /* n sin cota */ ] }
```

```csharp
// ✅ Colección propia del contexto owner, consultada por índice.
// interaction_interactions: { tenantId, partyId, occurredAt, channel, ... }
// Índice: { tenantId: 1, partyId: 1, occurredAt: -1 }
```

### 3. Índice sin `tenantId`

```csharp
// ❌ Escanea documentos de otros tenants antes de filtrar.
Builders<PropertyDocument>.IndexKeys.Ascending(p => p.City);
```

```csharp
// ✅ tenantId primero: sirve a todas las consultas scopeadas.
Builders<PropertyDocument>.IndexKeys
    .Ascending(p => p.TenantId)
    .Ascending(p => p.City)
    .Descending(p => p.UpdatedAt);
```

### 4. Unicidad global en vez de por tenant

```csharp
// ❌ Dos inmobiliarias no pueden tener a la misma persona como Party.
new CreateIndexModel<PartyDocument>(
    Builders<PartyDocument>.IndexKeys.Ascending(p => p.TaxId),
    new CreateIndexOptions { Unique = true });
```

```csharp
// ✅ Única dentro del tenant, y solo cuando el campo existe.
new CreateIndexModel<PartyDocument>(
    Builders<PartyDocument>.IndexKeys
        .Ascending(p => p.TenantId)
        .Ascending(p => p.TaxId),
    new CreateIndexOptions<PartyDocument>
    {
        Unique = true,
        PartialFilterExpression = Builders<PartyDocument>.Filter
            .Exists(p => p.TaxId, true)
    });
```

### 5. Update sin concurrencia optimista

```csharp
// ❌ Dos agentes editando la misma captación: gana el último, se pierde el otro cambio.
await _col.ReplaceOneAsync(x => x.Id == id, document, cancellationToken: ct);
```

```csharp
// ✅ Filtro por versión esperada; cero modificados significa conflicto.
var filter = Builders<CaptationDocument>.Filter.And(
    Builders<CaptationDocument>.Filter.Eq(x => x.Id, id),
    Builders<CaptationDocument>.Filter.Eq(x => x.TenantId, tenantId),
    Builders<CaptationDocument>.Filter.Eq(x => x.Version, expectedVersion));

document.Version = expectedVersion + 1;
var result = await _col.ReplaceOneAsync(filter, document, cancellationToken: ct);

if (result.ModifiedCount == 0)
    throw new ConcurrencyConflictException(id, expectedVersion);
```

### 6. Desconocido guardado como valor

```csharp
// ❌ "No preguntamos si acepta mascotas" queda indistinguible de "no acepta".
{ "petsAllowed": false }
```

```csharp
// ✅ El estado del dato es parte del dato.
{ "petsAllowed": { "state": "UNKNOWN" } }
{ "petsAllowed": { "state": "CONFIRMED", "value": false } }
```

### 7. Join encubierto entre servicios

```csharp
// ❌ matching-service resolviendo datos de property-service con $lookup.
var pipeline = new BsonDocument("$lookup", new BsonDocument
{
    { "from", "property_properties" },
    { "localField", "propertyId" },
    { "foreignField", "_id" },
    { "as", "property" }
});
```

```csharp
// ✅ Read model propio alimentado por eventos, o API pública del owner.
// matching_property_snapshots: { tenantId, propertyId, zone, price, updatedAt }
```

## Checklist antes de devolver código

- [ ] Cada colección tocada pertenece al servicio que está implementando la task.
- [ ] `tenantId` está en el documento, en todos los filtros y como primer campo de
      cada índice compuesto.
- [ ] Ningún array embebido puede crecer sin cota; los acotados tienen la cota
      documentada.
- [ ] Toda escritura de negocio afecta un único documento.
- [ ] Los updates usan `version` y detectan conflicto.
- [ ] Los índices se crean al arranque de forma idempotente.
- [ ] La unicidad es por tenant, con `partialFilterExpression` si el campo es
      opcional.
- [ ] No hay `$lookup` ni lecturas contra colecciones de otro servicio.
- [ ] Ningún dato desconocido quedó guardado como `false`, `0` o `[]`.
- [ ] No hay binarios ni PII innecesaria dentro del documento.
- [ ] Si la forma del documento cambió, hay `schemaVersion` y la lectura tolera lo
      viejo.

## Conexiones con otros skills

| Skill | Relación |
|---|---|
| `ddd-hexagonal-architecture` | Define el límite del aggregate. Esta skill lo traduce a documento; nunca al revés. |
| `mongodb-dotnet-driver` | *Pendiente.* API del driver, serializers, proyecciones, testing con Testcontainers. |
| `multitenancy-authorization` | *Pendiente.* De dónde sale el `tenantId` que acá se da por presente. |
| `cqrs-read-models-projections` | *Pendiente.* Colecciones derivadas, checkpoints y rebuild. |
| `event-driven-outbox-inbox` | *Pendiente.* La colección de outbox vive junto al aggregate que la genera. |
| `dotnet-adversarial-testing` | Escrituras concurrentes, documentos en el límite y datos malformados. |
