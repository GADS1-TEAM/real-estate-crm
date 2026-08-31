# W2-ORG-01 — Transferir cartera completa/parcial y visualizar carga organizacional

**Dependencias:** `W1-ORG-02`, `W1-PTY-01`, `W1-DEM-01`  
**Casos de uso:** `ORG-007`, `ORG-008`, `ORG-013`

## Casos de uso

- `ORG-007` Transferir cartera completa cuando un agente cambia de zona o se desvincula; `PortfolioReassignment`; evento `PortfolioReassigned`.
- `ORG-008` Transferir casos seleccionados con motivo/fecha/trazabilidad.
- `ORG-013` Consultar organigrama, ownership y carga operativa en `ManagerCockpit` para decidir reasignaciones.

## Reglas

`access-service` coordina autorización/solicitud; cada servicio propietario aplica el cambio de ownership de su aggregate. No existe update masivo directo entre colecciones.

## DoD
- [ ] Transferencia completa/parcial idempotente.
- [ ] Historial preservado.
- [ ] Manager ve carga según scope.
