# Board de implementación V2

Backlog activo del trabajo práctico. El orden es por dependencia y entrega;
una task no se considera lista solo por existir el archivo: debe cumplir su
Definition of Done y adjuntar evidencia.

## Orden vigente

| Orden | Task | Estado | Dependencias | Entregable |
|---:|---|---|---|---|
| 1 | [V2-SCP-001](wave-0-scope-design/V2-SCP-001-alcance-y-modelo-v2.md) | DONE | — | Alcance, relaciones del modelo y ADR |
| 2 | [V2-UX-001](wave-0-scope-design/V2-UX-001-incorporar-diseno-claude-design.md) | DONE | diseño recibido | Incorporación del diseño de Claude Design |
| 3 | [V2-FND-001](wave-1-foundation/V2-FND-001-esqueleto-poc.md) | DONE | SCP-001 | Esqueleto de solución y servicios |
| 4 | [V2-FND-002](wave-1-foundation/V2-FND-002-contratos-auth-y-persistencia.md) | DONE | FND-001 | Contratos, auth y puertos de persistencia |
| 5 | [V2-FND-003](wave-1-foundation/V2-FND-003-infra-ci-y-observabilidad.md) | TODO | FND-001 | Compose, CI y telemetría base |
| 6 | [V2-ACL-001](wave-2-access-catalogs/V2-ACL-001-usuarios-roles-permisos-y-asignacion.md) | TODO | FND-002 | Usuarios, roles, permisos y responsables |
| 7 | [V2-CAT-001](wave-2-access-catalogs/V2-CAT-001-catalogos-comerciales.md) | TODO | FND-002 | Catálogos comerciales versionados |
| 8 | [V2-PTY-001](wave-2-access-catalogs/V2-PTY-001-party-empresa-contacto-y-estados.md) | TODO | ACL-001, CAT-001 | Empresa, contacto, relación y estados Party |
| 9 | [V2-PRP-001](wave-3-real-estate-core/V2-PRP-001-property-listing-y-productos.md) | TODO | PTY-001, CAT-001 | Property, interés, Listing y producto inmobiliario |
| 10 | [V2-DMD-001](wave-3-real-estate-core/V2-DMD-001-requirement-captation-y-valuation.md) | TODO | PTY-001, PRP-001 | Demanda, captación, valuación y mandato mínimo |
| 11 | [V2-MAT-001](wave-3-real-estate-core/V2-MAT-001-matching-y-seleccion.md) | TODO | DMD-001, PRP-001 | Matching explicable Requirement-Listing |
| 12 | [V2-PIPE-001](wave-4-commercial-progression/V2-PIPE-001-oportunidades-como-proyeccion-y-embudo.md) | TODO | CAT-001, DMD-001, MAT-001 | Fachada de oportunidad y embudo configurable |
| 13 | [V2-COM-001](wave-4-commercial-progression/V2-COM-001-visita-negociacion-reserva-y-cierre.md) | TODO | PIPE-001 | Visita ocurrida, negociación, reserva y cierre |
| 14 | [V2-ACT-001](wave-5-history-analytics/V2-ACT-001-actividades-e-historial.md) | TODO | PTY-001, PIPE-001, COM-001 | Actividades manuales y timeline |
| 15 | [V2-ANA-001](wave-5-history-analytics/V2-ANA-001-metricas-y-estadisticas.md) | TODO | PIPE-001, COM-001, ACT-001 | Métricas, estadísticas y dashboard |
| 16 | [V2-AI-001](wave-6-ai-release/V2-AI-001-asistente-ia-con-revision-humana.md) | TODO | ANA-001, ACT-001, UX-001 | IA útil, autorizada y revisable |
| 17 | [V2-REL-001](wave-6-ai-release/V2-REL-001-busqueda-paginacion-y-entrega-final.md) | TODO | todas las anteriores aplicables | Regresión, aceptación y entrega final |

## Hitos

- **24/09 — primera entrega:** login, usuario habilitado, Empresa/Contacto,
  relación, Listing, alta/edición de oportunidad visible, responsable,
  producto, listado/detalle, embudo y cambio persistido de etapa.
- **12/11 — entrega final:** roles/permisos, catálogos, dominio inmobiliario,
  actividades e historial, funnel configurable, cierre ganado/perdido,
  motivos de pérdida, búsqueda/filtros/paginación, métricas y la IA si el
  core ya está probado.

## Regla de coordinación

Las tasks paralelas solo pueden modificar sus write zones declaradas. Las
fachadas, read models y contratos no otorgan permiso para escribir la
colección owner de otro servicio. Si aparece una entidad Opportunity física,
multi-tenant o una capacidad excluida, detener y reabrir ADR-001.
