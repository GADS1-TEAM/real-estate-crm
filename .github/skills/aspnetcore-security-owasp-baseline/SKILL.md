---
name: aspnetcore-security-owasp-baseline
description: |
  SIEMPRE ACTIVO al escribir o revisar código de un microservicio ASP.NET Core 8
  expuesto a la red. Es el baseline de seguridad OWASP que aplica a todo endpoint,
  parser, cliente saliente, acceso a datos y log. Triggers: cualquier controller o
  minimal API, manejo de input del usuario o de upstream, headers, autenticación/
  autorización, tokens/secretos, queries a la DB, construcción de URLs o comandos,
  serialización de respuestas, logging, manejo de errores, CORS, SSRF. Palabras
  clave: "endpoint", "input", "[FromBody]", "[FromQuery]", "[FromRoute]", "auth",
  "autenticación", "autorización", "token", "JWT", "[Authorize]", "policy", "claim",
  "OAuth2", "password", "secreto", "query a la DB", "EF Core", "native SQL",
  "armar URL", "redirect", "log", "error al cliente", "CORS", "CORS_POLICY_ORIGINS",
  "CORS_POLICY_METHODS", "CORS_POLICY_HEADERS", "CORS_POLICY_NAME", "MASKED_DATA",
  "PII", "datos sensibles", "número de cuenta", "datos bancarios", "APIM",
  "SQL injection", "SSRF", "deserialización", "XXE", "IActionResult", "rate limit",
  "validación de input", "inyección", "IDOR", "ownership", "stack trace al cliente",
  "epa-net-paas seguridad". Cubre OWASP Top 10 en contexto ASP.NET Core: inyección
  SQL via EF params, auth/authz con JWT validado y APIM, exposición de datos
  sensibles, XXE/deserialización insegura, control de acceso roto (IDOR), misconfig
  de seguridad (headers, CORS), componentes vulnerables, logging de eventos de
  seguridad, SSRF. NO se desactiva nunca para código de red; solo es irrelevante
  en scripts offline sin entrada no confiable.
---

# ASP.NET Core Security OWASP Baseline (siempre activo)

## Objetivo

El microservicio es parte de la infraestructura del banco: cada endpoint es
superficie de ataque potencial. Este skill es el **baseline de seguridad TRANSVERSAL
que aplica siempre**, alineado con OWASP Top 10 en el contexto ASP.NET Core 8 con
el arquetipo `epa-net-paas`. APIM se posiciona delante del microservicio y valida el
JWT antes de llegar al servicio; esto no exime al microservicio de verificar
autorización a nivel de recurso. Ante conflicto con otra preferencia,
**seguridad gana**.

## Cuándo activar

**Siempre** que el código:

- Reciba input del usuario o de un upstream (body, query, route params, headers).
- Autentique o autorice (`[Authorize]`, políticas, claims, JWT).
- Acceda a datos (EF Core, ADO.NET, Redis) usando valores externos.
- Construya URLs, paths, queries SQL nativas con datos externos.
- Serialice respuestas o escriba logs.
- Maneje secretos o datos personales (PII) / financieros.
- Configure CORS, cabeceras HTTP de seguridad, o middleware de error.

## Cuándo NO activar

- Scripts CLI offline sin entrada no confiable ni datos sensibles.

## Estado actual vs target

- **Target:** ASP.NET Core 8 con JWT validado por APIM + verificación de authz
  a nivel de recurso en el microservicio; EF Core con queries parametrizadas
  (previene SQL injection por defecto); Data Annotations + FluentValidation en el
  borde; logging JSON sin PII via `ILogger<T>` + `MASKED_DATA` del arquetipo;
  `IResponseBuilder`/`BaseErrorBuilder` del arquetipo para respuestas de error
  opacas; CORS configurado vía env vars EPA `CORS_POLICY_*`; NuGet actualizado.
- **Arquetipo `epa-net-paas`:** el pipeline del arquetipo (instalado vía
  `AddPaaS`/`UsePaas`) aplica cabeceras de seguridad, procesa `MASKED_DATA`, y
  ofusca `APP_ID`/`APP_KEY` automáticamente.
- Las reglas de authz, secretos, SSRF, headers, logging y errores aplican a
  **cualquier microservicio ASP.NET Core con exposición de red**.

## Decisiones del proyecto

- **Validar y tipar todo input en el borde** antes de usarlo. Nada de confiar en
  el cliente ni en el upstream.
- **Allowlist, no denylist:** definir lo permitido, rechazar el resto.
- **Errores opacos al cliente:** nunca exponer stack traces, mensajes internos de
  EF Core, nombres de tablas, ni detalles del upstream. Solo el correlation id.
- **Secretos nunca en logs.** Redacción obligatoria de PII y datos financieros.
- **Default deny en authz:** si no se verificó explícitamente el permiso, se
  rechaza. `[Authorize]` es el mínimo; agregar autorización por recurso (ownership).
- **EF Core previene SQL injection** cuando se usan queries parametrizadas o LINQ.
  Las raw SQL con interpolación de strings son la excepción peligrosa.
- **APIM delante:** aunque APIM valida la firma del JWT, el microservicio DEBE
  verificar que el sujeto autenticado puede operar sobre el recurso específico
  solicitado (IDOR check). APIM no sabe nada del ownership de datos de negocio.
- **CORS vía env vars EPA:** usar `CORS_POLICY_ORIGINS`, `CORS_POLICY_METHODS`,
  `CORS_POLICY_HEADERS`, `CORS_POLICY_NAME` del arquetipo para configurar CORS;
  nunca `AllowAnyOrigin()` en producción.

## Reglas obligatorias

### MUST

1. **MUST validar y sanear todo input externo** (body, query, route, headers) con
   Data Annotations (`[Required]`, `[StringLength]`, `[Range]`) o FluentValidation
   en el borde, rechazando lo que no matchea (400). Nunca pasar input no validado
   a capas internas.

2. **MUST verificar autorización en cada endpoint protegido** con `[Authorize]` y
   políticas. Comprobar que el sujeto autenticado puede operar sobre **ese** recurso
   específico: el claim del JWT (`sub`, `clienteId`) manda, no el ID del path. Evitar
   IDOR.

3. **MUST usar EF Core con LINQ o queries parametrizadas** para acceso a DB. Nunca
   interpolar o concatenar valores del usuario en un `FromSqlRaw` o `ExecuteSqlRaw`.

4. **MUST prevenir SSRF:** nunca construir una URL para llamadas salientes con datos
   no validados del usuario. Usar una allowlist de hosts permitidos gestionada en
   config.

5. **MUST redactar PII y datos financieros en logs.** Número de cuenta, CUIT, CBU,
   tokens, passwords nunca en texto plano en logs. Confiar en `MASKED_DATA` del
   arquetipo solo para los campos ya configurados; el código no debe enviar el dato
   si no está cubierto.

6. **MUST devolver errores opacos al cliente.** Solo correlation id y código de
   error genérico via `IResponseBuilder`/`BaseErrorBuilder` EPA. Sin stack traces,
   mensajes de EF Core, ni nombres de tablas.

7. **MUST configurar CORS restrictivo** vía las env vars EPA `CORS_POLICY_*` del
   arquetipo. No usar `AllowAnyOrigin()` en producción; solo los orígenes conocidos.

8. **MUST validar el token JWT correctamente:** firma, expiración (`exp`), issuer
   (`iss`), audience (`aud`). No confiar en el payload sin verificar la firma.
   APIM valida el JWT a nivel de gateway; el microservicio verifica claims de
   autorización a nivel de recurso.

9. **MUST loggear eventos de seguridad relevantes** (intentos de autenticación
   fallidos, accesos denegados, inputs con patrones de inyección detectados) al
   nivel `LogWarning` con `ISSUE_LOG` EPA, incluyendo el correlation id.

### MUST NOT

10. **MUST NOT deshabilitar la autenticación/autorización** en endpoints que
    devuelven datos de negocio. Todos los endpoints de negocio detrás de APIM
    deben tener `[Authorize]` o una política equivalente.

11. **MUST NOT usar `FromSqlRaw` o `ExecuteSqlRaw` con interpolación de strings**
    o concatenación de input del usuario:
    `context.Database.ExecuteSqlRaw($"DELETE FROM ... WHERE id = {id}")` → **SQL injection**.

12. **MUST NOT deserializar JSON de fuentes externas a tipos con polimorfismo
    sin discriminador validado.** La deserialización insegura (OWASP A08) puede
    ejecutar código arbitrario.

13. **MUST NOT exponer `ex.Message` de excepciones de EF Core, SQL Server o de
    librerías internas** al cliente. Puede contener detalles de la query, nombres
    de columnas, o información de infraestructura.

14. **MUST NOT confiar en el `X-Forwarded-For` header** sin verificar que viene
    de un proxy confiable (APIM o el ingress). Puede ser spoofed por el cliente.

## Recomendaciones

### SHOULD

- Aplicar cabeceras HTTP de seguridad (`Content-Security-Policy`, `X-Frame-Options`,
  `X-Content-Type-Options`, `Referrer-Policy`) a través del middleware del arquetipo
  o de `NWebsec`/`SecurityHeaders` middleware.
- Mantener los paquetes NuGet actualizados y ejecutar `dotnet list package --vulnerable`
  en CI para detectar componentes con CVEs conocidos.
- Usar `RequireHttpsMetadata = true` en el middleware JWT para evitar tokens
  transmitidos en texto plano fuera de entornos de desarrollo.
- Agregar rate limiting (`AddRateLimiter` en ASP.NET Core 8) en endpoints de
  alta sensibilidad o costosos computacionalmente.

### SHOULD NOT

- No confiar en el `Content-Type` header del request para decidir cómo procesarlo
  sin validación adicional; validar el body independientemente del header declarado.
- No agregar `[AllowAnonymous]` a endpoints que previamente estaban protegidos sin
  revisión de seguridad explícita y aprobación del equipo.

## Anti-patrones prohibidos

❌ IDOR: usar el ID del path sin verificar que pertenece al usuario autenticado:

```csharp
[HttpGet("cuentas/{cuentaId}/movimientos")]
[Authorize]
public async Task<IActionResult> GetMovimientos(string cuentaId, CancellationToken ct)
{
    // ❌ cualquier usuario autenticado puede ver movimientos de cualquier cuenta
    var movimientos = await _service.GetMovimientosAsync(cuentaId, ct);
    return Ok(movimientos);
}
```

✅ Verificar ownership con el claim del JWT:

```csharp
[HttpGet("cuentas/{cuentaId}/movimientos")]
[Authorize]
public async Task<IActionResult> GetMovimientos(string cuentaId, CancellationToken ct)
{
    var clienteId = User.FindFirstValue("clienteId"); // ✅ claim del JWT validado por APIM
    if (!await _cuentaService.PerteneceAlClienteAsync(cuentaId, clienteId, ct))
        return Forbid(); // ✅ IDOR bloqueado
    var movimientos = await _service.GetMovimientosAsync(cuentaId, ct);
    return Ok(movimientos);
}
```

❌ SQL injection via interpolación en `FromSqlRaw`:

```csharp
var nombre = request.Nombre; // input del usuario
var clientes = await context.Clientes
    .FromSqlRaw($"SELECT * FROM Clientes WHERE Nombre = '{nombre}'") // ❌ SQL injection
    .ToListAsync(ct);
```

✅ EF Core con parámetros (inmune a SQL injection):

```csharp
var clientes = await context.Clientes
    .Where(c => c.Nombre == request.Nombre) // ✅ LINQ → query parametrizada automáticamente
    .ToListAsync(ct);

// O con raw SQL parametrizado si es necesario:
var clientes = await context.Clientes
    .FromSqlRaw("SELECT * FROM Clientes WHERE Nombre = {0}", request.Nombre) // ✅ parámetro posicional
    .ToListAsync(ct);
```

❌ CORS abierto en producción:

```csharp
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()); // ❌ cualquier origen
```

✅ CORS restrictivo vía env vars EPA:

```csharp
// El arquetipo epa-net-paas lee CORS_POLICY_ORIGINS, CORS_POLICY_METHODS,
// CORS_POLICY_HEADERS y CORS_POLICY_NAME de las variables de entorno.
// En Program.cs solo se invoca AddPaaS/UsePaas:
builder.Services.AddPaaS(builder.Configuration); // ✅ CORS configurado por env vars EPA
app.UsePaas(); // ✅ middleware de CORS del arquetipo aplicado
```

❌ Stack trace expuesto al cliente en el handler de errores:

```csharp
public async ValueTask<bool> TryHandleAsync(
    HttpContext ctx, Exception ex, CancellationToken ct)
{
    await ctx.Response.WriteAsJsonAsync(new { error = ex.ToString() }, ct); // ❌ stack trace completo
    return true;
}
```

✅ Respuesta opaca con `IResponseBuilder` EPA:

```csharp
public async ValueTask<bool> TryHandleAsync(
    HttpContext ctx, Exception ex, CancellationToken ct)
{
    ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
    var response = _errorBuilder.Build(ex); // ✅ respuesta meta-data-error opaca, solo correlation id
    await ctx.Response.WriteAsJsonAsync(response, ct);
    return true;
}
```

## Checklist antes de devolver código

- [ ] Todos los endpoints tienen `[Authorize]` o política equivalente.
- [ ] La autorización verifica ownership del recurso (claim del JWT vs. ID del path/body).
- [ ] No hay EF Core `FromSqlRaw` con interpolación o concatenación de input externo.
- [ ] No hay PII ni secretos en los logs.
- [ ] No hay stack traces ni mensajes internos en las respuestas de error al cliente.
- [ ] CORS está configurado vía env vars EPA `CORS_POLICY_*`, no con `AllowAnyOrigin()`.
- [ ] Los tokens JWT se validan: firma, `exp`, `iss`, `aud`.
- [ ] Las llamadas HTTP salientes no construyen URLs con input no validado (anti-SSRF).
- [ ] Los paquetes NuGet no tienen CVEs conocidos.

## Conexiones con otros skills

- `aspnetcore-rest-layer` — autorización en cada endpoint; errores opacos al cliente; validación de input con Data Annotations.
- `dotnet-parsing-and-validation` — validación de input como primera línea de defensa ante inyecciones.
- `aspnetcore-error-and-observability` — no filtrar detalle interno en logs ni en respuestas; excepciones EPA opacas al cliente.
- `aspnetcore-config-and-secrets` — secretos y credenciales gestionados de forma segura; env vars EPA de CORS y tokens.
- `aspnetcore-outgoing-http` — allowlist de hosts para prevenir SSRF en llamadas salientes.
- `dotnet-adversarial-testing` — los tests adversariales validan las defensas OWASP (injection, SSRF, IDOR).
