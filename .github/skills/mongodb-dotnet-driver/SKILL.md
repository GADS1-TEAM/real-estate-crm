---
name: mongodb-dotnet-driver
description: |
  Activa cuando se escribe código .NET que habla con MongoDB en este CRM: registro
  del cliente, repositorios, serialización, queries, bootstrap de índices y testing
  contra una base real. Triggers: "MongoClient", "IMongoClient", "IMongoDatabase",
  "IMongoCollection", "MongoClientSettings", "connection string de Mongo",
  "repositorio Mongo", "FindAsync", "Find(", "ReplaceOneAsync", "UpdateOneAsync",
  "InsertOneAsync", "DeleteOneAsync", "BulkWrite", "FindOneAndUpdate",
  "AsQueryable", "IQueryable", "IMongoQueryable", "LINQ3", "LINQ2",
  "ExpressionNotSupportedException", "client-side projection",
  "EnableClientSideProjections", "TranslationOptions", "BsonSerializer",
  "RegisterSerializer", "GuidSerializer", "GuidRepresentation", "ObjectSerializer",
  "BsonClassMap", "ConventionPack", "BsonElement", "BsonIgnoreExtraElements",
  "BsonRepresentation", "Decimal128", "CreateIndexModel", "CreateManyAsync",
  "Indexes.CreateOne", "Testcontainers Mongo", "test de integración Mongo",
  "MongoWriteException", "MongoCommandException", "duplicate key", "E11000",
  "retryWrites", "driver 2.x", "driver 3.x", "upgrade del driver",
  "MongoDB.Driver".
  Garantiza cliente singleton, serialización configurada explícitamente al
  arranque, filtros por tenant, CancellationToken propagado, índices creados de
  forma idempotente y tests contra MongoDB real. NO activar para: decidir la forma
  del documento o los índices (eso es mongodb-document-modeling), reglas de negocio,
  mensajería ni capa REST.
---

# MongoDB .NET Driver

## Objetivo

`mongodb-document-modeling` decide **qué** se guarda. Esta skill cubre **cómo** se
lo escribe en C# sin romper nada.

El driver 3.x introdujo cambios incompatibles respecto de 2.x, y varios son
silenciosos: compilan igual y fallan al leer datos. Este proyecto arranca
directamente en 3.x, así que la regla es simple: **nunca copiar un snippet escrito
para 2.x**. La mayor parte de los ejemplos de blogs, respuestas de foros y memoria
de modelos siguen siendo de la 2.x.

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
- Outbox, consumidores y mensajería.
- Controllers, DTOs y contratos HTTP.

## Decisiones del proyecto

| Tema | Decisión |
|---|---|
| Paquete | `MongoDB.Driver` **3.11.1 exacta** |
| Gestión de versión | Central: `Directory.Packages.props` en la raíz, con `ManagePackageVersionsCentrally`. Ningún `.csproj` declara versión propia |
| Servidor mínimo | MongoDB **8.x Community**. El driver 3.10+ ya no soporta Server 4.2 o anterior |
| LINQ | LINQ3 únicamente. LINQ2 no existe en 3.x |
| Proyecciones client-side | **Deshabilitadas.** Se deja el default: una query no traducible tira `ExpressionNotSupportedException` |
| IDs | `Guid` serializado como **binario, `GuidRepresentation.Standard`** (subtype 4) |
| Cliente | `IMongoClient` singleton por proceso |
| Dinero | `decimal` → BSON `Decimal128` (default del driver 3.x). Nunca `double` |
| Tracing | El driver expone OpenTelemetry desde 3.7; se usa esa integración en vez de instrumentar a mano |
| Testing | MongoDB real vía Testcontainers. Sin mocks del driver |

### Por qué 3.11.1 exacta

- Corrige CVE-2026-81527, 81528, 81529 y 81530.
- El soporte de .NET 10 y C# 14 entra recién en 3.6/3.7.
- 3.5 y 3.10 introdujeron breaking changes **dentro** del major: un rango flotante
  `[3.11.1,4.0.0)` no es seguro.

### Migración 2.x → 3.x (referencia)

Este proyecto nace en 3.x, pero al portar código o snippets externos:

| Tema | 2.x | 3.x |
|---|---|---|
| Ruta de upgrade | — | 2.x → **2.30** → 3.0. No se puede saltar |
| LINQ | LINQ2 + LINQ3 | solo LINQ3 |
| Queryable | `IMongoQueryable` | `IQueryable` |
| GUIDs | dos modos | solo V3; hay que registrar el serializer |
| `decimal` | BSON string | BSON `Decimal128` |
| `DateTimeOffset` | array | documento |
| `TimeOnly` | documento | int64 (ticks) |
| `MongoClient` / `Database` / `Collection` | heredables | **sellados**: usar interfaces |
| Proyecciones client-side | permitidas | fallan salvo opt-in |
| Paquete v1 `mongocsharpdriver` | existía | eliminado |
| TLS | 1.0/1.1 | mínimo 1.2 |
| AWS auth / encryption | incluidos | paquetes aparte |

Los tres silenciosos son `decimal`, `DateTimeOffset` y GUIDs: **no fallan al
compilar, fallan al leer**.

## Estado actual vs target

- **Estado:** no hay repositorios implementados. `W1-PTY-01` y `W1-PRP-01` son los
  primeros.
- **Target:** cada servicio registra cliente y serializadores una sola vez en su
  composition root, expone repositorios que implementan puertos del dominio, y
  crea sus índices al arrancar.

## Reglas obligatorias

### MUST

- **MUST** registrar `IMongoClient` como **singleton**. Es thread-safe y maneja su
  propio pool de conexiones.
- **MUST** registrar la configuración de serialización **una sola vez, en el
  arranque, antes de cualquier operación**. Registrar un serializer después de que
  el driver ya serializó ese tipo lanza excepción.
- **MUST** registrar el `GuidSerializer` con `GuidRepresentation.Standard`
  explícitamente. El driver 3.x no asume una representación por defecto.
- **MUST** depender de `IMongoClient`, `IMongoDatabase` e `IMongoCollection<T>`,
  nunca de las clases concretas: están selladas desde 3.0.
- **MUST** propagar `CancellationToken` a **toda** operación del driver.
- **MUST** incluir `tenantId` en el filtro de toda query y todo update.
- **MUST** crear los índices al arranque con `CreateManyAsync`, que es idempotente
  para definiciones equivalentes.
- **MUST** exponer el repositorio como implementación de un puerto declarado en el
  dominio o la aplicación, dentro del proyecto `*.Infrastructure`.
- **MUST** mapear los errores del driver a errores de dominio o de aplicación:
  `E11000` (duplicate key) no debe llegar crudo al usuario.
- **MUST** usar `decimal` para dinero. `double` no se usa para importes.

### MUST NOT

- **MUST NOT** instanciar `new MongoClient(...)` por request, por repositorio o por
  operación.
- **MUST NOT** usar `IMongoQueryable`: no existe en 3.x. Es `IQueryable`.
- **MUST NOT** habilitar `EnableClientSideProjections`, ni global ni por query, sin
  aprobación explícita del equipo.
- **MUST NOT** materializar la colección para filtrar en memoria
  (`.ToListAsync()` seguido de `.Where(...)`).
- **MUST NOT** hacer mocks de `IMongoCollection<T>` ni de los tipos del driver: los
  tests de repositorio corren contra MongoDB real.
- **MUST NOT** usar transacciones multi-documento salvo necesidad demostrada y
  documentada.
- **MUST NOT** exponer `IQueryable` fuera del repositorio: filtra el ownership y
  arrastra infraestructura hacia arriba.
- **MUST NOT** declarar la versión del driver en un `.csproj` individual.
- **MUST NOT** copiar snippets de driver 2.x sin verificarlos contra la tabla de
  migración.

## Recomendaciones

### SHOULD

- **SHOULD** proyectar a DTO en la query cuando no se necesita el documento
  completo: menos ancho de banda y menos deserialización.
- **SHOULD** aplicar `BsonIgnoreExtraElements` en los documentos para que un campo
  nuevo escrito por una versión más reciente no rompa la lectura.
- **SHOULD** centralizar `ConventionPack` y serializadores en un módulo compartido
  de infraestructura, para que todos los servicios se comporten igual.
- **SHOULD** usar `FindOneAndUpdate` con `ReturnDocument.After` cuando se necesita
  el estado resultante, en vez de leer y volver a escribir.
- **SHOULD** apoyarse en la integración OpenTelemetry del driver para las trazas de
  base de datos.
- **SHOULD** dejar activos los reintentos del driver (`retryWrites`, `retryReads`),
  que son seguros porque las escrituras son idempotentes por diseño.

### SHOULD NOT

- **SHOULD NOT** usar `BsonDocument` como tipo de trabajo cuando existe un POCO: se
  pierde tipado y validación.
- **SHOULD NOT** poner atributos `[Bson*]` en las clases del dominio: los documentos
  son tipos propios de `Infrastructure`.
- **SHOULD NOT** abrir sesiones ni transacciones para leer.

## Anti-patrones prohibidos

### 1. Cliente por request

```csharp
// ❌ Un pool nuevo por request: agota sockets y mata la latencia.
public class PartyRepository : IPartyRepository
{
    public async Task<Party?> GetAsync(PartyId id, CancellationToken ct)
    {
        var client = new MongoClient(_connectionString);
        // ...
    }
}
```

```csharp
// ✅ Singleton en el composition root; la colección se resuelve barata.
services.AddSingleton<IMongoClient>(_ =>
{
    var settings = MongoClientSettings.FromConnectionString(cfg.ConnectionString);
    return new MongoClient(settings);
});

services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>()
    .GetDatabase(cfg.Database));
```

### 2. No configurar la serialización al arranque

```csharp
// ❌ Sin registro explícito: los Guid se guardan de forma no determinada y
//    cualquier registro posterior explota.
var client = new MongoClient(connectionString);
```

```csharp
// ✅ Una sola vez, antes de cualquier operación.
public static class MongoSerializationSetup
{
    private static int _done;

    public static void Configure()
    {
        if (Interlocked.Exchange(ref _done, 1) == 1) return;

        // Guid binario, subtype 4. Decisión del proyecto.
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

        var pack = new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new IgnoreExtraElementsConvention(true),
            new EnumRepresentationConvention(BsonType.String)
        };

        ConventionRegistry.Register("crm-conventions", pack, _ => true);
    }
}
```

### 3. API de la 2.x

```csharp
// ❌ IMongoQueryable no existe en 3.x.
IMongoQueryable<PartyDocument> q = collection.AsQueryable();
```

```csharp
// ✅ IQueryable, como cualquier otro proveedor LINQ.
IQueryable<PartyDocument> q = collection.AsQueryable()
    .Where(p => p.TenantId == tenantId && p.Status == PartyStatus.Active);
```

### 4. Filtrar en memoria

```csharp
// ❌ Trae la colección entera y filtra en el proceso.
var all = await _col.Find(FilterDefinition<PropertyDocument>.Empty).ToListAsync(ct);
var mine = all.Where(p => p.TenantId == tenantId && p.City == city).ToList();
```

```csharp
// ✅ El filtro viaja al servidor y usa el índice.
var filter = Builders<PropertyDocument>.Filter.And(
    Builders<PropertyDocument>.Filter.Eq(p => p.TenantId, tenantId),
    Builders<PropertyDocument>.Filter.Eq(p => p.City, city));

var mine = await _col.Find(filter)
    .SortByDescending(p => p.UpdatedAt)
    .Limit(pageSize)
    .ToListAsync(ct);
```

### 5. Índices creados fuera del código

```csharp
// ❌ Creados a mano en una consola: no existen en CI, ni en la máquina del que
//    clona el repo, ni en producción si alguien se olvida.
```

```csharp
// ✅ Idempotente, al arranque del servicio.
public async Task EnsureIndexesAsync(CancellationToken ct)
{
    var models = new[]
    {
        new CreateIndexModel<PartyDocument>(
            Builders<PartyDocument>.IndexKeys
                .Ascending(p => p.TenantId)
                .Ascending(p => p.Status)
                .Descending(p => p.UpdatedAt),
            new CreateIndexOptions { Name = "tenant_status_updated" }),

        new CreateIndexModel<PartyDocument>(
            Builders<PartyDocument>.IndexKeys
                .Ascending(p => p.TenantId)
                .Ascending(p => p.TaxId),
            new CreateIndexOptions<PartyDocument>
            {
                Name = "tenant_taxid_unique",
                Unique = true,
                PartialFilterExpression =
                    Builders<PartyDocument>.Filter.Exists(p => p.TaxId, true)
            })
    };

    await _col.Indexes.CreateManyAsync(models, ct);
}
```

### 6. Mockear el driver

```csharp
// ❌ Sellado, y además testea el mock, no el mapeo ni los índices.
var mock = new Mock<IMongoCollection<PartyDocument>>();
```

```csharp
// ✅ MongoDB real y efímero. El mock va en el puerto, un nivel más arriba.
await using var mongo = new MongoDbBuilder().WithImage("mongo:8").Build();
await mongo.StartAsync();

var repo = new PartyRepository(new MongoClient(mongo.GetConnectionString()), "test");
await repo.EnsureIndexesAsync(CancellationToken.None);
```

### 7. Error del driver sin traducir

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
    throw new PartyAlreadyExistsException(document.TaxId);
}
```

## Checklist antes de devolver código

- [ ] `IMongoClient` registrado como singleton; ningún `new MongoClient` fuera del
      composition root.
- [ ] Serializadores y conventions registrados una sola vez al arranque, antes de
      cualquier operación.
- [ ] `GuidSerializer` con `GuidRepresentation.Standard` registrado.
- [ ] Se usan las interfaces del driver, no las clases selladas.
- [ ] `CancellationToken` en todas las llamadas.
- [ ] `tenantId` en todos los filtros.
- [ ] Ningún `IQueryable` cruza el borde del repositorio.
- [ ] Ningún filtrado en memoria sobre resultados materializados.
- [ ] Índices creados desde código de forma idempotente.
- [ ] Errores del driver traducidos a errores de dominio.
- [ ] Tests de repositorio contra MongoDB real, sin mocks del driver.
- [ ] Ningún snippet de driver 2.x sin verificar contra la tabla de migración.
- [ ] La versión del driver no está declarada en el `.csproj`.

## Conexiones con otros skills

| Skill | Relación |
|---|---|
| `mongodb-document-modeling` | Define documento e índices. Esta skill los escribe en C#. |
| `ddd-hexagonal-architecture` | El repositorio implementa un puerto; los documentos viven en `Infrastructure`, no en `Domain`. |
| `dotnet-async-and-concurrency` | `CancellationToken`, `async` hasta el borde, sin `.Result` ni `.Wait()`. |
| `dotnet-unit-testing` | Tests de repositorio con Testcontainers; los mocks van en el puerto. |
| `aspnetcore-config-and-secrets` | El connection string es un secreto: viene de configuración, nunca hardcodeado. |
| `aspnetcore-error-and-observability` | Trazas del driver vía OpenTelemetry; sin PII en los logs de query. |
| `multitenancy-authorization` | *Pendiente.* De dónde sale el `tenantId` que acá se exige en cada filtro. |
| `event-driven-outbox-inbox` | *Pendiente.* La escritura del outbox comparte el boundary con el aggregate. |
