# Desarrollo — CRM Inmobiliario V2

Comandos locales para el esqueleto creado en `V2-FND-001`. No reemplaza a
`README.md` ni a `ARCHITECTURE.md`: es la referencia operativa mínima para
compilar y testear lo que existe hoy en el repo. Este archivo es una guía para levantar y apagar el software de manera local, de forma que esa PC levante el backend y el frontend en Docker junto con los microservicios utilizados para que cualquier persona que tenga el enlace pueda acceder al CRM Inmobiliario vía internet. 

## Requisitos

- Ya haber cumplido al menos una vez con los pasos mencionados en `DEVELOPMENT.md`

## Infraestructura local (V2-FND-003)

```bash
docker compose up -d      # Mongo (replica set rs0), RabbitMQ, Keycloak (realm crm-dev)
```

## Carga Masiva de Datos de Prueba (Seeding)

Para poblar la base de datos MongoDB con el conjunto completo de datos de prueba (empresas, contactos, inmuebles, publicaciones, captaciones, búsquedas/demandas, oportunidades del embudo, reservas y actividades) a través del Operations BFF (`http://localhost:5137`):

1. Iniciar los servicios con `bash scripts/run-slice.sh` (o `scripts/run-slice.ps1` en Windows).
2. En otra terminal, ejecutar:
   ```bash
   node scripts/seed-data.js
   ```

Tener en cuenta que el Compose no tiene volumenes persistentes, esto se repite cada vez que se recrea el
contenedor de Keycloak.

## Demo remota por tunel (ngrok u otro)

Para que alguien fuera de la red vea la app completa (front + BFF + servicios), se expone
**un solo puerto**: un proxy local (Caddy) sirve front, BFF y Keycloak bajo el mismo origen.
Asi no hay CORS, ni cookies cross-site, y el login OIDC funciona.

```powershell
# 0) dominio publico, sin https:// ni barra final
$env:DEMO_PUBLIC_DOMAIN="tu-dominio.ngrok-free.dev"

# 1) infra con el overlay de demo (Keycloak pasa a colgar de /auth)
docker compose -f docker-compose.yml -f docker-compose.demo.yml up -d

# 2) backend (servicios + BFF) apuntando al dominio publico
.\scripts\start-remote-demo.ps1

# 3) front (otra terminal)
Copy-Item apps\crm-web\.env.demo.example apps\crm-web\.env.local
cd apps\crm-web ; npm install ; npm run build ; npm run start

# 4) proxy (otra terminal, desde la raiz)
caddy run --config infra\demo\Caddyfile

# 5) tunel (otra terminal)
ngrok http --domain=$env:DEMO_PUBLIC_DOMAIN 8000
```

# Para dar de baja todo seguir los siguientes pasos:

# 01. Primer paso:
```bash
docker compose down -v    # bajar todo (no hay volúmenes persistentes: es infra POC efímera)
```
# 02. Segundo paso:
Cerrar todas las terminales `.NET` (total de 12 terminales).

# 03. Tercer paso:
Presionar "Ctrl + C" en la terminal de ngrok.

# 04. Cuarto paso:
Cerrar todas las terminales de Visual Studio Code.