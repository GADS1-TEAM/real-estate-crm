# Publicación de las interfaces en Cloudflare Workers

Este repositorio contiene dos sitios Next.js independientes. Cloudflare Workers
publica sus **exports estáticos** por separado mediante Static Assets:

| Aplicación | Worker | Modo compilado | Salida |
| --- | --- | --- | --- |
| `apps/crm-web` | `real-estate-crm-demo` | `NEXT_PUBLIC_CRM_WEB_MODE=demo` | `out/` |
| `apps/platform-admin-web` | `real-estate-platform-admin-demo` | `NEXT_PUBLIC_PLATFORM_ADMIN_MODE=review` | `out/` |

Sitios publicados:

- CRM: <https://real-estate-crm-demo.gastonlrossy-casa-salta.workers.dev>
- Platform Admin: <https://real-estate-platform-admin-demo.gastonlrossy-casa-salta.workers.dev>

Estos despliegues son demostraciones navegables. El estado de prueba se guarda en
el `localStorage` de cada navegador y **no se comparte entre usuarios**. No hay
persistencia central, autenticación real ni llamadas a los BFF. Los servicios .NET,
MongoDB, RabbitMQ y Keycloak de `docker-compose.yml` no se publican con estos
comandos. Para publicar el CRM conectado a datos reales hace falta desplegar esos
servicios y configurar sus URL, sesiones y políticas de origen por separado.

## Primera publicación

Desde la raíz del repositorio, con Node.js y una sesión de Cloudflare autorizada:

```powershell
cd apps/crm-web
npm ci
npx wrangler login
npm run build:cloudflare
npm run cf:deploy

cd ../platform-admin-web
npm ci
npm run build:cloudflare
npm run cf:deploy
```

En publicaciones posteriores se ejecutan `npm run build:cloudflare` y
`npm run cf:deploy` dentro de cada aplicación. Estos Workers se publican desde la
CLI: un push a GitHub no reconstruye los sitios automáticamente. `npm run cf:dev`
permite probar el `out/` localmente antes de publicar.

Los nombres, la ruta de salida y la fecha de compatibilidad están fijados en
`apps/*/wrangler.jsonc`. Ningún secreto se almacena allí. El modo demo/revisión se
inyecta al compilar y los archivos `out/` no deben commitearse.

## Adaptaciones de rutas

- `crm-web` genera todas las secciones del producto mediante `generateStaticParams`.
  Workers Static Assets redirige `/` a `/inicio` y `/__design` a `/design` con
  `public/_redirects`; el rewrite de Next.js sigue disponible en modo servidor.
- `platform-admin-web` genera las rutas del inventario mediante
  `generateStaticParams`. El parámetro opcional `?screen=PA-xxx` se aplica en el
  navegador después de cargar la ruta, por lo que continúa funcionando en el
  export estático.
- `build` y los modos de servidor existentes permanecen disponibles. El export
  estático se activa únicamente con `build:cloudflare`.

## Verificación

En cada aplicación:

```powershell
npm run lint
npm run typecheck
npm test
npm run test:e2e
npm run build:cloudflare
```

Tras publicar, comprobar `/`, una ruta de producto profunda y un parámetro
`?screen=` en cada dominio `workers.dev`, además de que los assets `/_next/static/*`
respondan correctamente.
