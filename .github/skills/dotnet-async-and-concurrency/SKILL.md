---
name: dotnet-async-and-concurrency
description: |
  Activa cuando se coordina trabajo asíncrono o paralelo en un microservicio
  ASP.NET Core 8: fan-out a múltiples servicios, procesar listas llamando un
  servicio por ítem, paralelizar tareas, o usar Task/async/await con concurrencia
  controlada. Triggers: "async", "await", "Task<T>", "ValueTask<T>", "async void",
  "CancellationToken", "Task.WhenAll", "Task.WhenAny", "SemaphoreSlim",
  "Parallel.ForEachAsync", "IAsyncEnumerable", "ConfigureAwait", "Task.Run",
  ".Result", ".Wait()", ".GetAwaiter().GetResult()", "deadlock", "en paralelo",
  "llamar a N servicios", "para cada ítem llamar a", "fan-out", "fan-in",
  "batch async", "limitar concurrencia", "demasiadas llamadas en paralelo",
  "se satura el upstream", "Channel<T>", "Dataflow", "TPL". Garantiza
  concurrencia acotada, propagación de CancellationToken, manejo correcto de
  errores parciales, y backpressure para no saturar upstreams ni agotar el
  thread pool de ASP.NET Core. NO activar para: una sola operación async
  aislada, código puramente síncrono, ni sincronización de estado compartido
  (ver dotnet-thread-safety-and-shared-state).
---

# .NET Async & Concurrency

## Objetivo

`async`/`await` hace fácil escribir código no bloqueante... y fácil escribir
**concurrencia ilimitada**. Un `Task.WhenAll` sobre una lista que viene del
upstream puede disparar miles de tasks simultáneas, agotar el thread pool,
saturar los connection pools y tumbar al upstream de un golpe. Bloquear con
`.Result` o `.Wait()` en código async produce **deadlocks** en el contexto de
sincronización de ASP.NET Core. Este skill define cómo coordinar trabajo
asíncrono con **concurrencia acotada**, propagación correcta de
`CancellationToken`, manejo de errores parciales y `ConfigureAwait(false)` donde
corresponde.

## Cuándo activar

- Se usa `Task.WhenAll` o `Task.WhenAny` para trabajo paralelo.
- Se hace fan-out: una operación async por cada ítem de una lista.
- Se coordinan múltiples llamadas a servicios upstream en paralelo.
- Se procesa un lote (`List<T>`) aplicando trabajo async por elemento.
- Se reporta saturación, timeout, o un upstream que se cae bajo carga.
- Se usa `IAsyncEnumerable<T>` o `Channel<T>` para procesamiento streaming.
- Se detecta código async que bloquea con `.Result`, `.Wait()` o
  `.GetAwaiter().GetResult()`.

## Cuándo NO activar

- Una sola operación async aislada (un `await service.ObtenerAsync(id, ct)` simple).
- Código puramente síncrono.
- Sincronización de estado compartido entre hilos (ver
  `dotnet-thread-safety-and-shared-state`).

## Estado actual vs target

- **Target:** ASP.NET Core 8, `async`/`await` de punta a punta sin bloqueos
  síncronos, `CancellationToken` propagado en toda la cadena de I/O,
  `Task.WhenAll` solo para número fijo y pequeño de tasks conocidos,
  `SemaphoreSlim` o `Parallel.ForEachAsync` con `MaxDegreeOfParallelism` para
  listas dinámicas, nunca `async void` salvo event handlers de UI.
- **Arquetipo `epa-net-paas`:** los controllers reciben `CancellationToken` desde
  el framework; ese token debe propagarse hasta la capa de repositorio y las
  llamadas HTTP salientes.

## Decisiones del proyecto

- **Concurrencia siempre acotada** cuando la cantidad de operaciones depende del
  tamaño del input (lista del request, resultado de una query).
- **`Task.WhenAll` solo para un número fijo y pequeño** de tasks conocidos en
  tiempo de escritura (ej. 2-3 llamadas a servicios distintos). Para listas
  usar `SemaphoreSlim` o `Parallel.ForEachAsync` con `MaxDegreeOfParallelism`.
- **`async void` prohibido** en código de aplicación; las excepciones que lanza
  no son observables y tumban el proceso.
- **`CancellationToken` obligatorio** en todo método async público que toca I/O
  (HTTP, DB, mensajería). Propagarlo hacia abajo: controller → service → repo →
  llamada HTTP/EF Core.
- **`ConfigureAwait(false)` en librerías** y helpers sin contexto de UI/ASP.NET;
  en el código de la aplicación ASP.NET Core es irrelevante (no hay
  `SynchronizationContext`), pero es correcto en helpers de librería.
- **`Task.Run` no es para envolver I/O.** `Task.Run` mueve trabajo al thread pool;
  si el trabajo ya es async no hace falta y si es CPU-bound, evaluar si ese
  trabajo debería estar en el microservicio.

## Reglas obligatorias

### MUST

1. **MUST incluir `CancellationToken` en todo método async público que toca I/O.**
   El token debe venir del caller y propagarse hasta la llamada de red, EF Core
   o el cliente HTTP.

2. **MUST acotar la concurrencia** cuando el número de operaciones depende del
   tamaño del input. Usar `SemaphoreSlim`, `Parallel.ForEachAsync` con
   `MaxDegreeOfParallelism`, o chunks fijos.

3. **MUST NOT usar `async void`** en código de aplicación (fuera de event
   handlers). Las excepciones en `async void` no son capturables con `try/catch`
   ni con `await` y tumban el proceso.

4. **MUST manejar errores de tasks individuales** cuando se usa `Task.WhenAll`.
   Un `AggregateException` no manejado descarta errores parciales. Evaluar si
   corresponde "todo o nada" o "best-effort con recolección de errores".

5. **MUST propagar `CancellationToken`** hacia toda la cadena de calls async.
   Recibir el token y no usarlo es equivalente a ignorar la cancelación del
   cliente.

6. **MUST aplicar un timeout o CancellationToken con deadline** a operaciones
   async que pueden colgarse indefinidamente. Usar
   `CancellationTokenSource.CreateLinkedTokenSource` con `CancelAfter` o
   `TimeoutException` tipada.

### MUST NOT

7. **MUST NOT bloquear código async con `.Result`, `.Wait()` o
   `.GetAwaiter().GetResult()`** en código que puede correr en un contexto con
   `SynchronizationContext`. Produce deadlocks en ASP.NET Core clásico y es
   innecesario en ASP.NET Core moderno.

8. **MUST NOT usar `Task.WhenAll` sobre una lista dinámica** cuyo tamaño depende
   del input sin acotar la concurrencia. El límite debe ser explícito.

9. **MUST NOT usar `Task.Run` para envolver operaciones I/O ya async.** No agrega
   valor y desperdicia un thread del pool.

10. **MUST NOT ignorar el `Task` retornado de una operación async** sin `await`.
    Un `Task` no awaited descarta silenciosamente cualquier excepción que lance.

## Recomendaciones

### SHOULD

- Usar `Parallel.ForEachAsync` (disponible en .NET 6+) para fan-out sobre listas
  con `MaxDegreeOfParallelism` explícito; es más legible que `SemaphoreSlim`
  para la mayoría de los casos.
- Preferir `IAsyncEnumerable<T>` y `await foreach` para streaming de resultados
  desde la DB o servicios upstream; evita materializar toda la colección en
  memoria.
- Usar `ConfigureAwait(false)` en métodos de librería o helpers que no necesitan
  el contexto de ASP.NET Core.
- Documentar en código la semántica elegida para errores parciales (todo-o-nada
  vs best-effort).

### SHOULD NOT

- No usar `Thread.Sleep` en código async; usar `await Task.Delay(ms, ct)` para
  respetar el `CancellationToken`.
- No crear `Task` con el constructor (`new Task(...)`) en lugar de métodos de
  fábrica (`Task.Run`, `Task.FromResult`).

## Anti-patrones prohibidos

❌ Fan-out sin límite sobre lista dinámica:

```csharp
// ❌ si cuentas tiene 10.000 elementos, lanza 10.000 tasks simultáneas
var tasks = cuentas.Select(c => _saldoService.ObtenerAsync(c.Id, ct));
await Task.WhenAll(tasks);
```

✅ Fan-out con SemaphoreSlim acotado:

```csharp
// ✅ máximo 10 llamadas concurrentes
var semaphore = new SemaphoreSlim(10);
var tasks = cuentas.Select(async c =>
{
    await semaphore.WaitAsync(ct);
    try { return await _saldoService.ObtenerAsync(c.Id, ct); }
    finally { semaphore.Release(); }
});
await Task.WhenAll(tasks);
```

✅ Fan-out con Parallel.ForEachAsync (alternativa más legible):

```csharp
// ✅ MaxDegreeOfParallelism controla la concurrencia explícitamente
await Parallel.ForEachAsync(cuentas, new ParallelOptions
{
    MaxDegreeOfParallelism = 10,
    CancellationToken = ct
},
async (cuenta, token) =>
{
    var saldo = await _saldoService.ObtenerAsync(cuenta.Id, token);
    resultados.Add(saldo); // usar ConcurrentBag o canal para writes concurrentes
});
```

❌ Bloqueo síncrono en código async (deadlock potential):

```csharp
public IActionResult ObtenerCuenta(int id)
{
    // ❌ .Result bloquea el hilo; si hay SynchronizationContext → deadlock
    var cuenta = _cuentaService.ObtenerAsync(id, CancellationToken.None).Result;
    return Ok(cuenta);
}
```

✅ Async de punta a punta:

```csharp
public async Task<IActionResult> ObtenerCuenta(int id, CancellationToken ct)
{
    // ✅ await libera el hilo; el CancellationToken viene del framework
    var cuenta = await _cuentaService.ObtenerAsync(id, ct);
    return _responseBuilder.AddData(cuenta).BuildResponse(StatusCodes.Status200OK);
}
```

❌ async void (excepción no observable):

```csharp
// ❌ si ObtenerAsync lanza, el proceso termina sin manejar la excepción
public async void ProcesarEnBackground(int id)
{
    var datos = await _service.ObtenerAsync(id, CancellationToken.None);
    await _store.GuardarAsync(datos, CancellationToken.None);
}
```

✅ Retornar Task y awaitar desde el caller:

```csharp
// ✅ el caller puede observar el resultado y manejar excepciones
public async Task ProcesarEnBackgroundAsync(int id, CancellationToken ct)
{
    var datos = await _service.ObtenerAsync(id, ct);
    await _store.GuardarAsync(datos, ct);
}
```

❌ CancellationToken recibido pero no propagado:

```csharp
public async Task<SaldoDto> ObtenerSaldoAsync(int id, CancellationToken cancellationToken)
{
    // ❌ recibe el token pero no lo pasa a EF Core; la cancelación del cliente se ignora
    var entidad = await _dbContext.Cuentas.FindAsync(id);
    return _mapper.Map<SaldoDto>(entidad);
}
```

✅ CancellationToken propagado a toda la cadena:

```csharp
public async Task<SaldoDto> ObtenerSaldoAsync(int id, CancellationToken cancellationToken)
{
    // ✅ el token llega hasta EF Core y el cliente HTTP
    var entidad = await _dbContext.Cuentas
        .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    return _mapper.Map<SaldoDto>(entidad);
}
```

## Checklist antes de devolver código

- [ ] Todo método async público que toca I/O tiene `CancellationToken` y lo propaga.
- [ ] Toda operación fan-out sobre lista dinámica tiene concurrencia acotada.
- [ ] No hay `async void` en código de aplicación.
- [ ] No hay `.Result`, `.Wait()` ni `.GetAwaiter().GetResult()` en código async.
- [ ] Las tasks de `Task.WhenAll` tienen manejo de errores (AggregateException o
      unwrap con `await`).
- [ ] Se decidió y documentó la semántica de error (todo-o-nada vs best-effort).
- [ ] Las operaciones que pueden colgarse tienen timeout o deadline.

## Conexiones con otros skills

- `dotnet-thread-safety-and-shared-state` — sincronización del estado accedido
  desde múltiples tasks concurrentes.
- `aspnetcore-outgoing-http` — las tasks async suelen envolver llamadas HTTP;
  aplicar timeouts, `CancellationToken` y circuit breaker.
- `aspnetcore-error-and-observability` — propagación de correlation id y tracing
  en contextos async.
- `aspnetcore-di-and-middleware-pipeline` — `IHttpContextAccessor` no es seguro
  en contextos async fuera del hilo del request.
