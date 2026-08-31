# W3-VIS-04 — Notas en vivo, grabación consentida y extracción de feedback

**Dependencias:** `W3-VIS-03`, `W3-DOC-02`  
**Casos de uso:** `VIS-010`, `VIS-011`, `VIS-014`

## Casos de uso

- `VIS-010` Agregar `VisitNote` rápida durante visita; `VisitNoteAdded`.
- `VIS-011` Capturar audio/media solo si policy/consent lo permite; storage por adapter; `VisitRecordingStored`.
- `VIS-014` Extraer intereses, objeciones, follow-ups y sugerencias de Requirement desde notas/transcripción; `VisitInsightsExtracted`.

## Reglas

Grabación nunca silenciosa. IA propone cambios con provenance/confidence; no sobreescribe Requirement sin policy/autonomía aplicable.

## DoD
- [ ] Consent gate testeado.
- [ ] Notas rápidas funcionan sin formulario largo.
- [ ] Insights trazables a fuente.
