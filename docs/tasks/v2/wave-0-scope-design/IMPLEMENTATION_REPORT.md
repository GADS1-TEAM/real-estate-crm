# IMPLEMENTATION_REPORT · V2-UX-001

## Resultado

Se implementó el frontend de `design_handoff_crm` en `apps/crm-web`.

- 172 entradas tipadas en `screenRegistry`.
- 137 superficies V2 resueltas por vistas funcionales de módulo.
- 35 superficies F2 conservadas como diferidas/deshabilitadas.
- Shell productivo con rutas de negocio, búsqueda global, creación rápida, permisos efectivos y navegación mobile.
- Design system interno con tokens Brick/eminent, primitives semánticos e iconografía SVG.
- Store demo explícito, persistente y aislado; BFF sin fallback productivo.
- No se modificó `design_handoff_platform_admin`.

## Casos de uso y ownership

| UC | Cobertura | Owner visual | Persistencia productiva |
|---|---|---|---|
| UX-001 | Fuentes HTML inventariadas y convertidas a tokens, componentes, decisiones y catálogo | `crm-web` | No aplica |
| UX-002 | Pantallas registradas con journey, módulo, ruta, patrón y task | `crm-web` | BFF por contrato |
| UX-003 | Estados loading/empty/error/forbidden/success/validation/offline/responsive | `crm-web` | BFF para hechos reales |

Aggregates/entidades owner referenciados visualmente: Party, Property, Listing, Requirement, CaptationCase/Valuation/Mandate, Match, CommercialPipelineItem, Activity, Reservation, Operation, User/Role y Catalog. La UI no crea una entidad física `Opportunity` ni consulta colecciones de otros servicios.

## Interfaces y write zones

- Consume en productivo: BFF HTTP mediante `BffCrmDataSource` (`GET /screens/:screenId`, `POST /mutations/:name`).
- Consume en demo: `DemoCrmDataSource` más `DemoStoreProvider` con `useReducer`.
- Produce en productivo: ninguna llamada directa a DB ni eventos desde el frontend.
- Write zone: `apps/crm-web` y documentación UX de esta task.
- Fuera de write zone: aggregates backend, contratos de dominio, `design_handoff_platform_admin` y cualquier capacidad F2.

## Acceptance checklist

- [x] Inventario de 172 superficies recibido desde `design_handoff_crm` y registrado.
- [x] 137 V2 y 35 F2 verificados por test de conteo/fase.
- [x] Cada entrada tiene mapping de render, journey y ruta a través del registro tipado.
- [x] Se implementaron los cuatro recorridos: consulta, captación, equipo/métricas y reserva/cierre/administración.
- [x] `CommercialPipelineItem` se representa mediante `sourceType=REQUIREMENT|CAPTATION_CASE`; no se usa `Opportunity` como fuente de verdad.
- [x] Email/WhatsApp son registros históricos manuales y no acciones de envío.
- [x] Mandato sin firma advierte y no bloquea publicación.
- [x] Seña de reserva es informativa; no hay pagos, recibos ni cobranza.
- [x] `UNKNOWN` conserva incertidumbre y no se convierte en falso/cero.
- [x] Estados persistentes usan `Alert`; confirmaciones transitorias usan `Toast`.
- [x] Mobile incluye bottom bar, creación rápida, actividad y cola offline demo.
- [x] La ruta `/__design` expone el catálogo interno sin incorporarlo al shell productivo.
- [x] No se agregó `tenantId`, selector de organización, agenda, tareas, recordatorios, notificaciones, integraciones, alquileres ni Platform Admin.

## Verificación ejecutada

Desde `apps/crm-web`:

| Comando | Resultado |
|---|---|
| `npm.cmd install` | OK |
| `npm.cmd test -- --run` | OK, 10 archivos / 87 tests |
| `npm.cmd run typecheck` | OK |
| `npm.cmd run lint` | OK |
| `npm.cmd run test:e2e` | 9 escenarios correctos / 3 omitidos intencionalmente por proyecto; el teardown de Next quedó retenido en Windows y requirió limpieza controlada |
| `npm.cmd run build` | OK, build Next.js de producción sin modo demo |
| `npm.cmd run build:demo` | OK, build Next.js de producción |

El entorno de OneDrive produjo inicialmente un `Access denied` de esbuild al resolver el directorio superior desde la ruta larga. Los tests unitarios se repitieron desde una letra temporal con `subst`; es una limitación del entorno, no una excepción de la aplicación.

## QA visual y fidelity ledger

| Punto | Referencia del handoff | Implementación | Verificación |
|---:|---|---|---|
| 1 | Eminent: sidebar oscuro + naranja Brick | `CrmShell`, tokens CSS y nav activo | Revisado en Browser/IAB desktop |
| 2 | Dashboard denso con atención y comisión estimada | `HomeView`, alerta derivada y funnel sin pipeline ponderado | Revisado en Browser/IAB desktop |
| 3 | Mobile bottom bar y registro rápido | `SHL-06`, drawer de creación rápida, targets táctiles | Revisado en `390×844` |
| 4 | Score explicado y criterios con pesos | `MatchScoreExplanation`, `CriterionEditor`, score demo | Cubierto por test de componentes y vista demo |
| 5 | Catálogo interno con V2/F2 y tokens | `/__design` y `DesignCatalog` | Revisado en Browser/IAB desktop/mobile |

La comparación pixel a pixel contra los HTML autocontenidos queda no verificada: Browser/IAB bloqueó abrir directamente referencias `file://`. No se afirma fidelidad completa de screenshots hasta contar con una ruta de referencia servida o una verificación manual autorizada.

## Pendientes explícitos

- Conectar `BffCrmDataSource` al BFF real cuando existan sus endpoints y contrato desplegado.
- Reemplazar placeholders por imágenes reales del cliente.
- Completar los journeys contra backend autenticado; la demo no representa autorización real ni sincronización real.
- Las 35 superficies F2 siguen diferidas según el alcance aprobado.
