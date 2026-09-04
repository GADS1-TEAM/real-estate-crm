---
name: aspnetcore-security-owasp-baseline
description: |
  SIEMPRE ACTIVO al escribir o revisar código de un servicio o BFF de este CRM
  expuesto a la red. Baseline de seguridad OWASP que aplica a todo endpoint, parser,
  cliente saliente, acceso a datos y log. Triggers: cualquier controller o minimal
  API, manejo de input del usuario o de upstream, headers, autenticación,
  autorización, tokens, secretos, queries a la base, construcción de URLs,
  serialización de respuestas, logging, manejo de errores, CORS, SSRF. Palabras
  clave: "endpoint", "input", "[FromBody]", "[FromQuery]", "[FromRoute]", "auth",
  "token", "JWT", "[Authorize]", "policy", "claim", "password", "secreto",
  "query a la base", "query nativa", "inyección", "NoSQL injection", "armar URL",
  "redirect", "upload", "archivo adjunto", "log", "error al cliente", "CORS",
  "PII", "datos sensibles", "DNI", "CUIT", "IDOR", "BOLA", "ownership",
  "rate limit", "stack trace al cliente", "cabeceras de seguridad".
  Cubre OWASP Top 10 en contexto ASP.NET Core: control de acceso roto, inyección,
  exposición de datos sensibles, deserialización insegura, misconfiguración, SSRF y
  logging de eventos de seguridad. NO se desactiva nunca para código de red.
---

# Baseline de Seguridad OWASP

## Objetivo

Este CRM guarda datos que a una persona le importan mucho: documento de identidad,
CUIT, teléfono, domicilio, situación patrimonial, con quién negocia y por cuánto.
Y los guarda de **muchas inmobiliarias a la vez, en la misma base**.

Eso cambia el perfil de riesgo. La vulnerabilidad más probable acá no es una
inyección exótica: es **control de acceso roto** — un filtro sin `tenantId`, un ID
que no se verificó, un permiso que se asumió. Falla en silencio y devuelve datos de
más.

Esta skill es el baseline transversal. La aplicación concreta de tenancy y permisos
está en `multitenancy-authorization`.

Fuentes: [`ARCHITECTURE.md`](../../../ARCHITECTURE.md) §10,
[`ADR-005`](../../../docs/adr/005-keycloak-realm-unico-y-patron-bff.md),
[`ADR-006`](../../../docs/adr/006-contrato-de-error-publico.md).

## Cuándo activar

Siempre que el código toque red, input externo, datos de negocio o logs. En la
práctica: casi toda task de backend o BFF.

## Cuándo NO activar

Solo scripts offline sin entrada no confiable ni datos reales.

## Decisiones del proyecto

| Tema | Decisión |
|---|---|
| Autenticación | Keycloak/OIDC. Tokens retenidos en el BFF; el navegador solo tiene cookie `HttpOnly` |
| Autorización | `access-service`. **Cada servicio revalida**; el BFF no es la única barrera |
| Recurso ajeno | **404**, no 403 |
| Errores | `problem+json` con `code` estable, sin detalle interno |
| CORS | Orígenes explícitos desde configuración. Nunca `AllowAnyOrigin()` |
| Transporte | HTTPS fuera de desarrollo local; TLS 1.2 mínimo |
| PII | Nunca en logs, trazas, eventos ni respuestas de error |
| Binarios | Detrás de `IObjectStorage`, nunca servidos desde una ruta armada con input |
| IA | Sin acceso directo a la base: opera por APIs, commands y queries autorizadas |

## Estado actual vs target

- **Estado:** no implementado. `FND-006` fija seguridad y tenancy.
- **Target:** el building block de plataforma aplica cabeceras, CORS y autenticación;
  cada servicio revalida autorización y ningún endpoint consulta sin `tenantId`.

## Reglas obligatorias

### MUST

- **MUST** verificar en el servicio propietario que el actor puede operar sobre
  **ese** recurso concreto, no solo que está autenticado.
- **MUST** incluir `tenantId` en todo filtro de lectura y escritura.
- **MUST** devolver **404** ante un recurso de otro tenant.
- **MUST** validar todo input externo en el borde: body, query, ruta, headers,
  respuestas de upstream y mensajes del broker.
- **MUST** construir los filtros de MongoDB con los builders tipados del driver,
  nunca concatenando input en un documento de consulta.
- **MUST** devolver errores opacos: `code`, `title` y `correlationId`. Sin stack
  traces, nombres de índice, mensajes del driver ni rutas internas.
- **MUST** configurar CORS con orígenes explícitos.
- **MUST** loguear los eventos de seguridad — fallo de autenticación, denegación de
  autorización, uso de override — con `correlationId` y sin PII.
- **MUST** validar tipo, tamaño y contenido de todo archivo subido, y no confiar en
  el nombre ni en el content-type que envía el cliente.
- **MUST** validar el destino de toda URL construida con datos externos, para no
  habilitar SSRF.
- **MUST** mantener las dependencias actualizadas y sin vulnerabilidades conocidas.

### MUST NOT

- **MUST NOT** confiar en que el ID del request pertenece al actor.
- **MUST NOT** aceptar `tenantId` desde el request.
- **MUST NOT** autorizar únicamente en el frontend o en el BFF.
- **MUST NOT** entregar tokens al navegador ni guardarlos donde JavaScript los lea.
- **MUST NOT** loguear PII, secretos, tokens ni headers de autorización.
- **MUST NOT** devolver detalle interno en un error, ni distinguir "no existe" de
  "no podés verlo" por mensaje, status o tiempo de respuesta.
- **MUST NOT** usar `AllowAnyOrigin()`, ni combinarlo con credenciales.
- **MUST NOT** deserializar tipos arbitrarios desde input externo.
- **MUST NOT** confiar en headers como `X-Forwarded-For` salvo que vengan de un
  proxy confiable configurado explícitamente.
- **MUST NOT** exponer endpoints de diagnóstico que devuelvan datos de negocio.

## Recomendaciones

### SHOULD

- **SHOULD** aplicar rate limiting en los endpoints públicos del BFF, sobre todo
  login y búsquedas.
- **SHOULD** aplicar las cabeceras de seguridad habituales desde el building block
  compartido, no servicio por servicio.
- **SHOULD** hacer que el `tenantId` sea imposible de omitir por construcción: un
  repositorio base que lo exija en la firma vale más que una regla escrita.
- **SHOULD** revisar en cada PR los endpoints nuevos preguntando "¿qué pasa si otro
  tenant llama a esto con un ID que adivinó?".
- **SHOULD** limitar el tamaño máximo del body y de los archivos.
- **SHOULD** correr el escaneo de dependencias en CI (`FND-002`).

### SHOULD NOT

- **SHOULD NOT** confiar en la ofuscación como control: un ID difícil de adivinar no
  reemplaza la verificación de pertenencia.
- **SHOULD NOT** exponer identificadores internos secuenciales en contratos
  públicos.

## Anti-patrones prohibidos

### 1. Control de acceso roto (IDOR/BOLA)

```csharp
// ❌ El riesgo número uno de este sistema. Cambiando el ID se leen datos ajenos.
[HttpGet("{id}")]
public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    => Ok(await _repo.GetAsync(id, ct));
```

```csharp
// ✅ Tenant en el filtro y verificación de scope; si no corresponde, 404.
[HttpGet("{id}")]
public async Task<IActionResult> Get(Guid id, CancellationToken ct)
{
    var property = await _repo.GetAsync(id, _actor.Current.TenantId, ct);
    if (property is null) return NotFound();

    var decision = await _authorization.EvaluateAsync(
        _actor.Current, Action.View, Resource.Property, id, ct);

    return decision.Allowed ? Ok(_mapper.ToResponse(property)) : NotFound();
}
```

### 2. Inyección NoSQL

```csharp
// ❌ Input externo interpretado como estructura de consulta.
var filter = BsonDocument.Parse($"{{ email: '{request.Email}' }}");
```

```csharp
// ✅ Builders tipados: el valor es un valor, nunca estructura.
var filter = Builders<PartyDocument>.Filter.And(
    Builders<PartyDocument>.Filter.Eq(p => p.TenantId, tenantId),
    Builders<PartyDocument>.Filter.Eq(p => p.Email, request.Email));
```

### 3. Detalle interno en el error

```csharp
// ❌ Revela colección, índice y estructura interna.
catch (MongoWriteException ex) { return Conflict(ex.Message); }
```

```csharp
// ✅ Opaco hacia afuera, completo del lado del servidor.
throw new ConflictException("PARTY_ALREADY_EXISTS");
```

### 4. PII en logs

```csharp
// ❌ El documento y el teléfono quedan en el sistema de logs.
_logger.LogInformation("Buscando party {Dni} tel {Phone}", dni, phone);
```

```csharp
// ✅ Identificadores, no contenidos.
_logger.LogInformation("Búsqueda de party tenant={TenantId} corr={CorrelationId}",
    tenantId, correlationId);
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

### 6. SSRF al armar una URL con input

```csharp
// ❌ El cliente elige a quién llama el servidor: red interna alcanzable.
var response = await _client.GetAsync(request.CallbackUrl, ct);
```

```csharp
// ✅ Solo destinos previamente habilitados.
if (!_allowedHosts.Contains(new Uri(request.CallbackUrl).Host))
    throw new ValidationException("CALLBACK_HOST_NOT_ALLOWED");
```

### 7. Upload sin validar

```csharp
// ❌ Confía en el nombre y el content-type que manda el cliente.
await File.WriteAllBytesAsync($"./uploads/{file.FileName}", bytes, ct);
```

```csharp
// ✅ Tamaño, tipo real y nombre generado por el sistema, detrás del puerto.
if (file.Length > _limits.MaxUploadBytes)
    throw new ValidationException("FILE_TOO_LARGE");

var assetId = await _objectStorage.StoreAsync(
    stream, contentType: detectedType, tenantId, ct);
```

## Checklist antes de devolver código

- [ ] Todo acceso por ID verifica tenant y scope, y devuelve 404 si no corresponde.
- [ ] `tenantId` en todos los filtros; ninguno viene del request.
- [ ] Autorización revalidada en el servicio propietario.
- [ ] Input externo validado en el borde.
- [ ] Filtros de MongoDB con builders tipados, sin concatenación.
- [ ] Errores opacos, sin stack traces ni detalle interno.
- [ ] Ninguna PII, token ni secreto en logs o trazas.
- [ ] CORS con orígenes explícitos.
- [ ] Uploads validados en tamaño y tipo, con nombre generado por el sistema.
- [ ] URLs construidas con input externo validadas contra una lista permitida.
- [ ] Eventos de seguridad logueados con `correlationId`.
- [ ] Hay test de acceso con ID de otro tenant.

## Conexiones con otros skills

| Skill | Relación |
|---|---|
| `multitenancy-authorization` | Aplicación concreta del control de acceso: tenant, scope, overrides. |
| `oidc-keycloak-aspnetcore` | Autenticación, tokens fuera del navegador y validación de JWT. |
| `dotnet-parsing-and-validation` | Validación de todo dato que cruza un borde de confianza. |
| `aspnetcore-rest-layer` | Contrato de error opaco y status codes correctos. |
| `aspnetcore-error-and-observability` | Logging sin PII y sin detalle interno al cliente. |
| `aspnetcore-config-and-secrets` | Secretos, CORS y credenciales fuera del código. |
| `mongodb-dotnet-driver` | Filtros tipados y errores del driver traducidos. |
| `dotnet-adversarial-testing` | IDs ajenos, input hostil, uploads maliciosos. |
