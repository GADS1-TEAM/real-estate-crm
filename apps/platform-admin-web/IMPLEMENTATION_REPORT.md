# Implementation report — Platform Admin Web

## Alcance realizado

Se creó `apps/platform-admin-web` como aplicación independiente Next.js + React + TypeScript. El frontend resuelve las 72 superficies `PA-xxx` del handoff mediante un `screenRegistry` tipado y renderers reutilizables; no se modificaron `crm-web` ni `design_handoff_platform_admin`.

La entrega incluye shell de escritorio, navegación canónica, command palette, ambiente visible, permisos efectivos, selector de persona solo para revisión local, VersionHeader, validación persistente, estados de acceso, listas, detalle, versionado, diffs, editores, simuladores, pilotos, ambientes, inspector de configuración efectiva, auditoría y correcciones.

## Journeys cubiertos

- J1: referencia de pack a workflow draft, corrección y bloqueo de publicación.
- J2: splits con suma inválida, corrección al 100 % y simulación.
- J3: dependencia hacia etapa posterior y corrección en el editor de workflow.
- J4: solapamientos y huecos en la matriz documental.
- J5: `AUTO_EXECUTE_WITH_GUARDRAILS` sin límite de volumen.
- J6: impacto de deprecar una capability utilizada.
- J7: piloto con dependencia faltante.
- J8: configuración efectiva en modo solo lectura, precedencia y motivo.
- J9: promoción bloqueada por dependencia o permiso.
- J10: corrección administrativa con permiso, motivo y auditoría.

Los comportamientos inferidos del handoff se implementan como supuestos de frontend; no redefinen ownership ni agregan comandos de dominio.

## Fidelidad visual

| Punto de comparación | Implementación |
| --- | --- |
| Sidebar | 236 px, grupos de navegación, marca y ambiente activo |
| Topbar | 44 px, búsqueda global, ambiente y usuario/permisos |
| Superficie de trabajo | Fondo gris, cards blancas, bordes suaves, densidad de backoffice |
| Sistema visual | Inter, eminent `#003973`, dark `#002041`, brand `#fa6400`, radios 4/8 px |
| Gobierno | VersionHeader, severidades textuales, validation drawer y acciones restringidas visibles |
| Responsive | Ancho mínimo de escritorio y aviso explícito debajo de 1024 px con scroll horizontal |

La referencia aprobada fueron los HTML del handoff. No se copiaron runtime ni markup del handoff. El entorno Browser/IAB puede bloquear `file://`; cuando ocurre, la comparación directa queda marcada como no verificada.

## Evidencia de verificación

Evidencia actual del paquete:

- `npm.cmd run lint`: correcto.
- `npm.cmd run typecheck`: correcto.
- `npm.cmd test`: 30 tests correctos en 8 archivos.
- `npm.cmd run build:review`: correcto con la fuente local explícita.
- La suite E2E de Chromium ejecutó sus 6 escenarios y todos reportaron `ok`, cubriendo identidad, navegación, J1–J10, permisos y responsive. El runner quedó retenido en el teardown de procesos hijos de Next sobre Windows y requirió limpieza controlada; por eso no se reporta como un exit 0 limpio.

La build productiva conserva el comportamiento fail-closed: sin BFF configurado muestra conexión pendiente y no usa datos locales. La auditoría local conserva actor, motivo, `before`, `after` y `correlationId`; las acciones con cambios enlazan el toast a su registro. `IN_REVIEW` se muestra como estado explícito del borrador cuando está disponible en el contrato de revisión.

## Límites

No se implementaron BFF, backend, OIDC/Keycloak, almacenamiento de dominio, migraciones, overrides editables ni integración real con servicios externos. El código deja contratos y estados preparados para esa integración y evita el fallback silencioso.
