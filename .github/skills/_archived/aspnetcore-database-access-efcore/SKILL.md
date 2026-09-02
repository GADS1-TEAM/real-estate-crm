---
name: aspnetcore-database-access-efcore
description: |
  ARCHIVADA. No activar. La persistencia de este proyecto es MongoDB
  (ver docs/implementation/POC_TECH_DECISIONS.md). Esta skill enseña EF Core sobre
  bases relacionales y proviene de otro proyecto. Se conserva solo como referencia
  por si algún servicio futuro justifica una base relacional mediante ADR.
  Para acceso a datos usar las skills de MongoDB (ver docs/skills/SKILL_GAPS.md).
---

> **⚠️ SKILL ARCHIVADA — NO USAR**
>
> Este proyecto persiste en **MongoDB**, no en una base relacional. Las reglas de
> abajo asumen EF Core, `DbContext`, migraciones y joins, y llevan al anti-patrón
> explícito de *modelar MongoDB como SQL*.
>
> Reemplazada por `mongodb-document-modeling` y `mongodb-dotnet-driver`, pendientes
> de escribir (`docs/skills/SKILL_GAPS.md`).
>
> Reactivar solo si un ADR aprueba una base relacional para algún servicio.

# ASP.NET Core Database Access (EF Core)

## Objetivo

EF Core 8 facilita el acceso a datos relacionales, pero también facilita escribir
bugs de performance graves: el problema N+1 (una query por cada ítem de una lista),
`DbContext` como singleton que corrompe el estado entre requests concurrentes,
cargar columnas innecesarias, o materializar millones de registros sin paginación.
Este skill define cómo usar EF Core 8 correctamente en microservicios ASP.NET Core 8
con el arquetipo `epa-net-paas`: ciclo de vida del `DbContext`, migraciones
versionadas, evitar N+1, projections para lecturas, paginación, transacciones bien
delimitadas, y mapeo de errores a `DatabaseException` EPA.

> **Nota:** Si el equipo usa Dapper o ADO.NET puro (por performance o por queries
> muy complejas), las mismas reglas de ciclo de vida, paginación, parámetros
> seguros y mapeo de errores aplican. Adaptar la sintaxis.

## Cuándo activar

- Se define un `DbContext`, una entidad, o un repositorio.
- Se diseña una query (LINQ, `FromSqlRaw`, `FromSqlInterpolated`, Dapper).
- Se delimitan transacciones o se llama a `SaveChangesAsync`.
- Se detecta N+1 o una query lenta.
- Se configura la conexión o el pool de conexiones.
- Se agrega o modifica una migración de BD.
- Se trabaja con paginación, projections o bulk operations.

## Cuándo NO activar

- Lógica de negocio que no toca la BD.
- Llamadas HTTP salientes (ver `aspnetcore-outgoing-http`).
- Mensajería (ver `aspnetcore-messaging`).

## Estado actual vs target

- **Target:** ASP.NET Core 8, EF Core 8, `DbContext` registrado como **Scoped**
  (una instancia por HTTP request), migraciones con `dotnet ef migrations add`,
  `AsNoTracking()` para lecturas que no van a ser escritas, projections a `record`
  / DTO para queries que no necesitan tracking, paginación con `Skip`/`Take` +
  `OrderBy` estable, `SaveChangesAsync(cancellationToken)` en toda escritura,
  errores de DB mapeados a `DatabaseException` EPA.
- **Arquetipo `epa-net-paas`:** el connection string llega por env vars; no
  hardcodear credenciales de BD en el código fuente.

## Decisiones del proyecto

- **`DbContext` siempre Scoped:** una instancia por HTTP request. Si se necesita
  acceso desde un `BackgroundService` (singleton), usar
  `IServiceScopeFactory.CreateScope()` para obtener un scope por operación de
  background; nunca inyectar el `DbContext` directamente en un singleton.
- **No exponer `IQueryable<T>` fuera del repositorio.** El repositorio es la
  frontera de la capa de datos; devuelve tipos concretos (`IReadOnlyList<T>`,
  `T?`), no queryables que permiten que el caller construya queries arbitrarias.
- **Projections para lecturas.** Cuando solo se necesitan N campos, usar
  `.Select(x => new MiDto { ... })` en lugar de cargar la entidad completa y
  mapear después. Evita traer columnas innecesarias y deshabilita el change
  tracker implícitamente.
- **Paginación obligatoria** para colecciones con volumen indeterminado. Siempre
  `Skip`/`Take` con un `OrderBy` estable (la paginación sin orden determinístico
  devuelve páginas inconsistentes).
- **Transacciones explícitas solo cuando hay múltiples `SaveChanges`** en la
  misma unidad de trabajo. Un solo `SaveChangesAsync` es atómico por sí mismo.
- **`AsNoTracking()`** en toda query de solo lectura (listados, reportes, respuestas
  de GET). Reduce la memoria del `DbContext` y evita que el change tracker
  interfiera en escrituras posteriores.

## Reglas obligatorias

### MUST

1. **MUST registrar `DbContext` como Scoped** en `Program.cs`:
   `builder.Services.AddDbContext<MiDbContext>(...)`. Nunca Singleton.

2. **MUST usar `AsNoTracking()`** en toda query cuyo resultado no se va a modificar
   y guardar en el mismo scope de la operación.

3. **MUST usar projections** (`.Select` a DTO o `record`) para queries de solo
   lectura donde no se necesita la entidad completa. No cargar columnas de más.

4. **MUST detectar y eliminar el problema N+1.** Toda relación de navegación
   accedida en un loop debe resolverse con `.Include()` + `.ThenInclude()`, con
   una projection que materialice los datos en una sola query, o con split queries.

5. **MUST paginar colecciones de tamaño indeterminado** con `Skip`/`Take` y un
   `OrderBy` estable. No `ToListAsync()` sin límite sobre tablas con volumen
   desconocido.

6. **MUST versionar el schema con migraciones de EF Core** (`dotnet ef migrations add`).
   Nunca `EnsureCreated()` ni `Database.EnsureDeleted()` fuera de tests.

7. **MUST pasar `CancellationToken`** a `SaveChangesAsync`, `ToListAsync`,
   `FirstOrDefaultAsync`, `SingleOrDefaultAsync`, `ExecuteUpdateAsync` y demás
   métodos async de EF Core.

8. **MUST mapear `DbUpdateException` y `DbUpdateConcurrencyException`** a
   `DatabaseException` EPA. No relanzar la excepción de EF Core cruda al service
   o controller.

### MUST NOT

9. **MUST NOT registrar `DbContext` como Singleton.** El change tracker no es
   thread-safe; provoca corrupción de estado entre requests concurrentes.

10. **MUST NOT exponer `IQueryable<T>` fuera del repositorio.** El caller no debe
    poder agregar `.Where()`, `.Include()` o `.Take()` fuera de la capa de datos.

11. **MUST NOT usar `FromSqlRaw` con interpolación directa de strings** del input
    del usuario. Usar `FromSqlInterpolated` o parámetros explícitos
    (`new SqlParameter(...)`) para evitar SQL injection.

12. **MUST NOT ignorar el resultado de `SaveChangesAsync`.** Si devuelve 0 rows
    affected cuando se esperaban 1+, puede indicar un conflicto de concurrencia
    optimista o un registro que no existe.

## Recomendaciones

### SHOULD

- Usar `IEntityTypeConfiguration<T>` en lugar de configurar entidades directamente
  en `OnModelCreating`; escala mejor al crecer el modelo de dominio.
- Para bulk operations (update/delete masivo), considerar `ExecuteUpdateAsync` /
  `ExecuteDeleteAsync` (EF Core 7+) en lugar de cargar entidades, modificarlas, y
  llamar a `SaveChanges`.
- Usar `[Timestamp]` / `RowVersion` para concurrencia optimista en entidades con
  alta contención de escritura.
- Configurar el pool de conexiones explícitamente en el connection string
  (`Max Pool Size`, `Min Pool Size`, `Connection Timeout`).
- Usar `AsNoTrackingWithIdentityResolution` cuando se necesita evitar duplicados
  en el grafo de entidades sin el overhead del change tracker completo.

### SHOULD NOT

- No usar `context.Database.EnsureCreated()` en código que no sea de tests; no es
  compatible con el workflow de migraciones y puede borrar datos accidentalmente.
- No llamar a `context.ChangeTracker.DetectChanges()` manualmente en loops; EF
  Core lo invoca automáticamente en `SaveChanges` y puede ser costoso si se
  duplica por cada ítem.

## Anti-patrones prohibidos

❌ `DbContext` Singleton que comparte estado entre requests:

```csharp
// En Program.cs
builder.Services.AddSingleton<AppDbContext>(); // ❌ singleton → corrupción de estado entre requests concurrentes
```

✅ `DbContext` Scoped (una instancia por request):

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
// ✅ AddDbContext registra como Scoped por defecto
```

❌ Problema N+1 con navigation properties:

```csharp
var clientes = await _context.Clientes.ToListAsync(ct); // 1 query para clientes
foreach (var c in clientes)
{
    var cuentas = c.Cuentas.ToList(); // ❌ 1 query por cada cliente = N+1
    procesarCuentas(cuentas);
}
```

✅ Eager loading con `.Include()` y `AsNoTracking`:

```csharp
var clientes = await _context.Clientes
    .Include(c => c.Cuentas)
    .AsNoTracking() // ✅ sin change tracker para lectura
    .ToListAsync(ct); // ✅ 1 sola query (o split query; igualmente eficiente)
```

❌ `IQueryable<T>` expuesto fuera del repositorio:

```csharp
public IQueryable<Movimiento> GetMovimientos() =>
    _context.Movimientos; // ❌ el caller puede agregar .Where(), .Include() o .Take() ad-hoc
```

✅ Repositorio que devuelve tipos concretos paginados:

```csharp
public async Task<IReadOnlyList<MovimientoDto>> GetMovimientosPaginadosAsync(
    int cuentaId, int page, int pageSize, CancellationToken ct)
{
    return await _context.Movimientos
        .Where(m => m.CuentaId == cuentaId)
        .OrderByDescending(m => m.Fecha)         // ✅ orden estable
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .AsNoTracking()
        .Select(m => new MovimientoDto(m.Id, m.Monto, m.Fecha, m.Descripcion)) // ✅ projection
        .ToListAsync(ct);                         // ✅ CancellationToken
}
```

❌ SQL injection con interpolación directa en `FromSqlRaw`:

```csharp
var movimientos = _context.Movimientos
    .FromSqlRaw($"SELECT * FROM Movimientos WHERE CuentaId = {request.CuentaId}"); // ❌ SQL injection
```

✅ Parámetros seguros con `FromSqlInterpolated`:

```csharp
var movimientos = _context.Movimientos
    .FromSqlInterpolated($"SELECT * FROM Movimientos WHERE CuentaId = {request.CuentaId}");
// ✅ FromSqlInterpolated parametriza automáticamente; nunca concatena strings
```

❌ Excepción de EF Core cruda relanzada al service:

```csharp
public async Task<Cliente> ObtenerAsync(int id, CancellationToken ct)
{
    return await _context.Clientes.FindAsync(new object[] { id }, ct)
        ?? throw new DbUpdateException("Cliente no encontrado"); // ❌ excepción de EF Core expuesta
}
```

✅ Excepción mapeada a `DatabaseException` EPA:

```csharp
public async Task<Cliente?> ObtenerAsync(int id, CancellationToken ct)
{
    try
    {
        return await _context.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }
    catch (Exception ex) when (ex is DbUpdateException or InvalidOperationException)
    {
        throw new DatabaseException($"Error al consultar cliente {id}.", ex); // ✅ excepción EPA
    }
}
```

## Checklist antes de devolver código

- [ ] `DbContext` registrado como Scoped, nunca Singleton.
- [ ] `AsNoTracking()` en toda query de solo lectura.
- [ ] No hay `IQueryable<T>` expuesto fuera del repositorio.
- [ ] No hay N+1: navigation properties accedidas en loops usan `.Include()` o projection.
- [ ] Toda colección de tamaño indeterminado tiene `Skip`/`Take` + `OrderBy` estable.
- [ ] `SaveChangesAsync` y demás métodos async reciben `CancellationToken`.
- [ ] Los errores de DB se mapean a `DatabaseException` EPA (no `DbUpdateException` cruda).
- [ ] No hay `FromSqlRaw` con concatenación de strings del input del usuario.
- [ ] Existe una migración versionada para cada cambio de schema.

## Conexiones con otros skills

- `aspnetcore-di-and-middleware-pipeline` — registro correcto del `DbContext` como Scoped; scope por mensaje en `BackgroundService`.
- `aspnetcore-error-and-observability` — loggear `DatabaseException` con tipo `ISSUE_LOG`; métricas de latencia de queries con OpenTelemetry.
- `dotnet-parsing-and-validation` — validar el input antes de construir queries; no usar datos sin validar en `FromSqlRaw`.
- `aspnetcore-security-owasp-baseline` — prevenir SQL injection en queries raw; no exponer detalles de la BD al cliente.
- `dotnet-performance-and-memory` — projections y `AsNoTracking` como herramientas de performance; bulk ops con `ExecuteUpdateAsync`/`ExecuteDeleteAsync`.
