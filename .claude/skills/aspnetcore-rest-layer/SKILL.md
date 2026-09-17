---
name: aspnetcore-rest-layer
description: |
  Activa cuando se crea o modifica la capa REST de un servicio de dominio o de
  operations-bff de este CRM: controllers, endpoints, el pipeline de request y el
  contrato de respuesta. Triggers: "[ApiController]", "[Route]", "[HttpGet]",
  "[HttpPost]", "[HttpPut]", "[HttpDelete]", "[HttpPatch]", "endpoint", "ruta",
  "[FromBody]", "[FromQuery]", "[FromRoute]", "[FromHeader]", "DTO", "request
  model", "response model", "DataAnnotations", "FluentValidation",
  "ControllerBase", "IActionResult", "Task<IActionResult>", "ActionResult<T>",
  "Minimal API", "contrato de respuesta", "status code", "404", "409", "422",
  "versionado de API", "paginación", "PageV1", "serializar respuesta",
  "ProblemDetails", "problem+json", "IExceptionHandler", "manejo de errores HTTP",
  "código de error", "error code", "controller delgado", "screens", "mutations",
  "operations-bff".
  Garantiza controllers sin lógica de negocio, validación en el borde, errores
  siempre en formato ProblemDetailsV1 con errorCode estable, status codes
  correctos (403 prohibido / 404 inexistente) y el contrato BFF↔crm-web de V2.
  NO activar para: reglas de dominio, acceso a datos, mensajería ni evaluación de
  permisos (solo delega en el puerto de autorización).
---

# Capa REST en ASP.NET Core

## Objetivo

El controller es un **traductor**: convierte HTTP en un command o una query, y el
resultado en una respuesta. Nada más.

Cuando se le agrega lógica, esa lógica queda fuera del dominio, sin tests de
aggregate y sin poder reutilizarse desde un consumidor de eventos.

Y cuando cada endpoint arma sus errores a mano, el frontend termina manejando
formatos distintos para lo mismo.

Fuentes: `PRPs/_backlog/2026-09-17-plan-wave-2-acceso-catalogos-party.md` (§3,
decisiones D1, D4, D5, D11), `IMPLEMENTATION_REPORT-V2-FND-002.md`.

## Cuándo activar

- Se crea o modifica un controller, un endpoint o una ruta.
- Se definen DTOs de request o response.
- Se decide qué status code corresponde.
- Se toca el manejo global de errores.
- Se implementan screens o mutations de `operations-bff`.

## Cuándo NO activar

- Reglas de negocio e invariantes (ver `ddd-hexagonal-architecture`).
- Acceso a datos (ver las skills de MongoDB).
- Mensajería y eventos (ver `event-driven-outbox-inbox`).
- Cómo se evalúa un permiso (el controller solo llama al puerto de autorización).

## Decisiones del proyecto

| Tema | Decisión |
|---|---|
| Base | `ControllerBase` de ASP.NET Core. No hay clase base propia en `building-blocks` |
| Estilo (D1) | Controllers `[ApiController]`, rutas `/api/v1/<recurso>` en cada servicio de dominio. Minimal API solo para health |
| Errores | `ProblemDetailsV1` (`contracts/RealEstateCrm.Contracts/Errors`) con `ErrorCode` estable: los 5 genéricos de `ErrorCodes` (`validation_error`, `unauthorized`, `forbidden`, `not_found`, `conflict`) o un código de dominio propio del servicio, snake_case con prefijo (D11: ej. `catalog_entry_inactive`, `party_not_found`) |
| Acción prohibida vs inexistente (D4) | Prohibido → **403** `forbidden`; inexistente → **404** `not_found` |
| Contrato BFF↔crm-web (D5) | `operations-bff` implementa el contrato que ya usa `crm-web`: `GET /screens/{screenId}` y `POST /mutations/{name}`, mapeando cada screen/mutation a llamadas REST a los servicios de dominio. `crm-web` no se reescribe |
| Autorización | El controller no evalúa permisos: llama al puerto de autorización (`IAuthorizationPort`, definido en `V2-ACL-001a`) y traduce el resultado a 403/200 |
| Paginación | Toda colección responde `PageV1<TItem>` (`contracts/RealEstateCrm.Contracts/Paging`); cada servicio fija y documenta su `pageSize` máximo |

### Formato de error

```json
{
  "type": "about:blank",
  "title": "No se pudo validar el token.",
  "status": 403,
  "detail": "El actor no tiene el permiso requerido.",
  "instance": "/api/v1/parties/9c0d1e2f-3a4b-4c5d-8e9f-0a1b2c3d4e44",
  "errorCode": "forbidden",
  "correlationId": "1a2b3c4d-5e6f-4a1b-9c2d-3e4f5a6b7c22"
}
```

`errorCode` es estable y accionable por el cliente. `title` y `detail` pueden
cambiar de redacción sin romper a nadie.

### Mapeo errorCode → status (mínimo transversal)

| `ErrorCodes` | Status |
|---|---|
| `validation_error` | 400 |
| `unauthorized` | 401 |
| `forbidden` | 403 |
| `not_found` | 404 |
| `conflict` | 409 |

Cada servicio agrega sus propios códigos de dominio (D11) mapeados al status que
corresponda (por ejemplo un código de regla de negocio violada puede ir a 422).

## Estado actual vs target

- **Estado:** no hay controllers todavía. `ProblemDetailsV1`/`ErrorCodes`/`PageV1<T>`
  existen en `contracts` (V2-FND-002); `AddCrmHealthChecks`/`AddCrmObservability`/
  `UseCrmCorrelationId` existen en `BuildingBlocks.Infrastructure` (V2-FND-003) pero
  **sin wire-up** en ningún `Program.cs`.
- **Target:** `V2-ACL-001` hace el primer wire-up real de un `Program.cs` (servicio y
  BFF), con auth, health, observabilidad y correlationId.

## Reglas obligatorias

### MUST

- **MUST** mantener el controller delgado: valida forma, traduce a command o query,
  devuelve.
- **MUST** centralizar el mapeo de excepciones/errores en un único mecanismo por
  servicio (ej. `IExceptionHandler` de ASP.NET Core), no repetirlo por endpoint.
- **MUST** devolver todos los errores como `ProblemDetailsV1` con `errorCode`
  estable.
- **MUST** validar la forma del input en el borde con DataAnnotations o
  FluentValidation.
- **MUST** propagar `CancellationToken` desde la acción hasta la capa de aplicación.
- **MUST** devolver **403** `forbidden` para una acción prohibida y **404**
  `not_found` para un recurso inexistente (D4) — nunca 404 para ocultar un 403.
- **MUST** paginar toda respuesta de colección con `PageV1<TItem>`, con `pageSize`
  máximo documentado por el servicio.
- **MUST** usar DTOs propios de la capa REST: nunca exponer el aggregate ni el
  documento de MongoDB.
- **MUST** implementar `operations-bff` sobre `GET /screens/{screenId}` y
  `POST /mutations/{name}` (D5), no como REST por recurso.

### MUST NOT

- **MUST NOT** poner reglas de negocio en el controller ni en el BFF.
- **MUST NOT** hacer `try/catch` en el controller para mapear errores: eso es del
  mecanismo centralizado.
- **MUST NOT** devolver stack traces, nombres de índice, connection strings ni PII.
- **MUST NOT** exponer entidades de dominio ni documentos de MongoDB como response.
- **MUST NOT** acceder a MongoDB desde un controller o desde el BFF.
- **MUST NOT** devolver 200 con un cuerpo que indique error.
- **MUST NOT** inventar un formato de error propio por endpoint.
- **MUST NOT** evaluar permisos con lógica propia del controller: siempre vía el
  puerto de autorización.

## Recomendaciones

### SHOULD

- **SHOULD** nombrar rutas por recurso y en plural: `/api/v1/parties`,
  `/api/v1/catalogs`.
- **SHOULD** devolver `201 Created` con `Location` al crear.
- **SHOULD** mantener el catálogo de `errorCode` del servicio versionado como
  código, junto al `Api`/`Application`.
- **SHOULD** usar `ActionResult<T>` para que el tipo de respuesta quede explícito.
- **SHOULD** documentar los endpoints con XML doc.

### SHOULD NOT

- **SHOULD NOT** crear un endpoint por cada campo editable: los commands de la task
  son los que definen la superficie.
- **SHOULD NOT** usar verbos en las rutas salvo acciones de negocio que no sean CRUD
  (`/api/v1/catalogs/{type}/publish`).

## Anti-patrones prohibidos

### 1. Lógica de negocio en el controller

```csharp
// ❌ La invariante queda fuera del dominio y no se puede testear ni reutilizar.
[HttpPost("{id}/deactivate")]
public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
{
    var entry = await _repo.GetByIdAsync(id, ct);
    if (entry.Active is false)
        return BadRequest("Ya está inactivo.");

    entry.Active = false;
    await _repo.UpdateAsync(entry, ct);
    return Ok();
}
```

```csharp
// ✅ Traduce y delega. La regla vive en el aggregate.
[HttpPost("{id}/deactivate")]
public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
{
    await _sender.SendAsync(new DeactivateCatalogEntry(id), ct);
    return NoContent();
}
```

### 2. Manejo de errores ad-hoc

```csharp
// ❌ Cada endpoint inventa su formato; el frontend maneja variantes distintas.
try
{
    await _sender.SendAsync(cmd, ct);
    return Ok();
}
catch (Exception ex)
{
    return BadRequest(new { error = ex.Message });
}
```

```csharp
// ✅ El controller no atrapa nada. El handler global traduce a ProblemDetailsV1.
await _sender.SendAsync(cmd, ct);
return NoContent();
```

```csharp
// Registrado una sola vez, por servicio (o compartido si building-blocks lo expone).
public sealed class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var (status, errorCode) = ex switch
        {
            CatalogEntryNotFoundException => (StatusCodes.Status404NotFound, "catalog_entry_not_found"),
            PermissionDeniedException     => (StatusCodes.Status403Forbidden, ErrorCodes.Forbidden),
            _                             => (StatusCodes.Status500InternalServerError, "internal_error")
        };

        var problem = new ProblemDetailsV1(
            Type: "about:blank",
            Title: _titles.For(errorCode),
            Status: status,
            Detail: null,
            Instance: ctx.Request.Path,
            ErrorCode: errorCode,
            CorrelationId: ctx.TraceIdentifier is { Length: > 0 } ? Guid.Parse(ctx.TraceIdentifier) : Guid.Empty);

        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}
```

### 3. Exponer el aggregate

```csharp
// ❌ El contrato público queda atado a la forma interna del dominio.
[HttpGet("{id}")]
public async Task<ActionResult<Party>> Get(Guid id, CancellationToken ct)
    => Ok(await _repo.GetByIdAsync(id, ct));
```

```csharp
// ✅ DTO propio de la capa REST.
[HttpGet("{id}")]
public async Task<ActionResult<PartyResponse>> Get(Guid id, CancellationToken ct)
    => Ok(await _queries.GetPartyAsync(id, ct));
```

### 4. Colección sin paginar

```csharp
// ❌ Una base con miles de parties tumba la respuesta.
[HttpGet]
public async Task<IActionResult> List(CancellationToken ct)
    => Ok(await _queries.ListAllAsync(ct));
```

```csharp
// ✅ PageV1<T> con tope documentado.
[HttpGet]
public async Task<ActionResult<PageV1<PartySummary>>> List(
    [FromQuery] int page, [FromQuery] int pageSize, CancellationToken ct)
    => Ok(await _queries.SearchAsync(page, Math.Min(pageSize, MaxPageSize), ct));
```

### 5. 404 en vez de 403

```csharp
// ❌ V2 usa 403 para prohibido; ocultarlo como 404 contradice D4.
if (!decision.Allowed) return NotFound();
```

```csharp
// ✅ 403 explícito cuando el recurso existe pero la acción está prohibida.
if (!decision.Allowed) return Forbid();
```

## Checklist antes de devolver código

- [ ] Ningún controller contiene reglas de negocio.
- [ ] Ningún `try/catch` de mapeo de errores en controllers.
- [ ] Todos los errores salen como `ProblemDetailsV1` con `errorCode` estable.
- [ ] 403 para prohibido, 404 para inexistente (nunca 404 para ocultar un 403).
- [ ] Validación de forma en el borde.
- [ ] `CancellationToken` propagado.
- [ ] Respuestas de colección con `PageV1<T>` y `pageSize` máximo documentado.
- [ ] DTOs propios; ningún aggregate ni documento expuesto.
- [ ] Sin stack traces, nombres de índice ni PII en las respuestas.
- [ ] `operations-bff` sigue `GET /screens/{screenId}` / `POST /mutations/{name}` (D5).
- [ ] Los `errorCode` nuevos están documentados en el reporte del servicio.

## Conexiones con otros skills

| Skill | Relación |
|---|---|
| `ddd-hexagonal-architecture` | El controller traduce a command o query; la regla vive en el aggregate. |
| `aspnetcore-security-owasp-baseline` | El controller delega en `IAuthorizationPort`; el 403 viene de ahí. |
| `aspnetcore-error-and-observability` | El mecanismo centralizado, los logs y las trazas del error. |
| `dotnet-parsing-and-validation` | Validación de forma del input en el borde. |
| `dotnet-code-documentation-xmldoc` | Documentación de la API pública. |
