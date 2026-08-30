---
name: aspnetcore-config-and-secrets
description: |
  Activa cuando se lee configuración, secretos o variables de entorno en un
  microservicio ASP.NET Core 8 con el arquetipo epa-net-paas, o cuando se definen
  opciones tipadas o presupuestos de recursos del proceso. Triggers: "IOptions<T>",
  "IOptionsSnapshot<T>", "IOptionsMonitor<T>", "IConfiguration", "appsettings.json",
  "appsettings.Development.json", "appsettings.Production.json", "appsettings.{env}",
  "ASPNETCORE_ENVIRONMENT", "X509_CERTIFICATE__{i}", "MASKED_DATA", "APP_ID",
  "APP_KEY", "CORS_POLICY_ORIGINS", "CORS_POLICY_METHODS", "CORS_POLICY_HEADERS",
  "CORS_POLICY_NAME", "Options pattern", "Bind", "ValidateDataAnnotations",
  "ValidateOnStart", "ConfigurationException", "secreto", "credencial", "API key",
  "connection string", "timeout", "pool size", "Vault", "K8s secrets",
  "user-secrets", "leer config", "variable de entorno", "fail-fast",
  "validar config al arranque", "valores hardcodeados", "magic number",
  "epa-net-paas config", "configuración tipada", "epa-net-paas". Garantiza que toda
  la config se valide al arranque (fail-fast con ValidateOnStart), no haya secretos
  ni valores mágicos hardcodeados, y los presupuestos de recursos sean explícitos y
  por entorno. MUST referenciar epa-net-paas. NO activar para: constantes de dominio
  puro que no cambian por entorno, ni valores de presentación sin impacto en recursos
  o seguridad.
---

# ASP.NET Core Config & Secrets

## Objetivo

Un microservicio en producción falla de dos formas evitables:

1. **Config inválida descubierta tarde:** una env var faltante o mal tipada que
   explota en runtime cuando llega el primer request que la usa, en vez de al
   arranque.

2. **Recursos sin presupuesto:** timeouts infinitos, pools sin límite, cuerpos de
   request sin tope. Bajo carga esto degrada o tumba el proceso.

Este skill exige: **validar toda la config al arranque (fail-fast con `ValidateOnStart()`)**,
**cero secretos/valores mágicos hardcodeados**, y **presupuestos de recursos explícitos
y parametrizados por entorno**. Aplica al arquetipo `epa-net-paas` y sus env vars
específicas (`X509_CERTIFICATE__{i}`, `MASKED_DATA`, `CORS_POLICY_*`, `APP_ID`,
`APP_KEY`).

## Cuándo activar

- Se define una clase de opciones con `IOptions<T>`, `IOptionsSnapshot<T>` o `IOptionsMonitor<T>`.
- Se lee una env var directamente desde `IConfiguration` o `Environment.GetEnvironmentVariable`.
- Se configura un cliente HTTP, pool de conexiones, pool de DB, o timeout.
- Se introduce un valor que varía entre dev/qa/prod.
- Hay valores numéricos o strings "mágicos" sin nombre ni origen claro.
- Se manejan secretos (passwords de DB, API keys, tokens, certificados).
- Se usan variables de entorno EPA: `ASPNETCORE_ENVIRONMENT`, `X509_CERTIFICATE__{i}`,
  `MASKED_DATA`, `CORS_POLICY_ORIGINS/METHODS/HEADERS/NAME`, `APP_ID`, `APP_KEY`.

## Cuándo NO activar

- Constantes de dominio que no dependen del entorno (códigos de error de negocio, enums fijos).
- Strings de presentación o i18n sin impacto en recursos o seguridad.

## Estado actual vs target

- **Target:** ASP.NET Core 8 con el patrón Options (`IOptions<T>`), binding tipado
  vía `.Bind()` o `services.Configure<T>()`, validación al arranque con
  `.ValidateDataAnnotations().ValidateOnStart()`, secretos inyectados por Vault o
  Kubernetes Secrets (nunca en `appsettings.json` commiteado con valores reales),
  `user-secrets` de .NET en desarrollo local, variables de entorno EPA consumidas
  y expuestas por el arquetipo `epa-net-paas`.
- Las reglas aplican a cualquier versión ASP.NET Core 6+.

## Decisiones del proyecto

- **Patrón Options para toda config tipada:** crear una clase POCO por grupo de
  config relacionada, bindearla con `services.Configure<T>(section)` y anotarla
  con Data Annotations para validación automática.
- **`IOptions<T>`** para config que no cambia tras el arranque (la más común).
- **`IOptionsSnapshot<T>`** para config que puede cambiar entre requests (reload
  en vivo desde `appsettings.{env}.json`); válido en servicios Scoped.
- **`IOptionsMonitor<T>`** para notificaciones de cambio en servicios Singleton
  o en background workers de larga vida.
- **`.ValidateDataAnnotations().ValidateOnStart()`** es el estándar del proyecto:
  Data Annotations expresan los constraints; `ValidateOnStart()` hace que una
  config inválida cancele el arranque con `OptionsValidationException` en vez de
  fallar silenciosamente en runtime. Lanzar `ConfigurationException` EPA si la
  validación personalizada detecta config crítica ausente.
- **Secretos nunca commiteados:** usar `dotnet user-secrets` en desarrollo local,
  Vault o Kubernetes Secrets en QA/prod. La key en `appsettings.json` puede
  existir como placeholder vacío o sin valor real; el valor llega por la jerarquía
  de configuración en runtime.
- **Jerarquía de configuración ASP.NET Core** (de menor a mayor prioridad):
  `appsettings.json` → `appsettings.{env}.json` → variables de entorno → command
  line args. Los secretos del orquestador sobreescriben siempre los defaults de
  archivos.
- **Variables de entorno EPA del arquetipo `epa-net-paas`:** consumidas por el
  arquetipo automáticamente; el equipo no debe redefinirlas manualmente a menos
  que sea necesario. Ejemplos: `CORS_POLICY_ORIGINS` configura los orígenes CORS
  permitidos, `MASKED_DATA` enumera los campos a enmascarar en logs, `APP_ID` y
  `APP_KEY` son procesados por el pipeline del arquetipo.

## Reglas obligatorias

### MUST

1. **MUST usar el patrón Options con `IOptions<T>`** (o sus variantes) para
   consumir config tipada. No leer `IConfiguration["clave"]` disperso en múltiples
   clases.

2. **MUST llamar `.ValidateDataAnnotations().ValidateOnStart()`** al registrar
   cada clase de opciones con constraints de validación. El proceso debe fallar
   al arrancar si la config es inválida, no cuando llega el primer request.

3. **MUST externalizar secretos a Vault o Kubernetes Secrets.** Nunca commitear
   credenciales con valor real en `appsettings.json` ni en `appsettings.{env}.json`.
   Usar `dotnet user-secrets` en desarrollo local.

4. **MUST externalizar todos los presupuestos de recursos** (timeouts, pool sizes,
   límites de concurrencia, tamaños de cuerpo de request) en la config con defaults
   razonables y documentados.

5. **MUST lanzar `ConfigurationException` EPA** si una validación personalizada
   detecta una config crítica ausente o inconsistente (ej.: certificado requerido
   no presente). No continuar con config parcial.

6. **MUST NO loggear valores de config sensibles** (passwords, tokens, API keys,
   valores de `APP_KEY`). Usar `MASKED_DATA` para los campos cubiertos por el
   arquetipo; para el resto, excluir el dato antes de llegar al logger.

7. **MUST elegir la variante de `IOptions<T>` correcta:**
   - `IOptions<T>` para config que se lee una vez al arranque y no cambia.
   - `IOptionsSnapshot<T>` en servicios Scoped que necesitan la config actualizada
     en cada request (reload en vivo).
   - `IOptionsMonitor<T>` en servicios Singleton o backgrounds de larga vida que
     deben reaccionar a cambios de config sin reiniciar.

### MUST NOT

8. **MUST NOT hardcodear secretos en el código** (passwords, API keys, tokens,
   connection strings completas). Ni en constantes, ni en constructores, ni en
   `appsettings.json` con valores reales.

9. **MUST NOT usar `IConfiguration["clave"]` disperso** para grupos de propiedades
   relacionadas. Crear una clase Options y bindear la sección completa.

10. **MUST NOT usar valores mágicos** (números o strings sin nombre) para configurar
    recursos. `5000` como timeout → `TimeSpan` en la clase Options con el nombre
    correcto y un rango documentado.

11. **MUST NOT tomar decisiones de lógica de negocio basadas en `ASPNETCORE_ENVIRONMENT`**
    (`if (env.IsProduction())`). Los perfiles de entorno son para config, no para
    branches de lógica.

## Recomendaciones

### SHOULD

- Documentar el default y el rango válido de cada propiedad de recurso crítica
  como comentario en XML doc o en el propio `appsettings.json` de ejemplo:
  `"ReadTimeoutSeconds": 10 // [1-30]`.
- Agregar un `appsettings.example.json` (o equivalente) en el repo con todas las
  keys necesarias y placeholders, para que sea obvio qué se debe configurar antes
  de arrancar.
- Preferir `TimeSpan` o tipos semánticos sobre `int` de milisegundos o segundos
  para duraciones en las clases Options: `ReadTimeout = TimeSpan.FromSeconds(10)`.
- Validar con `IValidateOptions<T>` para reglas cross-property que Data Annotations
  no puede expresar (ej.: si `MaxRetries > 0` entonces `RetryDelayMs` no puede
  ser cero).

### SHOULD NOT

- No mezclar configuración de infraestructura y de negocio en la misma clase
  Options: separar `HttpClientOptions` de `PaymentPolicyOptions`.
- No sobrecargar `ASPNETCORE_ENVIRONMENT` con nombres de entornos no estándar sin
  documentarlos: el arquetipo puede comportarse diferente según ese valor.

## Anti-patrones prohibidos

❌ `IConfiguration["clave"]` disperso sin validación:

```csharp
public class SaldoClient(IConfiguration config)
{
    public async Task<SaldoDto> GetAsync(string cuentaId, CancellationToken ct)
    {
        var url = config["SaldoService:Url"];        // ❌ no validado, puede ser null en runtime
        var timeout = int.Parse(config["SaldoService:TimeoutMs"]); // ❌ int disperso, magic number implícito
        // ...
    }
}
```

✅ Patrón Options con validación al arranque:

```csharp
// Clase de opciones con constraints:
public class SaldoClientOptions
{
    [Required, Url]
    public string BaseUrl { get; set; } = string.Empty;

    [Range(100, 30_000)]
    public int TimeoutMs { get; set; } = 5_000; // default razonable documentado [100-30000]
}

// Registro en Program.cs:
builder.Services.Configure<SaldoClientOptions>(
    builder.Configuration.GetSection("SaldoClient"))
    .ValidateDataAnnotations()
    .ValidateOnStart(); // ✅ falla en startup si la config es inválida

// Consumo en el service:
public class SaldoClient(IOptions<SaldoClientOptions> opts, HttpClient http)
{
    private readonly SaldoClientOptions _opts = opts.Value;
    // ...
}
```

❌ Secreto hardcodeado en appsettings.json commiteado:

```json
{
  "Database": {
    "ConnectionString": "Server=prod-db;Password=Sup3rS3cr3t!" // ❌ commiteado con valor real
  }
}
```

✅ Placeholder en appsettings + secreto inyectado por el orquestador:

```json
// appsettings.json (commiteado, sin valor sensible):
{
  "Database": {
    "ConnectionString": "" // ✅ vacío; el valor real llega vía variable de entorno o K8s Secret
  }
}
```

```csharp
// Validación al arranque detecta el vacío:
public class DatabaseOptions
{
    [Required]
    public string ConnectionString { get; set; } = string.Empty; // ✅ Required → falla en startup si está vacío
}
```

❌ `IOptionsMonitor<T>` en un servicio Scoped (no tiene sentido semántico):

```csharp
// Registro: builder.Services.AddScoped<IPagoService, PagoService>();
public class PagoService(IOptionsMonitor<PagoOptions> monitor) : IPagoService
{
    // ❌ IOptionsMonitor es para larga vida (Singleton/backgrounds);
    //    en un Scoped usar IOptionsSnapshot<T>
}
```

✅ Variante correcta según el lifetime:

```csharp
// Servicio Scoped → IOptionsSnapshot<T> para config recargable por request:
public class PagoService(IOptionsSnapshot<PagoOptions> snapshot) : IPagoService
{
    private readonly PagoOptions _opts = snapshot.Value; // ✅ valor fresco por request si hay reload
}

// Servicio Singleton / background → IOptionsMonitor<T> para reaccionar a cambios:
public class TokenRefreshWorker(IOptionsMonitor<TokenOptions> monitor) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        monitor.OnChange(opts => _logger.LogInformation("Config de token actualizada")); // ✅
        // ...
    }
}
```

## Checklist antes de devolver código

- [ ] Toda config tipada usa una clase Options; no hay `IConfiguration["clave"]` disperso.
- [ ] Se llama `.ValidateDataAnnotations().ValidateOnStart()` al registrar las opciones.
- [ ] No hay secretos con valor real en `appsettings.json` commiteado.
- [ ] Los timeouts y pool sizes están en la clase Options, no hardcodeados.
- [ ] Los defaults de propiedades críticas son razonables y están documentados.
- [ ] Se eligió la variante correcta de `IOptions<T>` según el lifetime del servicio.
- [ ] Se lanza `ConfigurationException` EPA si falta config crítica en validaciones personalizadas.

## Conexiones con otros skills

- `aspnetcore-di-and-middleware-pipeline` — `IOptions<T>` es un bean del DI container; los servicios Options se registran en `Program.cs`.
- `aspnetcore-outgoing-http` — URLs base y timeouts de clientes HTTP salientes desde la clase Options.
- `aspnetcore-database-access-efcore` — connection string y pool size de EF Core desde config.
- `aspnetcore-security-owasp-baseline` — secretos y credenciales gestionados de forma segura; env vars EPA de CORS y tokens.
- `aspnetcore-error-and-observability` — `ConfigurationException` EPA para fallo rápido ante config inválida; no loggear config sensible.
