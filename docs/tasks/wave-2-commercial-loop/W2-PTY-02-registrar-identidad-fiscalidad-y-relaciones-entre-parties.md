# W2-PTY-02 — Registrar identidad/fiscalidad y relaciones entre Parties con vigencia

**Dependencias:** `W2-PTY-01`  
**Casos de uso:** `PTY-006`, `PTY-007`, `PTY-008`, `PTY-009`

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `event-driven-outbox-inbox`, `dotnet-parsing-and-validation`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Casos de uso

- `PTY-006` Agregar metadata de DNI/pasaporte/u otro documento; archivo en Documents; eventos `PartyIdentityAttributesChanged`, `DocumentUploaded`.
- `PTY-007` Agregar CUIT/CDI u otro identificador fiscal según país/tipo; evento `PartyIdentityAttributesChanged`.
- `PTY-008` Crear `PartyRelationship` para cónyuge, representante, apoderado, socio, garante, cotitular, beneficiario u otro vínculo con vigencia; evento `PartyRelationshipCreated`.
- `PTY-009` Finalizar relación preservando historia; evento `PartyRelationshipEnded`.

## Reglas

Documento binario no vive dentro de Party. Relaciones son explícitas, tipadas y temporales cuando corresponda. Cambios identificatorios disparan Identity Resolution.

## DoD
- [ ] UC completos/testeados.
- [ ] Provenance e historial preservados.
- [ ] Tenant/scope validado.
