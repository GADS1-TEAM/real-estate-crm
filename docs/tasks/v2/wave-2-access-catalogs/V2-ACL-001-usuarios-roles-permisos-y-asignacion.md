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
