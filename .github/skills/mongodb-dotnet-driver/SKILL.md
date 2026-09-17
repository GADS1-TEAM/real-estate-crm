---
name: mongodb-dotnet-driver
description: |
  Activa cuando se escribe código .NET que habla con MongoDB en este CRM: registro
  del cliente, repositorios, serialización, queries, bootstrap de índices y testing
  contra una base real. Triggers: "MongoClient", "IMongoClient", "IMongoDatabase",
  "IMongoCollection", "MongoClientSettings", "connection string de Mongo",
  "repositorio Mongo", "MongoRepository", "FindAsync", "Find(", "ReplaceOneAsync",
  "UpdateOneAsync", "InsertOneAsync", "DeleteOneAsync", "BulkWrite",
  "FindOneAndUpdate", "AsQueryable", "IQueryable", "LINQ3",
  "ExpressionNotSupportedException", "BsonSerializer", "RegisterSerializer",
  "GuidSerializer", "GuidRepresentation", "BsonClassMap", "ConventionPack",
  "BsonElement", "BsonIgnoreExtraElements", "BsonRepresentation", "Decimal128",
  "CreateIndexModel", "CreateManyAsync", "Indexes.CreateOne",
  "RequiresMongo", "MongoWriteException", "MongoCommandException",
  "duplicate key", "E11000", "retryWrites", "MongoDB.Driver", "MongoUnitOfWork",
  "MongoSessionAccessor".
  Garantiza cliente singleton, serialización configurada explícitamente al
  arranque, `CancellationToken` propagado, índices creados de forma idempotente y
  tests contra MongoDB real (Traits + Compose, no Testcontainers). NO activar para:
  decidir la forma del documento o los índices (eso es mongodb-document-modeling),
  reglas de negocio, mensajería ni capa REST.
---

# MongoDB .NET Driver

## Objetivo

`mongodb-document-modeling` decide **qué** se guarda. Esta skill cubre **cómo** se
lo escribe en C# sin romper nada, siguiendo lo que ya implementó
`RealEstateCrm.BuildingBlocks.Infrastructure` en `V2-FND-002`.

El driver 3.x introdujo cambios incompatibles respecto de 2.x, y varios son
silenciosos: compilan igual y fallan al leer datos. Este proyecto usa 3.x
directamente, así que la regla es simple: **nunca copiar un snippet escrito para
2.x** sin verificarlo.

Fuentes: `IMPLEMENTATION_REPORT-V2-FND-002.md`,
`IMPLEMENTATION_REPORT-V2-FND-003.md`.

## Cuándo activar

- Se registra el cliente de MongoDB en el contenedor de DI.
- Se implementa un repositorio o cualquier adapter de persistencia.
- Se configura serialización: GUIDs, enums, decimales, fechas, conventions.
- Se escribe una query, una proyección o una agregación.
- Se crean índices desde código.
- Se escriben tests de integración contra MongoDB.
- Aparece un error del driver: duplicate key, timeout, `ExpressionNotSupportedException`.

## Cuándo NO activar

- Forma del documento, embed vs reference, qué índices hacen falta (ver
  `mongodb-document-modeling`).
- Aggregates e invariantes (ver `ddd-hexagonal-architecture`).
- Outbox, consumidores y mensajería (ver `event-driven-outbox-inbox`).
- Controllers, DTOs y contratos HTTP.

## Decisiones del proyecto

| Tema | Decisión |
|---|---|
| Paquete | `MongoDB.Driver` **3.11.2 exacta**, declarada directo en el `.csproj` de `RealEstateCrm.BuildingBlocks.Infrastructure` (sin gestión central de versiones en este repo) |
| Servidor | MongoDB `7.0.43` en Compose, como **replica set de un nodo** (`rs0`). El driver 3.x no soporta versiones de servidor muy antiguas |
| Connection string | El host necesita `directConnection=true` contra el miembro anunciado como `mongo:27017` (DNS de Compose); ver `.env.example` |
| LINQ | LINQ3 únicamente |
| IDs | `Guid` serializado como **binario, `GuidRepresentation.Standard`**, registrado una única vez por proceso vía `MongoGuidSerializationBootstrap` (`[ModuleInitializer]`) |
| `_id` de tipos sin miembro mapeado | `MongoClassMapBootstrap` registra `BsonClassMap` por código (ej. `OutboxMessage.EventId` → `_id`) cuando el tipo vive en el proyecto de puertos y no puede llevar atributos `[Bson*]` |
| Cliente | `IMongoClient` singleton por proceso, resuelto vía `AddMongoPersistence` (`MongoServiceCollectionExtensions`) |
| Dinero | `decimal` → BSON `Decimal128` (default del driver 3.x). Nunca `double` |
| Testing | MongoDB real, con Trait `[Trait("Category", "RequiresMongo")]` y `scripts/test-integration.{sh,ps1}` (levanta Compose). Sin Testcontainers ni mocks del driver |

### Migración 2.x → 3.x (referencia rápida)

| Tema | 2.x | 3.x |
|---|---|---|
| LINQ | LINQ2 + LINQ3 | solo LINQ3 |
| Queryable | `IMongoQueryable` | `IQueryable` |
| GUIDs | dos modos | solo V3; hay que registrar el serializer |
| `decimal` | BSON string | BSON `Decimal128` |
| `MongoClient` / `Database` / `Collection` | heredables | **sellados**: usar interfaces |

Los tres silenciosos son `decimal`, `DateTimeOffset` y GUIDs: **no fallan al
compilar, fallan al leer**. `MongoGuidSerializationBootstrap` ya resuelve el caso
de GUIDs para todo el building block; no hay que repetirlo por servicio.

## Estado actual vs target

- **Estado:** `IMongoClient`, `MongoRepository<TAggregate, TId>`, `MongoUnitOfWork`
  (transacción vía `IUnitOfWork.ExecuteInTransactionAsync`), `MongoOutbox` y
  `MongoInbox` existen en `BuildingBlocks.Infrastructure` (`V2-FND-002`), probados
  contra Mongo real. Ningún servicio de dominio registra todavía un repositorio
  concreto: no hay aggregates aún.
- **Target:** cada servicio extiende `MongoRepository<TAggregate, TId>` con su
  colección y su selector de id, y crea sus índices al arrancar.

## Reglas obligatorias

### MUST

- **MUST** registrar `IMongoClient` como **singleton**. Es thread-safe y maneja su
  propio pool de conexiones.
- **MUST** registrar la configuración de serialización **una sola vez, en el
  arranque, antes de cualquier operación** (ya lo hace
  `MongoGuidSerializationBootstrap` para GUIDs; no duplicarlo por servicio).
- **MUST** depender de `IMongoClient`, `IMongoDatabase` e `IMongoCollection<T>`,
  nunca de las clases concretas: están selladas desde 3.0.
- **MUST** propagar `CancellationToken` a **toda** operación del driver.
- **MUST** crear los índices al arranque con `CreateManyAsync`, que es idempotente
  para definiciones equivalentes.
- **MUST** implementar el repositorio extendiendo `MongoRepository<TAggregate,
  TId>` o implementando `IRepository<TAggregate, TId>` directamente, dentro del
  proyecto `*.Infrastructure`.
- **MUST** mapear los errores del driver a errores de dominio o de aplicación:
  `E11000` (duplicate key) no debe llegar crudo al usuario.
- **MUST** usar `decimal` para dinero. `double` no se usa para importes.
- **MUST** escribir aggregate + outbox dentro de `IUnitOfWork.ExecuteInTransactionAsync`
  cuando el aggregate publica eventos (ver `event-driven-outbox-inbox`).

### MUST NOT

- **MUST NOT** instanciar `new MongoClient(...)` por request, por repositorio o por
  operación.
- **MUST NOT** usar `IMongoQueryable`: no existe en 3.x. Es `IQueryable`.
- **MUST NOT** materializar la colección para filtrar en memoria
  (`.ToListAsync()` seguido de `.Where(...)`).
- **MUST NOT** hacer mocks de `IMongoCollection<T>` ni de los tipos del driver: los
  tests de repositorio corren contra MongoDB real (`RequiresMongo` + Compose).
- **MUST NOT** usar transacciones multi-documento salvo la escritura conjunta de
  aggregate + outbox.
- **MUST NOT** exponer `IQueryable` fuera del repositorio.
- **MUST NOT** copiar snippets de driver 2.x sin verificarlos contra la tabla de
  migración.

## Recomendaciones

### SHOULD

- **SHOULD** proyectar a DTO en la query cuando no se necesita el documento
  completo.
- **SHOULD** aplicar `BsonIgnoreExtraElements` en los documentos para que un campo
  nuevo escrito por una versión más reciente no rompa la lectura.
- **SHOULD** usar `FindOneAndUpdate` con `ReturnDocument.After` cuando se necesita
  el estado resultante, en vez de leer y volver a escribir.
- **SHOULD** dejar activos los reintentos del driver (`retryWrites`, `retryReads`).

### SHOULD NOT

- **SHOULD NOT** usar `BsonDocument` como tipo de trabajo cuando existe un POCO.
- **SHOULD NOT** poner atributos `[Bson*]` en las clases del dominio: los
  documentos son tipos propios de `Infrastructure`. Si el tipo vive en el proyecto
  de puertos (como `OutboxMessage`), mapear por `BsonClassMap` en código, no con
  atributos.
- **SHOULD NOT** abrir sesiones ni transacciones para leer.

## Anti-patrones prohibidos

### 1. Cliente por request

```csharp
// ❌ Un pool nuevo por request: agota sockets y mata la latencia.
public class PartyRepository
{
    public async Task<Party?> GetAsync(Guid id, CancellationToken ct)
    {
        var client = new MongoClient(_connectionString);
        // ...
    }
}
```

```csharp
// ✅ Singleton resuelto por AddMongoPersistence; la colección se resuelve barata.
services.AddMongoPersistence(configuration);
```

### 2. API de la 2.x

```csharp
// ❌ IMongoQueryable no existe en 3.x.
IMongoQueryable<PartyDocument> q = collection.AsQueryable();
```

```csharp
// ✅ IQueryable, como cualquier otro proveedor LINQ.
IQueryable<PartyDocument> q = collection.AsQueryable()
    .Where(p => p.CommercialStatus == "POTENTIAL");
```

### 3. Filtrar en memoria

```csharp
// ❌ Trae la colección entera y filtra en el proceso.
var all = await _col.Find(FilterDefinition<PartyDocument>.Empty).ToListAsync(ct);
var mine = all.Where(p => p.ResponsibleUserId == userId).ToList();
```

```csharp
// ✅ El filtro viaja al servidor y usa el índice.
var filter = Builders<PartyDocument>.Filter.Eq(p => p.ResponsibleUserId, userId);

var mine = await _col.Find(filter)
    .SortByDescending(p => p.UpdatedAt)
    .Limit(pageSize)
    .ToListAsync(ct);
```

### 4. Índices creados fuera del código

```csharp
// ❌ Creados a mano en una consola: no existen en CI ni en la máquina de otro dev.
```

```csharp
// ✅ Idempotente, al arranque del servicio.
public async Task EnsureIndexesAsync(CancellationToken ct)
{
    var models = new[]
    {
        new CreateIndexModel<PartyDocument>(
            Builders<PartyDocument>.IndexKeys
                .Ascending(p => p.ResponsibleUserId)
                .Descending(p => p.UpdatedAt),
            new CreateIndexOptions { Name = "responsible_updated" })
    };

    await _col.Indexes.CreateManyAsync(models, ct);
}
```

### 5. Mockear el driver

```csharp
// ❌ Sellado, y además testea el mock, no el mapeo ni los índices.
var mock = new Mock<IMongoCollection<PartyDocument>>();
```

```csharp
// ✅ MongoDB real vía Compose, marcado con el Trait de infraestructura.
[Trait("Category", "RequiresMongo")]
public class PartyRepositoryTests
{
    // MONGO_CONNECTION_STRING la resuelve scripts/test-integration.{sh,ps1}.
}
```

### 6. Error del driver sin traducir

```csharp
// ❌ El usuario recibe un E11000 con el nombre del índice.
await _col.InsertOneAsync(document, cancellationToken: ct);
```

```csharp
// ✅ Traducido a un error del dominio, estable en el contrato público.
try
{
    await _col.InsertOneAsync(document, cancellationToken: ct);
}
catch (MongoWriteException ex)
    when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
{
    throw new CatalogEntryAlreadyExistsException(document.Code);
}
```

## Checklist antes de devolver código

- [ ] `IMongoClient` registrado como singleton; ningún `new MongoClient` fuera del
      composition root.
- [ ] Se usan las interfaces del driver, no las clases selladas.
- [ ] `CancellationToken` en todas las llamadas.
- [ ] Ningún `IQueryable` cruza el borde del repositorio.
- [ ] Ningún filtrado en memoria sobre resultados materializados.
- [ ] Índices creados desde código de forma idempotente.
- [ ] Errores del driver traducidos a errores de dominio.
- [ ] Tests de repositorio contra MongoDB real (`RequiresMongo` + Compose), sin
      mocks del driver.
- [ ] Ningún snippet de driver 2.x sin verificar contra la tabla de migración.

## Conexiones con otros skills

| Skill | Relación |
|---|---|
| `mongodb-document-modeling` | Define documento e índices. Esta skill los escribe en C#. |
| `ddd-hexagonal-architecture` | El repositorio implementa un puerto; los documentos viven en `Infrastructure`, no en `Domain`. |
| `event-driven-outbox-inbox` | La escritura del outbox comparte la transacción con el aggregate vía `IUnitOfWork`. |
| `aspnetcore-error-and-observability` | Trazas del driver vía OpenTelemetry; sin PII en los logs de query. |
| `dotnet-unit-testing` | Tests de repositorio contra Mongo real; los mocks van en el puerto. |
