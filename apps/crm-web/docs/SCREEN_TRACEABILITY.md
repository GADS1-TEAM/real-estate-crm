# Matriz de trazabilidad visual · CRM V2

Fuente de diseño: `design_handoff_crm/designs/00 - Mapa de Navegacion.html` a `08 - Decisiones de Producto.html`. La matriz completa y ejecutable vive en `src/lib/screen-registry.ts`: cada entrada tiene `id`, `title`, `module`, `phase`, `pattern`, `task`, `renderKey` y `journey`. El test `src/lib/__tests__/screen-registry.test.ts` falla si faltan IDs, fases o mappings.

## Resumen verificable

| Alcance | Cantidad | Resolución |
|---|---:|---|
| V2 | 137 | Vista funcional de módulo reutilizable |
| F2 | 35 | Superficie inventariada y diferida |
| Total | 172 | Registro tipado completo |

## Pantalla → journey → ruta → estado

La siguiente tabla agrupa únicamente pantallas con el mismo contrato de navegación y estado base. Los IDs listados son entradas individuales del registro; no representan una pantalla inventada adicional.

| Pantallas | Journey | Ruta primaria | Estado visual mínimo implementado |
|---|---|---|---|
| SHL-01…SHL-06 | Shell | Todas las rutas V2 | Shell normal, búsqueda global, command palette, drawer de creación rápida, menú de usuario y navegación mobile |
| SHL-07 | F2 | `/inicio` | Diferida/deshabilitada |
| AUT-01…AUT-04 | Shell | `/login` / `/inicio?screen=AUT-*` | Ingreso, sesión expirada con borrador, sin habilitación y primer uso |
| INI-01…INI-02 | J1 | `/inicio` | Dashboard del agente, atención derivada, loading/empty/error/offline |
| INI-03 | J3 | `/inicio?screen=INI-03` | Dashboard de responsable y drill-down |
| PTY-01…PTY-13 | J1 | `/contactos` | Tabla, drawer, Contacto/Empresa 360, tabs, relaciones, estado, archivado, identidad técnica y solo lectura |
| PTY-14 | F2 | `/contactos` | Diferida/deshabilitada |
| PRP-01…PRP-13 | J2 | `/inmuebles` | Tabla/lista, mapa consultable, alta, Property 360, geo UNKNOWN, multimedia placeholder, historia y archivado |
| LST-01…LST-09 | J2 | `/publicaciones` | Tabla/cards, altas derivadas, detalle, mandato como advertencia, estado, contenido y rendimiento |
| LST-10 | F2 | `/publicaciones` | Diferida/deshabilitada |
| CAP-01…CAP-08 | J2 | `/captaciones` | Tablero, alta, Captación 360, tasación, brecha expectativa/valoración, mandato, cierre y reactivación |
| DEM-01…DEM-07 | J1 | `/busquedas` | Tabla, alta, editor de criterios, UNKNOWN, score recalculado, pausa/cierre y sugerencia |
| MAT-01…MAT-06 | J1 | `/compatibilidades` | Score explicado, evidencia, presentación, favoritos/descartes y compatibilidad invalidada |
| OPP-01…OPP-11 | J1 | `/oportunidades` | Board/lista, alta, detalle, `sourceType`, cambio de etapa, historial, cierre, reasignación y filtros |
| COM-01…COM-14 | J4 | `/oportunidades` | Visita ocurrida, feedback, negociación, propuesta, reserva con seña informativa, operación y cierre |
| ACT-01…ACT-06 | Transversal | `/actividad` | Registro, timeline por agregado, actividad reciente, corrección y offline queue |
| ANA-01…ANA-07 | J3 | `/metricas` | Panel agente/responsable/dirección, embudo, oferta-demanda, filtros, definición/lineage y recalculando; sin exportación |
| IA-01…IA-05 | J1 | `/asistente` | Sugerencia contextual, evidencia, revisión humana, descarte, historial e información faltante |
| ADM-01…ADM-13 | J4 | `/administracion` | Usuarios, invitación, roles/permisos efectivos, catálogos versionados y primer uso |
| GLB-01…GLB-12 | Transversal | `/inicio` y vistas consumidoras | Vacío, carga, error parcial/total, offline, permiso, solo lectura, no disponible, UNKNOWN, archivado y recalculando |
| AGD-01…AGD-03 | F2 | `/agenda` | Diferida/deshabilitada; no agenda en V2 |
| OMN-01…OMN-04 | F2 | `/actividad` | Diferida/deshabilitada; no omnicanalidad en V2 |
| SYN-01…SYN-02 | F2 | `/publicaciones` | Diferida/deshabilitada; no portales en V2 |
| DOC-01…DOC-04 | F2 | `/oportunidades` | Diferida/deshabilitada; no expediente documental en V2 |
| CMS-01…CMS-02 | F2 | `/administracion` | Diferida/deshabilitada; no políticas/cálculo de comisión en V2 |
| RNT-01…RNT-06 | F2 | `/administracion` | Diferida/deshabilitada; alquileres fuera de alcance |
| MTN-01…MTN-02 | F2 | `/administracion` | Diferida/deshabilitada; mantenimiento diferido |
| IDR-01…IDR-03 | F2 | `/contactos` | Diferida/deshabilitada; unificación futura |
| PAD-01…PAD-06 | F2 | — | No se implementan acá; pertenecen a `design_handoff_platform_admin` |

## Actores, contratos y write zones

| Journey | Actor principal | Lecturas | Mutaciones demo representadas | Write zone frontend |
|---|---|---|---|---|
| J1 Consulta → operación | Vendedor | Party, Requirement, Listing, Match, pipeline | Criterio, score, actividad, etapa | `apps/crm-web`; en productivo el BFF |
| J2 Captación → publicación | Vendedor/captador | Property, Captation, Valuation, Listing | Captación, valoración, publicación | `apps/crm-web`; sin consultar DB ajena |
| J3 Equipo y métricas | Responsable/dirección | Pipeline y read models analíticos | Reasignación representada por estado de UI; métricas consultivas | `apps/crm-web`; BFF para persistencia real |
| J4 Reserva → cierre/admin | Vendedor/administradora | Reservation, Operation, Users, Catalogs | Reserva, cierre, rol/catálogo demo | `apps/crm-web`; BFF para persistencia real |

La app no consulta DB directamente, no produce eventos de dominio y no modifica aggregates. `BffCrmDataSource` es la frontera HTTP prevista; `DemoCrmDataSource` y `DemoStoreProvider` solo existen para revisión local explícita.

## Estados transversales

- Persistentes: `Alert` para carga, vacío, error, permiso insuficiente, solo lectura, capacidad no disponible, sesión expirada, sin habilitación y conflicto.
- Transitorios: `Toast` para guardado, descarte, etapa cambiada, actividad encolada y confirmaciones.
- Datos: `UNKNOWN` se muestra como “Desconocido” y nunca como cero/falso; `CurrencyAmount` mantiene ARS/USD y formato argentino.
- Offline mobile: la actividad puede encolarse en demo; el estado queda visible como cola, no se simula sincronización productiva.
- Diferidas: F2 conserva contexto y aparece con etiqueta `Diferida`; no habilita agenda, tareas, notificaciones, portales, pagos, alquileres ni Platform Admin.

## Responsive y QA visual

El shell cambia a bottom bar bajo `860px`; los controles de campo mantienen targets de `44–52px`. Se revisaron desktop `1440×900` y mobile `390×844` en Browser/IAB para inicio, creación rápida, actividad y catálogo. La comparación directa con los HTML de referencia no pudo ejecutarse porque el navegador bloqueó abrir referencias `file://`; por eso la fidelidad visual contra esas capturas queda documentada como pendiente de verificación externa, no como un resultado afirmado.
