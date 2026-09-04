---
name: dotnet-thread-safety-and-shared-state
description: |
  Activa SIEMPRE que haya estado compartido o potencialmente mutable en un
  microservicio ASP.NET Core 10. Los servicios registrados como Singleton son
  accedidos desde múltiples request-threads simultáneamente. Transversal:
  aplica a cualquier capa (controller, service, repository, middleware).
  Triggers: "campo no readonly en un servicio singleton", "variable de
  instancia mutable", "static mutable", "lock", "Monitor", "Interlocked",
  "InterlockedIncrement", "ConcurrentDictionary", "ConcurrentBag",
  "ConcurrentQueue", "ReaderWriterLockSlim", "race condition", "condición de
  carrera", "estado compartido", "campo estático mutable", "ThreadLocal",
  "AsyncLocal", "IHttpContextAccessor en singleton", "HttpContext en singleton",
  "hilo", "thread", "concurrencia", "singleton con estado", "contador global",
  "cache en memoria sin sincronización", "Dictionary compartido", "List
  compartida", "AddSingleton con estado mutable". Garantiza que los servicios
  singleton no tengan estado mutable sin sincronización, que IHttpContextAccessor
  no se capture en singletons, que las colecciones compartidas sean thread-safe,
  y que ThreadLocal/AsyncLocal se limpien correctamente. NO activar para:
  coordinación de tareas async (ver dotnet-async-and-concurrency), ni diseño
  de la capa de persistencia con EF Core.
---

# .NET Thread Safety & Shared State

## Objetivo

ASP.NET Core es **multithreaded**: cada request HTTP llega en un hilo distinto y
todos comparten los mismos servicios registrados como `Singleton` en el DI
container. Un campo mutable en un servicio singleton es un campo mutable
compartido entre todos los requests simultáneos. Esto es invisible en desarrollo
(un request a la vez) y explosivo en producción (N requests concurrentes). El
anti-patrón estrella en microservicios ASP.NET Core es **capturar
`IHttpContextAccessor` o `HttpContext` en un singleton**: `HttpContext` es
per-request y accederlo desde un singleton puede devolver el contexto de otro
request o `null`. Este skill define cómo gestionar estado compartido de forma
segura.

## Cuándo activar

- Se agrega un campo mutable a un servicio registrado como `Singleton` en el DI.
- Se usa un `Dictionary`, `List`, o cualquier colección como campo de instancia en
  un singleton.
- Se implementa un cache en memoria, un contador, o acumulador en un servicio.
- Se usa `IHttpContextAccessor` en un servicio singleton.
- Se captura `HttpContext` fuera del scope de un request.
- Se ve un `static` mutable en cualquier clase del microservicio.
- Se usa `ThreadLocal<T>` o `AsyncLocal<T>` para propagar contexto entre capas.
- Se reporta comportamiento inconsistente bajo carga que no se reproduce en local.

## Cuándo NO activar

- Coordinación de tareas async con `Task.WhenAll` o concurrencia controlada
  (ver `dotnet-async-and-concurrency`).
- Diseño de la capa de persistencia y transacciones EF Core.
- Variables locales dentro de un método (son thread-safe por naturaleza).

## Estado actual vs target

- **Target:** servicios singleton sin estado mutable (stateless); si se necesita
  estado compartido, usar `ConcurrentDictionary<K,V>`, `Interlocked` para
  contadores, o un store externo (Redis, DB). `IHttpContextAccessor` solo en
  servicios `Scoped` o `Transient`, nunca en `Singleton`. `AsyncLocal<T>` para
  contexto de tracing; siempre con cleanup.
- **En este proyecto:** `HttpContext` es per-request. Acceder a
  `IHttpContextAccessor.HttpContext` desde un singleton puede retornar el
  contexto de otro request concurrente o `null` en contextos background.

## Decisiones del proyecto

- **El diseño correcto es stateless.** Un servicio no debería tener campos
  mutables: el estado vive en la DB, Redis, o en el scope del request, no en el
  singleton.
- **Si necesitás estado compartido:** `Interlocked.Increment`/`Decrement` para
  contadores simples; `ConcurrentDictionary<K,V>` para maps; `IMemoryCache` del
  framework para caches en memoria con expiration; considerar Redis para estado
  que deba sobrevivir reinicios o compartirse entre instancias.
- **`lock` como último recurso** para bloqueos complejos que no expresan las
  primitivas concurrent; `lock` con granularidad fina y sin I/O dentro del bloque.
- **`IHttpContextAccessor` solo en servicios Scoped o Transient.** Si se necesita
  en un singleton, inyectar el valor específico (ej. el `ILogger`) en el
  constructor, no el accessor completo.
- **`AsyncLocal<T>` para contexto de propagación** (correlation id, tenant id,
  user id). Limpiar al final del middleware que lo setea.

## Reglas obligatorias

### MUST

1. **MUST asegurar que los servicios singleton no tengan campos mutables sin
   sincronización.** Toda variable de instancia en un singleton debe ser
   `readonly` + inmutable, o usar una estructura thread-safe
   (`ConcurrentDictionary`, `Interlocked`, etc.).

2. **MUST usar `ConcurrentDictionary<K,V>` para diccionarios compartidos entre
   threads** en un singleton. `Dictionary<K,V>` no es thread-safe y produce
   resultados indeterministas bajo contención (incluyendo corrupciones de memoria).

3. **MUST usar `Interlocked.Increment`/`Decrement`/`Add` para contadores
   compartidos.** `count++` no es atómico aunque la variable sea `volatile`.

4. **MUST NOT inyectar `IHttpContextAccessor` en servicios Singleton.** El
   `HttpContext` es per-request; capturarlo en un singleton puede devolver el
   contexto de otro request o `null`. Registrar el servicio como `Scoped` o
   `Transient` si necesita acceso al `HttpContext`.

5. **MUST limpiar `AsyncLocal<T>` al final del middleware que lo popula** para
   evitar filtraciones de contexto entre requests en el mismo thread.

6. **MUST evitar campos `static` mutables** en cualquier clase del microservicio.
   Son peor que los campos de instancia: se comparten entre todos los threads y
   tests, y no son visibles en el grafo de dependencias del DI.

### MUST NOT

7. **MUST NOT usar `Dictionary<K,V>`, `List<T>` o `HashSet<T>` como campos
   mutables de instancia en singletons** sin sincronización explícita.

8. **MUST NOT hacer operaciones compound (`read-modify-write`) sobre campos
   compartidos** sin sincronización. `_cache[key] = value` sobre un
   `Dictionary` no es atómico; ni siquiera una lectura y escritura separadas
   son seguras.

9. **MUST NOT usar `lock(this)` en servicios** del DI container. `this` es
   público; cualquier código externo puede lockear el mismo objeto y producir
   deadlocks. Usar un objeto `private readonly object _lock = new()` dedicado.

10. **MUST NOT asumir que `volatile` es suficiente para compound operations.**
    `volatile` garantiza visibilidad (no caching en registros), no atomicidad.
    `_count++` sobre un `volatile int` sigue siendo una race condition.

## Recomendaciones

### SHOULD

- Preferir diseño stateless: si necesitás estado compartido, evaluar primero si
  puede vivir en `IMemoryCache` (con expiration y size limits) o en Redis en lugar
  de un campo mutable propio.
- Usar `ConcurrentDictionary<K,V>.GetOrAdd` y `AddOrUpdate` para operaciones
  atómicas de put-if-absent o update condicional.
- Usar `ReaderWriterLockSlim` cuando el patrón de acceso es mayoritariamente
  lectura y escrituras ocasionales (más throughput que `lock`).

### SHOULD NOT

- No sincronizar con `lock` y también usar `Interlocked` sobre el mismo campo; son
  mecanismos incompatibles y producen race conditions.
- No capturar el `IServiceProvider` root en un singleton para resolver servicios
  `Scoped` (captive dependency): el servicio scoped vive más tiempo del esperado
  y puede tener estado per-request incorrecto.

## Anti-patrones prohibidos

❌ Campo mutable en singleton (race condition):

```csharp
// ❌ race condition: N threads leen y escriben simultáneamente
public class ContadorService
{
    private int _visitas = 0; // ❌ no thread-safe
    public void Registrar() => _visitas++;
    public int Obtener() => _visitas;
}
// Registrado como: services.AddSingleton<ContadorService>();
```

✅ Usar Interlocked para contadores:

```csharp
public class ContadorService
{
    private int _visitas = 0;
    public void Registrar() => Interlocked.Increment(ref _visitas); // ✅ atómico
    public int Obtener() => Interlocked.CompareExchange(ref _visitas, 0, 0);
}
```

❌ Dictionary compartido sin sincronización:

```csharp
public class CacheService
{
    private readonly Dictionary<string, object> _datos = new(); // ❌ no thread-safe
    public void Guardar(string key, object val) => _datos[key] = val;
    public object Obtener(string key) => _datos[key];
}
```

✅ ConcurrentDictionary:

```csharp
public class CacheService
{
    private readonly ConcurrentDictionary<string, object> _datos = new(); // ✅
    public void Guardar(string key, object val) => _datos[key] = val;
    public object? Obtener(string key) => _datos.GetValueOrDefault(key);
}
```

❌ IHttpContextAccessor en singleton (ANTI-PATRÓN ESTRELLA):

```csharp
// ❌ el HttpContext es per-request; en un singleton puede ser null o el de otro request
public class AuditoriaService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    public AuditoriaService(IHttpContextAccessor accessor)
        => _httpContextAccessor = accessor;

    public string ObtenerUsuario()
        => _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "UNKNOWN";
        // ❌ en background tasks o threads concurrentes esto devuelve null o datos ajenos
}
// Registrado como: services.AddSingleton<AuditoriaService>(); ← bug silencioso
```

✅ Servicio Scoped o recibir el valor como parámetro:

```csharp
// ✅ Opción 1: registrar como Scoped (vive solo durante el request)
// services.AddScoped<AuditoriaService>();

// ✅ Opción 2: si debe ser singleton, recibir el usuario como parámetro del método
public class AuditoriaService
{
    public Task RegistrarAsync(string usuario, string accion, CancellationToken ct)
    {
        // el llamador extrae el usuario del HttpContext en su propio scope
        return _repo.GuardarAsync(new AuditoriaEntry(usuario, accion), ct);
    }
}
```

❌ lock(this) en servicio del DI:

```csharp
public class RecursoCompartidoService
{
    public void Actualizar(string valor)
    {
        lock (this) { _recurso = valor; } // ❌ this es accesible externamente
    }
}
```

✅ Objeto de lock privado dedicado:

```csharp
public class RecursoCompartidoService
{
    private readonly object _lock = new();
    private string _recurso = string.Empty;

    public void Actualizar(string valor)
    {
        lock (_lock) { _recurso = valor; } // ✅ lock object privado y no expuesto
    }
}
```

## Checklist antes de devolver código

- [ ] Todos los campos de instancia en servicios Singleton son `readonly` e
      inmutables, o usan estructuras thread-safe (`ConcurrentDictionary`, `Interlocked`).
- [ ] No hay `Dictionary<K,V>` / `List<T>` mutables como campos de servicios Singleton.
- [ ] Los contadores usan `Interlocked.Increment`/`Decrement`.
- [ ] No hay `IHttpContextAccessor` ni captura de `HttpContext` en servicios Singleton.
- [ ] Los `AsyncLocal<T>` se limpian en el middleware al final del request.
- [ ] No hay campos `static` mutables.
- [ ] Los `lock` usan un objeto `private readonly` dedicado (no `lock(this)`).

## Conexiones con otros skills

- `dotnet-async-and-concurrency` — propagación de contexto a tasks concurrentes;
  `IHttpContextAccessor` tampoco es seguro en contextos async fuera del thread de request.
- `aspnetcore-di-and-middleware-pipeline` — scopes del DI (Singleton/Scoped/Transient);
  captive dependency antipattern.
- `aspnetcore-error-and-observability` — `AsyncLocal<T>` para correlation id; cleanup obligatorio.
