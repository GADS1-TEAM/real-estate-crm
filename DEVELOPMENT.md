# Desarrollo — CRM Inmobiliario V2

Comandos locales para el esqueleto creado en `V2-FND-001`. No reemplaza a
`README.md` ni a `ARCHITECTURE.md`: es la referencia operativa mínima para
compilar y testear lo que existe hoy en el repo.

## Requisitos

- .NET 10 SDK (`dotnet --version` → 10.x)
- Node 22+ y npm (`node -v`)
- Docker Desktop (para infraestructura de `V2-FND-003`, todavía no agregada)

## Backend (.NET)

```bash
# Compilar toda la solución
dotnet build RealEstateCrm.slnx

# Correr los tests (por ahora, solo el test de arquitectura)
dotnet test RealEstateCrm.slnx
```

## Estructura de la solución

- `services/<nombre>-service/src/` — 11 servicios de dominio, cada uno con
  capas `Domain` → `Application` → `Infrastructure` → `Api` (hexagonal;
  `Domain` no depende de ningún otro proyecto ni paquete de infraestructura).
- `bffs/<nombre>-bff/src/` — `operations-bff` y `platform-admin-bff`, cada uno
  con `Application` + `Api`.
- `contracts/` — contratos compartidos entre servicios (vacío por ahora).
- `building-blocks/` — building blocks reutilizables (vacío por ahora).
- `tests/RealEstateCrm.ArchitectureTests/` — reglas de dependencia: falla si
  un proyecto `Domain` referencia MongoDB/RabbitMQ/Keycloak/ASP.NET Core, o si
  un servicio referencia proyectos de otro servicio.

Ningún proyecto de este esqueleto se conecta todavía a MongoDB, RabbitMQ ni
Keycloak: esas integraciones entran con `V2-FND-002` y `V2-FND-003`.

## Frontend (apps/)

`apps/crm-web` y `apps/platform-admin-web` ya existen (`V2-UX-001`). Ver
`apps/README.md` para el comando único que instala y construye ambas, o
`apps/crm-web/README.md` / `apps/platform-admin-web/README.md` para comandos
específicos de cada app.

```bash
bash apps/scripts/build-webs.sh
```

## Convenciones

- Target framework: `net10.0` (fijado en `Directory.Build.props`).
- Un servicio no referencia proyectos de otro servicio (verificado por el
  test de arquitectura).
- Sin `tenantId` ni `organizationId` en ningún proyecto de este esqueleto.
