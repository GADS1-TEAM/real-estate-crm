# Platform Admin Web

Frontend independiente de Platform Admin, construido con Next.js, React y TypeScript. La interfaz administra artefactos versionados, reglas operativas, releases, pilotos, ambientes, auditoría y correcciones sin acceder directamente al dominio ni a MongoDB.

## Ejecución

Desde `apps/platform-admin-web`:

```powershell
npm.cmd install
npm.cmd run dev:review
```

La revisión local inicia en `http://localhost:3000` con datos aislados en `localStorage`. Es una fuente explícita para revisión funcional; la aplicación no la usa como fallback cuando se ejecuta conectada al BFF.

Para ejecutar la experiencia productiva:

```powershell
$env:NEXT_PUBLIC_PLATFORM_ADMIN_BFF_URL = "http://localhost:xxxx/api/platform-admin"
npm.cmd run dev
```

Sin `NEXT_PUBLIC_PLATFORM_ADMIN_BFF_URL`, el shell muestra una conexión pendiente y no presenta datos locales. El BFF determina la sesión, permisos efectivos, ambiente y errores públicos.

## Variables

- `NEXT_PUBLIC_PLATFORM_ADMIN_MODE=bff|review`: el valor productivo es `bff`; `review` solo se habilita de forma explícita.
- `NEXT_PUBLIC_PLATFORM_ADMIN_BFF_URL`: URL base del `platform-admin-bff`.
- `NEXT_PUBLIC_PLATFORM_ADMIN_ENVIRONMENT`: ambiente inicial enviado como contexto de consulta; el BFF sigue siendo la autoridad en producción.

## Rutas canónicas

El registro tipado resuelve las 72 superficies a rutas de producto reutilizando renderers por módulo. La matriz completa está en [`SCREEN_MATRIX.md`](./SCREEN_MATRIX.md):

- `/`, `/centro-de-atencion`, `/historial-producto`, `/buscar`.
- `/packs`, `/capabilities`, `/catalogos-base`.
- `/workflows`, `/requisitos`, `/compliance`, `/comisiones`, `/automatizaciones`, `/metricas`.
- `/conectores`, `/feature-flags`, `/extension-schemas`.
- `/release/impacto`, `/release/pilotos`, `/release/ambientes`.
- `/diagnostico/inmobiliarias`, `/auditoria`, `/correcciones`, `/validacion`.
- `/login`, `/session-expirada`, `/sin-habilitacion`.

El parámetro `?screen=PA-xxx` queda reservado para QA y permite abrir cualquier superficie del inventario sin agregar páginas duplicadas.

## Verificación

```powershell
npm.cmd run lint
npm.cmd run typecheck
npm.cmd test
npm.cmd run test:e2e
npm.cmd run build:review
```

La verificación local de esta revisión ejecutó lint, typecheck, 30 tests unitarios en 8 archivos y `build:review`. La suite Playwright de Chromium ejecutó sus 6 escenarios y todos reportaron `ok`, cubriendo identidad, navegación, J1–J10, permisos y responsive. El runner quedó retenido en el teardown de procesos hijos de Next sobre Windows y requirió limpieza controlada; por eso no se reporta como un exit 0 limpio.

## Arquitectura frontend

- `src/lib/screen-registry.ts`: inventario completo y resolución pantalla → ruta → renderer.
- `src/lib/types.ts`: contratos públicos de artefactos, validación, impacto, releases y sesión.
- `src/lib/data-source.ts`: frontera `PlatformAdminDataSource`, BFF sin fallback y fuente local explícita.
- `src/lib/review-store.ts`: reducer, transiciones de draft/revisión/publicación/deprecación, persistencia aislada y auditoría local con `before`/`after`.
- `src/lib/validation.ts`: validaciones explicables de J1–J10.
- `src/components/platform-shell.tsx`: shell de 236 px, topbar de 44 px, búsqueda, ambiente y permisos efectivos.
- `src/components/platform-workspace.tsx`: renderers de las superficies y componentes de dominio.
- `src/components/ui/primitives.tsx`: tokens visuales y primitives accesibles.

En el modo de revisión local, el menú de usuario permite seleccionar una persona de revisión y el shell muestra que ese selector no reemplaza OIDC ni los permisos del BFF. Las publicaciones, deprecaciones y correcciones enlazan su toast con la entrada de auditoría correspondiente.

## Límites de esta entrega

No se implementan backend, `platform-admin-bff`, Keycloak/OIDC real, MongoDB, overrides de tenant editables, migración histórica ni cambios en `crm-web` o en `design_handoff_platform_admin`. Las acciones de integración productiva quedan detrás del contrato BFF.

La comparación visual se realizó sobre la implementación navegable. El Browser/IAB del entorno puede bloquear referencias `file://`; si esa limitación reaparece, la comparación directa contra los HTML se considera no verificada y no se afirma fidelidad completa.
