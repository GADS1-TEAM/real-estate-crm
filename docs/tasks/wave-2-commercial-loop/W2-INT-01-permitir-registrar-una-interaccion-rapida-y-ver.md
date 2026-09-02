# W2-INT-01 — Permitir registrar una interacción rápida y ver timeline omnicanal aunque todavía no haya conectores

**Dependencias:** `W1-PTY-01`  
**Casos de uso:** `INT-014`, `INT-016`, `INT-018`

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `event-driven-outbox-inbox`, `rabbitmq-dotnet`, `aspnetcore-messaging`, `aspnetcore-outgoing-http`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Casos de uso

- `INT-014` **Crear nota manual de interacción** (Agente): Registrar una Interaction breve por contacto presencial/externo sin exigir transcripción completa. Servicios: interaction-service. Objetos: Interaction. Eventos: InteractionRecorded.
- `INT-016` **Consultar timeline omnicanal** (Agente): Mostrar contactos de todos los canales en orden temporal con filtros y links de dominio. Servicios: analytics-copilot-service. Objetos: Party360 / conversation read model. Eventos: —.
- `INT-018` **Quick capture post-contacto** (Agente): Después de llamada/presencial permitir solo resultado + próxima acción, dejando enriquecimiento opcional. Servicios: interaction-service. Objetos: Interaction. Eventos: InteractionRecorded.

## Reglas

- Aplicar `AGENTS.md`, `ARCHITECTURE.md` y decisiones POC.
- Owner único de datos; cross-context por contratos/eventos/read models.
- `tenantId` obligatorio; sin acceso a colecciones ajenas.
- Datos enriquecidos opcionales salvo necesidad real.
- BFF sin lógica de dominio; UI task-driven.
- Eventos versionados + Outbox/Inbox cuando aplique.

## DoD

- [ ] UC completos y testeados.
- [ ] Aislamiento tenant/autorización.
- [ ] Contratos/eventos y observabilidad actualizados.
- [ ] PR acotado.
