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
  "schemaVersion", "migración de documentos", "crecimiento sin límite",
  "paginación de subdocumentos", "crm_access", "crm_platform_config", "crm_party".
  Garantiza que el documento siga el límite del aggregate, que ningún array crezca
  sin cota, que la concurrencia optimista use el campo `version`, y que UNKNOWN no
  se degrade a false o cero. NO activar para: la API del driver de .NET, mensajería,
  capa REST ni reglas de negocio puras.
---

# MongoDB Document Modeling

## Objetivo

La forma de un documento decide qué se puede escribir de manera atómica, qué crece
sin control y qué consultas van a ser baratas. En este proyecto hay dos modos
típicos de fallar:

1. **Modelar Mongo como SQL:** normalizar todo, referenciar por ID y resolver cada
   lectura con `$lookup`. Se pierden las ventajas del modelo documental y se gana
   latencia — y además `$lookup` entre colecciones de servicios distintos está
   directamente prohibido (ver `ddd-hexagonal-architecture`).
2. **Modelar Mongo como un cajón:** embeber todo "porque se lee junto". Aparecen
   documentos que crecen sin cota, contención de escritura y, eventualmente, el
   techo duro de 16 MiB.

Esta skill define el criterio del proyecto para elegir entre las dos, y las reglas
de índices y concurrencia que van con esa elección.

Fuentes: `PRPs/_backlog/2026-09-17-plan-wave-2-acceso-catalogos-party.md` (§0, §5
D8), `IMPLEMENTATION_REPORT-V2-FND-002.md`, `IMPLEMENTATION_REPORT-V2-FND-003.md`.

## Cuándo activar

- Se define la colección y la forma del documento de un aggregate.
- Se decide si una parte del aggregate va embebida o referenciada.
- Se agrega un índice o se diagnostica una consulta lenta.
- Se implementa búsqueda por zona, texto o mapa.
- Hay riesgo de escrituras concurrentes sobre el mismo aggregate.
- Un documento existente cambia de forma y hay datos ya guardados.

## Cuándo NO activar

- API concreta del driver: `MongoClient`, serializers, `CancellationToken`,
  proyecciones (ver `mongodb-dotnet-driver`).
- Reglas de negocio e invariantes (ver `ddd-hexagonal-architecture`).
- Mensajería, outbox y consumidores (ver `event-driven-outbox-inbox`).

## Decisiones del proyecto

| Tema | Decisión |
|---|---|
| Instancia | Una sola instancia MongoDB, compartida por todos los servicios, levantada como **replica set de un nodo** (`mongo:7.0.43` en Compose, habilita transacciones) |
| Ownership | Cada colección tiene **un único servicio owner**. Compartir instancia no autoriza acceso cruzado |
| Bases y colecciones (D8) | Una base por servicio en la misma instancia: `crm_access`, `crm_platform_config`, `crm_party` (y así para los siguientes servicios). Colección por aggregate root en snake_case plural (`user_accounts`, `catalog_entries`, `parties`, `party_relationships`). `outbox_messages`/`inbox` dentro de la base de cada servicio |
| Unidad | Una colección por **raíz de agregado**, no por clase |
| Alcance de instalación | Una única inmobiliaria: ningún documento, índice ni filtro lleva un identificador de organización |
| Unicidad | **Sin** índice único de CUIT/email/teléfono ni deduplicación: `V2-PTY-001` la excluye explícitamente del alcance |
| Concurrencia | Campo `version` (entero) por aggregate, con concurrencia optimista al actualizar — coincide con los aggregates de V2 (`UserAccount.version`, `CatalogEntry.version`) |
| Búsqueda | MongoDB nativo: índices, `$text`, `2dsphere`. Sin motor dedicado |
| Historial | Sin Event Sourcing global: estado actual + historia de negocio significativa donde la task lo pida |
| Binarios | Fuera de alcance de V2: no hay `IObjectStorage` ni subida de archivos en las tasks de esta wave |

### Límites duros de MongoDB

| Límite | Valor |
|---|---|
| Tamaño máximo de documento BSON | **16 MiB** |
| Niveles de anidamiento | 100 |
| Índices por colección | 64 |
| Campos en un índice compuesto | 32 |

El límite de 16 MiB **no es un objetivo de diseño**: un documento que se acerca a
ese orden ya venía mal desde mucho antes.

## Estado actual vs target

- **Estado:** no hay colecciones creadas todavía. `V2-ACL-001`, `V2-CAT-001` y
  `V2-PTY-001` son las primeras que materializan documentos reales, cada una sobre
  su propia base (D8).
- **Target:** cada repositorio crea sus índices al arranque de forma idempotente,
  y ningún documento tiene arrays de crecimiento ilimitado.

## Reglas obligatorias

### MUST

- **MUST** hacer que el documento coincida con el límite del aggregate definido por
  `ddd-hexagonal-architecture`. La forma del documento no redefine el dominio.
- **MUST** usar el nombre de base y colección según D8: base por servicio
  (`crm_<servicio>`), colección snake_case plural por aggregate root.
- **MUST** llevar un campo `version` (entero) por aggregate y actualizar con
  concurrencia optimista: filtrar por `version` esperada e incrementarla en el
  mismo update.
- **MUST** resolver una operación de negocio con **una sola escritura de documento**.
  La atomicidad de MongoDB es por documento. La única excepción sancionada es la
  escritura conjunta de aggregate + outbox (ver `event-driven-outbox-inbox`).
- **MUST** embeber únicamente cuando se cumplen las cuatro condiciones: forma parte
  de la misma invariante, no tiene ciclo de vida propio, el volumen está acotado y
  se lee o escribe habitualmente junto.
- **MUST** referenciar cuando se da cualquiera de estas: ciclo de vida propio,
  crecimiento sin cota razonable, necesidad de concurrencia independiente, otro
  aggregate lo referencia, u ownership distinto.
- **MUST** crear los índices al arranque del servicio de forma idempotente, no a
  mano en un entorno.
- **MUST** guardar un `schemaVersion` en documentos cuya forma se espera que
  evolucione, y tolerar en lectura las versiones anteriores.
- **MUST** representar lo desconocido de forma explícita y distinguible de un valor
  confirmado (ej. un dato de contacto opcional queda `null`, no `""`).

### MUST NOT

- **MUST NOT** acceder a una colección cuyo owner es otro servicio, ni siquiera para
  leer, ni siquiera con `$lookup`.
- **MUST NOT** embeber una colección que crece con el uso: historial de cambios,
  relaciones, mensajes.
- **MUST NOT** crear un índice único de CUIT, email o teléfono: `V2-PTY-001`
  excluye deduplicación y resolución de duplicados de su alcance.
- **MUST NOT** guardar `false`, `0` o `[]` para representar un dato desconocido.
- **MUST NOT** usar transacciones multi-documento para compensar un límite de
  aggregate mal elegido. La única excepción permitida es la escritura conjunta de
  aggregate + outbox.
- **MUST NOT** guardar binarios ni PII innecesaria dentro del documento.

## Recomendaciones

### SHOULD

- **SHOULD** diseñar los índices compuestos con el orden igualdad → orden → rango.
- **SHOULD** usar `partialFilterExpression` para índices sobre campos opcionales o
  sobre subconjuntos activos.
- **SHOULD** usar `2dsphere` para ubicación y `$text` para búsqueda textual, en
  línea con la decisión de no incorporar un motor dedicado.
- **SHOULD** mover la historia significativa a su propia colección
  (`<contexto>_<aggregate>_history`) en vez de acumularla dentro del aggregate.
- **SHOULD** acotar explícitamente todo array embebido y documentar la cota
  esperada.

### SHOULD NOT

- **SHOULD NOT** replicar en un documento datos cuyo owner es otro servicio salvo
  como snapshot inmutable, con `occurredAt`, entendido como copia y no como verdad
  (ej. `originCode` + `catalogVersion` guardados en Party, ver D7 en
  `ddd-hexagonal-architecture`).
- **SHOULD NOT** crear un índice por cada consulta nueva sin revisar si un
  compuesto existente ya la cubre por prefijo.

## Anti-patrones prohibidos

### 1. Array de crecimiento ilimitado

```csharp
// ❌ Las relaciones crecen para siempre dentro de la Party.
{ "_id": "party-1", "relationships": [ /* n sin cota */ ] }
```

```csharp
// ✅ Colección propia del contexto owner, consultada por índice.
// party_relationships: { partyId, relatedPartyId, kind, createdAt, ... }
// Índice: { partyId: 1, createdAt: -1 }
```

### 2. Update sin concurrencia optimista

```csharp
// ❌ Dos requests editando el mismo registro: gana el último, se pierde el otro cambio.
await _col.ReplaceOneAsync(x => x.Id == id, document, cancellationToken: ct);
```

```csharp
// ✅ Filtro por versión esperada; cero modificados significa conflicto.
var filter = Builders<CatalogEntryDocument>.Filter.And(
    Builders<CatalogEntryDocument>.Filter.Eq(x => x.Id, id),
    Builders<CatalogEntryDocument>.Filter.Eq(x => x.Version, expectedVersion));

document.Version = expectedVersion + 1;
var result = await _col.ReplaceOneAsync(filter, document, cancellationToken: ct);

if (result.ModifiedCount == 0)
    throw new ConcurrencyConflictException(id, expectedVersion);
```

> `MongoRepository<TAggregate, TId>.UpdateAsync` (`BuildingBlocks.Infrastructure`)
> hace `ReplaceOneAsync` por id sin filtrar por `version`: el servicio que necesita
> concurrencia optimista agrega el filtro de `version` en su propio repositorio,
> extendiendo o envolviendo la base genérica.

### 3. Índice único de un dato personal

```csharp
// ❌ V2-PTY-001 excluye explícitamente deduplicación de CUIT/email/teléfono.
new CreateIndexModel<PartyDocument>(
    Builders<PartyDocument>.IndexKeys.Ascending(p => p.TaxId),
    new CreateIndexOptions { Unique = true });
```

```csharp
// ✅ Índice no único, solo para búsqueda.
new CreateIndexModel<PartyDocument>(
    Builders<PartyDocument>.IndexKeys.Ascending(p => p.TaxId),
    new CreateIndexOptions { Name = "taxid_lookup" });
```

### 4. Desconocido guardado como valor

```csharp
// ❌ "No se cargó un segundo dato de contacto" queda indistinguible de "no tiene".
{ "secondaryContact": "" }
```

```csharp
// ✅ El estado del dato es parte del dato.
{ "secondaryContact": null }
```

### 5. Join encubierto entre servicios

```csharp
// ❌ party-service resolviendo datos de platform-config-service con $lookup.
var pipeline = new BsonDocument("$lookup", new BsonDocument
{
    { "from", "catalog_entries" },
    { "localField", "originCode" },
    { "foreignField", "code" },
    { "as", "origin" }
});
```

```csharp
// ✅ Consulta HTTP al owner con caché corta (D7), o snapshot guardado al validar.
// party.originCode + party.originCatalogVersion (snapshot inmutable, no verdad viva)
```

## Checklist antes de devolver código

- [ ] Cada colección tocada pertenece al servicio que está implementando la task.
- [ ] Nombre de base/colección según D8 (`crm_<servicio>`, snake_case plural).
- [ ] Ningún array embebido puede crecer sin cota; los acotados tienen la cota
      documentada.
- [ ] Toda escritura de negocio afecta un único documento, salvo la escritura
      conjunta de aggregate + outbox.
- [ ] Los updates usan `version` y detectan conflicto.
- [ ] Los índices se crean al arranque de forma idempotente.
- [ ] Sin índices únicos de CUIT/email/teléfono ni lógica de deduplicación.
- [ ] No hay `$lookup` ni lecturas contra colecciones de otro servicio.
- [ ] Ningún dato desconocido quedó guardado como `false`, `0` o `[]`.
- [ ] No hay binarios ni PII innecesaria dentro del documento.

## Conexiones con otros skills

| Skill | Relación |
|---|---|
| `ddd-hexagonal-architecture` | Define el límite del aggregate. Esta skill lo traduce a documento; nunca al revés. |
| `mongodb-dotnet-driver` | API del driver, serializers, proyecciones. |
| `event-driven-outbox-inbox` | La colección de outbox pertenece al mismo servicio y se escribe junto al aggregate. Única excepción a la regla de transacciones. |
| `dotnet-adversarial-testing` | Escrituras concurrentes, documentos en el límite y datos malformados. |
