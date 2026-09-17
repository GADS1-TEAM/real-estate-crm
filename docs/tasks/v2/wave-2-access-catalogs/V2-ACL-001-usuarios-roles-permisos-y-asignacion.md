# V2-ACL-001 — Usuarios, roles, permisos y responsables comerciales

- **Ola:** 2 — Acceso, Party y catálogos
- **Estado:** TODO
- **Dependencias:** V2-FND-002
- **UCs:** USR-001, USR-002, AUTHZ-001, AUTHZ-002, ASSIGN-001, ASSIGN-002
- **Owner:** access-service
- **Write zone:** access-service y sus contratos; no escribe Party, Property ni proyecciones

## Resultado esperado

El Administrador puede crear, modificar y desactivar usuarios, asignar los
roles Administrador, Vendedor y Responsable Comercial, y asignar o reasignar
un responsable sobre registros de negocio mediante comandos autorizados.

## Alcance trazable

| UC | Actor | Comportamiento | Owner | Evidencia |
|---|---|---|---|---|
| USR-001 | Administrador | Crear y modificar usuario habilitado. | access-service | API + test de aplicación. |
| USR-002 | Administrador | Desactivar usuario sin borrar la historia de sus registros. | access-service | Estado INACTIVE y test de auditoría. |
| AUTHZ-001 | Administrador | Asignar uno de los tres roles obligatorios. | access-service | RoleAssignment persistido. |
| AUTHZ-002 | Sistema | Resolver permiso backend para leer/escribir una capacidad. | access-service | Matriz de permisos testeada. |
| ASSIGN-001 | Administrador/Vendedor autorizado | Asignar responsable comercial a un Party o proceso. | Owner del recurso + access-service | Command autorizado. |
| ASSIGN-002 | Administrador/Responsable Comercial | Reasignar responsable y conservar actor/fecha. | Owner del recurso + access-service | Evento de reasignación. |

## Interfaces

- Commands: CreateUser, UpdateUser, DeactivateUser, AssignRole y
  ResolvePermission.
- Query: GetUsers y GetEffectivePermissions.
- Port consumido por owners: IAuthorizationPort con actorId, permission,
  resourceType y resourceId.
- EventEnvelope v1: UserCreated, UserUpdated, UserDeactivated,
  RoleAssigned y ResponsibleAssigned.
- DTO mínimo de usuario: userId, displayName, email, status y roleCodes.

## Reglas

- Una cuenta de usuario no es Party.
- Los permisos se validan en backend; ocultar botones no es autorización.
- Administrador puede gestionar usuarios, catálogos y asignaciones.
- Vendedor trabaja sobre registros asignados según la regla del owner.
- Responsable Comercial puede consultar y reasignar dentro del equipo definido
  por la instalación única.
- Desactivar un usuario no elimina sus actividades, oportunidades ni
  relaciones históricas.
- No agregar scopes por tenant, sucursal o organización.

## Criterios de aceptación

- [ ] Un usuario habilitado puede iniciar sesión y recibir su rol.
- [ ] El Administrador puede crear, editar y desactivar usuarios.
- [ ] Las acciones prohibidas devuelven 403 Problem Details desde backend.
- [ ] El Vendedor no puede editar un registro que no tiene permitido.
- [ ] El Responsable Comercial puede reasignar un registro y el cambio queda
  identificado con usuario y fecha.
- [ ] Las consultas de autorización no leen colecciones de los owners.

## Overrides POC

- Se permite seed de un Administrador, un Vendedor y un Responsable Comercial.
- El realm local de Keycloak entrega identidad; access-service decide permisos.
- No se implementan invitaciones externas, email de invitación ni
  organizaciones múltiples.

## Definition of Done

- [ ] Usuarios y roles persistidos.
- [ ] Matriz de permisos en backend.
- [ ] Tests de autorización y desactivación.
- [ ] Contratos y eventos documentados.

## Evidencia requerida

1. Matriz rol → permiso.
2. Requests autorizados y rechazados.
3. Test de desactivación con historial preservado.
4. Evidencia de una reasignación.

## Handoff (implementación completada, ver `IMPLEMENTATION_REPORT-V2-ACL-001.md`)

**Qué quedó:** `access-service` real (Mongo/RabbitMQ/Keycloak, `docker-compose.yml`) con
`UserAccount`/`RoleAssignment`, matriz rol→permiso en backend, los 6 endpoints `/api/v1/...`
documentados en el reporte (§"Contratos publicados"), `operations-bff` con sesión OIDC + token
relay + screens/mutations de `ADM-01`/`03`/`04`, seed de 3 usuarios de dev, relay de outbox y
concurrencia optimista reutilizables en `BuildingBlocks.Infrastructure`, 176 tests (fast +
integración contra infra real) en verde.

**Qué falta / queda parcial:**
- **Evidencia requerida #4 y criterio de aceptación 5** (reasignación de responsable): esta task
  solo implementa la *validación* (`POST /api/v1/assignments/validate`, decisión explícita del
  humano — ver reporte, "Decisiones locales" #1). La persistencia real de `responsibleUserId` y
  la publicación de `ResponsibleAssigned` quedan para **V2-PTY-001** (contrato de payload
  `ResponsibleAssignedV1` ya publicado en `contracts/Events/Access/`).
- Login interactivo del BFF (`/login` real en navegador) no se probó manualmente: no hay UI
  conectada todavía (`crm-web` sigue en modo demo, D5/sección 10 del plan Wave 2).
- `scripts/run-slice.{sh,ps1}` (D10) no se creó — ver Follow-ups del reporte.

**Cómo verificar:**
```bash
bash scripts/test-fast.sh          # 160 tests, sin Docker
bash scripts/test-integration.sh   # +16 tests, contra Mongo/RabbitMQ/Keycloak reales
```
Correr manualmente: `dotnet run --project services/access-service/src/AccessService.Api` +
`dotnet run --project bffs/operations-bff/src/OperationsBff.Api` con la infra de
`docker-compose.yml` levantada; credenciales de `dev.administrador`/`dev.vendedor`/
`dev.responsable` en `.env.example`/`DEVELOPMENT.md`.

**Para quien continúe (V2-CAT-001 en paralelo, V2-PTY-001 después):** el adapter HTTP real de
`IAuthorizationPort` ya existe (`HttpAuthorizationPort` +
`services.AddAuthorizationHttpClients(configuration)`, sección `AccessService:BaseUrl`/
`CacheDuration`) — dejar de inyectar `FakeAuthorizationPort` y usar ese adapter una vez mergeada
esta task.
