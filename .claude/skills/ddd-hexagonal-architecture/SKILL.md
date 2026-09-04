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
  "referencias entre servicios", "leer datos de otro servicio", "nuevo
  microservicio", "dependency rule", "anemic model", "modelo anémico",
  "consistencia", "transacción entre servicios", "read model", "proyección".
  Garantiza que el dominio no dependa de MongoDB, RabbitMQ ni Keycloak, que cada
  colección tenga un único servicio owner, que ningún servicio lea datos de otro y
  que las invariantes vivan en el aggregate y no en un controller o un servicio
  anémico. NO activar para: detalles de la capa REST, mapeo de documentos MongoDB,
  configuración de infraestructura ni UI.
---

# DDD + Arquitectura Hexagonal

## Objetivo

Este CRM se distribuye en ~20 servicios de dominio que comparten una instancia de
MongoDB y un broker. Sin límites explícitos, esa topología degenera en un monolito
distribuido: servicios que leen colecciones ajenas, lógica de negocio repartida en
controllers, y aggregates que son bolsas de propiedades con getters y setters.

Esta skill define **dónde vive cada cosa** y **qué puede depender de qué**. Es la
skill base de todo servicio de dominio: se aplica antes que cualquier decisión de
persistencia, transporte o mensajería.

Fuentes de verdad: [`README.md`](../../../README.md) (dominio),
[`ARCHITECTURE.md`](../../../ARCHITECTURE.md) §2, §6, §7, §9 y
[`AGENTS.md`](../../../AGENTS.md).

## Cuándo activar

- Se crea un servicio de dominio nuevo o se define su estructura de carpetas.
- Se modela un aggregate, entidad o value object.
- Se decide dónde poner una regla de negocio.
- Se necesita un dato que pertenece a otro bounded context.
- Se define un puerto o se escribe un adapter.
- Se discute si algo debe ser evento de dominio o de integración.
- Se evalúa crear un microservicio nuevo.
- Aparece una operación que abarca dos aggregates.

## Cuándo NO activar

- Detalles de controllers, rutas o contratos HTTP (ver `aspnetcore-rest-layer`).
- Mapeo concreto a documentos, índices o queries MongoDB (ver las skills de
  MongoDB, pendientes en [`SKILL_GAPS.md`](../../../docs/skills/SKILL_GAPS.md)).
- Configuración, secretos, observabilidad o pipeline de middleware.
- Frontend y UX.

## Decisiones del proyecto

Estas decisiones ya están tomadas. No se re-litigan dentro de una task.

| Tema | Decisión |
|---|---|
| Límites | Bounded contexts según el Context Map de `README.md` §3. |
| Servicios | Por capacidad de negocio, no por entidad. El catálogo está en `ARCHITECTURE.md` §6. |
| Capas | Hexagonal dentro de cada servicio: `Domain` → `Application` → `Infrastructure` / `Api`. |
| Persistencia | MongoDB, una colección por raíz de agregado, un único servicio owner por colección. |
| Integración | HTTP para queries sincrónicas puntuales; RabbitMQ para hechos ocurridos. |
| Consistencia | Fuerte dentro del aggregate. Eventual entre aggregates y entre servicios. |
| CQRS | Selectivo. Writes al servicio owner; read models solo para vistas compuestas. |
| Multi-tenancy | `tenantId` en todo dato de negocio, derivado del contexto autenticado. |

> **Bounded context ≠ microservicio desplegable.** Son fronteras semánticas. Varios
> contextos pueden vivir en un mismo proceso; lo que nunca se relaja es el
> ownership de datos ni la dirección de las dependencias.

## Estado actual vs target

- **Estado:** el repositorio todavía no tiene código. `FND-001` crea la estructura
  y `FND-003` los contratos compartidos.
- **Target:** cada servicio expone su dominio en un proyecto sin dependencias de
  infraestructura, y la infraestructura implementa puertos declarados por el
  dominio o la aplicación.

Estructura de referencia de un servicio:

```text
services/party-service/
├── Party.Domain/          # aggregates, VOs, invariantes, eventos de dominio, puertos
├── Party.Application/     # commands, queries, handlers, orquestación de casos de uso
├── Party.Infrastructure/  # repositorios MongoDB, publisher RabbitMQ, clientes HTTP
└── Party.Api/             # controllers, DI, composition root
```

`Party.Domain` **no referencia ningún paquete de infraestructura**. Si necesita
compilar contra `MongoDB.Driver`, el diseño está mal.

## Reglas obligatorias

### MUST

- **MUST** poner las invariantes dentro del aggregate root. Un aggregate expone
  métodos que expresan operaciones del negocio (`Party.AddContactPoint(...)`), no
  setters públicos.
- **MUST** tratar el aggregate como unidad de consistencia: una operación de
  negocio modifica **un** aggregate y se persiste atómicamente.
- **MUST** declarar los puertos (`IPartyRepository`, `IEventPublisher`,
  `IObjectStorage`, `IClock`) en `Domain` o `Application`, e implementarlos en
  `Infrastructure`.
- **MUST** referenciar aggregates de otros contextos **solo por su ID público**.
- **MUST** obtener datos de otro contexto por su API pública, por un evento
  consumido o por un read model propio alimentado por eventos.
- **MUST** incluir `tenantId` en todo aggregate de negocio y en todo filtro de
  lectura.
- **MUST** modelar como value object todo concepto sin identidad propia: `Money`,
  `Address`, `GeoPoint`, `TaxId`, `DateRange`. Inmutables, con igualdad por valor.
- **MUST** validar en el constructor o factory del aggregate: no debe existir una
  instancia inválida.
- **MUST** distinguir evento de dominio (interno al servicio, puede ser rico) de
  evento de integración (público, versionado, payload mínimo, sin PII innecesaria).
- **MUST** abrir un [ADR](../../../docs/adr/README.md) antes de crear un
  microservicio, mover ownership de una colección o cambiar una invariante core.

### MUST NOT

- **MUST NOT** leer o escribir una colección cuyo owner es otro servicio, aunque
  compartan la misma instancia de MongoDB.
- **MUST NOT** compartir entidades de dominio entre servicios vía `packages/*` o
  proyectos comunes. Los contratos se comparten; el dominio no.
- **MUST NOT** poner reglas de negocio en controllers, en el BFF ni en un
  repositorio.
- **MUST NOT** exponer colecciones mutables ni setters públicos desde un aggregate.
- **MUST NOT** abarcar dos aggregates en una misma transacción. Si hace falta,
  el límite del aggregate está mal o corresponde una saga por eventos. El outbox no
  cuenta: son los eventos del mismo aggregate y es la única excepción sancionada.
- **MUST NOT** publicar un evento de integración que sea un comando disfrazado
  (`SendWelcomeEmail` no es un evento; `PartyRegistered` sí).
- **MUST NOT** inventar campos obligatorios que el dominio define opcionales, ni
  convertir `UNKNOWN` en `false`, `0` o colección vacía.
- **MUST NOT** tratar un read model como fuente de verdad ni enviarle comandos.
- **MUST NOT** permitir que la IA acceda a MongoDB: opera por APIs, commands,
  queries autorizadas, eventos y tools declaradas.

## Recomendaciones

### SHOULD

- **SHOULD** mantener los aggregates chicos. La pregunta guía es qué debe ser
  consistente *en el mismo instante*, no qué se lee junto.
- **SHOULD** nombrar tipos y métodos con el lenguaje ubicuo del `README.md`:
  `CaptationCase`, `Requirement`, `Listing`, `Proposal`. Nunca `Lead`, `Opportunity`
  genérica ni `Customer`.
- **SHOULD** usar un command handler por caso de uso, delgado: cargar aggregate,
  invocar método de dominio, persistir, publicar.
- **SHOULD** mantener las queries de lectura fuera del aggregate; pueden proyectar
  directo a DTO sin rehidratar el modelo rico.
- **SHOULD** hacer explícito lo desconocido con un tipo o estado propio, en línea
  con la distinción `UNKNOWN` vs valor confirmado del dominio.
- **SHOULD** ubicar el `correlationId` y el actor en el contexto de aplicación, no
  en el dominio.

### SHOULD NOT

- **SHOULD NOT** crear un servicio por entidad. `property-service` cubre Property,
  jerarquías, intereses y datos registrales; no hay `property-interest-service`.
- **SHOULD NOT** introducir un repositorio genérico `IRepository<T>` con `IQueryable`
  expuesto: filtra el ownership y arrastra infraestructura al dominio.
- **SHOULD NOT** llamar sincrónicamente a otro servicio dentro de un command handler
  si el dato puede llegar por evento.
- **SHOULD NOT** versionar un evento público con cambios incompatibles: se publica
  una versión nueva.

## Anti-patrones prohibidos

### 1. Modelo anémico

```csharp
// ❌ El aggregate es una bolsa de datos y la regla vive afuera.
public class Listing
{
    public ListingStatus Status { get; set; }
    public DateOnly? PublishedAt { get; set; }
}

public class ListingService
{
    public void Activate(Listing listing)
    {
        if (listing.Status == ListingStatus.Closed)
            throw new InvalidOperationException("cerrado");
        listing.Status = ListingStatus.Active;
        listing.PublishedAt = DateOnly.FromDateTime(DateTime.UtcNow);
    }
}
```

```csharp
// ✅ La invariante vive donde vive el estado.
public sealed class Listing
{
    public ListingStatus Status { get; private set; }
    public DateOnly? PublishedAt { get; private set; }

    public void Activate(DateOnly today)
    {
        if (Status is ListingStatus.Closed)
            throw new ListingClosedException(Id);

        Status = ListingStatus.Active;
        PublishedAt = today;
        Raise(new ListingActivated(Id, TenantId, today));
    }
}
```

### 2. Leer la colección de otro servicio

```csharp
// ❌ matching-service entrando a las colecciones de property-service.
var property = await _mongo
    .GetDatabase("crm")
    .GetCollection<PropertyDocument>("property_properties")
    .Find(p => p.Id == propertyId)
    .FirstOrDefaultAsync(ct);
```

```csharp
// ✅ Puerto propio, resuelto por API pública o por un read model alimentado por eventos.
public interface IPropertySnapshotPort
{
    Task<PropertySnapshot?> GetAsync(PropertyId id, CancellationToken ct);
}
```

### 3. Infraestructura dentro del dominio

```csharp
// ❌ El dominio compila contra el driver.
namespace Party.Domain;

using MongoDB.Bson.Serialization.Attributes;

public class Party
{
    [BsonId] public ObjectId Id { get; set; }
}
```

```csharp
// ✅ El dominio ignora cómo se guarda. El mapeo vive en Infrastructure.
namespace Party.Domain;

public sealed class Party
{
    public PartyId Id { get; }
    public TenantId TenantId { get; }
}
```

### 4. Transacción que abarca dos aggregates

```csharp
// ❌ Dos aggregates, una transacción, invariante inventada.
using var session = await _client.StartSessionAsync(cancellationToken: ct);
session.StartTransaction();
await _reservations.SaveAsync(reservation, ct);
await _listings.MarkAsReservedAsync(listingId, ct);
await session.CommitTransactionAsync(ct);
```

```csharp
// ✅ Un aggregate por transacción; el otro reacciona al evento.
await _reservations.SaveWithOutboxAsync(reservation, events, ct);  // aggregate + outbox: excepción sancionada
// listing-service consume ReservationConfirmed y decide qué hacer con el Listing.
```

### 5. Convertir lo desconocido en un valor

```csharp
// ❌ "No sabemos si acepta mascotas" se vuelve "no acepta".
bool petsAllowed = requirement.PetsAllowed ?? false;
```

```csharp
// ✅ Desconocido es un estado del dominio, no un default.
CriterionValue<bool> pets = requirement.PetsAllowed; // Confirmed(true|false) | Unknown
```

### 6. Evento de integración que es un comando

```csharp
// ❌ El emisor decide qué hace el receptor.
await _publisher.PublishAsync(new SendWelcomeEmail(partyId, email), ct);
```

```csharp
// ✅ Hecho ocurrido, payload mínimo. El consumidor decide.
await _publisher.PublishAsync(new PartyRegistered(partyId, tenantId, occurredAt), ct);
```

## Checklist antes de devolver código

- [ ] El proyecto `*.Domain` no referencia MongoDB, RabbitMQ, Keycloak, ASP.NET
      Core ni ningún cliente HTTP.
- [ ] Cada regla de negocio de la task vive en un aggregate, no en un handler ni en
      un controller.
- [ ] Ninguna operación toca una colección de otro servicio.
- [ ] Ningún aggregate expone setters públicos ni colecciones mutables.
- [ ] Una operación de negocio = un aggregate modificado.
- [ ] `tenantId` presente en el aggregate y en todos los filtros.
- [ ] Los eventos publicados son hechos ocurridos, versionados y con payload mínimo.
- [ ] Los nombres coinciden con el lenguaje ubicuo del `README.md`.
- [ ] Ningún campo opcional del dominio se volvió obligatorio.
- [ ] Ningún `UNKNOWN` se degradó a `false`, `0` o lista vacía.
- [ ] Si algo requirió cambiar ownership, invariante o límites de servicio, hay un
      ADR abierto y la task está frenada.

## Conexiones con otros skills

| Skill | Relación |
|---|---|
| `aspnetcore-rest-layer` | El controller traduce HTTP a command/query. La regla de negocio nunca vive ahí. |
| `aspnetcore-messaging` | Patrones agnósticos de consumo. El *qué* se publica lo define esta skill. |
| `dotnet-parsing-and-validation` | Valida forma del input en el borde. Las invariantes de negocio son de esta skill. |
| `dotnet-unit-testing` | Los aggregates son el mejor lugar para testear sin infraestructura. |
| `mongodb-document-modeling` | Traduce estas fronteras a documentos, embed vs reference e índices. |
| `event-driven-outbox-inbox` | Cómo se publica confiablemente lo que este skill decide publicar. |
| `multitenancy-authorization` | De dónde sale `tenantId` y cómo se autoriza. |
| `cqrs-read-models-projections` | Cuándo un read model se justifica y cómo se reconstruye. |
