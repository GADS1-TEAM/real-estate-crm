---
name: aspnetcore-di-and-middleware-pipeline
description: |
  Activa cuando se registra un servicio en el contenedor de DI o se arma el pipeline
  de un servicio o BFF de este CRM. Triggers: "Program.cs", "composition root",
  "AddScoped", "AddSingleton", "AddTransient", "IServiceCollection",
  "IServiceProvider", "CreateScope", "IServiceScopeFactory", "lifetime",
  "captive dependency", "inyección de dependencias", "registrar el servicio",
  "middleware", "pipeline", "UseRouting", "UseAuthentication", "UseAuthorization",
  "UseExceptionHandler", "app.Use", "orden del middleware", "IMiddleware",
  "IHostedService", "BackgroundService", "extension method de registro",
  "AddCrmPlatform", "IOptions<T>", "service locator".
  Garantiza lifetimes correctos, ausencia de captive dependencies, orden del
  pipeline válido y registro centralizado en un building block compartido en vez de
  repetido en veinte servicios. NO activar para: reglas de dominio, contratos HTTP
  ni acceso a datos.
---

# DI y Pipeline de Middleware

## Objetivo

El `Program.cs` es el **composition root**: el único lugar donde se decide qué
implementación concreta satisface cada puerto. Dos errores lo arruinan y ninguno
falla al compilar:

- **Lifetime equivocado.** Un `Scoped` capturado por un `Singleton` sobrevive al
  request que lo creó. Con MongoDB y contexto de tenant, eso significa que un
  request puede terminar operando con el `ActorContext` de otro.
- **Orden del pipeline equivocado.** Autorizar antes de autenticar deja pasar
  requests que debían rechazarse.

Con veinte servicios, la otra regla es que **nadie arme su propio bootstrap**: la
plataforma se registra una sola vez en un building block compartido.

Fuentes: [`ARCHITECTURE.md`](../../../ARCHITECTURE.md) §2 y §5.

## Cuándo activar

- Se escribe o modifica `Program.cs`.
- Se registra un servicio, repositorio, adapter o `BackgroundService`.
- Se elige o discute un lifetime.
- Se agrega o reordena middleware.
- Se crea un extension method de registro.

## Cuándo NO activar

- Invariantes y reglas de negocio (ver `ddd-hexagonal-architecture`).
- Forma de los endpoints (ver `aspnetcore-rest-layer`).
- Contenido de la configuración (ver `aspnetcore-config-and-secrets`).

## Decisiones del proyecto

| Tema | Decisión |
|---|---|
| Bootstrap | Un building block compartido expone `AddCrmPlatform()` / `UseCrmPlatform()`: telemetría, handler de errores, health, autenticación y contexto de actor |
| Composition root | `Program.cs` de cada servicio. Es el único lugar que conoce implementaciones concretas |
| `IMongoClient` | **Singleton** |
| Repositorios y handlers | **Scoped** |
| `ActorContext` | **Scoped**, resuelto una vez en el borde |
| Relay de outbox y consumidores | `BackgroundService`, con scope propio por mensaje |
| Caché de autorización | `IMemoryCache` singleton, con TTL corto |
| Registro por capa | Cada proyecto expone su `Add<Capa>()`; `Program.cs` los compone |

### Orden del pipeline

```csharp
var app = builder.Build();

app.UseCrmPlatform();        // errores, telemetría, correlationId
app.UseRouting();
app.UseAuthentication();     // primero: quién sos
app.UseAuthorization();      // después: qué podés
app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
```

El manejo de errores va **primero** para que capture todo lo que ocurra después.
Autenticación **antes** de autorización, siempre.

## Estado actual vs target

- **Estado:** no hay servicios. `FND-001` crea la estructura, `FND-007` la
  plataforma compartida.
- **Target:** el `Program.cs` de cada servicio es corto y legible: llama a
  `AddCrmPlatform()`, registra sus capas y compone el pipeline.

## Reglas obligatorias

### MUST

- **MUST** registrar `IMongoClient` como **singleton**, y los repositorios como
  **scoped**.
- **MUST** resolver el `ActorContext` como **scoped**, una vez por request.
- **MUST** crear un scope explícito por mensaje en `BackgroundService` y
  consumidores: no hay request que lo provea.
- **MUST** ubicar `UseAuthentication()` antes de `UseAuthorization()`.
- **MUST** registrar el manejo de errores al principio del pipeline.
- **MUST** registrar los adapters contra sus **puertos**, no contra clases
  concretas.
- **MUST** exponer el registro de cada capa como un extension method
  (`AddPartyDomain()`, `AddPartyInfrastructure()`).
- **MUST** validar la configuración al arranque, para que el servicio no levante con
  config inválida.

### MUST NOT

- **MUST NOT** inyectar un `Scoped` dentro de un `Singleton`: es una captive
  dependency y con tenancy es un bug de aislamiento.
- **MUST NOT** usar `IServiceProvider` como service locator dentro de la lógica de
  negocio.
- **MUST NOT** repetir el bootstrap de plataforma en cada servicio: va en el
  building block.
- **MUST NOT** registrar como singleton nada que guarde estado por request o por
  tenant.
- **MUST NOT** hacer trabajo bloqueante ni de red en el constructor de un servicio.
- **MUST NOT** registrar el mismo puerto dos veces con implementaciones distintas
  sin que sea deliberado y documentado.

## Recomendaciones

### SHOULD

- **SHOULD** mantener `Program.cs` corto: si crece, faltan extension methods.
- **SHOULD** preferir `Scoped` como default y subir a `Singleton` solo con motivo.
- **SHOULD** mantener los singletons **inmutables** o con estado protegido.
- **SHOULD** registrar los `BackgroundService` al final, después de sus dependencias.
- **SHOULD** verificar el grafo de DI en un test de arranque: que el host construya
  y resuelva todo.

### SHOULD NOT

- **SHOULD NOT** usar `Transient` para algo caro de construir.
- **SHOULD NOT** poner lógica condicional compleja en el composition root: si el
  registro depende de muchos flags, hay un problema de diseño.

## Anti-patrones prohibidos

### 1. Captive dependency

```csharp
// ❌ El singleton captura un scoped: el ActorContext de un request queda
//    congelado y otro request opera con el tenant equivocado.
builder.Services.AddSingleton<PortfolioCache>();   // recibe IActorContext scoped
builder.Services.AddScoped<IActorContext, ActorContext>();
```

```csharp
// ✅ El singleton pide un scope cuando lo necesita.
public sealed class PortfolioCache(IServiceScopeFactory scopeFactory)
{
    public async Task<Portfolio> GetAsync(TenantId tenant, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();
        return await repo.LoadAsync(tenant, ct);
    }
}
```

### 2. Consumidor sin scope por mensaje

```csharp
// ❌ Un BackgroundService es singleton: los scoped que resuelva viven para siempre.
public sealed class OutboxRelay(IPartyRepository repo) : BackgroundService { }
```

```csharp
// ✅ Un scope por mensaje, como un request.
public sealed class OutboxRelay(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IOutboxPublisher>();
            await publisher.PublishPendingAsync(ct);
        }
    }
}
```

### 3. Orden del pipeline inválido

```csharp
// ❌ Autoriza antes de saber quién es el usuario.
app.UseAuthorization();
app.UseAuthentication();
```

```csharp
// ✅ Autenticación primero.
app.UseAuthentication();
app.UseAuthorization();
```

### 4. Service locator en el dominio

```csharp
// ❌ El dominio pasa a depender del contenedor y deja de ser testeable solo.
public void Activate(IServiceProvider services)
{
    var clock = services.GetRequiredService<IClock>();
}
```

```csharp
// ✅ Dependencias explícitas por constructor o parámetro.
public void Activate(DateOnly today) { /* ... */ }
```

### 5. Bootstrap duplicado en cada servicio

```csharp
// ❌ Veinte servicios configurando telemetría, errores y auth a su manera.
builder.Services.AddOpenTelemetry()/* 40 líneas */;
builder.Services.AddAuthentication()/* 30 líneas */;
```

```csharp
// ✅ Una sola implementación compartida.
builder.Services.AddCrmPlatform(builder.Configuration);
builder.Services.AddPartyDomain();
builder.Services.AddPartyInfrastructure(builder.Configuration);
```

### 6. Registro contra la clase concreta

```csharp
// ❌ La capa de aplicación termina conociendo infraestructura.
builder.Services.AddScoped<MongoPartyRepository>();
```

```csharp
// ✅ Se registra el puerto; el dominio solo conoce la interfaz.
builder.Services.AddScoped<IPartyRepository, MongoPartyRepository>();
```

## Checklist antes de devolver código

- [ ] `IMongoClient` singleton; repositorios y handlers scoped.
- [ ] Ningún scoped inyectado en un singleton.
- [ ] `BackgroundService` y consumidores crean scope por mensaje.
- [ ] `UseAuthentication()` antes de `UseAuthorization()`.
- [ ] Manejo de errores al principio del pipeline.
- [ ] Adapters registrados contra puertos.
- [ ] Sin service locator en dominio ni aplicación.
- [ ] Sin bootstrap de plataforma duplicado.
- [ ] Configuración validada al arranque.
- [ ] Hay test de que el host construye y resuelve el grafo.

## Conexiones con otros skills

| Skill | Relación |
|---|---|
| `ddd-hexagonal-architecture` | El composition root es el único que conoce implementaciones concretas. |
| `aspnetcore-rest-layer` | Registro del handler global de errores y su posición. |
| `aspnetcore-config-and-secrets` | Validación de configuración al arranque. |
| `mongodb-dotnet-driver` | Lifetime del cliente y registro de serializadores. |
| `multitenancy-authorization` | `ActorContext` scoped, resuelto en el borde. |
| `dotnet-thread-safety-and-shared-state` | Los singletons deben ser inmutables o proteger su estado. |
| `oidc-keycloak-aspnetcore` | Registro de autenticación y su orden en el pipeline. |
