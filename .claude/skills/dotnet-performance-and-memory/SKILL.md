---
name: dotnet-performance-and-memory
description: |
  Activa cuando se trabaja con performance o uso de memoria en un microservicio
  ASP.NET Core 10: latencia en hot paths, presión sobre el GC, allocations
  innecesarias, boxing/unboxing, streaming de datos, o sizing de pools.
  Triggers: "Span<T>", "Memory<T>", "ReadOnlySpan", "ArrayPool", "ObjectPool",
  "MemoryPool", "boxing", "unboxing", "value type", "struct", "readonly struct",
  "StringBuilder", "string concatenation en loop", "IAsyncEnumerable", "yield
  return", "ToList() innecesario", "materializar IEnumerable", "GC pressure",
  "gen 0", "gen 1", "gen 2", "LOH", "Large Object Heap", "closure", "captura
  en lambda", "hot path", "allocation", "benchmark", "BenchmarkDotNet", "profiler",
  "dotnet-trace", "dotnet-counters", "latencia alta", "memoria alta", "OOM",
  "OutOfMemoryException", "IMemoryCache sizing", "HttpClient reutilización",
  "connection pool". Garantiza que las decisiones de performance sean deliberadas,
  medidas y no prematuras. NO activar para: concurrencia de tasks (ver
  dotnet-async-and-concurrency), sincronización de estado compartido (ver
  dotnet-thread-safety-and-shared-state), ni seguridad.
---

# .NET Performance & Memory

## Objetivo

El GC de .NET gestiona la memoria automáticamente, pero el código de aplicación
controla cuánta presión ejerce sobre él. En un microservicio de alta carga, la
diferencia entre devolver un `byte[]` y usar `Span<byte>`, o entre `string + string`
en un loop y `StringBuilder`, puede ser la diferencia entre un GC suave y pauses
que aumentan la latencia de P99. Este skill define cómo tomar decisiones de
performance deliberadas en ASP.NET Core 10: cuándo usar `Span<T>`, cuándo evitar
allocations en hot paths, cómo dimensionar pools y cuándo streaming con
`IAsyncEnumerable<T>` evita materializar colecciones completas en memoria.

## Cuándo activar

- Se trabaja con buffers, parsing de bytes o procesamiento de strings en hot paths.
- Se detecta presión de GC (pauses, métricas de gen 0/1/2, tamaño del heap).
- Se hace `ToList()` o `.ToArray()` sobre colecciones grandes innecesariamente.
- Se concatena strings en loops.
- Se usan wrappers de value types (boxing) en código de alta frecuencia.
- Se configura `IMemoryCache`, `ArrayPool`, o `ObjectPool`.
- Se evalúa el impacto de una estructura de datos en el uso de memoria.
- Se hace profiling o se analiza una traza de `dotnet-trace`.

## Cuándo NO activar

- Coordinación de tareas asíncronas (ver `dotnet-async-and-concurrency`).
- Sincronización de estado compartido (ver `dotnet-thread-safety-and-shared-state`).
- Problemas de correctitud de negocio que no son de performance.

## Estado actual vs target

- **Target:** ASP.NET Core 10 con .NET 10 GC optimizado, `Span<T>`/`Memory<T>` para
  procesamiento de buffers sin allocations, `ArrayPool<T>.Shared` para buffers
  temporales de vida corta, `IAsyncEnumerable<T>` para streaming desde DB o
  servicios upstream, `StringBuilder` para concatenación en loops, y decisiones
  de optimización respaldadas por `BenchmarkDotNet` o `dotnet-trace`.
- **En este proyecto:** microservicios I/O-bound donde el cuello de botella
  es red/DB; optimizar allocations y GC tiene impacto real en P99 bajo carga alta.

## Decisiones del proyecto

- **No optimizar sin medir.** Profilear con `dotnet-trace`, `dotnet-counters` o
  `BenchmarkDotNet` antes de cambiar estructuras de datos o algoritmos.
- **`Span<T>` y `Memory<T>` para evitar copies de buffers** en parsing y
  procesamiento de bytes. `Span<T>` es stack-only; `Memory<T>` puede ir al heap.
- **`ArrayPool<T>.Shared` para buffers temporales** que se crean y descartan en
  el scope de un request. Siempre devolver al pool en `try/finally`.
- **`IAsyncEnumerable<T>` para streaming:** evita cargar toda la colección en
  memoria cuando el consumidor puede procesar elemento a elemento.
- **`struct` como `readonly struct`** cuando representa un value object pequeño
  e inmutable pasado por valor; evitar `struct` para tipos grandes (>16 bytes)
  o mutables (copias costosas).
- **GC generacional:** gen 0 es barato (compacta rápido); gen 2 y LOH (objetos
  > 85KB) son costosos. El objetivo es que los objetos de vida corta mueran en gen 0.

## Reglas obligatorias

### MUST

1. **MUST medir antes de optimizar.** No cambiar estructuras de datos ni algoritmos
   basándose en intuición. Usar `BenchmarkDotNet`, `dotnet-trace`, `dotnet-counters`
   o métricas de `System.Diagnostics.Metrics` primero.

2. **MUST usar `StringBuilder` para concatenación de strings en loops.** El operador
   `+` en un loop crea un `string` inmutable por iteración: O(n²) en tiempo y
   presión de GC lineal al número de iteraciones.

3. **MUST devolver los buffers tomados de `ArrayPool<T>` al pool** en un bloque
   `try/finally`. Un buffer no devuelto es una fuga de memoria del pool.

4. **MUST evitar boxing de value types en hot paths.** Pasar un `int`/`long`/`struct`
   a un parámetro `object`, `Enum`, o interfaz sin generic lo boxea: allocation
   en heap y presión de GC.

5. **MUST usar `IAsyncEnumerable<T>` y `await foreach`** cuando se devuelve una
   colección grande desde la DB o se hace streaming a un cliente; evita materializar
   toda la colección en memoria con `.ToList()`.

### MUST NOT

6. **MUST NOT llamar `.ToList()`, `.ToArray()`, o `.Count()` sobre una secuencia
   `IQueryable`/`IEnumerable` grande** cuando solo se necesita iterar una vez.
   Materializa toda la colección en memoria y puede causar OOM.

7. **MUST NOT crear closures que capturen objetos grandes o colecciones en hot
   paths.** Las lambdas que capturan variables del scope externo generan clases
   anónimas que mantienen vivas esas referencias.

8. **MUST NOT crear objetos de corta vida en grandes cantidades en hot paths.**
   Cada allocation es potencial presión de GC gen 0; en loops de alta frecuencia,
   preferir `Span<T>` o reusar objetos con `ObjectPool<T>`.

9. **MUST NOT poner objetos mayores de 85KB en el LOH sin necesidad.** Los objetos
   en el LOH se compactan raramente y generan fragmentación; usar `ArrayPool<T>`
   en lugar de `byte[]` grande allocado ad-hoc.

## Recomendaciones

### SHOULD

- Habilitar métricas de GC con `dotnet-counters monitor --counters
System.Runtime` en staging para monitorear gen 0/1/2 GC rate y heap size.
- Usar `ObjectPool<T>` (de `Microsoft.Extensions.ObjectPool`) para objetos de
  instanciación costosa que se reutilizan frecuentemente (ej. `StringBuilder`,
  serializadores).
- Preferir `readonly struct` para value objects pequeños e inmutables para
  eliminar allocations en hot paths.
- Usar `System.Text.Json` (source generation) para evitar allocations de
  reflection en serialización/deserialización frecuente.

### SHOULD NOT

- No usar `string.Format()` ni interpolación `$"..."` en logging de hot path sin
  verificar que el nivel de log está habilitado; usar log parametrizado:
  `_logger.LogDebug("Procesando {Id}", id)`.
- No asumir que `IMemoryCache` no consume memoria: configurar `SizeLimit` y
  `ExpirationScanFrequency` para evitar que crezca sin límite.

## Anti-patrones prohibidos

❌ String concatenation en loop (O(n²)):

```csharp
// ❌ crea un string nuevo por iteración; O(n²) en tiempo y memoria
string resultado = "";
foreach (var item in items)
    resultado += item + ",";
```

✅ StringBuilder:

```csharp
// ✅ O(n); una sola allocation del buffer final
var sb = new StringBuilder();
foreach (var item in items)
    sb.Append(item).Append(',');
string resultado = sb.ToString();
```

❌ Boxing innecesario en hot path:

```csharp
// ❌ cada int se boxea a object en la lista → allocation por elemento
var ids = new List<object>();
for (int i = 0; i < 100_000; i++)
    ids.Add(i);
```

✅ Tipo genérico o primitivo:

```csharp
// ✅ sin boxing; List<int> usa un array de primitivos internamente
var ids = new List<int>(capacity: 100_000);
for (int i = 0; i < 100_000; i++)
    ids.Add(i);
```

❌ Buffer grande allocado ad-hoc (presión LOH):

```csharp
// ❌ byte[] > 85KB va al LOH; se aloca y descarta por cada request
public async Task<byte[]> LeerArchivoAsync(string path, CancellationToken ct)
{
    var buffer = new byte[1024 * 1024]; // 1MB → LOH
    using var fs = File.OpenRead(path);
    await fs.ReadAsync(buffer, ct);
    return buffer;
}
```

✅ ArrayPool para buffers temporales:

```csharp
// ✅ toma del pool y devuelve; no genera presión de GC
public async Task ProcesarArchivoAsync(string path, CancellationToken ct)
{
    var buffer = ArrayPool<byte>.Shared.Rent(1024 * 1024);
    try
    {
        using var fs = File.OpenRead(path);
        int leido = await fs.ReadAsync(buffer.AsMemory(), ct);
        ProcesarSpan(buffer.AsSpan(0, leido)); // ✅ Span sin copia
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(buffer); // ✅ siempre devolver al pool
    }
}
```

❌ Materializar IQueryable completa innecesariamente:

```csharp
// ❌ trae todos los registros a memoria; luego toma solo los primeros 10
var todos = await _dbContext.Cuentas.ToListAsync(ct); // ❌ 1M de registros en RAM
var primeros = todos.Take(10).ToList();
```

✅ Paginación en la query:

```csharp
// ✅ la DB devuelve solo los 10 registros necesarios
var primeros = await _dbContext.Cuentas
    .OrderBy(c => c.Id)
    .Take(10)
    .ToListAsync(ct);
```

❌ ToList() sobre IAsyncEnumerable cuando se puede streamear:

```csharp
// ❌ materializa toda la colección antes de responder al cliente
public async Task<IActionResult> Exportar(CancellationToken ct)
{
    var todos = await _repo.ObtenerTodosAsync(ct).ToListAsync(ct); // ❌
    return Ok(todos);
}
```

✅ Streaming con IAsyncEnumerable:

```csharp
// ✅ el cliente recibe datos a medida que llegan; memoria constante
public async IAsyncEnumerable<CuentaDto> Exportar(
    [EnumeratorCancellation] CancellationToken ct)
{
    await foreach (var cuenta in _repo.ObtenerTodosAsync(ct))
        yield return _mapper.Map<CuentaDto>(cuenta);
}
```

## Checklist antes de devolver código

- [ ] No hay string concatenation en loops ni en logging de hot path.
- [ ] Los buffers de `ArrayPool<T>` se devuelven en `try/finally`.
- [ ] No hay boxing innecesario de value types en código de alta frecuencia.
- [ ] No hay `.ToList()` / `.ToArray()` sobre colecciones grandes donde alcanza
      con iterar una vez.
- [ ] Las optimizaciones tienen métricas que las respaldan (BenchmarkDotNet /
      dotnet-trace / dotnet-counters).
- [ ] Si se usa `IMemoryCache`, tiene `SizeLimit` configurado.

## Conexiones con otros skills

- `aspnetcore-di-and-middleware-pipeline` — `ObjectPool<T>` se registra en el
  DI; tamaños de pool deben venir de config.
- `aspnetcore-error-and-observability` — métricas de System.Runtime y
  Micrometer para medir latencia antes de optimizar.
- `dotnet-thread-safety-and-shared-state` — los pools compartidos
  (`ArrayPool`, `ObjectPool`) son thread-safe por diseño; los objetos
  pooled no necesitan sincronización adicional en el scope de uso.
- `dotnet-async-and-concurrency` — `IAsyncEnumerable<T>` combinado con
  `CancellationToken` para streaming cancelable.
