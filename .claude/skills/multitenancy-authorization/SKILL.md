---
name: multitenancy-authorization
description: |
  Activa cuando el código de este CRM decide de qué tenant es un dato o si un actor
  puede ver o hacer algo: derivación de tenantId, filtros por tenant, RBAC con scope
  organizacional, ownership, overrides temporales, contexto de actor entre servicios
  y pruebas de aislamiento. Triggers: "tenantId", "tenant", "multi-tenant",
  "multitenancy", "aislamiento entre tenants", "de qué inmobiliaria es",
  "ActorContext", "actor", "usuario actual", "claims", "permiso", "permisos
  efectivos", "PermissionGrant", "autorización", "authorize", "[Authorize]",
  "policy", "RBAC", "rol", "AGENT", "MANAGER", "DIRECTOR", "scope", "OWN", "TEAM",
  "UNIT", "UNIT_AND_DESCENDANTS", "ORGANIZATION", "ownership", "cartera",
  "override", "UserPermissionOverride", "elevar permisos", "reemplazo temporal",
  "IDOR", "BOLA", "acceso a un recurso ajeno", "puede ver este inmueble",
  "filtro por tenant", "cache de permisos", "service-to-service", "propagar el
  actor", "auditoría de permisos", "test de aislamiento".
  Garantiza que el tenant salga siempre del contexto autenticado y nunca del
  request, que cada servicio revalide autorización, que todo acceso por ID
  verifique pertenencia y que los overrides tengan motivo, aprobador y vencimiento.
  NO activar para: configuración de Keycloak y validación de tokens (eso es
  oidc-keycloak-aspnetcore), modelado de documentos ni reglas de negocio.
---

# Multi-tenancy y Autorización

## Objetivo

Todos los tenants comparten la misma instancia de MongoDB, los mismos servicios y
el mismo broker. Lo único que separa los datos de una inmobiliaria de los de otra
es **código que puede olvidarse de filtrar**. Un `tenantId` faltante en un filtro
no rompe nada visible: devuelve datos de más, en silencio.

A eso se suma que la autorización de este dominio no es un `role == "MANAGER"`. Un
agente ve su cartera; un gerente su unidad; un director puede tener alcance global;
y encima existen excepciones temporales con aprobador y vencimiento.

Esta skill fija de dónde sale el tenant, quién decide los permisos y qué hay que
probar antes de dar por terminada una task.

Fuentes: [`ARCHITECTURE.md`](../../../ARCHITECTURE.md) §10,
[`README.md`](../../../README.md) §4.1, [`AGENTS.md`](../../../AGENTS.md).

## Cuándo activar

- Se lee o escribe cualquier dato de negocio.
- Se implementa un endpoint, un command handler o una query.
- Se decide si un actor puede ver o hacer algo.
- Se propaga contexto entre servicios o se consume un evento.
- Se implementan overrides, reasignación de cartera o permisos efectivos.
- Se escriben tests de una task que toca datos de tenant.

## Cuándo NO activar

- Configuración de Keycloak, flujo OIDC, validación de firma del JWT, refresh y
  logout (ver `oidc-keycloak-aspnetcore`).
- Forma de los documentos e índices (ver `mongodb-document-modeling`).
- Invariantes de negocio (ver `ddd-hexagonal-architecture`).
- OWASP general no relacionado con acceso a datos (ver
  `aspnetcore-security-owasp-baseline`).

## Decisiones del proyecto

| Tema | Decisión |
|---|---|
| Modelo | Multi-tenancy **lógico**: misma instancia, mismos servicios, separación por `tenantId` |
| Origen del tenant | **Siempre** del contexto autenticado. Nunca de body, query, ruta o header |
| Autenticación | Keycloak responde *quién sos* |
| Autorización | `access-service` responde *qué podés hacer dentro de la inmobiliaria* |
| Modelo de permisos | `PermissionGrant` = acción + recurso + **scope** |
| Scopes | `OWN`, `TEAM`, `UNIT`, `UNIT_AND_DESCENDANTS`, `ORGANIZATION`, `CUSTOM` |
| Roles iniciales | `AGENT`, `TEAM_LEAD`, `MANAGER`, `DIRECTOR`, `ADMIN`, `COMPLIANCE`, `FINANCE/RENTAL_ADMIN` |
| Excepciones | `UserPermissionOverride` con motivo, aprobador, vigencia y trazabilidad |
| Barreras | El BFF autoriza **además**, no en lugar de. Cada servicio revalida |
| Defaults | Agente ve su cartera; gerencia su scope organizacional; dirección puede recibir scope global |
| Caché | `IMemoryCache` con TTL corto. Nunca caché sin vencimiento |
| Identidad | `UserAccount` **no es** `Party`. Lifecycles separados |

> **Regla de oro:** compartir infraestructura física no autoriza acoplamiento de
> datos. Que dos tenants vivan en la misma instancia de MongoDB no cambia nada
> respecto de que sus datos sean invisibles entre sí.

## Estado actual vs target

- **Estado:** no implementado. `FND-006` fija `tenantId`, `ActorContext`, el
  adapter OIDC, el puerto de autorización y el envelope de auditoría.
- **Target:** un building block compartido resuelve el `ActorContext` en el borde;
  ningún repositorio puede consultar sin tenant; cada task incluye su test de
  aislamiento.

## Reglas obligatorias

### MUST

- **MUST** derivar `tenantId` del contexto autenticado, y solo de ahí.
- **MUST** incluir `tenantId` en el filtro de **toda** lectura y escritura, incluso
  cuando el `_id` ya sea único a nivel global.
- **MUST** verificar pertenencia al cargar por ID: que el recurso exista **y** sea
  de este tenant y esté dentro del scope del actor. Si no, responder como si no
  existiera.
- **MUST** revalidar autorización en el servicio propietario. El BFF no es la única
  barrera.
- **MUST** resolver permisos como acción + recurso + scope, no como comparación de
  rol.
- **MUST** propagar un `ActorContext` explícito en las llamadas entre servicios:
  tenant, usuario, roles efectivos, scope y `correlationId`.
- **MUST** derivar el tenant de un evento consumido desde el **envelope**, nunca del
  payload.
- **MUST** dar a los `UserPermissionOverride` motivo, aprobador, vigencia y registro
  de auditoría, y hacerlos **vencer**.
- **MUST** auditar overrides, reasignaciones de cartera y todo acceso ampliado.
- **MUST** incluir en cada task que toque datos de negocio un **test de aislamiento**
  entre tenants y un test de autorización.
- **MUST** aplicar el mismo filtrado a read models, proyecciones y exportaciones: un
  dashboard filtra igual que un endpoint.

### MUST NOT

- **MUST NOT** aceptar `tenantId` desde body, query string, parámetro de ruta o
  header. Aunque venga "solo para debug".
- **MUST NOT** exponer un endpoint o una query sin filtro de tenant, ni siquiera
  interno o de diagnóstico.
- **MUST NOT** confiar en que el ID que llegó en el request pertenece al actor.
- **MUST NOT** autorizar únicamente en el frontend o en el BFF.
- **MUST NOT** hardcodear reglas de negocio de permisos en Keycloak: allí va la
  autenticación, no el organigrama de la inmobiliaria.
- **MUST NOT** crear roles artificiales para resolver una excepción puntual: para eso
  existe el override.
- **MUST NOT** cachear decisiones de autorización sin TTL ni invalidación.
- **MUST NOT** tratar `UserAccount` como `Party`: deshabilitar un usuario no elimina
  a la persona ni su historial comercial.
- **MUST NOT** revelar la existencia de un recurso ajeno mediante un `403` distinto
  de un `404`, ni por mensajes de error, tiempos o contadores.
- **MUST NOT** dar acceso directo a MongoDB a la IA ni a ningún componente que no
  pase por el puerto de autorización.

## Recomendaciones

### SHOULD

- **SHOULD** hacer que el `tenantId` sea imposible de omitir por construcción: un
  repositorio base que lo exija en la firma es mejor que una regla escrita.
- **SHOULD** resolver el `ActorContext` una sola vez en el borde y pasarlo hacia
  abajo, en vez de leer claims en cada capa.
- **SHOULD** mantener el `ActorContext` en la capa de aplicación, no dentro del
  aggregate: el dominio no debería conocer JWTs.
- **SHOULD** usar TTL cortos en la caché de permisos y invalidarla ante cambios de
  membership, rol u override.
- **SHOULD** cubrir con tests el caso "mismo ID, otro tenant": es el que más se
  escapa en revisión.
- **SHOULD** registrar en el audit trail quién consultó datos sensibles cuando el
  acceso vino de un scope ampliado.
- **SHOULD** tratar los scopes jerárquicos (`UNIT_AND_DESCENDANTS`) resolviendo el
  árbol organizacional en `access-service`, no replicando la jerarquía en cada
  servicio.

### SHOULD NOT

- **SHOULD NOT** consultar `access-service` en cada operación de un bucle: resolver
  el scope una vez por request.
- **SHOULD NOT** modelar el scope como una lista de IDs congelada en el token: la
  cartera cambia y el token no se entera.

## Anti-patrones prohibidos

### 1. Tenant que viene del cliente

```csharp
// ❌ Cambiar un campo del body y leer datos de otra inmobiliaria.
[HttpGet("parties")]
public async Task<IActionResult> Search([FromQuery] string tenantId, ...)
    => Ok(await _query.SearchAsync(tenantId, ...));
```

```csharp
// ✅ Sale del contexto autenticado; el cliente no puede influirlo.
[HttpGet("parties")]
public async Task<IActionResult> Search([FromQuery] PartySearchRequest request,
    CancellationToken ct)
    => Ok(await _query.SearchAsync(_actor.Current.TenantId, request, ct));
```

### 2. Cargar por ID sin verificar pertenencia (IDOR/BOLA)

```csharp
// ❌ Con adivinar o filtrar un ID se accede al inmueble de otra inmobiliaria.
var property = await _properties.Find(p => p.Id == id).FirstOrDefaultAsync(ct);
return Ok(property);
```

```csharp
// ✅ Tenant en el filtro; si no pertenece, no existe.
var property = await _properties
    .Find(p => p.Id == id && p.TenantId == _actor.Current.TenantId)
    .FirstOrDefaultAsync(ct);

return property is null ? NotFound() : Ok(property);
```

Responder `404` y no `403` evita confirmar que el recurso existe en otro tenant.

### 3. Autorizar solo en el BFF

```csharp
// ❌ El servicio confía en que alguien ya validó. Cualquier llamada directa
//    —otro servicio, un test, un script— entra sin control.
public async Task<Result> HandleAsync(CloseTransaction cmd, CancellationToken ct)
{
    var tx = await _repo.GetAsync(cmd.TransactionId, ct);
    tx.Close();
    // ...
}
```

```csharp
// ✅ El servicio propietario revalida siempre.
public async Task<Result> HandleAsync(CloseTransaction cmd, CancellationToken ct)
{
    await _authorization.EnsureAsync(
        _actor.Current, Action.Close, Resource.Transaction, cmd.TransactionId, ct);

    var tx = await _repo.GetAsync(cmd.TransactionId, _actor.Current.TenantId, ct);
    tx.Close();
    // ...
}
```

### 4. Comparar rol en vez de resolver permiso y scope

```csharp
// ❌ Ignora la unidad organizacional, la cartera y los overrides vigentes.
if (user.Role != "MANAGER")
    return Forbid();
```

```csharp
// ✅ Acción + recurso + scope, resueltos por access-service.
var decision = await _authorization.EvaluateAsync(
    _actor.Current, Action.ViewPortfolio, Resource.Requirement, requirementId, ct);

if (!decision.Allowed)
    return Forbid();
```

### 5. Override sin vencimiento ni rastro

```csharp
// ❌ Un permiso elevado "temporal" que nadie recuerda quitar es permanente.
await _overrides.GrantAsync(userId, Permission.ViewAllUnits, ct);
```

```csharp
// ✅ Motivo, aprobador, vigencia y auditoría. Vence solo.
await _overrides.GrantAsync(new UserPermissionOverride(
    UserId: userId,
    Permission: Permission.ViewAllUnits,
    Reason: "Reemplazo por licencia del gerente de sucursal Centro",
    ApprovedBy: approverId,
    ValidFrom: today,
    ValidTo: today.AddDays(30)), ct);
```

### 6. Llamada entre servicios sin contexto de actor

```csharp
// ❌ matching-service pide datos y el otro extremo no sabe en nombre de quién.
var listing = await _http.GetFromJsonAsync<ListingDto>($"/listings/{id}", ct);
```

```csharp
// ✅ El actor viaja explícito y el otro extremo revalida.
var listing = await _supplyClient.GetListingAsync(id, _actor.Current, ct);
```

### 7. Caché de permisos sin vencimiento

```csharp
// ❌ Se revoca un permiso y el servicio sigue autorizando por horas.
_cache.Set($"perm:{userId}", permissions);
```

```csharp
// ✅ TTL corto, e invalidación explícita ante cambios de membership u override.
_cache.Set($"perm:{tenantId}:{userId}", permissions,
    new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
```

## Tests obligatorios por task

Toda task que toque datos de negocio incluye, como mínimo:

```csharp
// 1. Aislamiento: mismo ID, otro tenant → como si no existiera.
[Fact]
public async Task Get_WithIdFromAnotherTenant_ReturnsNotFound() { /* ... */ }

// 2. Listado: nunca aparecen documentos de otro tenant.
[Fact]
public async Task Search_NeverReturnsDocumentsFromOtherTenants() { /* ... */ }

// 3. Autorización: el actor sin permiso o fuera de scope no puede.
[Fact]
public async Task Command_WithActorOutOfScope_IsForbidden() { /* ... */ }

// 4. Override vencido: deja de habilitar.
[Fact]
public async Task ExpiredOverride_NoLongerGrantsAccess() { /* ... */ }
```

## Checklist antes de devolver código

- [ ] `tenantId` sale del contexto autenticado en el 100% de los caminos.
- [ ] Ningún endpoint, query o comando acepta el tenant desde el request.
- [ ] Toda lectura y escritura filtra por `tenantId`.
- [ ] Todo acceso por ID verifica pertenencia y devuelve `404`, no `403`.
- [ ] El servicio propietario revalida autorización, no solo el BFF.
- [ ] Los permisos se resuelven por acción + recurso + scope.
- [ ] El `ActorContext` se propaga en llamadas entre servicios.
- [ ] El tenant de un evento sale del envelope.
- [ ] Los overrides tienen motivo, aprobador, vigencia y auditoría.
- [ ] La caché de autorización tiene TTL y se invalida.
- [ ] Read models y exportaciones filtran igual que los endpoints.
- [ ] Están los cuatro tests de arriba y pasan.

## Conexiones con otros skills

| Skill | Relación |
|---|---|
| `oidc-keycloak-aspnetcore` | Keycloak dice **quién** es el usuario; esta skill, **qué puede hacer**. |
| `mongodb-document-modeling` | `tenantId` como primer campo de todo índice compuesto y unicidad por tenant. |
| `mongodb-dotnet-driver` | Los filtros por tenant se escriben en cada repositorio. |
| `ddd-hexagonal-architecture` | El `ActorContext` vive en aplicación, no en el dominio. |
| `event-driven-outbox-inbox` | El tenant viaja en el envelope; los consumidores lo respetan igual. |
| `aspnetcore-security-owasp-baseline` | IDOR/BOLA y control de acceso roto: esta skill es su aplicación concreta. |
| `dotnet-adversarial-testing` | IDs de otro tenant, tokens manipulados, overrides vencidos. |
