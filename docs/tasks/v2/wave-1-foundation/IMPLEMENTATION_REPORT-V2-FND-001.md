# Implementation report — V2-FND-001 — Esqueleto ejecutable de la POC

## UCs cubiertos

- **FOUND-001** — Solution .NET 10 (`RealEstateCrm.slnx`) con 51 proyectos: 11 servicios × 4 capas + 2 BFF × 2 capas + `contracts` + `building-blocks` + 1 proyecto de test de arquitectura. `dotnet build` compila toda la solution sin errores ni warnings.
- **FOUND-002** — `apps/crm-web` y `apps/platform-admin-web` (ya existentes, V2-UX-001 DONE) instalan y construyen con `apps/scripts/build-webs.sh` / `.ps1`, sin modificar su código fuente.
- **FOUND-003** — Boundaries creados para los 11 servicios del alcance V2 (`access`, `party`, `property`, `supply`, `demand`, `matching`, `commercial`, `activity`, `analytics`, `platform-config`, `automation-ai`) y los 2 BFF (`operations`, `platform-admin`), con capas hexagonales por servicio. Test de arquitectura (`RealEstateCrm.ArchitectureTests`) verifica las reglas de dependencia y se demostró en rojo y en verde (ver evidencia).
- **FOUND-004** — `DEVELOPMENT.md` (raíz) y `apps/README.md` documentan instalación, build y test locales.

## Archivos

Creados (write zone declarada: solution, `apps/` solo scripts/README, `bffs/`, `services/`, `contracts/`, `building-blocks/`, `tests/`):

- `RealEstateCrm.slnx`, `Directory.Build.props`, `.gitignore` (nuevo, no existía a nivel raíz).
- `DEVELOPMENT.md` (nuevo, raíz).
- `services/<11 servicios>/src/{Domain,Application,Infrastructure,Api}/` — 44 proyectos.
- `bffs/{operations-bff,platform-admin-bff}/src/{Application,Api}/` — 4 proyectos.
- `contracts/RealEstateCrm.Contracts/` — class library vacía pero compilable.
- `building-blocks/RealEstateCrm.BuildingBlocks/` — class library vacía pero compilable.
- `tests/RealEstateCrm.ArchitectureTests/` — `RepoLocator.cs`, `CsprojFile.cs`, `DependencyRulesTests.cs`.
- `apps/README.md`, `apps/scripts/build-webs.sh`, `apps/scripts/build-webs.ps1` (nuevos; no se tocó código de `crm-web` ni `platform-admin-web`).

No se modificó ningún archivo de la lista prohibida (`README.md`, `ARCHITECTURE.md`, `AGENTS.md`, `docs/adr/`, `docs/implementation/`, `docs/tasks/wave-*`, `design_handoff_*`).

## Comandos y resultados

```text
$ dotnet build RealEstateCrm.slnx
Compilación correcta.
    0 Advertencia(s)
    0 Errores
Tiempo transcurrido 00:00:06.00
```

```text
$ dotnet test RealEstateCrm.slnx
Correctas! - Con error: 0, Superado: 2, Omitido: 0, Total: 2, Duración: 100 ms
```

Demostración en rojo (evidencia de que el test de arquitectura detecta violaciones reales, revertida inmediatamente después):

```text
# Se agregó temporalmente:
#   - PackageReference MongoDB.Driver en PartyService.Domain.csproj
#   - ProjectReference de PropertyService.Application a AccessService.Application

$ dotnet test tests/RealEstateCrm.ArchitectureTests/RealEstateCrm.ArchitectureTests.csproj
[FAIL] Service_projects_do_not_reference_projects_of_another_service
  PropertyService.Application.csproj (property-service) referencia un proyecto de
  otro servicio: AccessService.Application.csproj (access-service)
[FAIL] Domain_projects_do_not_reference_infrastructure_frameworks_or_other_projects
  PartyService.Domain.csproj referencia paquete(s) de infraestructura prohibidos:
  MongoDB.Driver
Con error! - Con error: 2, Superado: 0, Omitido: 0, Total: 2

# dotnet remove package / dotnet remove reference revirtió ambos cambios.
# Re-ejecución posterior: 2/2 en verde (ver bloque anterior).
```

```text
$ bash apps/scripts/build-webs.sh
==> crm-web: npm install / npm run build → Next.js 15.5.25, "Compiled successfully"
==> platform-admin-web: npm install / npm run build → Next.js 15.5.25, "Compiled successfully"
```

Árbol de proyectos: 51 archivos `.csproj` bajo `services/`, `bffs/`, `contracts/`, `building-blocks/`, `tests/` (listado completo disponible con `find services bffs contracts building-blocks tests -iname "*.csproj"`).

Verificación de guardrails: `grep -rniE "tenantId|organizationId"` sobre toda la write zone no encontró usos reales (única coincidencia: la frase que documenta su ausencia en `DEVELOPMENT.md`). No existen carpetas `rental-service`, `scheduling-service`, `notification-service`, `syndication-service`, `maintenance-service` ni `Organization`.

## Decisiones locales

- **Formato de solution:** `dotnet new sln` en .NET 10 SDK genera `.slnx` (XML) en lugar del `.sln` clásico; se usó el formato por defecto del SDK.
- **Convención de nombres:** carpeta kebab-case (`access-service`) + proyectos PascalCase con sufijo de capa (`AccessService.Domain`, `AccessService.Application`, `AccessService.Infrastructure`, `AccessService.Api`).
- **Capas por servicio:** `Domain` sin dependencias (ni paquetes ni `ProjectReference`) → `Application` solo referencia `Domain` → `Infrastructure` referencia `Domain` + `Application` → `Api` (ASP.NET Core "web" vacío) referencia `Application` + `Infrastructure`. Los BFF no tienen capa `Domain` (no poseen aggregates, `ARCHITECTURE.md` §5); solo `Application` + `Api`.
- **Librería de test de arquitectura:** implementación propia (xUnit + `System.Xml.Linq` sobre los `.csproj`) en lugar de `NetArchTest.Rules`, para no depender de un paquete de terceros sin soporte confirmado en `net10.0` y porque la regla pedida es sobre referencias declaradas (paquetes/proyectos), no sobre tipos compilados.
- **Reglas verificadas por el test:** (1) ningún `*.Domain.csproj` bajo `services/` tiene `PackageReference` que empiece con `MongoDB`, `RabbitMQ`, `Microsoft.AspNetCore` o `Keycloak`, ni ningún `ProjectReference`; (2) ningún proyecto bajo `services/<X>` referencia un proyecto bajo `services/<Y>` con `Y != X` (las referencias a `contracts/` o `building-blocks/` sí están permitidas, son shared kernel sin ownership de servicio).
- **`.gitignore` en la raíz:** no existía; se agregó (`bin/`, `obj/`, `.vs/`, `.idea/`, artefactos de SO) porque el build de 51 proyectos genera esos artefactos y antes solo `apps/*/.gitignore` cubría Next.js.
- **Documentación de desarrollo:** `DEVELOPMENT.md` nuevo en la raíz (no se tocó `README.md`) + `apps/README.md` y `apps/scripts/build-webs.{sh,ps1}` dentro de `apps/`, según lo habilitado por la write zone ("en `apps/` solo scripts/README de build").

## Supuestos

- El board (`docs/tasks/v2/TASK_BOARD.md`) todavía marca `V2-SCP-001` como `READY`, no `DONE`; el cierre de esa task y el resto del Paso 0 fueron confirmados explícitamente por el humano en esta conversación. No se modificó el board (fuera de write zone; además un humano es quien marca DONE).
- El entorno de la PC tiene Node `v24.16.0` (el plan pide "Node 22+"); ninguna de las dos webs declara `engines` en su `package.json`, así que no hubo conflicto de versión. Se documenta la versión real usada como evidencia.
- No se agregaron endpoints `/health/live` ni `/health/ready`, ni ningún `Program.cs` más allá del template mínimo de ASP.NET Core vacío: eso es explícitamente alcance de `V2-FND-003`.
- No se agregó ningún `PackageReference` real (Mongo/RabbitMQ/Keycloak) a ningún proyecto: esas integraciones son de `V2-FND-002`/`V2-FND-003`.

## Follow-ups

- `V2-FND-002` debe llenar `contracts/RealEstateCrm.Contracts` (ProblemDetails v1, `Page<T>`, `ExecutionContext`, `EventEnvelope`) y `building-blocks/RealEstateCrm.BuildingBlocks` (ports `IRepository<T>`, `IUnitOfWork`, `IOutbox`, `IInbox`, `IEventPublisher`, `IEventConsumer`, `IAuthenticationPort`) hoy vacíos pero compilables.
- `V2-FND-003` debe agregar Compose, CI (que corra `dotnet build`/`dotnet test RealEstateCrm.slnx` y el build de ambas webs) y observabilidad; hoy no existe ningún workflow de CI.
- El test de arquitectura solo cubre las dos reglas explícitas de la task (dominio sin infraestructura, sin referencias cross-servicio). Si aparecen más invariantes de dependencia en waves posteriores, ampliarlo ahí en vez de duplicar lógica.
