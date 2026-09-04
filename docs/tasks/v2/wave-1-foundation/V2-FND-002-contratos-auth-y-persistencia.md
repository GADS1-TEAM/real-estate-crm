# V2-FND-002 — Contratos, autenticación OIDC y persistencia por ports

- **Ola:** 1 — Fundación ejecutable
- **Estado:** TODO
- **Dependencias:** V2-FND-001
- **UCs:** CONT-001, CONT-002, AUTH-001, AUTH-002, PERSIST-001, MSG-001
- **Owner:** building-blocks, contracts y access adapter
- **Write zone:** contracts, building-blocks, adapters de autenticación/persistencia/mensajería y tests contractuales

## Resultado esperado

Existe una gramática común para APIs y eventos, login OIDC contra Keycloak,
contexto de actor sin tenant, repositories Mongo detrás de ports y Outbox/Inbox
idempotentes para los eventos que alimentan proyecciones.

## Alcance trazable

| UC | Actor | Comportamiento | Owner | Evidencia |
|---|---|---|---|---|
| CONT-001 | Servicios | Serializar Problem Details, paginación, errores estables y versionado. | contracts | Contract tests. |
| CONT-002 | Servicios | Propagar actorId, roles, permissions, correlationId y causationId. | building-blocks | Evento de ejemplo validado. |
| AUTH-001 | Usuario | Iniciar sesión y recuperar sesión OIDC. | auth adapter | Happy/error path. |
| AUTH-002 | Sistema | Resolver usuario y roles sin tenant context. | access adapter | Claims/context tests. |
| PERSIST-001 | Servicio owner | Leer/escribir Mongo mediante un port propio. | building-blocks | Repository contract test. |
| MSG-001 | Servicio owner | Guardar outbox y procesar inbox sin duplicar side effects. | messaging | Idempotency test. |

## Interfaces

- API: ProblemDetails v1, Page<T> v1, ExecutionContext v1 sin tenantId.
- Evento: EventEnvelope v1 con eventId, name, version, occurredAt, actorId,
  correlationId, causationId, aggregateId y payload.
- Ports mínimos: IAuthenticationPort, IRepository<T>, IUnitOfWork,
  IOutbox, IInbox, IEventPublisher y IEventConsumer.
- Keycloak adapter: devuelve userId, displayName, email y claims de roles; la
  autorización de negocio se resuelve en access-service.

## Reglas

- No añadir tenantId, organizationId ni filtros de organización a los
  contratos V2.
- No compartir tipos de dominio entre servicios; contracts contienen DTOs y
  envelopes, no aggregates.
- Los repositorios reciben el actor context cuando una query requiere
  autorización, pero la base no inventa un tenant.
- Outbox e Inbox deben aceptar reintentos y conservar eventId.
- Un token válido no equivale automáticamente a permiso para toda operación.

## Criterios de aceptación

- [ ] Un contrato incompatible rompe el contract test.
- [ ] Un evento de ejemplo puede serializarse/deserializarse con metadata de
  actor y correlación.
- [ ] No existe un filtro tenant obligatorio en los repositorios V2.
- [ ] Un segundo consumo del mismo eventId no repite la proyección.
- [ ] Keycloak autentica un usuario de desarrollo y un token inválido produce
  Problem Details estable.

## Overrides POC

- Se permite fake auth para unit tests y un realm local de Keycloak para
  integración.
- MongoDB Community es la persistencia; no se agrega otra base.
- RabbitMQ Community se conecta en la infraestructura local.

## Definition of Done

- [ ] Contracts versionados y ejemplos.
- [ ] Ports y adapters compilables.
- [ ] Tests de auth, serialización e idempotencia en verde.
- [ ] Documentación de breaking/non-breaking changes.

## Evidencia requerida

1. Ejemplos JSON de API y evento.
2. Resultado de tests contractuales.
3. Flujo de login local.
4. Test que demuestre idempotencia del Inbox.
