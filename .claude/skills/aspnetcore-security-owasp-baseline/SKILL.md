---
name: aspnetcore-security-owasp-baseline
description: |
  SIEMPRE ACTIVO al escribir o revisar código de un servicio o BFF de este CRM
  expuesto a la red. Baseline de seguridad OWASP que aplica a todo endpoint, parser,
  cliente saliente, acceso a datos y log. Triggers: cualquier controller o minimal
  API, manejo de input del usuario o de upstream, headers, autenticación,
  autorización, tokens, secretos, queries a la base, construcción de URLs,
  serialización de respuestas, logging, manejo de errores, CORS. Palabras clave:
  "endpoint", "input", "[FromBody]", "[FromQuery]", "[FromRoute]", "auth", "token",
  "JWT", "[Authorize]", "policy", "claim", "password", "secreto", "query a la
  base", "query nativa", "inyección", "NoSQL injection", "log", "error al
  cliente", "CORS", "PII", "datos sensibles", "DNI", "CUIT", "IDOR", "BOLA",
  "ownership", "responsibleUserId", "stack trace al cliente".
  Cubre OWASP Top 10 en contexto ASP.NET Core: control de acceso roto, inyección,
  exposición de datos sensibles, misconfiguración y logging de eventos de
  seguridad. NO se desactiva nunca para código de red.
---

# Baseline de Seguridad OWASP

## Objetivo

Este CRM guarda datos que a una persona le importan mucho: documento de identidad,
CUIT, teléfono, domicilio, situación patrimonial, con quién negocia y por cuánto.

La entrega V2 es para **una única inmobiliaria** (sin multi-tenancy): la
vulnerabilidad más probable acá no es una inyección exótica, es **control de
acceso roto** — un permiso que se asumió sin evaluar, un ID que no se verificó
contra el owner del registro, o una llamada que confía en el frontend. Falla en
silencio y devuelve datos de más.

Esta skill es el baseline transversal. La evaluación concreta de permisos la hace
`access-service` a través del puerto de autorización (definido en `V2-ACL-001a`).

Fuentes: `PRPs/_backlog/2026-09-17-plan-wave-2-acceso-catalogos-party.md` (§3 y §5,
decisiones D2, D3, D4, D6), `IMPLEMENTATION_REPORT-V2-FND-002.md`.

## Cuándo activar

Siempre que el código toque red, input externo, datos de negocio o logs. En la
práctica: casi toda task de backend o BFF.

## Cuándo NO activar

Solo scripts offline sin entrada no confiable ni datos reales.

## Decisiones del proyecto

| Tema | Decisión |
|---|---|
| Autenticación | Keycloak/OIDC. Tokens retenidos en el BFF; el navegador solo tiene cookie `HttpOnly` (ver `oidc-keycloak-aspnetcore`) |
| Autorización (D2) | `access-service` es dueño de la matriz rol→permiso y la expone por HTTP interno. Los owners llaman vía adapter del puerto de autorización con caché corta (`IMemoryCache`, ≤60 s) |
| Propiedad del registro (D2) | La evalúa el servicio owner con su propio dato (ej. `responsibleUserId` en Party) — no la resuelve `access-service` |
| Acción prohibida vs inexistente (D4) | **403** `forbidden`; **404** `not_found` |
| Identidad BFF→servicios (D6) | Token relay: el BFF reenvía el access token del usuario como Bearer; cada servicio valida el JWT y reconstruye `ExecutionContextV1`. Sin client credentials internas en la POC |
| Errores | `ProblemDetailsV1` con `errorCode` estable, sin detalle interno |
| CORS | Orígenes explícitos desde configuración. Nunca `AllowAnyOrigin()` |
| PII | Nunca en logs, trazas, eventos ni respuestas de error |

## Estado actual vs target

- **Estado:** `ExecutionContextV1`, `IAuthenticationPort`/`ClaimsPrincipalAuthenticationPort`
  y la validación JWT existen (V2-FND-002), sin wire-up en ningún `Program.cs`. El
  puerto de autorización (`IAuthorizationPort`) todavía no existe: lo crea
  `V2-ACL-001a`.
- **Target:** cada servicio owner revalida autorización vía ese puerto antes de
  operar sobre un recurso concreto; `V2-ACL-001` hace el primer wire-up real.

## Reglas obligatorias

### MUST

- **MUST** verificar en el servicio propietario que el actor puede operar sobre
  **ese** recurso concreto, no solo que está autenticado (`[Authorize]` no alcanza).
- **MUST** validar la propiedad del registro cuando la regla de negocio lo exige
  (ej. Vendedor solo edita lo que tiene asignado como `responsibleUserId`) en el
  servicio owner, con su propio dato.
- **MUST** devolver **403** `forbidden` ante una acción prohibida y **404**
  `not_found` ante un recurso inexistente — nunca ocultar un 403 como 404.
- **MUST** validar todo input externo en el borde: body, query, ruta, headers,
  respuestas de upstream y mensajes del broker.
- **MUST** construir los filtros de MongoDB con los builders tipados del driver,
  nunca concatenando input en un documento de consulta.
- **MUST** devolver errores opacos vía `ProblemDetailsV1`: `errorCode`, `title` y
  `correlationId`. Sin stack traces, nombres de índice, mensajes del driver ni
  rutas internas.
- **MUST** configurar CORS con orígenes explícitos.
- **MUST** loguear los eventos de seguridad — fallo de autenticación, denegación de
  autorización — con `correlationId` y sin PII.
- **MUST** mantener las dependencias actualizadas y sin vulnerabilidades conocidas.

### MUST NOT

- **MUST NOT** confiar en que el ID del request pertenece al actor sin verificarlo
  contra el dato de ownership del servicio owner.
- **MUST NOT** autorizar únicamente en el frontend o en el BFF: cada servicio
  owner revalida.
- **MUST NOT** entregar tokens al navegador ni guardarlos donde JavaScript los lea.
- **MUST NOT** loguear PII, secretos, tokens ni headers de autorización.
- **MUST NOT** devolver detalle interno en un error (stack trace, nombre de
  índice, mensaje del driver, ruta interna) — pero sí distinguir 403 de 404
  (D4): V2 no oculta un permiso denegado detrás de un 404.
- **MUST NOT** usar `AllowAnyOrigin()`, ni combinarlo con credenciales.
- **MUST NOT** deserializar tipos arbitrarios desde input externo.
- **MUST NOT** confiar en headers como `X-Forwarded-For` salvo que vengan de un
  proxy confiable configurado explícitamente.
- **MUST NOT** exponer endpoints de diagnóstico que devuelvan datos de negocio.

## Recomendaciones

### SHOULD

- **SHOULD** cachear la evaluación de `IAuthorizationPort` en el owner con
  `IMemoryCache` de vida corta (≤60 s, D2), nunca indefinida.
- **SHOULD** aplicar las cabeceras de seguridad habituales desde el building block
  compartido, no servicio por servicio.
- **SHOULD** revisar en cada PR los endpoints nuevos preguntando "¿qué pasa si un
  actor sin el permiso llama a esto con un ID que adivinó?".
- **SHOULD** limitar el tamaño máximo del body.

### SHOULD NOT

- **SHOULD NOT** confiar en la ofuscación como control: un ID difícil de adivinar
  no reemplaza la verificación de autorización.

## Anti-patrones prohibidos

### 1. Control de acceso roto (IDOR/BOLA)

```csharp
// ❌ El riesgo número uno de este sistema. Cualquier actor autenticado lee
//    o modifica cualquier registro cambiando el ID.
[HttpGet("{id}")]
public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    => Ok(await _repo.GetByIdAsync(id, ct));
```

```csharp
// ✅ Autorización revalidada en el owner antes de devolver el recurso.
[HttpGet("{id}")]
public async Task<IActionResult> Get(Guid id, CancellationToken ct)
{
    var party = await _repo.GetByIdAsync(id, ct);
    if (party is null) return NotFound();

    var decision = await _authorization.EvaluateAsync(
        _actor.Current.ActorId, Permissions.PartiesRead, "party", id, ct);

    return decision.Allowed ? Ok(_mapper.ToResponse(party)) : Forbid();
}
```

### 2. Inyección NoSQL

```csharp
// ❌ Input externo interpretado como estructura de consulta.
var filter = BsonDocument.Parse($"{{ email: '{request.Email}' }}");
```

```csharp
// ✅ Builders tipados: el valor es un valor, nunca estructura.
var filter = Builders<PartyDocument>.Filter.Eq(p => p.Email, request.Email);
```

### 3. Detalle interno en el error

```csharp
// ❌ Revela colección, índice y estructura interna.
catch (MongoWriteException ex) { return Conflict(ex.Message); }
```

```csharp
// ✅ Opaco hacia afuera, completo del lado del servidor.
throw new PartyAlreadyExistsException(); // el handler global mapea a ProblemDetailsV1
```

### 4. PII en logs

```csharp
// ❌ El documento y el teléfono quedan en el sistema de logs.
_logger.LogInformation("Buscando party {Dni} tel {Phone}", dni, phone);
```

```csharp
// ✅ Identificadores, no contenidos.
_logger.LogInformation("Búsqueda de party corr={CorrelationId}", correlationId);
```

### 5. CORS permisivo

```csharp
// ❌ Cualquier origen, con credenciales: combinación prohibida.
policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod().AllowCredentials();
```

```csharp
// ✅ Orígenes explícitos desde configuración validada.
policy.WithOrigins(corsOptions.AllowedOrigins)
      .AllowAnyHeader()
      .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
      .AllowCredentials();
```

### 6. Confiar en el BFF sin revalidar en el servicio

```csharp
// ❌ El servicio de dominio confía en que si el request llegó, ya está autorizado.
[HttpPost]
public async Task<IActionResult> AssignResponsible(AssignRequest req, CancellationToken ct)
    => Ok(await _sender.SendAsync(new AssignPartyResponsible(req.PartyId, req.UserId), ct));
```

```csharp
// ✅ El servicio owner vuelve a evaluar el permiso, no confía en el BFF (D6:
//    el BFF solo reenvía el token del usuario, no decide autorización).
[HttpPost]
public async Task<IActionResult> AssignResponsible(AssignRequest req, CancellationToken ct)
{
    var decision = await _authorization.EvaluateAsync(
        _actor.Current.ActorId, Permissions.PartiesAssignResponsible, "party", req.PartyId, ct);
    if (!decision.Allowed) return Forbid();

    await _sender.SendAsync(new AssignPartyResponsible(req.PartyId, req.UserId), ct);
    return NoContent();
}
```

## Checklist antes de devolver código

- [ ] Todo acceso por ID revalida autorización en el owner, no solo autenticación.
- [ ] Prohibido → 403; inexistente → 404 (nunca 404 para ocultar un 403).
- [ ] Ownership de registro (ej. `responsibleUserId`) verificado en el owner cuando
      la regla lo exige.
- [ ] Input externo validado en el borde.
- [ ] Filtros de MongoDB con builders tipados, sin concatenación.
- [ ] Errores opacos vía `ProblemDetailsV1`, sin stack traces ni detalle interno.
- [ ] Ninguna PII, token ni secreto en logs o trazas.
- [ ] CORS con orígenes explícitos.
- [ ] Eventos de seguridad logueados con `correlationId`.
- [ ] Hay test de acceso sin el permiso requerido (403 esperado).

## Conexiones con otros skills

| Skill | Relación |
|---|---|
| `oidc-keycloak-aspnetcore` | Autenticación, tokens fuera del navegador y validación de JWT. |
| `aspnetcore-rest-layer` | Contrato de error opaco y status codes correctos. |
| `aspnetcore-error-and-observability` | Logging sin PII y sin detalle interno al cliente. |
| `mongodb-dotnet-driver` | Filtros tipados y errores del driver traducidos. |
| `ddd-hexagonal-architecture` | Dónde vive la regla de ownership que esta skill exige revalidar. |
| `dotnet-adversarial-testing` | IDs ajenos, input hostil, permisos denegados. |
