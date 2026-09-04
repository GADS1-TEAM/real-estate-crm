---
name: aspnetcore-rest-layer
description: |
  Activa cuando se crea o modifica la capa REST de un servicio o BFF de este CRM:
  controllers, endpoints, el pipeline de request y el contrato de respuesta.
  Triggers: "[ApiController]", "[Route]", "[HttpGet]", "[HttpPost]", "[HttpPut]",
  "[HttpDelete]", "[HttpPatch]", "endpoint", "ruta", "[FromBody]", "[FromQuery]",
  "[FromRoute]", "[FromHeader]", "DTO", "request model", "response model",
  "DataAnnotations", "FluentValidation", "ControllerBase", "IActionResult",
  "Task<IActionResult>", "ActionResult<T>", "Minimal API", "contrato de respuesta",
  "status code", "404", "409", "422", "versionado de API", "paginación",
  "serializar respuesta", "ProblemDetails", "problem+json", "RFC 9457",
  "IExceptionHandler", "AddProblemDetails", "manejo de errores HTTP",
  "código de error", "error code", "controller delgado".
  Garantiza controllers sin lógica de negocio, validación en el borde, errores
  siempre en formato Problem Details con código estable, y status codes correctos.
  NO activar para: reglas de dominio, acceso a datos, mensajería ni decisiones de
  autorización.
---

# Capa REST en ASP.NET Core

## Objetivo

El controller es un **traductor**: convierte HTTP en un command o una query, y el
resultado en una respuesta. Nada más.

Cuando se le agrega lógica, esa lógica queda fuera del dominio, sin tests de
aggregate y sin poder reutilizarse desde un consumidor de eventos o desde el BFF.

Y cuando cada endpoint arma sus errores a mano, el frontend termina manejando
veinte formatos distintos para lo mismo.

Fuentes: [`ARCHITECTURE.md`](../../../ARCHITECTURE.md) §5,
[`ADR-006`](../../../docs/adr/006-contrato-de-error-publico.md).

## Cuándo activar

- Se crea o modifica un controller, un endpoint o una ruta.
- Se definen DTOs de request o response.
- Se decide qué status code corresponde.
- Se toca el manejo global de errores.
- Se versiona una API pública.

## Cuándo NO activar

- Reglas de negocio e invariantes (ver `ddd-hexagonal-architecture`).
- Acceso a datos (ver las skills de MongoDB).
- Mensajería y eventos.
- Decisión de si el actor puede hacer algo (ver `multitenancy-authorization`).

## Decisiones del proyecto

| Tema | Decisión |
|---|---|
| Base | `ControllerBase` de ASP.NET Core. **No hay clase base propia** |
| Estilo | Controllers con `[ApiController]`. Minimal API solo para health y endpoints técnicos |
| Errores | `application/problem+json` según **RFC 9457**, con extensión `code` estable. Ver [ADR-006](../../../docs/adr/006-contrato-de-error-publico.md) |
| Mapeo de errores | Centralizado en un `IExceptionHandler` con `AddProblemDetails()`. Los controllers no arman errores |
| Excepciones | Tipadas, en `building-blocks`: `ValidationException`, `NotFoundException`, `ForbiddenException`, `ConflictException`, `BusinessRuleViolationException`, `DependencyUnavailableException` |
| Recurso ajeno | **404, no 403** (ver `multitenancy-authorization`) |
| BFF | Compone experiencia; no decide reglas de dominio ni toca MongoDB |

### Formato de error

```json
{
  "type": "https://crm.local/errors/party-already-exists",
  "title": "La Party ya existe en esta organización.",
  "status": 409,
  "detail": "Ya hay una Party con ese CUIT en el tenant.",
  "instance": "/parties",
  "code": "PARTY_ALREADY_EXISTS",
  "correlationId": "0f9c…"
}
```

`code` es estable y accionable por el cliente. `title` y `detail` pueden cambiar de
redacción sin romper a nadie.

### Mapeo excepción → status

| Excepción | Status |
|---|---|
| `ValidationException` | 400 |
| `UnauthenticatedException` | 401 |
| `ForbiddenException` | 403 |
| `NotFoundException` | 404 |
| `ConflictException` | 409 |
| `BusinessRuleViolationException` | 422 |
| `DependencyUnavailableException` | 503 |

## Estado actual vs target

- **Estado:** no hay controllers. `FND-003` crea las excepciones y el handler;
  `FND-005` los shells y BFFs.
- **Target:** todo servicio registra el mismo handler de errores; ningún controller
  contiene `try/catch` de mapeo ni lógica de negocio.

## Reglas obligatorias

### MUST

- **MUST** mantener el controller delgado: valida forma, traduce a command o query,
  devuelve.
- **MUST** centralizar el mapeo de excepciones en un `IExceptionHandler` global.
- **MUST** devolver todos los errores como `application/problem+json` con `code`
  estable.
- **MUST** validar la forma del input en el borde con DataAnnotations o
  FluentValidation.
- **MUST** propagar `CancellationToken` desde la acción hasta la capa de aplicación.
- **MUST** usar status codes semánticamente correctos según la tabla de arriba.
- **MUST** devolver **404** cuando el recurso pertenece a otro tenant.
- **MUST** paginar toda respuesta de colección, con límite máximo.
- **MUST** usar DTOs propios de la capa REST: nunca exponer el aggregate.

### MUST NOT

- **MUST NOT** poner reglas de negocio en el controller ni en el BFF.
- **MUST NOT** hacer `try/catch` en el controller para mapear errores: eso es del
  handler global.
- **MUST NOT** devolver stack traces, nombres de índice, connection strings ni PII.
- **MUST NOT** exponer entidades de dominio ni documentos de MongoDB como response.
- **MUST NOT** acceder a MongoDB desde un controller o desde el BFF.
- **MUST NOT** devolver 200 con un cuerpo que indique error.
- **MUST NOT** aceptar `tenantId` como parámetro del request.
- **MUST NOT** inventar un formato de error propio por endpoint.

## Recomendaciones

### SHOULD

- **SHOULD** nombrar rutas por recurso y en plural: `/parties`, `/properties`.
- **SHOULD** devolver `201 Created` con `Location` al crear.
- **SHOULD** mantener el catálogo de `code` del servicio versionado como código.
- **SHOULD** usar `ActionResult<T>` para que el tipo de respuesta quede explícito.
- **SHOULD** documentar los endpoints con XML doc y exponerlos vía OpenAPI.
- **SHOULD** versionar la API cuando un cambio es incompatible, en vez de romper.

### SHOULD NOT

- **SHOULD NOT** crear un endpoint por cada campo editable: la UI es journey-driven,
  no CRUD-driven.
- **SHOULD NOT** usar verbos en las rutas salvo acciones de negocio que no sean CRUD
  (`/listings/{id}/activate`).

## Anti-patrones prohibidos

### 1. Lógica de negocio en el controller

```csharp
// ❌ La invariante queda fuera del dominio y no se puede testear ni reutilizar.
[HttpPost("{id}/activate")]
public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
{
    var listing = await _repo.GetAsync(id, ct);
    if (listing.Status == ListingStatus.Closed)
        return BadRequest("El listing está cerrado.");

    listing.Status = ListingStatus.Active;
    await _repo.SaveAsync(listing, ct);
    return Ok();
}
```

```csharp
// ✅ Traduce y delega. La regla vive en el aggregate.
[HttpPost("{id}/activate")]
public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
{
    await _sender.SendAsync(new ActivateListing(id), ct);
    return NoContent();
}
```

### 2. Manejo de errores ad-hoc

```csharp
// ❌ Cada endpoint inventa su formato; el frontend maneja veinte variantes.
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
// ✅ El controller no atrapa nada. El handler global traduce.
await _sender.SendAsync(cmd, ct);
return NoContent();
```

```csharp
// Registrado una sola vez, en building-blocks.
public sealed class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var (status, code) = ex switch
        {
            ValidationException            => (400, "VALIDATION_FAILED"),
            NotFoundException n            => (404, n.Code),
            ForbiddenException             => (403, "FORBIDDEN"),
            ConflictException c            => (409, c.Code),
            BusinessRuleViolationException b => (422, b.Code),
            DependencyUnavailableException => (503, "DEPENDENCY_UNAVAILABLE"),
            _                              => (500, "INTERNAL_ERROR")
        };

        var problem = new ProblemDetails
        {
            Type = $"https://crm.local/errors/{code.ToLowerInvariant().Replace('_','-')}",
            Title = _titles.For(code),
            Status = status,
            Instance = ctx.Request.Path
        };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = ctx.TraceIdentifier;

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
    => Ok(await _repo.GetAsync(id, ct));
```

```csharp
// ✅ DTO propio de la capa REST.
[HttpGet("{id}")]
public async Task<ActionResult<PartyResponse>> Get(Guid id, CancellationToken ct)
    => Ok(await _queries.GetPartyAsync(id, ct));
```

### 4. Colección sin paginar

```csharp
// ❌ Una inmobiliaria con 40.000 propiedades tumba la respuesta.
[HttpGet]
public async Task<IActionResult> List(CancellationToken ct)
    => Ok(await _queries.ListAllAsync(ct));
```

```csharp
// ✅ Paginación obligatoria con tope.
[HttpGet]
public async Task<ActionResult<PagedResponse<PropertySummary>>> List(
    [FromQuery] PageRequest page, CancellationToken ct)
    => Ok(await _queries.ListAsync(page.Normalized(maxPageSize: 100), ct));
```

### 5. Detalle interno filtrado al cliente

```csharp
// ❌ Revela el nombre del índice, la colección y la estructura interna.
catch (MongoWriteException ex)
{
    return Conflict(ex.Message); // "E11000 duplicate key ... party_taxid_unique"
}
```

```csharp
// ✅ El repositorio traduce a excepción de dominio; el handler la formatea.
throw new ConflictException("PARTY_ALREADY_EXISTS");
```

## Checklist antes de devolver código

- [ ] Ningún controller contiene reglas de negocio.
- [ ] Ningún `try/catch` de mapeo de errores en controllers.
- [ ] Todos los errores salen como `problem+json` con `code` estable.
- [ ] Status codes correctos según la tabla.
- [ ] Recurso de otro tenant devuelve 404.
- [ ] Validación de forma en el borde.
- [ ] `CancellationToken` propagado.
- [ ] Respuestas de colección paginadas y con tope.
- [ ] DTOs propios; ningún aggregate ni documento expuesto.
- [ ] Sin stack traces, nombres de índice ni PII en las respuestas.
- [ ] Los `code` nuevos están en el catálogo del servicio.

## Conexiones con otros skills

| Skill | Relación |
|---|---|
| `ddd-hexagonal-architecture` | El controller traduce a command o query; la regla vive en el aggregate. |
| `dotnet-parsing-and-validation` | Validación de forma del input en el borde. |
| `multitenancy-authorization` | Autorización y la regla de 404 en vez de 403. |
| `aspnetcore-error-and-observability` | El handler global, los logs y las trazas del error. |
| `aspnetcore-di-and-middleware-pipeline` | Orden del pipeline y registro del handler. |
| `dotnet-code-documentation-xmldoc` | Documentación de la API pública y OpenAPI. |
