---
name: aspnetcore-di-and-middleware-pipeline
description: |
  Activa cuando se trabaja con el contenedor de inyección de dependencias de
  ASP.NET Core o con el pipeline de middleware en Program.cs: registrar servicios,
  configurar lifetimes, ordenar middleware, o resolver problemas del contenedor.
  Triggers: "IServiceCollection", "AddTransient", "AddScoped", "AddSingleton",
  "AddPaaS", "UsePaas", "middleware", "pipeline", "Program.cs", "IOptions<T>",
  "IHostedService", "BackgroundService", "captive dependency", "dependencia
  capturada", "Scoped en Singleton", "IDisposable", "lifetime", "DI container",
  "inyección de dependencias", "registrar servicio", "DependencyInjectionException",
  "ObjectDisposedException", "IServiceScopeFactory". Garantiza lifetimes correctos,
  pipeline de middleware con AddPaaS/UsePaas en la posición correcta, ausencia de
  captive dependencies, y gestión explícita del ciclo de vida de recursos. NO
  activar para: lógica de negocio dentro del service, acceso a DB, ni llamadas HTTP.
---

# ASP.NET Core DI & Middleware Pipeline

## Objetivo

El contenedor de inyección de ASP.NET Core es el **esqueleto del microservicio**:
crea los servicios, resuelve las dependencias, y gestiona su ciclo de vida.
Usarlo mal genera captive dependencies (un Scoped que se convierte en Singleton
involuntariamente), servicios con estado mutable no thread-safe, I/O en el
constructor que falla silenciosamente en startup, y memory leaks por recursos no
liberados. El orden del pipeline de middleware también es crítico: un middleware
en la posición incorrecta puede hacer que error handling, auth o logging fallen.
Este skill define cómo registrar servicios correctamente, elegir el lifetime
adecuado, ordenar el pipeline con `AddPaaS`/`UsePaas`, y gestionar el ciclo de
vida de recursos.

## Cuándo activar

- Se registra un nuevo servicio en `IServiceCollection`.
- Se elige el lifetime de un servicio o se detecta un problema relacionado.
- Se detecta una captive dependency (Scoped inyectado en Singleton).
- Se configura el pipeline de middleware en `Program.cs`.
- Se posiciona `AddPaaS(builder.Configuration)`/`app.UsePaas()` del arquetipo.
- Se implementa `IHostedService` o `BackgroundService`.
- Se configura `IOptions<T>` para binding de configuración tipada.
- Hay un `ObjectDisposedException` o un `DependencyInjectionException` del arquetipo.

## Cuándo NO activar

- Lógica de negocio dentro de los métodos del service.
- Acceso a DB con EF Core o ADO.NET (ver el skill de acceso a datos).
- Llamadas HTTP salientes (ver `aspnetcore-outgoing-http`).

## Estado actual vs target

- **Target:** ASP.NET Core 8, inyección por constructor con primary constructors
  (C# 12) o constructor explícito, servicios Singleton con estado inmutable,
  `IHostedService`/`BackgroundService` para tareas de background o startup,
  `IOptions<T>` para binding de config, `IDisposable`/`IAsyncDisposable` para
  cleanup de recursos.
- Las reglas aplican a cualquier versión de ASP.NET Core 6+.

## Decisiones del proyecto

- **Inyección por constructor siempre** (no `IServiceProvider.GetService<T>()` en
  código de producción ni service locator). Con C# primary constructors el
  boilerplate se reduce al mínimo.
- **Lifetimes:**
  - `AddSingleton` para servicios stateless thread-safe o con estado inmutable
    (clientes HTTP vía `IHttpClientFactory`, caché, configuración).
  - `AddScoped` para servicios con estado por request (Unit of Work, `DbContext`).
  - `AddTransient` para servicios stateless ligeros que no acumulan recursos.
- **`AddPaaS(builder.Configuration)` primero** en la sección de servicios, antes
  de registrar servicios de aplicación que dependan de abstracciones del arquetipo
  (`IResponseBuilder`, `BaseErrorBuilder`, etc.).
- **`app.UsePaas()` después de `app.UseRouting()`** y antes de
  `app.UseAuthorization()` / `app.MapControllers()` para que los middlewares
  del arquetipo (error handling, correlation id, meta-data wrapping) estén en la
  posición correcta del pipeline.
- **Captive dependency es un bug silencioso:** un Singleton que inyecta un Scoped
  captura esa instancia en su creación; vive todo el tiempo de vida del Singleton,
  no se libera por request, y puede acumular estado entre requests.
- **Resolución de captive deps:** usar `IServiceScopeFactory` para crear un scope
  explícito y acotado dentro del Singleton cuando necesite consumir un Scoped.

## Reglas obligatorias

### MUST

1. **MUST usar inyección por constructor** para dependencias obligatorias del
   servicio. No `IServiceProvider.GetService<T>()` (service locator) en código
   de producción.

2. **MUST elegir el lifetime correcto:**
   - `AddScoped` para `DbContext` y cualquier clase que represente una unidad de
     trabajo por request.
   - `AddSingleton` solo para servicios stateless o con estado thread-safe.
   - `AddTransient` para servicios sin estado y sin recursos pesados.

3. **MUST colocar `AddPaaS(builder.Configuration)` antes de los registros de
   servicios de aplicación** que dependan de abstracciones del arquetipo
   (`IResponseBuilder`, `BaseErrorBuilder`, excepciones EPA, etc.).

4. **MUST colocar `app.UsePaas()` después de `app.UseRouting()`** y antes de
   `app.UseAuthorization()` y `app.MapControllers()` para que el pipeline de
   middlewares del arquetipo procese el request en el orden correcto.

5. **MUST implementar `IDisposable` o `IAsyncDisposable`** en servicios que
   contengan recursos no manejados (conexiones, channels, timers, `HttpClient`
   manual). El DI container invoca `Dispose()` automáticamente al final del
   lifetime del servicio.

6. **MUST usar `IHostedService` o `BackgroundService`** para tareas de background
   o inicialización que requieran acceso a servicios del container. No I/O en
   el constructor de un servicio.

### MUST NOT

7. **MUST NOT inyectar un servicio Scoped en un Singleton directamente.**
   Es una captive dependency: el Scoped se convierte efectivamente en Singleton,
   con riesgo de state leak entre requests. Usar `IServiceScopeFactory` si el
   Singleton necesita consumir un Scoped.

8. **MUST NOT hacer I/O en el constructor de un servicio** (conexiones a DB,
   llamadas HTTP, lectura de archivos). El DI container construye los servicios
   durante el startup y una excepción aquí cancela el arranque completo.

9. **MUST NOT registrar el mismo servicio múltiples veces con lifetimes
   contradictorios** (Scoped y Singleton para la misma interfaz) sin una
   justificación documentada.

10. **MUST NOT usar `builder.Build()` seguido de `app.Services.GetService<T>()`**
    dentro de `Program.cs` para resolver servicios en la fase de configuración.
    Genera un segundo contenedor que no comparte estado con el principal.

## Anti-patrones prohibidos

❌ Captive dependency (Scoped inyectado en Singleton):

```csharp
// Registros en Program.cs:
builder.Services.AddSingleton<INotificacionService, NotificacionService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>(); // Scoped: una instancia por request

// Singleton que captura el Scoped en construcción:
public class NotificacionService(IUnitOfWork unitOfWork) : INotificacionService
{
    // ❌ IUnitOfWork es Scoped pero este Singleton lo captura al crearse;
    //    la misma instancia se reutiliza en todos los requests → state leak.
}
```

✅ Singleton crea scope explícito cuando necesita un Scoped:

```csharp
public class NotificacionService(IServiceScopeFactory scopeFactory) : INotificacionService
{
    public async Task EnviarAsync(Notificacion notif, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope(); // ✅ scope acotado al método
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.NotificacionesRepository.InsertarAsync(notif, ct);
        await unitOfWork.CommitAsync(ct);
    } // ✅ el scope (y el UnitOfWork) se dispone al salir del bloque using
}
```

❌ I/O en el constructor:

```csharp
public class ConfiguracionRemota(HttpClient client)
{
    private readonly Dictionary<string, string> _config;
    public ConfiguracionRemota(HttpClient client)
    {
        // ❌ I/O bloqueante en constructor; si el servicio remoto no responde,
        //    el startup del microservicio falla con excepción no controlada
        _config = client.GetFromJsonAsync<Dictionary<string, string>>("/config").Result;
    }
}
```

✅ I/O en IHostedService durante el startup:

```csharp
public class ConfiguracionRemotaLoader(
    HttpClient client,
    IConfiguracionCache cache) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        var config = await client
            .GetFromJsonAsync<Dictionary<string, string>>("/config", ct); // ✅ async, cancelable
        cache.Inicializar(config);
    }
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}

// Registro:
builder.Services.AddHostedService<ConfiguracionRemotaLoader>(); // ✅
```

❌ Singleton con estado mutable no thread-safe:

```csharp
// Registrado como Singleton:
public class ContadorService
{
    private int _contador = 0; // ❌ estado mutable en Singleton; race condition bajo concurrencia
    public void Incrementar() => _contador++;
    public int ObtenerValor() => _contador;
}
```

✅ Estado mutable con tipos thread-safe:

```csharp
public class ContadorService
{
    private int _contador = 0;
    public void Incrementar() => Interlocked.Increment(ref _contador); // ✅ operación atómica
    public int ObtenerValor() => Volatile.Read(ref _contador);         // ✅ lectura con visibilidad garantizada
}
```

## Checklist antes de devolver código

- [ ] Todos los servicios nuevos usan inyección por constructor.
- [ ] `AddScoped` para `DbContext` y unidades de trabajo por request.
- [ ] `AddPaaS(builder.Configuration)` está antes de los registros de servicios de aplicación.
- [ ] `app.UsePaas()` está después de `UseRouting()` y antes de `UseAuthorization()`.
- [ ] No hay captive dependencies (Scoped inyectado directamente en Singleton).
- [ ] Servicios con recursos implementan `IDisposable`/`IAsyncDisposable`.
- [ ] No hay I/O en constructores de servicios.

## Conexiones con otros skills

- `dotnet-thread-safety-and-shared-state` — estado mutable en servicios Singleton.
- `aspnetcore-config-and-secrets` — `IOptions<T>` como forma correcta de inyectar config.
- `aspnetcore-outgoing-http` — clientes HTTP deben registrarse vía `IHttpClientFactory` (Singleton).
- `aspnetcore-rest-layer` — inyección del service en el controller, orden del pipeline.
