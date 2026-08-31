# W2-PTY-01 — Enriquecer Party como persona humana, jurídica o estructura jurídica

**Dependencias:** `W1-PTY-01`  
**Casos de uso:** `PTY-002`, `PTY-003`, `PTY-004`

## Casos de uso

- `PTY-002` Enriquecer `Party` NATURAL_PERSON con perfil, identificadores, domicilio y contactos; evento `PartyIdentityAttributesChanged`.
- `PTY-003` Registrar/enriquecer `Party` LEGAL_ENTITY con razón social, país, identificadores y relaciones de representación; `PartyRegistered`/cambios de identidad según alta o edición.
- `PTY-004` Modelar LEGAL_ARRANGEMENT como fideicomiso u otra estructura jurídica sin forzarla a persona humana/sociedad.

## Reglas

Party sigue siendo identidad única; perfiles son especializaciones contextuales. Nacionalidad/país no limitados a Argentina.

## DoD
- [ ] Tres tipos soportados.
- [ ] Captura parcial permitida.
- [ ] Cambios identitarios disparan resolución asíncrona.
