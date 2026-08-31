# W2-MAT-01 — Generar candidatos, calcular score explicable e invalidarlo por cambios

**Dependencias:** `W2-SUP-02`, `W2-DEM-02`  
**Casos de uso:** `MAT-001`, `MAT-002`, `MAT-003`

## Casos de uso

- `MAT-001` **Generar candidatos por Requirement** (Sistema): recuperar Listings potencialmente compatibles usando filtros duros/geo antes del scoring. POC: búsqueda MongoDB detrás de port; no desplegar search-service. Objetos: Requirement + Listing projection. Evento: MatchCandidatesGenerated.
- `MAT-002` **Calcular MatchScore** (Sistema): evaluar REQUIRED/PREFERRED, términos económicos y demás dimensiones con explicación y versión de algoritmo. Servicio: matching-service. Objetos: MatchCase, MatchEvaluation, ScoreResult. Evento: MatchCalculated.
- `MAT-003` **Invalidar match por cambio** (Sistema): marcar desactualizado cuando cambia Requirement/Listing y programar recálculo. Evento: MatchInvalidated.

## Reglas

- Score siempre explicable y versionado.
- Desconocido no penaliza como incumplimiento salvo regla explícita.
- Aplicar `AGENTS.md`, arquitectura POC, tenancy, Outbox/Inbox y ownership.

## DoD
- [ ] UC completos/testeados.
- [ ] Tests de scoring e invalidación.
- [ ] Tenant isolation y contracts.
