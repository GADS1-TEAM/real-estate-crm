# W1-ORG-02 — Usuarios, roles, scopes y acceso efectivo

**Dependencias:** `W1-ORG-01`  
**Casos de uso:** `ORG-003`, `ORG-004`, `ORG-009`, `ORG-012`

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `event-driven-outbox-inbox`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Objetivo

Permitir invitar usuarios, asignar rol/scope, consultar autorización efectiva y desactivar accesos sin borrar historia.

## Casos de uso

- `ORG-003` Invitar usuario: crear invitación y luego `Membership`; evento `MembershipCreated`.
- `ORG-004` Asignar rol y scope: `RoleDefinition`, `PermissionGrant`; evento `PermissionGrantChanged`.
- `ORG-009` Consultar autorización efectiva: resolver acción + recurso + actor y explicar rol/scope/override; no requiere evento artificial.
- `ORG-012` Desactivar usuario: finalizar `Membership`; evento `MembershipEnded`; la cartera no se reasigna silenciosamente.

## Reglas

- scope mínimo soportado: OWN / TEAM / UNIT / ORGANIZATION;
- no hardcodear `branchId` como única pertenencia del usuario;
- overrides son explícitos, auditables y potencialmente temporales;
- ocultar un botón nunca reemplaza la autorización backend.

## Criterios de aceptación

- [ ] Agente ve su scope por defecto.
- [ ] Manager puede operar dentro de su unidad según permisos.
- [ ] Overrides amplían/restringen sin cambiar el rol base.
- [ ] Desactivar acceso preserva trazabilidad y ownership histórico.
