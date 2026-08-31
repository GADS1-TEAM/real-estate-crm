# W2-MAT-02 — Presentar match y registrar descartes de cliente/agente

**Dependencias:** `W2-MAT-01`  
**Casos de uso:** `MAT-004`, `MAT-005`, `MAT-006`, `MAT-007`

## Casos de uso

- `MAT-004` Presentar candidatos al agente con razones a favor/en contra y datos faltantes; evento `MatchPresented`.
- `MAT-005` Registrar que un Listing fue presentado al cliente; evento `MatchPresentedToParty`.
- `MAT-006` Registrar descarte del cliente con motivo estructurado + texto libre; `MatchFeedback`; evento `MatchFeedbackRecorded`.
- `MAT-007` Registrar descarte del agente para aprendizaje y métricas; mismo objeto/evento con actor distinto.

## Reglas

Feedback nunca reescribe retrospectivamente el score original. Debe conservar actor, momento, motivo y provenance. UI rápida y explicable.

## DoD
- [ ] Presentación y feedback testeados.
- [ ] No se repite presentación sin indicarlo.
- [ ] Tenant/scope validado.
