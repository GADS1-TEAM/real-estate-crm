# ADR-005 — Un realm de Keycloak y tokens retenidos en el BFF

| Campo | Valor |
|---|---|
| Estado | `ACCEPTED` |
| Fecha | 2026-09-02 |
| Autor | Fabri |
| Task / PRP relacionado | `FND-005`, `FND-006` |
| Reemplaza a | — |
| Reemplazado por | — |

## Disparador

- [x] política global de tenancy/autorización
- [x] contrato público incompatible

## Contexto

`ARCHITECTURE.md` §10 dice que un realm de plataforma alcanza para la POC y que la
autorización de negocio vive en `access-service`, pero no define dónde viven los
tokens ni cómo llega el `tenantId`.

Las apps son Next.js con un BFF .NET por experiencia. Eso abre dos caminos: que el
SPA maneje los tokens, o que los retenga el BFF.

## Decisión

**Servidor:** Keycloak **26.7.x** Community, tag fijo, mínimo **26.7.2** por los CVE
corregidos ahí.

**Realm:** **uno solo** de plataforma. El tenant es un **claim**, no un realm. El
aislamiento real lo da `tenantId` en los datos, no la topología de Keycloak.

**Flujo:** Authorization Code + PKCE, **iniciado y terminado en el BFF**. El
navegador recibe una cookie de sesión `HttpOnly`, `Secure` y `SameSite` restrictivo.
**Ningún token llega al browser.**

**Servicios de dominio:** validan JWT bearer — emisor, audiencia, firma y vigencia.
Cada servicio declara su propia audiencia.

**Reparto:** Keycloak responde *quién sos*. `access-service` responde *qué podés
hacer*. En Keycloak no se modela el organigrama de la inmobiliaria.

**Realm como código:** JSON exportado, versionado e importado al arrancar. Cero
configuración manual como fuente.

## Alternativas consideradas

| Alternativa | Por qué no |
|---|---|
| Un realm por inmobiliaria | Aísla en Keycloak, pero multiplica la administración por cada tenant nuevo y no elimina la necesidad de filtrar por `tenantId` en los datos, que es donde el aislamiento realmente ocurre. |
| Tokens en el SPA (`localStorage`) | Simplifica el BFF, pero cualquier XSS roba el token y el atacante opera como el usuario hasta que expire. Con cookie `HttpOnly`, JavaScript no puede leerla. |
| Roles y permisos de negocio en Keycloak | Parece práctico al principio, pero los permisos de este dominio dependen de unidad organizacional, cartera y overrides vigentes: datos de negocio que viven en `access-service`, cambian solos y necesitan auditoría. |
| Access tokens de vida larga | Evita implementar refresh, pero vuelve imposible revocar de hecho: un usuario dado de baja sigue entrando hasta que el token expire. |

## Consecuencias

**Aceptamos:**

- un XSS en el frontend no expone credenciales de sesión reutilizables;
- alta de un tenant nuevo no requiere tocar Keycloak;
- la autorización de negocio es auditable y versionable como datos, no como
  configuración de un servidor de identidad;
- el entorno de desarrollo es reproducible: clonar y entrar.

**Perdemos:**

- el BFF pasa a tener responsabilidad de sesión y refresh;
- un realm compartido implica que un error de configuración afecta a todos los
  tenants a la vez.

**Deuda que queda abierta:**

- definir el nombre exacto del claim de tenant y el resto de los claims mínimos: es
  contenido de `FND-003`.

## Impacto en el repositorio

- Documentos actualizados: ninguno fuera de las skills.
- Skills afectadas: `oidc-keycloak-aspnetcore`, `multitenancy-authorization`.
- Tasks afectadas: `FND-004`, `FND-005`, `FND-006`.
- Contratos que cambian de versión: define el claim de tenant y las audiencias por
  servicio.
- Servicios que deben migrar datos: ninguno.

## Revisión

Se revisa si aparece un requisito de aislamiento de identidad por tenant que un
claim no pueda satisfacer, por ejemplo federación con el proveedor de identidad
propio de una inmobiliaria.
