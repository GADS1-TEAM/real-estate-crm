# W3-SUP-02 — Negociar, registrar, renovar y vigilar vencimiento del mandato

**Dependencias:** `W3-SUP-01`, `W3-DOC-01`  
**Casos de uso:** `SUP-009`, `SUP-010`, `SUP-011`, `SUP-012`

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `event-driven-outbox-inbox`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Casos de uso

- `SUP-009` Registrar negociación previa del `CommercialMandate`; `CommercialMandateNegotiationChanged`.
- `SUP-010` Registrar mandato/autorización con alcance, modalidad, exclusividad, vigencia, comisión y Parties; `CommercialMandateGranted`.
- `SUP-011` Renovar/reemplazar mediante nueva versión sin mutar histórico; `CommercialMandateSuperseded`.
- `SUP-012` Detectar expiración, alertar y aplicar policy a acciones que requieren mandato vigente; `CommercialMandateExpired`.

## Reglas

Mandato formal separado de Property/Listing y referenciado por las operaciones que lo usaron. Documentación y firmantes por IDs.

## DoD
- [ ] Lifecycle/versionado testeado.
- [ ] Expiración y policy contextual.
- [ ] Historial/auditoría preservados.
