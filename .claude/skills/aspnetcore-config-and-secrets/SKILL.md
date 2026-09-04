---
name: aspnetcore-config-and-secrets
description: |
  Activa cuando se lee configuración, se manejan secretos o se definen opciones
  tipadas en un servicio o BFF de este CRM. Triggers: "IOptions<T>",
  "IOptionsSnapshot<T>", "IOptionsMonitor<T>", "IConfiguration", "appsettings.json",
  "appsettings.Development.json", "ASPNETCORE_ENVIRONMENT", "variable de entorno",
  "Options pattern", "Bind", "ValidateDataAnnotations", "ValidateOnStart",
  "ConfigurationException", "secreto", "credencial", "client secret", "API key",
  "connection string", "user-secrets", "leer config", "valores hardcodeados",
  "magic number", "timeout", "pool size", "configuración tipada", "fail-fast",
  "validar config al arranque", "CORS", "orígenes permitidos", "config por entorno".
  Garantiza que toda configuración se valide al arranque, que no haya secretos ni
  valores mágicos en el código, y que los presupuestos de recursos sean explícitos y
  por entorno. NO activar para: constantes de dominio puro ni valores de
  presentación sin impacto en recursos o seguridad.
---

# Configuración y Secretos

## Objetivo

Una configuración inválida debe **impedir que el servicio levante**, no fallar tres
horas después cuando alguien use la función que la necesitaba.

Y en un proyecto con veinte servicios y un repositorio compartido, la otra regla es
que **ningún secreto real entre al repositorio**, ni siquiera de un entorno de
prueba.

Fuentes: [`POC_TECH_DECISIONS.md`](../../../docs/implementation/POC_TECH_DECISIONS.md),
[`ARCHITECTURE.md`](../../../ARCHITECTURE.md) §13.

## Cuándo activar

- Se lee un valor de configuración.
- Se define una clase de opciones.
- Se maneja un secreto: connection string, client secret, credencial.
- Se parametriza un timeout, un tamaño de página, un prefetch o un TTL.
- Se agrega una variable de entorno al compose.

## Cuándo NO activar

- Constantes de dominio que no cambian por entorno.
- Valores de presentación sin impacto en recursos ni seguridad.
- Configuración de autenticación en sí (ver `oidc-keycloak-aspnetcore`).

## Decisiones del proyecto

| Tema | Decisión |
|---|---|
| Patrón | Options pattern con clases tipadas por área |
| Validación | `ValidateDataAnnotations()` + **`ValidateOnStart()`**. Fail-fast siempre |
| Desarrollo | `appsettings.Development.json` con valores locales y **secretos ficticios evidentes** |
| Secretos reales | Variables de entorno. Nunca en el repositorio |
| Local | `dotnet user-secrets` cuando alguien necesita un valor propio |
| Origen | `docker compose` provee las variables de la POC |
| Excepción | `ConfigurationException` propia, definida en `building-blocks` |
| CORS | Orígenes permitidos por configuración tipada, nunca `AllowAnyOrigin()` |

### Qué es configuración en este proyecto

```text
Mongo:ConnectionString      Mongo:Database
RabbitMq:Uri                RabbitMq:PrefetchCount
Auth:Authority              Auth:Audience         Auth:ClientSecret
Cors:AllowedOrigins
Outbox:PollingInterval      Outbox:RetentionDays
Paging:MaxPageSize
Authorization:CacheTtl
```

Todo lo que cambia entre desarrollo, CI y un despliegue real, más todo presupuesto
de recursos: timeouts, tamaños de página, prefetch, TTL.

## Estado actual vs target

- **Estado:** no implementado. `FND-004` define las variables del compose;
  `FND-001` la estructura donde viven.
- **Target:** cada servicio declara sus opciones tipadas, validadas al arranque, y
  no levanta si falta algo crítico.

## Reglas obligatorias

### MUST

- **MUST** usar clases de opciones tipadas, no `IConfiguration["clave"]` disperso.
- **MUST** validar al arranque con `ValidateOnStart()`: el servicio no levanta con
  configuración inválida.
- **MUST** tomar todo secreto de configuración, nunca del código.
- **MUST** hacer explícito todo presupuesto de recursos: timeout, tamaño máximo de
  página, prefetch, TTL de caché.
- **MUST** usar `IOptions<T>` para valores fijos y `IOptionsMonitor<T>` solo cuando
  el valor realmente puede cambiar en caliente.
- **MUST** lanzar `ConfigurationException` cuando una validación propia falla.
- **MUST** mantener los valores de desarrollo **ficticios y evidentes**, para que
  nadie confunda uno con un secreto real.
- **MUST** documentar cada variable nueva en el compose y en el README de desarrollo.

### MUST NOT

- **MUST NOT** commitear un secreto real, ni siquiera de un entorno de prueba o
  "temporalmente".
- **MUST NOT** hardcodear URLs, credenciales, timeouts ni límites en el código.
- **MUST NOT** loguear valores de configuración sensibles, ni al arranque para
  "verificar que cargó".
- **MUST NOT** usar `IConfiguration` directamente dentro del dominio: la
  configuración entra por opciones tipadas en el composition root.
- **MUST NOT** dejar que el servicio arranque con configuración faltante y falle
  después.
- **MUST NOT** usar `AllowAnyOrigin()` en CORS.
- **MUST NOT** usar el mismo secreto en desarrollo y en un entorno real.

## Recomendaciones

### SHOULD

- **SHOULD** agrupar las opciones por área (`MongoOptions`, `RabbitMqOptions`,
  `AuthOptions`), no una clase gigante.
- **SHOULD** dar defaults sensatos a lo opcional y **ninguno** a lo obligatorio: si
  falta, tiene que fallar.
- **SHOULD** usar `[Required]`, `[Range]` y `[Url]` en las opciones para que la
  validación sea declarativa.
- **SHOULD** mantener una sola lista de variables por servicio, visible en el
  compose y en el README.
- **SHOULD** testear que el servicio **no arranca** con configuración inválida: es
  tan importante como que arranque con la válida.

### SHOULD NOT

- **SHOULD NOT** leer configuración en cada uso: se inyecta una vez.
- **SHOULD NOT** poner en configuración algo que nunca cambia entre entornos: eso es
  una constante.

## Anti-patrones prohibidos

### 1. Secreto en el repositorio

```json
// ❌ appsettings.json commiteado
{
  "Auth": { "ClientSecret": "8f2c1a94-real-secret-de-keycloak" }
}
```

```json
// ✅ appsettings.Development.json, ficticio y evidente
{
  "Auth": { "ClientSecret": "dev-not-a-real-secret" }
}
```

```yaml
# ✅ el valor real llega por entorno
environment:
  Auth__ClientSecret: ${AUTH_CLIENT_SECRET}
```

### 2. Sin validación al arranque

```csharp
// ❌ Levanta bien y falla al primer request que necesite el connection string.
builder.Services.Configure<MongoOptions>(builder.Configuration.GetSection("Mongo"));
```

```csharp
// ✅ Si falta o es inválido, el servicio no arranca.
builder.Services.AddOptions<MongoOptions>()
    .Bind(builder.Configuration.GetSection("Mongo"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

public sealed class MongoOptions
{
    [Required] public string ConnectionString { get; init; } = default!;
    [Required] public string Database { get; init; } = default!;
}
```

### 3. Valores mágicos

```csharp
// ❌ ¿Por qué 30? ¿Cambia por entorno? Nadie sabe.
var results = await _col.Find(filter).Limit(500).ToListAsync(ct);
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
```

```csharp
// ✅ Presupuestos explícitos y configurables.
var results = await _col.Find(filter).Limit(_paging.Value.MaxPageSize).ToListAsync(ct);
using var cts = new CancellationTokenSource(_http.Value.Timeout);
```

### 4. Configuración logueada al arranque

```csharp
// ❌ El client secret queda en el sistema de logs para siempre.
_logger.LogInformation("Configuración cargada: {@Options}", options);
```

```csharp
// ✅ Solo lo no sensible, y solo si aporta.
_logger.LogInformation(
    "Mongo configurado db={Database} rabbit={Host}",
    mongo.Database, rabbit.HostName);
```

### 5. `IConfiguration` dentro del dominio

```csharp
// ❌ El dominio pasa a depender del sistema de configuración.
public sealed class Requirement(IConfiguration config)
{
    private readonly int _maxCriteria = config.GetValue<int>("Demand:MaxCriteria");
}
```

```csharp
// ✅ El límite entra como dato al construir, desde la capa de aplicación.
public sealed class Requirement
{
    public static Requirement Create(IReadOnlyList<Criterion> criteria, int maxCriteria)
    { /* ... */ }
}
```

### 6. Default peligroso en algo obligatorio

```csharp
// ❌ Si falta la variable, el servicio apunta a localhost sin que nadie lo note.
public string ConnectionString { get; init; } = "mongodb://localhost:27017";
```

```csharp
// ✅ Sin default: si falta, falla al arrancar.
[Required] public string ConnectionString { get; init; } = default!;
```

## Checklist antes de devolver código

- [ ] Toda configuración se lee por opciones tipadas.
- [ ] Toda opción se valida con `ValidateOnStart()`.
- [ ] Ningún secreto real en el repositorio; los de desarrollo son evidentemente
      ficticios.
- [ ] Ninguna URL, credencial, timeout o límite hardcodeado.
- [ ] Ningún valor sensible logueado.
- [ ] `IConfiguration` no aparece en el dominio.
- [ ] Lo obligatorio no tiene default.
- [ ] CORS con orígenes explícitos.
- [ ] Las variables nuevas están en el compose y documentadas.
- [ ] Hay test de que el servicio no arranca con configuración inválida.

## Conexiones con otros skills

| Skill | Relación |
|---|---|
| `aspnetcore-di-and-middleware-pipeline` | Registro y validación de opciones en el composition root. |
| `aspnetcore-security-owasp-baseline` | Secretos, CORS y credenciales son superficie de seguridad. |
| `oidc-keycloak-aspnetcore` | Authority, audiencias y client secrets son configuración. |
| `mongodb-dotnet-driver` | El connection string es un secreto. |
| `rabbitmq-dotnet` | URI del broker y prefetch son configuración. |
| `aspnetcore-error-and-observability` | `ConfigurationException` para fallo rápido; no loguear config sensible. |
