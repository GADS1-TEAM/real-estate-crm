# Implementation report — V2-ACL-001a — Contrato de autorización

> Task definida en `PRPs/_backlog/2026-09-17-plan-wave-2-acceso-catalogos-party.md`,
> sección 7.1. No tiene archivo propio en `docs/tasks/v2/` ni fila en
> `docs/tasks/v2/TASK_BOARD.md`: es el contrato que bloquea a `V2-ACL-001`,
> `V2-CAT-001` y `V2-PTY-001`.

## UCs cubiertos

- **AUTHZ-002** (parcial, solo el contrato) — `IAuthorizationPort.EvaluateAsync(actorId,
  permission, resourceType, resourceId?, ct)` → `AuthorizationDecision`. La resolución real
  (matriz rol→permiso, persistencia, HTTP) la implementa `V2-ACL-001`; acá solo el puerto que
  van a consumir los owners y el stub de test.

## Archivos

Creados (write zone declarada: `building-blocks/RealEstateCrm.BuildingBlocks/Authorization/`,
`contracts/` DTOs de autorización, `tests/RealEstateCrm.TestSupport`, `tests/RealEstateCrm.ContractTests`):

- `contracts/RealEstateCrm.Contracts/Authorization/Permissions.cs`: 8 constantes de permiso v1
  (ver "Contratos publicados").
- `contracts/RealEstateCrm.Contracts/Authorization/ResourceTypes.cs`: 3 constantes de
  `resourceType` (`user`, `catalog`, `party`).
- `contracts/RealEstateCrm.Contracts/Authorization/DenyReasons.cs`: 3 constantes de motivo de
  denegación (`permission_not_granted`, `user_inactive`, `user_pending`).
- `contracts/RealEstateCrm.Contracts/Authorization/AuthorizationDecision.cs`: record
  `(bool Allowed, string? ReasonCode)` con factories `Allow()`/`Deny(reasonCode)`.
- `building-blocks/RealEstateCrm.BuildingBlocks/Authorization/IAuthorizationPort.cs`: el puerto.
- `tests/RealEstateCrm.TestSupport/Authorization/FakeAuthorizationPort.cs`: `AllowAll()`,
  `DenyAll(reasonCode?)`, `AllowingOnly(permissions)`.
- `tests/RealEstateCrm.ContractTests/Authorization/` (4 archivos): `AuthorizationDecisionContractTests`,
  `PermissionsContractTests`, `ResourceTypesContractTests`, `FakeAuthorizationPortTests`.

No se creó ningún `.csproj` nuevo: todo entra en proyectos ya existentes de `V2-FND-002`. No se
tocó persistencia, endpoints, adapter HTTP ni ningún `Program.cs` (fuera de alcance de esta
task, sección 7.1 del plan). No se modificó `NoTenantGuardrailTests`: ya escanea por reflection
los dos assemblies (`RealEstateCrm.Contracts`, `RealEstateCrm.BuildingBlocks`) donde viven estos
tipos nuevos, así que los cubre automáticamente.

## Comandos y resultados

```text
$ dotnet build RealEstateCrm.slnx
Compilación correcta.
    0 Advertencia(s)
    0 Errores

$ dotnet test RealEstateCrm.slnx --filter "Category!=RequiresMongo&Category!=RequiresRabbitMq&Category!=RequiresKeycloak"
RealEstateCrm.ArchitectureTests.dll                   → Superado: 3,  Total: 3
RealEstateCrm.ContractTests.dll                       → Superado: 51, Total: 51  (21 previos + 30 nuevos de Authorization)
RealEstateCrm.BuildingBlocks.Infrastructure.Tests.dll → Superado: 48, Total: 48

$ bash scripts/test-integration.sh   (Docker real: mongo, rabbitmq, keycloak)
==> healthchecks: crm-mongo / crm-rabbitmq / crm-keycloak → healthy
==> dotnet test --filter "Category=RequiresMongo|Category=RequiresRabbitMq|Category=RequiresKeycloak"
RealEstateCrm.BuildingBlocks.Infrastructure.Tests.dll → Superado: 11, Total: 11
```

Esta task no agrega ningún test marcado `RequiresMongo/RequiresRabbitMq/RequiresKeycloak`
(no toca persistencia ni mensajería): el 11/11 de `test-integration.sh` es el mismo set
heredado de `V2-FND-002`/`V2-FND-003`, corrido en verde para cumplir el gate de la sección 6.

Gate del Paso 0 (plan Wave 2, sección 2) verificado antes de escribir código: Wave 1 DONE en el
board, las 11 decisiones de la sección 5 ya estaban marcadas, Docker Desktop corriendo y
`bash scripts/test-integration.sh` en verde en `main` (mismo comando de arriba, corrido antes de
cualquier cambio).

## Contratos publicados

**Puerto** (`RealEstateCrm.BuildingBlocks.Authorization`):

```csharp
Task<AuthorizationDecision> EvaluateAsync(
    Guid actorId, string permission, string resourceType, Guid? resourceId,
    CancellationToken cancellationToken = default);
```

**DTO** (`RealEstateCrm.Contracts.Authorization`): `AuthorizationDecision(bool Allowed, string? ReasonCode)`.

**Permisos v1** (`Permissions`, convención `"<recurso>.<accion>"` snake_case; set completo de
Wave 2, no solo los ejemplos de la sección 7.1 del plan — waves siguientes agregan los suyos acá):

| Constante | Valor |
|---|---|
| `UsersRead` | `users.read` |
| `UsersManage` | `users.manage` |
| `CatalogsRead` | `catalogs.read` |
| `CatalogsManage` | `catalogs.manage` |
| `PartiesRead` | `parties.read` |
| `PartiesWrite` | `parties.write` |
| `PartiesChangeCommercialStatus` | `parties.change_commercial_status` |
| `PartiesAssignResponsible` | `parties.assign_responsible` |

**Tipos de recurso** (`ResourceTypes`): `User = "user"`, `Catalog = "catalog"`, `Party = "party"`.

**Motivos de denegación** (`DenyReasons`): `PermissionNotGranted = "permission_not_granted"`,
`UserInactive = "user_inactive"`, `UserPending = "user_pending"`. Deliberadamente **no** incluye
un motivo de "no es el responsable del registro": esa regla de propiedad la evalúa el servicio
owner con su propio dato (`responsibleUserId`), no `access-service` (decisión D2). Documentado
en el XML doc del puerto.

**Fake para tests** (`RealEstateCrm.TestSupport.Authorization.FakeAuthorizationPort`):
`AllowAll()`, `DenyAll(reasonCode = PermissionNotGranted)`, `AllowingOnly(params permissions)`.
Es lo que `V2-CAT-001` y `V2-PTY-001` inyectan hasta que `V2-ACL-001` publique el adapter HTTP
real.

### Matriz rol → permiso (propuesta, sin implementar)

No es código: es la referencia que `V2-ACL-001` debe confirmar o ajustar al implementar la
matriz real en `access-service`. Roles según D3/D9 del plan Wave 2 (Administrador, Vendedor,
Responsable Comercial — únicos tres roles de negocio de la instalación).

| Permiso | Administrador | Vendedor | Responsable Comercial | Notas |
|---|:---:|:---:|:---:|---|
| `users.read` | ✅ | ✅ | ✅ | Solo datos básicos (no gestión): con token relay (D6) el BFF lo necesita para mostrar el nombre del responsable en pantallas de Vendedor; Responsable Comercial además lo necesita para elegir a quién reasignar dentro de su equipo. |
| `users.manage` | ✅ | ❌ | ❌ | Alta/edición/baja de usuario y asignación de rol: solo Administrador (regla del plan Wave 2, D2 y task V2-ACL-001). |
| `catalogs.read` | ✅ | ✅ | ✅ | Todo autenticado lee catálogos (V2-CAT-001, "todos los autenticados leen"). |
| `catalogs.manage` | ✅ | ❌ | ❌ | Solo Administrador gestiona catálogos (V2-CAT-001, "Autorización"). |
| `parties.read` | ✅ | ✅ | ✅ | Sin restricción de propiedad para lectura (no está en las reglas de V2-PTY-001). |
| `parties.write` | ✅ | ✅ (con propiedad) | ✅ (con propiedad) | Este puerto solo concede el permiso genérico. La regla de propiedad ("Vendedor solo edita lo asignado", `responsibleUserId`) la aplica `party-service` (owner), no `access-service` (D2). |
| `parties.change_commercial_status` | ✅ | ❌ | ✅ (con propiedad) | PTY-007 (V2-PTY-001) asigna este UC a "Administrador/Responsable", sin Vendedor: cambia el `commercialStatus` y el responsable de la party con baja lógica, distinto de `parties.write` (edición de datos). |
| `parties.assign_responsible` | ✅ | ❌ | ✅ | Alcance trazable de V2-ACL-001 (ASSIGN-001/ASSIGN-002): "Administrador/Vendedor autorizado" y "Administrador/Responsable Comercial" asignan o reasignan responsable — se interpreta que el Vendedor **no** asigna/reasigna por sí mismo (solo ejecuta sobre lo ya asignado), y que Responsable Comercial reasigna "dentro del equipo definido por la instalación única" (regla de V2-ACL-001, sin scope adicional). `V2-ACL-001` confirma esta lectura al implementar. Nota: al crear una Party, `responsibleUserId` = creador por defecto (lo implementa `V2-PTY-001`), sin pasar por este permiso — la primera asignación implícita del creador no es una "asignación" en el sentido de ASSIGN-001. |

"Con propiedad" = el permiso genérico se concede siempre vía `IAuthorizationPort`; el owner
(`party-service`, en `V2-PTY-001`) además exige que `responsibleUserId` del registro sea el
actor, salvo que el actor sea Administrador. Esa comparación no pasa por este puerto.

## Decisiones locales

- **`AuthorizationDecision` vive en `contracts`, no en `building-blocks`**: es el DTO que
  cruzará HTTP cuando `V2-ACL-001` implemente `POST /api/v1/authorization/evaluate` (D2); ponerlo
  en `contracts` desde ahora evita que `V2-ACL-001` tenga que redefinirlo o mapearlo.
- **Permisos: set completo de Wave 2, no solo los 7 "ej." de la sección 7.1 del plan** — decisión
  confirmada por el humano (agregado `users.read`, total 8). Ninguno fuera de lo que
  `V2-ACL-001`/`V2-CAT-001`/`V2-PTY-001` ya declaran necesitar.
- **`resourceType` sigue siendo `string` en la firma del puerto** (no un enum ni un tipo fuerte):
  lo evalúa un servicio HTTP externo (`access-service`) y el plan fija la firma "actorId,
  permission, resourceType, resourceId?, ct" tal cual. `ResourceTypes` son constantes de
  conveniencia, no una restricción de tipo — confirmado por el humano.
- **Motivos de denegación acotados a rol/estado del actor** (`permission_not_granted`,
  `user_inactive`, `user_pending`): la regla de propiedad del registro nunca pasa por este
  puerto (D2) — confirmado por el humano, documentado en el XML doc de `IAuthorizationPort` y en
  `DenyReasons`.
- **Sin nuevo `.csproj`**: `Authorization/` entra como carpeta nueva dentro de los 4 proyectos ya
  existentes de `V2-FND-002` (`RealEstateCrm.Contracts`, `RealEstateCrm.BuildingBlocks`,
  `RealEstateCrm.TestSupport`, `RealEstateCrm.ContractTests`). No hace falta tocar
  `RealEstateCrm.slnx`.

## Supuestos

- La matriz rol→permiso de la sección "Contratos publicados" es una **propuesta no vinculante**:
  `V2-ACL-001` puede ajustarla al implementar (ej. si decide que Responsable Comercial sí puede
  asignar, no solo reasignar). Si `V2-ACL-001` cambia algo de esta matriz, debe actualizar esta
  tabla o la propia (ver Follow-ups).
- `users.read` no estaba en los "ej." de la sección 7.1 del plan; se agregó porque
  `ASSIGN-001`/`ASSIGN-002` (elegir/reasignar responsable) y la consulta de usuarios de
  `V2-ACL-001` (`GetUsers`) lo necesitan como permiso propio, distinto de `users.manage` — decisión
  confirmada por el humano.

## Follow-ups

- `V2-ACL-001`: implementar la matriz real (persistida o hardcodeada según decida esa task),
  el adapter HTTP de `IAuthorizationPort` en `BuildingBlocks.Infrastructure/Authorization/`, y
  confirmar o corregir la matriz propuesta de este reporte en su propio
  `IMPLEMENTATION_REPORT-V2-ACL-001.md`.
- `V2-CAT-001`/`V2-PTY-001`: usar `FakeAuthorizationPort` en tests hasta que `V2-ACL-001` mergee;
  después de ese merge, cambiar al adapter real vía inyección de dependencias en su propio
  `Program.cs` (fuera de esta task).
- Si una wave futura necesita un permiso nuevo, agregarlo a `Permissions` (mismo archivo,
  mismo namespace) en vez de crear un catálogo paralelo.
