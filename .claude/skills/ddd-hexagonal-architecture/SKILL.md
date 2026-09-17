---
name: ddd-hexagonal-architecture
description: |
  Activa cuando se decide dónde vive una pieza de código en un servicio de dominio
  de este CRM: qué es aggregate, entidad o value object, qué invariante protege
  quién, qué capa puede depender de qué, cómo se comunican dos bounded contexts y
  qué se publica como evento. Triggers: "aggregate", "aggregate root", "raíz de
  agregado", "entidad", "value object", "invariante", "bounded context", "contexto",
  "ownership", "quién es el owner", "dónde va esta lógica", "capa de dominio",
  "capa de aplicación", "application service", "domain service", "command",
  "command handler", "query", "query handler", "puerto", "port", "adapter",
  "ports and adapters", "hexagonal", "IRepository", "interfaz de repositorio",
  "domain event", "evento de dominio", "integration event", "evento de
  integración", "publicar un evento", "estructura de carpetas del servicio",
  "referencias entre servicios", "leer datos de otro servicio", "dependency rule",
  "anemic model", "modelo anémico", "consistencia", "transacción entre servicios".
  Garantiza que el dominio no dependa de MongoDB, RabbitMQ ni Keycloak, que cada
  colección tenga un único servicio owner, que ningún servicio lea datos de otro
  salvo por API/evento (con la excepción explícita de catálogos por HTTP, D7), y
  que las invariantes vivan en el aggregate y no en un controller. NO activar para:
  detalles de la capa REST, mapeo de documentos MongoDB, configuración de
  infraestructura ni UI.
---

# DDD + Arquitectura Hexagonal

## Objetivo

Este CRM se distribuye en los 11 servicios de dominio de V2-FND-001, que comparten
una instancia de MongoDB y un broker. Sin límites explícitos, esa topología
degenera en un monolito distribuido: servicios que leen colecciones ajenas, lógica
de negocio dispersa en controllers, y aggregates que son bolsas de campos
con getters y setters.

Esta skill define **dónde vive cada cosa** y **qué puede depender de qué**. Es la
skill base de todo servicio de dominio: se aplica antes que cualquier decisión de
persistencia, transporte o mensajería.

Fuentes: `PRPs/_backlog/2026-09-17-plan-wave-2-acceso-catalogos-party.md` (§0, §5
D7), `IMPLEMENTATION_REPORT-V2-FND-002.md`.

## Cuándo activar

- Se crea un servicio de dominio nuevo o se define su estructura de carpetas.
- Se modela un aggregate, entidad o value object.
- Se decide dónde poner una regla de negocio.
- Se necesita un dato que pertenece a otro bounded context.
- Se define un puerto o se escribe un adapter.
- Se discute si algo debe ser evento de dominio o de integración.
- Aparece una operación que abarca dos aggregates.

## Cuándo NO activar

- Detalles de controllers, rutas o contratos HTTP (ver `aspnetcore-rest-layer`).
- Mapeo concreto a documentos, índices o queries MongoDB (ver
  `mongodb-document-modeling`).
- Configuración, secretos u observabilidad.
- Frontend y UX.

## Decisiones del proyecto

Estas decisiones ya están tomadas. No se re-litigan dentro de una task.

| Tema | Decisión |
|---|---|
| Servicios | Los 11 servicios de dominio + 2 BFF de `V2-FND-001` (`RealEstateCrm.slnx`), por capacidad de negocio, no por entidad |
| Capas | Hexagonal dentro de cada servicio: `Domain` → `Application` → `Infrastructure` / `Api` |
| Persistencia | MongoDB, una colección por raíz de agregado, un único servicio owner por colección |
| Repositorio | `IRepository<TAggregate, TId>` (`building-blocks/RealEstateCrm.BuildingBlocks/Persistence`) es el puerto ya definido: `GetByIdAsync`/`AddAsync`/`UpdateAsync`, sin exponer `IQueryable` |
| Integración | HTTP para queries sincrónicas puntuales; RabbitMQ para hechos ocurridos |
| Excepción de lectura entre servicios (D7) | Consumir catálogos de `platform-config-service` por HTTP síncrono con caché corta **está permitido y es la forma recomendada** — no hace falta esperar el evento `CatalogVersionPublished` para leer |
| Consistencia | Fuerte dentro del aggregate. Eventual entre aggregates y entre servicios |
| Alcance de instalación | Una única inmobiliaria: sin aislamiento entre organizaciones ni ese tipo de identificador en aggregates o puertos |

## Estado actual vs target

- **Estado:** `RealEstateCrm.slnx` tiene los 11 servicios × 4 capas + 2 BFF
  (`V2-FND-001`); `contracts` y los puertos de `building-blocks` existen
  (`V2-FND-002`). Ningún servicio tiene aggregates reales todavía.
- **Target:** cada servicio expone su dominio en un proyecto sin dependencias de
  infraestructura, y la infraestructura implementa puertos declarados por el
  dominio o la aplicación.

Estructura de referencia de un servicio:

```text
services/party-service/
├── PartyService.Domain/          # aggregates, VOs, invariantes, eventos de dominio, puertos
├── PartyService.Application/     # commands, queries, handlers, orquestación de casos de uso
├── PartyService.Infrastructure/  # repositorios MongoDB, publisher RabbitMQ, clientes HTTP
└── PartyService.Api/             # controllers, DI, composition root
```

`PartyService.Domain` **no referencia ningún paquete de infraestructura**. Si
necesita compilar contra `MongoDB.Driver`, el diseño está mal. Tampoco referencia
`RealEstateCrm.BuildingBlocks.Infrastructure` directamente: hay una regla de
arquitectura (`Application_projects_do_not_reference_BuildingBlocks_Infrastructure`,
`tests/RealEstateCrm.ArchitectureTests`) que lo impide desde `Application`.

## Reglas obligatorias

### MUST

- **MUST** poner las invariantes dentro del aggregate root. Un aggregate expone
  métodos que expresan operaciones del negocio (`Party.ChangeCommercialStatus(...)`),
  no setters públicos.
- **MUST** tratar el aggregate como unidad de consistencia: una operación de
  negocio modifica **un** aggregate y se persiste atómicamente.
- **MUST** declarar los puertos propios del servicio (ej. `IPartySnapshotPort`) en
  `Domain` o `Application`, e implementarlos en `Infrastructure`.
- **MUST** referenciar aggregates de otros contextos **solo por su ID público**.
- **MUST** obtener datos de otro contexto por su API pública o por un evento
  consumido — salvo el caso ya decidido de catálogos por HTTP con caché (D7).
- **MUST** validar en el constructor o factory del aggregate: no debe existir una
  instancia inválida.
- **MUST** distinguir evento de dominio (interno al servicio) de evento de
  integración (público, versionado vía `EventEnvelopeV1<TPayload>`, payload
  mínimo, sin PII innecesaria).
- **MUST** implementar el repositorio del aggregate contra `IRepository<TAggregate,
  TId>` (o extender `MongoRepository<TAggregate, TId>`), sin exponer `IQueryable`
  fuera de `Infrastructure`.

### MUST NOT

- **MUST NOT** leer o escribir una colección cuyo owner es otro servicio, aunque
  compartan la misma instancia de MongoDB.
- **MUST NOT** compartir entidades de dominio entre servicios vía proyectos
  comunes. Los contratos (`contracts/`) se comparten; el dominio no.
- **MUST NOT** poner reglas de negocio en controllers, en el BFF ni en un
  repositorio.
- **MUST NOT** exponer colecciones mutables ni setters públicos desde un aggregate.
- **MUST NOT** abarcar dos aggregates en una misma transacción. La única excepción
  sancionada es aggregate + outbox del mismo servicio (ver
  `event-driven-outbox-inbox`).
- **MUST NOT** publicar un evento de integración que sea un comando disfrazado
  (`SendWelcomeEmail` no es un evento; `PartyRegistered` sí).
- **MUST NOT** convertir un dato opcional del dominio en obligatorio, ni un estado
  desconocido en `false`, `0` o colección vacía.
- **MUST NOT** llamar sincrónicamente a otro servicio dentro de un command handler
  si el dato puede llegar por evento — salvo la excepción explícita de D7
  (consulta de catálogos con caché corta).

## Recomendaciones

### SHOULD

- **SHOULD** mantener los aggregates chicos. La pregunta guía es qué debe ser
  consistente *en el mismo instante*, no qué se lee junto.
- **SHOULD** usar un command handler por caso de uso, delgado: cargar aggregate,
  invocar método de dominio, persistir, publicar.
- **SHOULD** mantener las queries de lectura fuera del aggregate; pueden proyectar
  directo a DTO sin rehidratar el modelo rico.
- **SHOULD** guardar `originCode`/`catalogVersion` (o equivalente) junto al dato
  consultado a `platform-config-service`, para que quede registrado con qué
  versión de catálogo se validó (D7).

### SHOULD NOT

- **SHOULD NOT** crear un servicio por entidad: la unidad de despliegue es la
  capacidad de negocio, ya fijada en `V2-FND-001`.
- **SHOULD NOT** versionar un evento público con cambios incompatibles: se publica
  una versión nueva.

## Anti-patrones prohibidos

### 1. Modelo anémico

```csharp
// ❌ El aggregate es una bolsa de datos y la regla vive afuera.
public class CatalogEntry
{
    public bool Active { get; set; }
}

public class CatalogEntryService
{
    public void Deactivate(CatalogEntry entry)
    {
        entry.Active = false;
    }
}
```

```csharp
// ✅ La invariante vive donde vive el estado.
public sealed class CatalogEntry
{
    public bool Active { get; private set; }

    public void Deactivate()
    {
        if (!Active) throw new CatalogEntryAlreadyInactiveException(Id);
        Active = false;
        Raise(new CatalogEntryDeactivated(Id, DateTimeOffset.UtcNow));
    }
}
```

### 2. Leer la colección de otro servicio

```csharp
// ❌ party-service entrando directo a la colección de platform-config-service.
var entry = await _mongo
    .GetDatabase("crm_platform_config")
    .GetCollection<CatalogEntryDocument>("catalog_entries")
    .Find(e => e.Code == originCode)
    .FirstOrDefaultAsync(ct);
```

```csharp
// ✅ Cliente HTTP del catálogo, con caché corta (D7).
public interface ICommercialOriginCatalogPort
{
    Task<CatalogEntrySnapshot?> GetActiveAsync(string code, CancellationToken ct);
}
```

### 3. Infraestructura dentro del dominio

```csharp
// ❌ El dominio compila contra el driver.
namespace PartyService.Domain;

using MongoDB.Bson.Serialization.Attributes;

public class Party
{
    [BsonId] public ObjectId Id { get; set; }
}
```

```csharp
// ✅ El dominio ignora cómo se guarda. El mapeo vive en Infrastructure.
namespace PartyService.Domain;

public sealed class Party
{
    public Guid Id { get; }
}
```

### 4. Transacción que abarca dos aggregates

```csharp
// ❌ Dos aggregates de servicios distintos, una transacción: no es posible ni
//    deseable en este diseño.
using var session = await _client.StartSessionAsync(cancellationToken: ct);
session.StartTransaction();
await _parties.UpdateAsync(party, ct);
await _catalogEntries.UpdateAsync(entry, ct);
await session.CommitTransactionAsync(ct);
```

```csharp
// ✅ Un aggregate por transacción; el otro reacciona al evento o se consulta por
//    API/HTTP (D7 para catálogos).
await _unitOfWork.ExecuteInTransactionAsync(async ct2 =>
{
    await _repository.UpdateAsync(party, ct2);
    await _outbox.EnqueueAsync(OutboxMessage.From(envelope, DateTimeOffset.UtcNow), ct2);
}, ct);
```

### 5. Convertir lo desconocido en un valor

```csharp
// ❌ "No sabemos el dato de contacto adicional" se vuelve "no tiene".
string? secondaryContact = request.SecondaryContact ?? "";
```

```csharp
// ✅ Ausente es ausente; no se inventa un valor por defecto.
string? secondaryContact = request.SecondaryContact; // null es un estado válido
```

### 6. Evento de integración que es un comando

```csharp
// ❌ El emisor decide qué hace el receptor.
await _publisher.PublishAsync(new SendWelcomeEmail(userId, email), ct);
```

```csharp
// ✅ Hecho ocurrido, payload mínimo. El consumidor decide.
await _outbox.EnqueueAsync(OutboxMessage.From(
    new EventEnvelopeV1<UserCreatedPayload>(...), DateTimeOffset.UtcNow), ct);
```

## Checklist antes de devolver código

- [ ] El proyecto `*.Domain` no referencia MongoDB, RabbitMQ, Keycloak, ASP.NET
      Core ni `BuildingBlocks.Infrastructure`.
- [ ] Cada regla de negocio de la task vive en un aggregate, no en un handler ni en
      un controller.
- [ ] Ninguna operación toca una colección de otro servicio (salvo la excepción de
      catálogos por HTTP con caché, D7).
- [ ] Ningún aggregate expone setters públicos ni colecciones mutables.
- [ ] Una operación de negocio = un aggregate modificado.
- [ ] Los eventos publicados son hechos ocurridos, con `EventEnvelopeV1<TPayload>`
      y payload mínimo.
- [ ] Ningún campo opcional del dominio se volvió obligatorio.
- [ ] Ningún dato desconocido se degradó a `false`, `0` o lista vacía.
- [ ] El repositorio implementa `IRepository<TAggregate, TId>` sin exponer
      `IQueryable`.

## Conexiones con otros skills

| Skill | Relación |
|---|---|
| `aspnetcore-rest-layer` | El controller traduce HTTP a command/query. La regla de negocio nunca vive ahí. |
| `mongodb-document-modeling` | Traduce estas fronteras a documentos, embed vs reference e índices. |
| `mongodb-dotnet-driver` | El repositorio implementa el puerto; los documentos viven en `Infrastructure`, no en `Domain`. |
| `event-driven-outbox-inbox` | Cómo se publica confiablemente lo que este skill decide publicar. |
| `dotnet-unit-testing` | Los aggregates son el mejor lugar para testear sin infraestructura. |
