---
name: oidc-keycloak-aspnetcore
description: |
  Activa cuando se configura o se toca la autenticación de este CRM: integración
  OIDC con Keycloak, login de las apps web a través del BFF, validación de tokens
  en los servicios, claims, sesión, refresh, logout y configuración reproducible del
  realm. Triggers: "Keycloak", "OIDC", "OpenID Connect", "OAuth2", "realm",
  "client", "client secret", "confidential client", "public client",
  "AddOpenIdConnect", "AddJwtBearer", "AddAuthentication", "JwtBearerOptions",
  "OpenIdConnectOptions", "Authorization Code", "PKCE", "authorization_code",
  "implicit flow", "access token", "id token", "refresh token", "bearer",
  "validar el token", "TokenValidationParameters", "issuer", "ValidateIssuer",
  "audience", "ValidateAudience", "audiencia del token", "firma del token", "JWKS",
  "discovery endpoint", ".well-known/openid-configuration", "claims", "claim
  mapper", "login", "logout", "single sign-out", "sesión", "cookie de sesión",
  "token expirado", "401", "WWW-Authenticate", "realm export", "importar realm".
  Garantiza que los tokens no lleguen nunca al navegador, que todo servicio valide
  emisor, audiencia, firma y vigencia, que el realm sea reproducible como código y
  que en Keycloak no se modelen reglas de negocio. NO activar para: decidir qué
  puede hacer un usuario dentro de la inmobiliaria (eso es
  multitenancy-authorization), modelado de datos ni reglas de dominio.
---

# OIDC con Keycloak en ASP.NET Core

## Objetivo

En este proyecto la identidad está partida en dos, y confundir las mitades es el
error caro:

- **Keycloak responde *quién sos*.** Autentica, emite tokens, maneja la sesión.
- **`access-service` responde *qué podés hacer dentro de la inmobiliaria*.** Rol,
  unidad organizacional, ownership de cartera y overrides.

Meter el organigrama de la inmobiliaria dentro de Keycloak parece práctico al
principio y se vuelve inmanejable: los permisos de este dominio dependen de datos
de negocio que viven en `access-service`, cambian solos y necesitan auditoría.

Esta skill cubre la primera mitad: cómo se autentica, cómo viajan los tokens y qué
valida cada servicio.

Fuentes: [`ARCHITECTURE.md`](../../../ARCHITECTURE.md) §5 y §10,
[`POC_TECH_DECISIONS.md`](../../../docs/implementation/POC_TECH_DECISIONS.md).

## Cuándo activar

- Se configura autenticación en un BFF o en un servicio.
- Se define o modifica el realm, un client o un mapper de claims.
- Se implementa login, logout o renovación de sesión.
- Se valida un token entrante.
- Aparece un `401`, un token rechazado o un problema de audiencia o emisor.
- Se levanta Keycloak en Docker Compose o en CI.

## Cuándo NO activar

- Permisos, scopes organizacionales, ownership, overrides, filtros por tenant (ver
  `multitenancy-authorization`).
- Reglas de negocio (ver `ddd-hexagonal-architecture`).
- Configuración general y secretos no relacionados con OIDC (ver
  `aspnetcore-config-and-secrets`).

## Decisiones del proyecto

| Tema | Decisión |
|---|---|
| Servidor | **Keycloak 26.7.x Community**, tag fijo. Mínimo **26.7.2** por los CVE corregidos ahí |
| Realm | **Un realm de plataforma** para toda la POC. No un realm por inmobiliaria |
| Multi-tenancy | El tenant es un **claim**, no un realm. La separación real la hace `tenantId` en los datos |
| Flujo web | **Authorization Code + PKCE**, iniciado y terminado en el **BFF** |
| Tokens en el browser | **Nunca.** El navegador recibe una cookie de sesión `HttpOnly`; los tokens quedan en el BFF |
| Clients | Uno **confidencial** por BFF (`operations-bff`, `platform-admin-bff`) |
| Servicios de dominio | Validan **JWT bearer**: emisor, audiencia, firma y vigencia |
| Claims | Mínimos: `sub`, `tenantId`, identificación básica. **Sin organigrama ni permisos de negocio** |
| Autorización | Fuera de Keycloak: la resuelve `access-service` |
| Realm en dev | **Como código**: JSON exportado, importado al arrancar. Cero clics manuales |
| Secretos | Client secrets por configuración. En dev, ficticios y evidentes |

### Por qué el BFF guarda los tokens

Si el SPA guarda el access token, cualquier XSS lo roba y el atacante actúa como el
usuario hasta que expire. Con el patrón BFF el navegador solo tiene una cookie
`HttpOnly` que JavaScript no puede leer, y el BFF adjunta el token al llamar a los
servicios. Es una decisión de seguridad, no de comodidad, y encaja con que el
proyecto ya tiene un BFF por experiencia.

## Estado actual vs target

- **Estado:** no implementado. `FND-004` levanta Keycloak; `FND-005` los shells web
  y sus BFFs; `FND-006` el adapter OIDC y el contexto de actor.
- **Target:** `docker compose up` deja un realm listo e idéntico para todos; los
  BFFs autentican por código + PKCE; los servicios validan bearer y delegan la
  autorización de negocio en `access-service`.

## Reglas obligatorias

### MUST

- **MUST** usar Authorization Code con **PKCE**. En ASP.NET Core viene activo por
  defecto para code flow: no desactivarlo.
- **MUST** terminar el flujo en el BFF y entregar al navegador una cookie de sesión
  `HttpOnly`, `Secure` y `SameSite` restrictivo.
- **MUST** validar en cada servicio: **emisor**, **audiencia**, **firma** y
  **vigencia** del token.
- **MUST** declarar una audiencia propia por servicio y rechazar tokens emitidos
  para otra.
- **MUST** obtener las claves de firma por el **discovery endpoint**
  (`.well-known/openid-configuration`) y dejar que el middleware rote el JWKS.
- **MUST** mantener el realm como **código versionado**, importado de forma
  reproducible al arrancar.
- **MUST** derivar el `tenantId` del token y pasarlo al `ActorContext`, nunca del
  request.
- **MUST** tomar los client secrets de configuración, jamás del código ni del
  repositorio.
- **MUST** implementar logout que invalide la sesión del BFF **y** la de Keycloak.
- **MUST** usar HTTPS fuera de desarrollo local.

### MUST NOT

- **MUST NOT** entregar access, id o refresh tokens al navegador, ni guardarlos en
  `localStorage`, `sessionStorage` o una cookie legible por JavaScript.
- **MUST NOT** usar implicit flow ni resource owner password credentials.
- **MUST NOT** desactivar `ValidateIssuer`, `ValidateAudience`, `ValidateLifetime`
  o la validación de firma, ni siquiera "temporalmente para probar".
- **MUST NOT** modelar roles de negocio, unidades organizacionales, carteras ni
  overrides dentro de Keycloak.
- **MUST NOT** autorizar operaciones de negocio con un `[Authorize(Roles = "...")]`
  basado en claims de Keycloak.
- **MUST NOT** configurar el realm a mano por la consola de administración como
  única fuente: se pierde y nadie lo puede reproducir.
- **MUST NOT** commitear secretos reales, ni siquiera de un entorno de prueba.
- **MUST NOT** aceptar un token cuyo emisor no sea exactamente el esperado, aunque
  la firma valide.
- **MUST NOT** usar un realm por inmobiliaria: el aislamiento lo da `tenantId`, no
  la topología de Keycloak.

## Recomendaciones

### SHOULD

- **SHOULD** mantener los access tokens de vida corta y resolver la continuidad con
  refresh en el BFF, no alargando el token.
- **SHOULD** limitar los claims a lo mínimo: un token no es un perfil de usuario ni
  un caché de permisos.
- **SHOULD** exponer el `tenantId` con un claim mapper explícito y nombre estable,
  documentado en `FND-003`.
- **SHOULD** tratar el `401` (no autenticado) y el `403` (autenticado sin permiso)
  como cosas distintas, y recordar que un recurso de otro tenant devuelve `404`.
- **SHOULD** cubrir con tests de integración el rechazo de tokens: expirado, firma
  inválida, emisor ajeno, audiencia ajena.
- **SHOULD** levantar Keycloak con Testcontainers en las pruebas que realmente
  necesitan un emisor real, y usar un emisor de prueba en el resto.
- **SHOULD** dejar el realm de desarrollo con usuarios y tenants de ejemplo, para
  que cualquiera clone y pueda entrar sin pedirle credenciales a nadie.

### SHOULD NOT

- **SHOULD NOT** compartir un mismo client entre `crm-web` y `platform-admin-web`:
  son experiencias con audiencias y riesgos distintos.
- **SHOULD NOT** llamar a Keycloak en cada request para validar: la validación de
  firma es local contra el JWKS cacheado.

## Anti-patrones prohibidos

### 1. Tokens en el navegador

```javascript
// ❌ Un XSS y el atacante opera como el usuario.
localStorage.setItem("access_token", token);
fetch("/api/parties", { headers: { Authorization: `Bearer ${token}` } });
```

```javascript
// ✅ El BFF tiene la sesión; el browser manda la cookie y nada más.
fetch("/bff/parties", { credentials: "include" });
```

### 2. Validación relajada

```csharp
// ❌ Cualquiera que firme un token entra.
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = false,
    ValidateAudience = false,
    ValidateLifetime = false
};
```

```csharp
// ✅ Los cuatro controles activos y explícitos.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = cfg.Authority;          // realm de Keycloak
        options.Audience  = cfg.ServiceAudience;    // audiencia propia del servicio
        options.RequireHttpsMetadata = !env.IsDevelopment();

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = cfg.Authority,
            ValidateAudience = true,
            ValidAudience = cfg.ServiceAudience,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
```

### 3. Organigrama dentro de Keycloak

```csharp
// ❌ Los permisos de este dominio dependen de unidad, cartera y overrides
//    vigentes. Nada de eso vive en el token ni puede vivir ahí.
[Authorize(Roles = "MANAGER_SUCURSAL_CENTRO")]
public async Task<IActionResult> ViewUnitPortfolio() { /* ... */ }
```

```csharp
// ✅ Keycloak autentica; access-service autoriza.
[Authorize]                                   // solo exige estar autenticado
public async Task<IActionResult> ViewUnitPortfolio(CancellationToken ct)
{
    var decision = await _authorization.EvaluateAsync(
        _actor.Current, Action.ViewPortfolio, Resource.OrganizationalUnit, unitId, ct);

    return decision.Allowed ? Ok(await _query.RunAsync(ct)) : Forbid();
}
```

### 4. Realm configurado a mano

```text
❌ "Entrá a la consola de Keycloak, creá el realm crm, después un client
   operations-bff, activá el flujo, agregá el mapper de tenantId..."
   Nadie lo reproduce igual y en CI no existe.
```

```yaml
# ✅ Realm exportado y versionado; se importa solo al arrancar.
keycloak:
  image: quay.io/keycloak/keycloak:26.7.2
  command: ["start-dev", "--import-realm"]
  volumes:
    - ./infra/keycloak/realm-crm.json:/opt/keycloak/data/import/realm-crm.json:ro
```

### 5. Confiar en el token sin verificar la audiencia

```csharp
// ❌ Un token válido emitido para platform-admin-bff abre operations-bff.
//    La firma valida: el problema es que nadie miró para quién era.
options.TokenValidationParameters.ValidateAudience = false;
```

```csharp
// ✅ Cada servicio declara su audiencia y rechaza las demás.
options.Audience = "party-service";
```

### 6. Alargar el token en vez de refrescar

```csharp
// ❌ Un access token de 12 horas no se puede revocar de hecho.
//    Si el usuario se va de la inmobiliaria, sigue entrando toda la tarde.
```

```csharp
// ✅ Token corto, refresh en el BFF, y la autorización se revalida contra
//    access-service, que sí ve los cambios de membership.
```

## Checklist antes de devolver código

- [ ] Authorization Code + PKCE; sin implicit ni password grant.
- [ ] Ningún token llega al navegador; la sesión es cookie `HttpOnly`.
- [ ] Emisor, audiencia, firma y vigencia validados en cada servicio.
- [ ] Cada servicio declara su propia audiencia.
- [ ] Las claves salen del discovery endpoint, no hardcodeadas.
- [ ] El realm está versionado como código y se importa solo.
- [ ] `tenantId` sale del token y va al `ActorContext`.
- [ ] Ningún rol o permiso de negocio modelado en Keycloak.
- [ ] Client secrets por configuración; nada real commiteado.
- [ ] Logout invalida sesión del BFF y de Keycloak.
- [ ] Tests de rechazo: token expirado, firma inválida, emisor ajeno, audiencia
      ajena.
- [ ] La imagen de Keycloak tiene tag fijo, mínimo 26.7.2.

## Conexiones con otros skills

| Skill | Relación |
|---|---|
| `multitenancy-authorization` | Keycloak dice **quién**; esa skill, **qué puede hacer**. El `tenantId` del token alimenta el `ActorContext`. |
| `aspnetcore-security-owasp-baseline` | Autenticación rota y exposición de tokens son OWASP Top 10. |
| `aspnetcore-config-and-secrets` | Authority, audiencias y client secrets son configuración, validada al arranque. |
| `aspnetcore-rest-layer` | Dónde se aplica `[Authorize]` y cómo se responde `401` vs `403`. |
| `aspnetcore-outgoing-http` | El BFF adjunta el token al llamar a los servicios de dominio. |
| `dotnet-adversarial-testing` | Tokens manipulados, expirados, de otro emisor o de otra audiencia. |
