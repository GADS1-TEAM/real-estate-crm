# CRM con backend y datos compartidos

Los dos Workers publicados actualmente son vistas de revisión. Para usar el CRM con
datos compartidos hay que ejecutar el backend .NET, MongoDB, RabbitMQ y Keycloak en
una máquina que permanezca encendida. `docker-compose.hosted.yml` prepara esa
instalación en una VM Linux ARM64 o AMD64. Cloudflare Tunnel publica **un solo
origen HTTPS**; no se exponen los puertos de las bases de datos ni de los servicios.

Esta configuración cubre `crm-web` y `operations-bff`. El panel
`platform-admin-web` sigue siendo una vista de revisión: su BFF actual no implementa
`/overview`, `/drafts`, `/publish`, `/deprecate` ni `/corrections/execute`, que son
los contratos que consume la interfaz. Publicar ese panel como si tuviera datos
reales sería engañoso.

## Requisitos pendientes para publicar

1. VM Linux con Docker Engine y Compose, preferentemente 12 GB RAM para probar el
   stack completo. La cantidad necesaria debe verificarse con mediciones reales.
2. Dominio administrado en Cloudflare y un hostname estable, por ejemplo
   `crm.example.com`. El flujo OIDC usa ese hostname para los callbacks.
3. Tunnel de Cloudflare que publique ese hostname hacia `http://caddy:8000` si
   `cloudflared` corre en Compose, o `http://localhost:8000` si corre en el host.
4. Contraseñas iniciales distintas y fuertes para RabbitMQ, PostgreSQL, Keycloak
   admin y el primer usuario de CRM.

Oracle Cloud Always Free ofrece VM ARM, pero la disponibilidad depende de la región.
La cuenta y la VM deben crearse en el proveedor antes de ejecutar estos comandos.
No convertir una cuenta de prueba en una VM de pago por accidente.

## Preparar la VM

Clonar el repositorio y situarse en su raíz:

```bash
cp .env.hosted.example .env.hosted
# Editar .env.hosted: hostname y contraseñas reales; no usar "replace-me".
docker run --rm --env-file .env.hosted --user "$(id -u):$(id -g)" \
  -v "$PWD:/work" -w /work node:22-bookworm-slim \
  node scripts/render-hosted-realm.mjs
docker compose --env-file .env.hosted -f docker-compose.hosted.yml config --quiet
docker compose --env-file .env.hosted -f docker-compose.hosted.yml up -d --build
```

El script genera `infra/hosting/generated/realm.json`, ignorado por Git, con el
único usuario inicial `dev.administrador` y la contraseña de
`CRM_INITIAL_ADMIN_PASSWORD`. Cambia las URIs OIDC al hostname configurado y
deshabilita el password grant. Keycloak importa ese realm **sólo si aún no existe**.
Editar `.env.hosted` y reiniciar un contenedor no cambia usuarios ya persistidos;
para cambiar contraseñas existentes usar la consola de Keycloak.

Antes de habilitar el Tunnel, revisar el estado:

```bash
docker compose --env-file .env.hosted -f docker-compose.hosted.yml ps
docker compose --env-file .env.hosted -f docker-compose.hosted.yml logs --tail 100 operations-bff keycloak
curl -I http://localhost:8000/
```

En el panel de Cloudflare, crear un Tunnel y una ruta pública para el hostname.
Si `cloudflared` se ejecuta en este mismo Compose, configurar el servicio de la
ruta como `http://caddy:8000`, guardar el token en `.env.hosted` y ejecutar:

```bash
docker compose --env-file .env.hosted -f docker-compose.hosted.yml --profile tunnel up -d cloudflared
```

El backend usa la autoridad OIDC `https://<hostname>/auth/realms/crm-dev`. Sólo
después de que esa URL responda por HTTPS conviene probar el login. Comprobar
`/api/v1/auth/me` (401 sin sesión), iniciar sesión desde el CRM y verificar una
consulta y una mutación con dos navegadores. El BFF exige sesión para `/screens`
y `/mutations` fuera de Development; la UI ofrece un botón de ingreso ante 401.

MongoDB, RabbitMQ y PostgreSQL usan volúmenes de Compose. `docker compose down`
conserva datos; **`docker compose down -v` los borra**. Hacer backups de los tres
volúmenes antes de una migración o cambio de máquina. El archivo `.env.hosted` y
el realm generado contienen secretos y no deben publicarse ni copiarse a Git.

## Estado de esta preparación

La definición de Compose y los Dockerfiles se validan sin arrancar contenedores.
El despliegue real y las pruebas de extremo a extremo dependen de una VM y un
hostname provistos. Los Workers de demo publicados siguen independientes de este
stack hasta que haya un origen real y se haga la prueba de login y persistencia.
