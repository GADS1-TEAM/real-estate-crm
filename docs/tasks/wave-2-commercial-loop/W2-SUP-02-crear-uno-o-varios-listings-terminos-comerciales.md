# W2-SUP-02 — Crear uno o varios Listings, términos comerciales y presentación base

**Dependencias:** `W2-SUP-01`  
**Casos de uso:** `SUP-016`, `SUP-017`, `SUP-018`, `SUP-019`

## Casos de uso

- `SUP-016` Crear `Listing` mínimo para una Property con operación + términos; evento `ListingCreated`.
- `SUP-017` Permitir múltiples Listings simultáneos sobre la misma Property (por ejemplo venta + alquiler).
- `SUP-018` Editar `CommercialTerms`: precio, moneda, financiación, permuta, contraprestaciones; conservar historial; evento `ListingCommercialTermsChanged`.
- `SUP-019` Editar `CanonicalPresentation` —presentación base del aviso— con título/descripción/media/orden que heredan canales; evento `ListingPresentationChanged`.

## Reglas

Property ≠ Listing. Un listing no se recicla para otra operación histórica. Overrides por canal se modelan posteriormente sin duplicar toda la presentación.

## DoD
- [ ] Simultaneidad e historial testeados.
- [ ] Términos económicos extensibles.
- [ ] Presentación base claramente separada de canal.
