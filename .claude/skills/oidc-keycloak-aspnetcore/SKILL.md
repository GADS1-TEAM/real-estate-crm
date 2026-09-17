---
name: oidc-keycloak-aspnetcore
description: |
  Activa cuando se configura o se toca la autenticación de este CRM: integración
  OIDC con Keycloak, login de crm-web a través de operations-bff, validación de
  tokens en los servicios de dominio, claims, sesión, logout y configuración
  reproducible del realm. Triggers: "Keycloak", "OIDC", "OpenID Connect", "OAuth2",
  "realm", "client", "client secret", "AddKeycloakJwtBearerAuthentication",
  "AddKeycloakOpenIdConnectCookieAuthentication", "AddAuthentication",
  "JwtBearerOptions", "OpenIdConnectOptions", "Authorization Code", "PKCE",
  "access token", "id token", "bearer", "token relay", "validar el token",
  "TokenValidationParameters", "issuer", "ValidateIssuer", "audience",
  "ValidateAudience", "claims", "login", "logout", "sesión", "cookie de sesión",
  "token expirado", "401", "403", "realm-export", "crm-dev", "dev.vendedor",
  "Direct Access Grants".
  Garantiza que los tokens no lleguen nunca al navegador, que todo servicio valide
  emisor/audiencia/firma/vigencia, que el realm sea reproducible como código y que
  Keycloak solo resuelva identidad (los permisos de negocio los resuelve
  access-service). NO activar para: qué puede hacer un usuario dentro del CRM (eso
  es aspnetcore-security-owasp-baseline / el puerto de autorización), modelado de
  datos ni reglas de dominio.
---

# OIDC con Keycloak en ASP.NET Core

## Objetivo

En este proyecto la identidad está partida en dos, y confundir las mitades es el
error caro (D3):

- **Keycloak responde *quién sos*.** Autentica, emite tokens, maneja la sesión de
  login. Los roles del realm **no se usan para autorizar**.
- **`access-service` responde *qué podés hacer dentro del CRM*.** Es la fuente de
  verdad de `UserAccount` y `RoleAssignment`, vinculada por el `sub` del token.

Meter permisos de negocio dentro de Keycloak parece práctico al principio y se
vuelve inmanejable: los permisos de este dominio dependen de datos que viven en
`access-service`, cambian solos (alta, baja, cambio de rol) y necesitan quedar
auditados como cualquier otro aggregate.

Esta skill cubre la primera mitad: cómo se autentica, cómo viajan los tokens y qué
valida cada servicio, usando lo que ya implementó `V2-FND-002`/`V2-FND-003`.

Fuentes: `PRPs/_backlog/2026-09-17-plan-wave-2-acceso-catalogos-party.md` (§5, D3,
D6), `IMPLEMENTATION_REPORT-V2-FND-002.md`, `IMPLEMENTATION_REPORT-V2-FND-003.md`.

## Cuándo activar

- Se configura autenticación en `operations-bff` o en un servicio de dominio.
- Se define o modifica el realm, un client o un mapper de claims.
- Se implementa login o logout.
- Se valida un token entrante.
- Aparece un `401`, un token rechazado o un problema de audiencia o emisor.
- Se levanta Keycloak en Docker Compose o en CI.

## Cuándo NO activar

- Qué permiso de negocio tiene un usuario, ownership, overrides (ver
  `aspnetcore-security-owasp-baseline` y el puerto de autorización de
  `V2-ACL-001a`).
- Reglas de negocio (ver `ddd-hexagonal-architecture`).

## Decisiones del proyecto

| Tema | Decisión |
|---|---|
| Servidor | Keycloak **26.7.4** (`quay.io/keycloak/keycloak:26.7.4` en Compose) |
| Realm | `crm-dev`, importado como código (`infra/keycloak/realm-export/crm-dev-realm.json`), roles Administrador/Vendedor/Responsable Comercial, usuario `dev.vendedor` |
| Fuente de roles (D3) | Keycloak = **solo identidad**. `access-service` es la fuente de verdad de `UserAccount`/`RoleAssignment`, vinculado por `sub`. Los roles del realm **se ignoran para autorizar**. Primer login de un `sub` desconocido → usuario `PENDING` sin permisos, hasta que un Administrador lo habilita en el CRM |
| Alta de usuarios (D3) | El usuario se crea en Keycloak manual o por realm-export (sin integrar la Admin API en la POC); el Administrador lo habilita y le asigna rol **en el CRM**, no en Keycloak |
| Flujo BFF | `AddKeycloakOpenIdConnectCookieAuthentication`: cookie `HttpOnly`, `SameSite=Lax`, `Secure=Always` + Authorization Code (`ResponseType="code"`) contra Keycloak. `SaveTokens=true`: el token queda del lado del BFF, el navegador solo ve la cookie |
| Tokens en el browser | **Nunca** |
| Servicios de dominio | `AddKeycloakJwtBearerAuthentication`: valida el JWT (issuer, audience si se configura, firma vía JWKS del discovery endpoint) y resuelve `IAuthenticationPort` (`ClaimsPrincipalAuthenticationPort`) desde los claims, con `MapInboundClaims = false` para no remapear `sub`/`email`/`name` a URIs largas |
| Identidad BFF→servicios (D6) | **Token relay**: el BFF reenvía el access token del usuario como Bearer; cada servicio valida el JWT y reconstruye `ExecutionContextV1` (`ActorId`, `DisplayName`, `Email`, `Roles`, `Permissions`, `CorrelationId`). Sin client credentials internas en la POC |
| Errores de auth | `401`/`403` se devuelven como `ProblemDetailsV1` vía `ProblemDetailsChallengeWriter` (`OnChallenge`/`OnForbidden` de `JwtBearerEvents`), con `ErrorCodes.Unauthorized`/`ErrorCodes.Forbidden` |
| Direct Access Grants | Habilitado en el client `operations-bff` **solo para pruebas de desarrollo/tests** (password grant); el login real de las webs siempre va por el BFF con cookie de sesión, nunca por password grant directo desde un front |
| Realm en dev | Como código: JSON exportado, importado al arrancar Compose |

### Por qué el BFF guarda los tokens

Si el SPA guarda el access token, cualquier XSS lo roba y el atacante actúa como el
usuario hasta que expire. Con el patrón BFF el navegador solo tiene una cookie
`HttpOnly` que JavaScript no puede leer, y el BFF adjunta el token al llamar a los
servicios de dominio (token relay, D6).

## Estado actual vs target

- **Estado:** `AddKeycloakJwtBearerAuthentication`/
  `AddKeycloakOpenIdConnectCookieAuthentication` (`BuildingBlocks.Infrastructure`,
  `V2-FND-002`) y el realm `crm-dev` con `dev.vendedor` (`V2-FND-003`) existen y
  están probados contra Keycloak real, pero **ningún `Program.cs`** de
  `services/`/`bffs/` los invoca todavía.
- **Target:** `V2-ACL-001` hace el primer wire-up real (JwtBearer en el servicio,
  cookie OIDC en el BFF) y siembra los 3 usuarios de dev (Administrador, Vendedor,
  Responsable Comercial) vinculados al realm (D9).

## Reglas obligatorias

### MUST

- **MUST** usar Authorization Code con **PKCE** en el BFF (activo por defecto en
  ASP.NET Core para code flow: no desactivarlo).
- **MUST** entregar al navegador una cookie de sesión `HttpOnly`, `Secure` y
  `SameSite=Lax` (o más restrictivo); el token nunca la cruza.
- **MUST** validar en cada servicio: **emisor**, **firma** y **vigencia** del
  token; validar **audiencia** cuando el servicio configura una propia.
- **MUST** obtener las claves de firma por el discovery endpoint
  (`.well-known/openid-configuration`) vía `Authority`, no hardcodeadas.
- **MUST** mantener el realm como **código versionado**
  (`infra/keycloak/realm-export/crm-dev-realm.json`), importado de forma
  reproducible al arrancar.
- **MUST** resolver el actor autenticado vía `IAuthenticationPort` (claims del
  token), nunca confiar en un header del request.
- **MUST** implementar el token relay del BFF hacia los servicios (D6): el BFF
  adjunta el access token guardado (`SaveTokens=true`) como Bearer.
- **MUST** tratar los roles del realm de Keycloak como identidad, nunca como
  fuente de autorización de negocio (D3): esa evaluación va al puerto de
  autorización de `V2-ACL-001a`.
- **MUST** usar HTTPS fuera de desarrollo local.

### MUST NOT

- **MUST NOT** entregar access o id tokens al navegador, ni guardarlos en
  `localStorage`, `sessionStorage` o una cookie legible por JavaScript.
- **MUST NOT** usar implicit flow.
- **MUST NOT** usar Direct Access Grants (password grant) para el login real de
  ninguna app: solo está habilitado para pruebas de desarrollo/tests (D3).
- **MUST NOT** desactivar la validación de emisor, firma o vigencia, ni siquiera
  "temporalmente para probar".
- **MUST NOT** autorizar operaciones de negocio con `[Authorize(Roles = "...")]`
  basado en claims de Keycloak: el rol de negocio vive en `access-service`.
- **MUST NOT** configurar el realm a mano por la consola de administración como
  única fuente: se pierde y nadie lo puede reproducir.
- **MUST NOT** commitear secretos reales, ni siquiera de un entorno de prueba.
- **MUST NOT** crear un usuario o asignarle rol de negocio directamente en
  Keycloak: el alta y el rol se gestionan en `access-service` (D3); Keycloak solo
  resuelve login.

## Recomendaciones

### SHOULD

- **SHOULD** limitar los claims a lo mínimo: `sub`, identificación básica. El
  token no es un caché de permisos.
- **SHOULD** tratar `401` (no autenticado) y `403` (autenticado sin permiso) como
  cosas distintas, ambas vía `ProblemDetailsV1`.
- **SHOULD** cubrir con tests de integración el rechazo de tokens: expirado, firma
  inválida, emisor ajeno — usando `[Trait("Category", "RequiresKeycloak")]` +
  `scripts/test-integration.{sh,ps1}`, no un emisor simulado cuando se necesita
  Keycloak real.
- **SHOULD** dejar el realm de desarrollo con usuarios de ejemplo (`dev.vendedor`)
  documentados en `.env.example`/`DEVELOPMENT.md`.

### SHOULD NOT

- **SHOULD NOT** llamar a Keycloak en cada request para validar: la validación de
  firma es local contra el JWKS cacheado por el middleware.

## Anti-patrones prohibidos

### 1. Tokens en el navegador

```javascript
// ❌ Un XSS y el atacante opera como el usuario.
localStorage.setItem("access_token", token);
fetch("/api/v1/parties", { headers: { Authorization: `Bearer ${token}` } });
```

```javascript
// ✅ El BFF tiene la sesión; el browser manda la cookie y nada más.
fetch("/screens/party-detail", { credentials: "include" });
```

### 2. Validación relajada

```csharp
// ❌ Cualquiera que firme un token entra.
services.AddKeycloakJwtBearerAuthentication(bearerOptions =>
{
    bearerOptions.TokenValidationParameters.ValidateIssuer = false;
    bearerOptions.TokenValidationParameters.ValidateLifetime = false;
});
```

```csharp
// ✅ Los controles activos, usando la extensión ya provista.
services.AddKeycloakJwtBearerAuthentication(configuration);
```

### 3. Rol de Keycloak usado para autorizar

```csharp
// ❌ Los permisos de negocio dependen de datos que viven en access-service,
//    no del realm de Keycloak (D3).
[Authorize(Roles = "Administrador")]
public async Task<IActionResult> DeactivateCatalogEntry(Guid id) { /* ... */ }
```

```csharp
// ✅ Keycloak autentica; el puerto de autorización de V2-ACL-001a resuelve el
//    permiso de negocio.
[Authorize] // solo exige estar autenticado
public async Task<IActionResult> DeactivateCatalogEntry(Guid id, CancellationToken ct)
{
    var decision = await _authorization.EvaluateAsync(
        _actor.Current.ActorId, Permissions.CatalogsManage, "catalog_entry", id, ct);

    return decision.Allowed ? await HandleAsync(id, ct) : Forbid();
}
```

### 4. Realm configurado a mano

```text
❌ "Entrá a la consola de Keycloak, creá el realm, después un client
   operations-bff, activá el flujo..."
   Nadie lo reproduce igual y en CI no existe.
```

```yaml
# ✅ Realm exportado y versionado; se importa solo al arrancar.
keycloak:
  image: quay.io/keycloak/keycloak:26.7.4
  command: ["start-dev", "--import-realm"]
  volumes:
    - ./infra/keycloak/realm-export/crm-dev-realm.json:/opt/keycloak/data/import/crm-dev-realm.json:ro
```

### 5. Password grant como login real

```csharp
// ❌ Direct Access Grants solo está habilitado para dev/tests (D3); usarlo desde
//    un front real salta el flujo del BFF.
var form = new Dictionary<string, string>
{
    ["grant_type"] = "password",
    ["username"] = username,
    ["password"] = password,
};
var token = await _http.PostAsync(tokenEndpoint, new FormUrlEncodedContent(form));
```

```csharp
// ✅ El login real siempre pasa por AddKeycloakOpenIdConnectCookieAuthentication
//    en operations-bff (Authorization Code + PKCE).
```

## Checklist antes de devolver código

- [ ] Authorization Code + PKCE en el BFF; sin implicit.
- [ ] Ningún token llega al navegador; la sesión es cookie `HttpOnly`.
- [ ] Emisor, firma y vigencia validados en cada servicio (audiencia si está
      configurada).
- [ ] Las claves salen del discovery endpoint (`Authority`), no hardcodeadas.
- [ ] El realm está versionado como código y se importa solo.
- [ ] Ningún permiso de negocio autorizado con `[Authorize(Roles = "...")]` de
      Keycloak.
- [ ] Client secrets/config por configuración; nada real commiteado.
- [ ] Direct Access Grants usado solo en tests/dev, nunca en el flujo de login real.
- [ ] Tests de rechazo de token cubiertos donde aplique (`RequiresKeycloak`).

## Conexiones con otros skills

| Skill | Relación |
|---|---|
| `aspnetcore-security-owasp-baseline` | Autenticación rota y exposición de tokens son OWASP Top 10; el puerto de autorización decide el permiso. |
| `aspnetcore-rest-layer` | Dónde se aplica `[Authorize]` y cómo se responde `401` vs `403`. |
| `aspnetcore-error-and-observability` | `ProblemDetailsChallengeWriter` produce el `ProblemDetailsV1` de los errores de auth. |
| `dotnet-adversarial-testing` | Tokens manipulados, expirados o de otro emisor. |
