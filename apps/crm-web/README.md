# CRM Web

Interfaz del CRM inmobiliario con Next.js, React, TypeScript y un design system interno compatible con las decisiones visuales Brick/eminent.

## Ejecutar la presentación

Desde `apps/crm-web`:

```powershell
npm.cmd install
npm.cmd run dev:preview
```

Abrí `http://localhost:3000/inicio`. El entorno de presentación usa una fuente aislada de revisión y conserva los cambios en `localStorage` para sobrevivir recargas.

## Ejecutar conectado a datos reales

```powershell
$env:NEXT_PUBLIC_CRM_BFF_URL = "http://localhost:xxxx/api"
npm.cmd run dev
```

Si el servicio de datos no está configurado o responde con error, la interfaz muestra un estado de indisponibilidad y espera la conexión real.

## Rutas

- `/inicio`
- `/contactos`
- `/inmuebles`
- `/publicaciones`
- `/captaciones`
- `/busquedas`
- `/compatibilidades`
- `/oportunidades`
- `/actividad`
- `/metricas`
- `/asistente`
- `/administracion`
- `/login`
- `/__design` — catálogo interno; no forma parte del shell productivo

Las superficies del inventario se seleccionan con `?screen=ID`, por ejemplo `/oportunidades?screen=OPP-05`. La ruta `/__design` usa un rewrite interno a `/design` para conservar la URL definida por el handoff.

## Verificación

```powershell
npm.cmd run lint
npm.cmd run typecheck
npm.cmd test
npm.cmd run test:e2e
npm.cmd run build
```

En la verificación de esta revisión, `typecheck`, `test` (10 archivos, 87
tests), `lint` y `build:demo` pasaron. La suite Playwright ejecutó 9
escenarios correctos y 3 omitidos por estar restringidos al proyecto de
viewport correspondiente. El journey principal de escritorio valida ahora el
flujo documentado de cambio de etapa con motivo obligatorio y el registro de
una contrapropuesta como secuencia inmutable.

En el checkout dentro de OneDrive, Vitest puede heredar una restricción de resolución de rutas largas de esbuild. La verificación reproducible se puede ejecutar desde una letra de unidad temporal:

```powershell
subst Z: "<ruta absoluta a apps\\crm-web>"
Set-Location Z:\
npm.cmd test -- --run
Set-Location "<ruta absoluta al repositorio>"
subst Z: /D
```

## Límites deliberados

- `design_handoff_platform_admin` no se modifica ni se incorpora al shell del CRM.
- No se agregan `tenantId`, selector de organización, agenda, tareas, recordatorios, notificaciones, integraciones, pagos, alquileres ni entidad física `Opportunity`.
- Email y WhatsApp son actividades históricas manuales; no hay botones de envío.
- Las imágenes inmobiliarias son placeholders explícitos porque el handoff no contiene assets reales.

## Estructura

- `src/lib/screen-registry.ts`: inventario ejecutable de las 172 superficies.
- `src/lib/demo-store.ts`: reducer, transiciones, cola offline y persistencia aislada de presentación.
- `src/lib/data-source.ts`: frontera de datos reales y presentación sin fallback implícito.
- `src/components/ui/primitives.tsx`: primitives y estados visuales.
- `src/components/domain/domain-components.tsx`: componentes de dominio del handoff.
- `src/components/crm-workspace.tsx`: composición por módulos y journeys.
- `docs/SCREEN_TRACEABILITY.md`: matriz compacta pantalla → journey → ruta → estado.
