---
name: aspnetcore-rest-layer
description: |
  Activa cuando se crea o modifica la capa REST de un microservicio ASP.NET Core 8
  con el arquetipo epa-net-paas: controllers, endpoints, el pipeline de request
  (middleware, filtros) o el contrato de respuesta. Triggers: "[ApiController]",
  "[Route]", "[HttpGet]/[HttpPost]/[HttpPut]/[HttpDelete]/[HttpPatch]", "endpoint",
  "ruta", "[FromBody]", "[FromQuery]", "[FromRoute]", "[FromHeader]", "DTO",
  "DataAnnotations", "FluentValidation", "PaasControllerBase", "IResponseBuilder",
  "meta-data-error", "IActionResult", "Task<IActionResult>", "CancellationToken",
  "contrato de respuesta", "status code", "versionado de API", "serializar respuesta",
  "manejar el request", "ProblemDetail", "RFC 7807", "ValidationException",
  "NotFoundException", "BusinessException", "epa-net-paas". Garantiza controllers
  delgados (sin lógica de negocio), validación en el borde, manejo de excepciones
  EPA centralizado con IResponseBuilder, y contratos de respuesta consistentes con
  el formato meta-data-error. NO activar para: lógica de negocio en services,
  acceso a DB (EF Core/ADO.NET), ni llamadas salientes HTTP.
---

# ASP.NET Core REST Layer

## Objetivo

El controller es el **borde HTTP** del microservicio: traduce un request en una
llamada a un service y devuelve una respuesta con el contrato correcto. Nada más.
La lógica de negocio, las llamadas a otros servicios y el acceso a datos viven en
services y repositories. Este skill define cómo estructurar la capa REST de
ASP.NET Core 8 con el arquetipo `epa-net-paas`: controllers delgados, validación
en el borde con DataAnnotations o FluentValidation, manejo centralizado de
excepciones EPA en middleware global, y contratos de respuesta consistentes usando
`IResponseBuilder` y el formato `meta-data-error`.

## Cuándo activar

- Se crea o modifica un controller con `[ApiController]`.
- Se define o modifica una ruta (`[HttpGet]`, `[HttpPost]`, etc.).
- Se configura validación de entrada (DataAnnotations, FluentValidation).
- Se define el shape de la respuesta, status codes o contratos de error.
- Se usa `IResponseBuilder`, `PaasControllerBase` o excepciones EPA.
- Se configura versionado de API, content negotiation o serialización JSON.

## Cuándo NO activar

- Lógica de negocio en services (responsabilidad del service layer).
- Acceso a DB con EF Core o ADO.NET.
- Llamadas HTTP salientes a otros servicios (ver `aspnetcore-outgoing-http`).
- Mensajería (ver el skill de mensajería correspondiente).

## Estado actual vs target

- **Target:** ASP.NET Core 8, `[ApiController]` con validación automática de
  modelo, `IResponseBuilder` del arquetipo `epa-net-paas` para construir
  respuestas en formato `meta-data-error`, excepciones tipadas EPA mapeadas
  centralmente en `IExceptionHandler` o middleware global, DTOs/records
  inmutables para contratos de entrada y salida, `async Task<IActionResult>`
  con `CancellationToken` en todos los endpoints.
- Las reglas (controller delgado, validar en el borde, filtro global de errores,
  contrato uniforme) aplican a cualquier versión de ASP.NET Core.

## Decisiones del proyecto

- **Controllers delgados:** reciben el request, validan (vía DataAnnotations +
  `[ApiController]`), delegan al service, devuelven usando `IResponseBuilder`.
  Cero lógica de negocio, cero acceso a datos directo.
- **Validación automática de modelo con `[ApiController]`:** cuando la validación
  de DataAnnotations falla, ASP.NET Core devuelve 400 automáticamente; no hace
  falta `ModelState.IsValid` explícito en el controller.
- **Errores centralizados** en `IExceptionHandler` (ASP.NET Core 8) o middleware
  de excepciones que traduce excepciones EPA a respuestas uniformes con
  `IResponseBuilder`/`BaseErrorBuilder`. El cliente nunca recibe un stack trace.
- **Formato `meta-data-error`** controlado por el arquetipo: el `ObjectResult`
  se auto-envuelve; las env vars `DISABLE_META_DATA`, `WRAP_CONTROLLED_RESPONSES`
  y `WRAP_UNHANDLED_EXCEPTION` controlan el comportamiento en cada entorno.
- **`PaasControllerBase`** es la base opcional que simplifica el uso de
  `IResponseBuilder`; usarla cuando el controller emite múltiples respuestas con
  shapes diferentes.
- **Status codes correctos:** 200/201/204 según corresponda, 4xx para errores de
  cliente, 5xx para fallas internas.
- **`CancellationToken`** en todos los endpoints async para honrar cancelaciones
  del cliente (timeout, desconexión).

## Reglas obligatorias

### MUST

1. **MUST mantener el controller delgado:** solo orquesta (recibe, valida vía
   `[ApiController]`, llama al service, devuelve con `IResponseBuilder` o
   `IActionResult`). Cero lógica de negocio.

2. **MUST validar todo input en el borde** con DataAnnotations en los DTOs.
   `[ApiController]` activa la validación automática de modelo; para validaciones
   complejas o condicionales, usar FluentValidation con un `IValidator<T>`
   registrado en el DI container.

3. **MUST centralizar el manejo de excepciones EPA** en `IExceptionHandler` o
   middleware global. Nunca hacer `try/catch` en el controller para
   `ValidationException`, `NotFoundException`, `BusinessException` ni
   `NetworkException`.

4. **MUST devolver contratos de error usando `IResponseBuilder`/`BaseErrorBuilder`:**
   `WithCode`, `WithReason`, `WithErrorType(TECHNICAL/FUNCTIONAL/INTERNAL)`,
   `WithDetail`, `WithMessage`, `.Build()`; luego
   `_responseBuilder.AddError(error).BuildResponse(statusCode)`.
   El cliente nunca recibe mensajes internos ni stack traces.

5. **MUST usar los status codes correctos:**
   - `200 OK` para lecturas exitosas con cuerpo.
   - `201 Created` para recursos creados.
   - `204 No Content` para operaciones sin cuerpo de respuesta.
   - `400 Bad Request` para validación fallida (`ValidationException`).
   - `404 Not Found` para recursos inexistentes (`NotFoundException`).
   - `409 Conflict` para conflictos de estado.
   - `422 Unprocessable Entity` para errores de reglas de negocio (`BusinessException`).
   - `500 Internal Server Error` para errores internos (`IOException`, `NetworkException`).

6. **MUST no exponer tipos internos** (entidades EF Core, modelos de dominio)
   como respuesta. Usar DTOs de respuesta o `record` de C# dedicados.

7. **MUST declarar `CancellationToken cancellationToken` como parámetro en todos
   los métodos async** y propagarlo hacia el service y el repositorio.

### MUST NOT

8. **MUST NOT poner lógica de negocio en el controller.** Ni validaciones de
   negocio, ni acceso a repos, ni transformaciones de dominio.

9. **MUST NOT devolver `null` de un endpoint.** Si no hay resultado, devolver
   `404` o `204` según corresponda.

10. **MUST NOT filtrar datos sensibles** silenciosamente: si un campo no debe
    exponerse, excluirlo explícitamente del DTO de respuesta (no con
    `[JsonIgnore]` en la entidad EF Core, que es un leak de la capa de
    persistencia).

11. **MUST NOT hacer `try/catch` en el controller** para relanzar la misma
    excepción EPA o para construir respuestas de error ad-hoc. El middleware
    global es el único lugar donde se manejan las excepciones EPA.

## Anti-patrones prohibidos

❌ Lógica de negocio en el controller:

```csharp
[HttpPost("transferencias")]
public async Task<IActionResult> Transferir([FromBody] TransferenciaRequest req)
{
    // ❌ validación de negocio y acceso a repo directamente en el controller
    if (req.Monto <= 0)
        return BadRequest("Monto inválido");
    await _cuentaRepository.DebitarAsync(req.CuentaOrigen, req.Monto);
    return Ok();
}
```

✅ Controller delgado que delega al service:

```csharp
[HttpPost("transferencias")]
public async Task<IActionResult> Transferir(
    [FromBody] TransferenciaRequest req,
    CancellationToken cancellationToken)
{
    // ✅ delega toda la lógica al service; devuelve con IResponseBuilder
    var resultado = await _transferenciaService.EjecutarAsync(req, cancellationToken);
    return _responseBuilder.AddData(resultado).BuildResponse(StatusCodes.Status201Created);
}
```

❌ Manejo de excepción EPA ad-hoc en el controller:

```csharp
[HttpGet("cuentas/{id}")]
public async Task<IActionResult> ObtenerCuenta(int id, CancellationToken ct)
{
    try
    {
        var cuenta = await _cuentaService.ObtenerAsync(id, ct);
        return Ok(cuenta); // ❌ no usa IResponseBuilder; formato no garantizado
    }
    catch (NotFoundException ex)
    {
        return NotFound(ex.Message); // ❌ respuesta ad-hoc, expone mensaje interno
    }
}
```

✅ Excepción EPA propagada al middleware global con IResponseBuilder:

```csharp
// En IExceptionHandler registrado en Program.cs:
public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
{
    if (ex is not NotFoundException nfe) return false;
    var error = _errorBuilder
        .WithCode("NOT_FOUND")
        .WithReason(nfe.Message)
        .WithErrorType(ErrorType.FUNCTIONAL)
        .Build();
    ctx.Response.StatusCode = StatusCodes.Status404NotFound;
    await ctx.Response.WriteAsJsonAsync(
        _responseBuilder.AddError(error).BuildResponse(StatusCodes.Status404NotFound), ct);
    return true; // ✅ centralizado, formato meta-data-error consistente
}

// Controller sin try/catch:
[HttpGet("cuentas/{id}")]
public async Task<IActionResult> ObtenerCuenta(int id, CancellationToken ct)
{
    var cuenta = await _cuentaService.ObtenerAsync(id, ct); // ✅ propaga NotFoundException
    return _responseBuilder.AddData(cuenta).BuildResponse(StatusCodes.Status200OK);
}
```

❌ Exponer entidad EF Core directamente:

```csharp
[HttpGet("productos/{id}")]
public async Task<Producto> ObtenerProducto(int id)
{
    // ❌ retorna la entidad con navigation properties; leak de capa de persistencia
    return await _context.Productos.Include(p => p.Categoria).FirstOrDefaultAsync(p => p.Id == id);
}
```

✅ Usar record de respuesta dedicado:

```csharp
[HttpGet("productos/{id}")]
public async Task<IActionResult> ObtenerProducto(int id, CancellationToken ct)
{
    var producto = await _productoService.ObtenerAsync(id, ct);
    var dto = new ProductoResponse(producto.Id, producto.Nombre, producto.Precio); // ✅ record inmutable
    return _responseBuilder.AddData(dto).BuildResponse(StatusCodes.Status200OK);
}

public record ProductoResponse(int Id, string Nombre, decimal Precio);
```

## Checklist antes de devolver código

- [ ] El controller no tiene lógica de negocio ni acceso a repos.
- [ ] Todos los DTOs de entrada tienen DataAnnotations o hay un `IValidator<T>` registrado.
- [ ] Los DTOs de respuesta no son entidades EF Core.
- [ ] Existe un `IExceptionHandler` o middleware global que maneja las excepciones EPA.
- [ ] Se usa `IResponseBuilder` para construir respuestas (no `Ok()`/`BadRequest()` ad-hoc sin formato EPA).
- [ ] Los status codes son semánticamente correctos y mapean a las excepciones EPA correspondientes.
- [ ] Todos los endpoints async reciben `CancellationToken`.
- [ ] Se verificó que el endpoint está protegido (ver `aspnetcore-security-owasp-baseline`).

## Conexiones con otros skills

- `dotnet-parsing-and-validation` — validación de DTOs, esquemas, tipos.
- `aspnetcore-security-owasp-baseline` — protección del endpoint, authz.
- `aspnetcore-error-and-observability` — logging del request, correlation id, métricas.
- `aspnetcore-di-and-middleware-pipeline` — inyección del service en el controller, orden del pipeline.
