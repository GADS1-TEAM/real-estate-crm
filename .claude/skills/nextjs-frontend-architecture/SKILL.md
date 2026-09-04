---
name: nextjs-frontend-architecture
description: |
  Activa cuando se escribe o modifica código de `crm-web` o `platform-admin-web`:
  estructura de la app Next.js, componentes, obtención de datos contra el BFF,
  estado, tipos, sesión y testing de frontend. Triggers: "Next.js", "React",
  "TypeScript", "App Router", "server component", "client component",
  "use client", "server action", "page.tsx", "layout.tsx", "route handler",
  "fetch", "data fetching", "SWR", "TanStack Query", "react-query", "estado",
  "useState", "useContext", "store", "revalidate", "cache", "loading state",
  "error boundary", "Suspense", "crm-web", "platform-admin-web", "componente",
  "props", "hook", "formulario", "react-hook-form", "zod", "tipos del contrato",
  "llamar a la API", "sesión en el front", "token en el front", "Testing Library",
  "Playwright", "test de componente".
  Garantiza que el front hable solo con su BFF, que ningún token llegue al
  navegador, que los tipos deriven del contrato y que el estado del servidor no se
  duplique a mano. NO activar para: código .NET, decisiones de dominio, ni patrones
  de UX de captura (eso es crm-ux-quick-capture).
---

# Arquitectura Frontend: Next.js y React

## Objetivo

Las dos apps web de este CRM no son un cliente REST genérico: **hablan solo con su
BFF**, que compone la experiencia. Esa restricción es lo que evita que el frontend
termine conociendo la topología de veinte microservicios.

Y hay una regla de seguridad que condiciona toda la arquitectura de sesión:
**ningún token llega al navegador**. El BFF retiene los tokens y el browser tiene
una cookie `HttpOnly` (ver [ADR-005](../../../docs/adr/005-keycloak-realm-unico-y-patron-bff.md)).

Fuentes: [`ARCHITECTURE.md`](../../../ARCHITECTURE.md) §4 y §14,
[`POC_TECH_DECISIONS.md`](../../../docs/implementation/POC_TECH_DECISIONS.md).

## Cuándo activar

- Se crea o modifica una página, layout o componente.
- Se obtienen datos del BFF.
- Se define estado, caché o revalidación.
- Se manejan sesión, login o logout desde el front.
- Se escriben tests de frontend.

## Cuándo NO activar

- Código backend o BFF en .NET.
- Reglas de dominio.
- Patrones de captura de datos y progressive disclosure (ver
  `crm-ux-quick-capture`).

## Decisiones del proyecto

| Tema | Decisión |
|---|---|
| Framework | **Next.js 16.x**, Active LTS (mínimo 16.3.3 por los security releases) |
| Router | **App Router**. Server Components por defecto |
| Lenguaje | TypeScript en modo estricto. Sin `any` |
| Apps | `crm-web` (operativo) y `platform-admin-web` (backoffice), separadas |
| Backend | **Solo su BFF.** El front nunca llama a un servicio de dominio ni a MongoDB |
| Sesión | Cookie `HttpOnly` emitida por el BFF. **Ningún token en el navegador** |
| Tipos | Derivados del contrato del BFF (OpenAPI), no escritos a mano |
| Estado de servidor | Caché de Next o una librería de data fetching. **Nunca duplicado en estado local** |
| Estado de cliente | Local y acotado. Sin store global salvo necesidad demostrada |
| Formularios | `react-hook-form` + `zod`, con el esquema compartido entre validación y tipos |
| Errores | El BFF responde `problem+json`; el front decide por `code`, no por texto |
| Tests | Testing Library para componentes; Playwright para journeys críticos |

## Estado actual vs target

- **Estado:** no hay frontend. `FND-005` crea los shells de ambas apps.
- **Target:** ambas apps comparten convenciones y componentes base, consumen solo su
  BFF y no manejan tokens.

## Reglas obligatorias

### MUST

- **MUST** llamar únicamente al BFF de la app. Nunca a un servicio de dominio.
- **MUST** enviar las peticiones con la cookie de sesión (`credentials: "include"`),
  sin headers de autorización armados en el cliente.
- **MUST** derivar los tipos de request y response del contrato del BFF.
- **MUST** usar Server Components por defecto y marcar `"use client"` solo donde
  hace falta interactividad.
- **MUST** manejar los tres estados de toda vista que carga datos: cargando, error y
  vacío. El vacío se distingue de "no cargado".
- **MUST** decidir el comportamiento ante un error usando el `code` de
  `problem+json`, nunca comparando el texto del mensaje.
- **MUST** mostrar el `correlationId` en los errores inesperados, para que el usuario
  pueda reportarlos.
- **MUST** mantener TypeScript en modo estricto y sin `any`.
- **MUST** tener tests de los journeys críticos: alta de Party, alta de Property,
  registrar interacción.

### MUST NOT

- **MUST NOT** guardar access, id o refresh tokens en `localStorage`,
  `sessionStorage`, una cookie legible ni memoria del cliente.
- **MUST NOT** llamar a un servicio de dominio ni a MongoDB desde el front.
- **MUST NOT** poner reglas de negocio en el frontend: la validación de UX no
  reemplaza la invariante del servidor.
- **MUST NOT** duplicar en estado local datos que ya vienen del servidor.
- **MUST NOT** usar `any` ni silenciar errores de tipos con `@ts-ignore`.
- **MUST NOT** asumir que un dato ausente es cero, falso o vacío: `UNKNOWN` se
  muestra como desconocido (ver `crm-ux-quick-capture`).
- **MUST NOT** exponer identificadores internos ni detalles de infraestructura en la
  UI.
- **MUST NOT** hacer que el front conozca qué microservicio resuelve cada dato.

## Recomendaciones

### SHOULD

- **SHOULD** organizar por **journey**, no por entidad: `app/(crm)/captaciones/…`
  antes que `app/party/list`.
- **SHOULD** mantener los componentes pequeños y sin lógica de datos: quien obtiene
  los datos es la página o un hook, no el componente de presentación.
- **SHOULD** usar `Suspense` y estados de carga a nivel de sección, no una pantalla
  en blanco entera.
- **SHOULD** compartir el esquema `zod` entre la validación del formulario y el tipo
  del payload.
- **SHOULD** revalidar tras una mutación en vez de mutar el caché a mano.
- **SHOULD** mantener un design system mínimo desde el principio: cambiar el estilo
  de veinte pantallas después es caro.
- **SHOULD** cubrir accesibilidad básica: labels asociados, foco visible, navegación
  por teclado, contraste.

### SHOULD NOT

- **SHOULD NOT** introducir un store global antes de tener un problema que lo
  justifique.
- **SHOULD NOT** marcar `"use client"` en un layout entero para habilitar un botón.

## Anti-patrones prohibidos

### 1. Token en el navegador

```tsx
// ❌ Un XSS y el atacante opera como el usuario hasta que expire.
localStorage.setItem("access_token", token);
fetch("/api/parties", { headers: { Authorization: `Bearer ${token}` } });
```

```tsx
// ✅ El BFF tiene la sesión; el browser manda la cookie.
await fetch("/bff/parties", { credentials: "include" });
```

### 2. Front hablando con servicios de dominio

```tsx
// ❌ El front pasa a conocer la topología interna y a componer él mismo.
const party = await fetch(`${PARTY_SERVICE}/parties/${id}`);
const props = await fetch(`${PROPERTY_SERVICE}/properties?partyId=${id}`);
```

```tsx
// ✅ El BFF compone la vista del journey.
const view = await fetch(`/bff/parties/${id}/overview`, { credentials: "include" });
```

### 3. Regla de negocio en el frontend

```tsx
// ❌ La invariante vive en el front: el servidor la desconoce y otro cliente la salta.
if (listing.status === "CLOSED") {
  return <p>No se puede activar.</p>;
}
```

```tsx
// ✅ El front muestra affordances; el servidor decide y responde con un code.
<Button disabled={!view.canActivate}>Activar</Button>
```

### 4. Estado del servidor duplicado

```tsx
// ❌ Dos fuentes de verdad que se desincronizan al primer refresh.
const { data } = useParties();
const [parties, setParties] = useState(data);
```

```tsx
// ✅ Una sola fuente; el estado local es solo para lo que es del cliente.
const { data: parties, isLoading, error } = useParties();
const [selectedId, setSelectedId] = useState<string | null>(null);
```

### 5. Decidir por el texto del error

```tsx
// ❌ Se rompe al cambiar una redacción o al traducir.
if (error.message.includes("ya existe")) { /* ... */ }
```

```tsx
// ✅ El `code` es contrato estable; `title` y `detail` no.
if (problem.code === "PARTY_ALREADY_EXISTS") {
  setFieldError("taxId", "Ya hay una Party con ese CUIT.");
}
```

### 6. Vacío indistinguible de desconocido

```tsx
// ❌ "0 m²" cuando en realidad nadie cargó la superficie.
<span>{property.surfaceM2 ?? 0} m²</span>
```

```tsx
// ✅ Lo desconocido se muestra como desconocido, y se ofrece completarlo.
{property.surface.state === "CONFIRMED"
  ? <span>{property.surface.value} m²</span>
  : <MissingData label="Superficie" onAdd={openSurfaceForm} />}
```

## Checklist antes de devolver código

- [ ] El front llama solo a su BFF.
- [ ] Ningún token en el navegador; peticiones con cookie de sesión.
- [ ] Tipos derivados del contrato, sin `any` ni `@ts-ignore`.
- [ ] Server Components por defecto; `"use client"` acotado.
- [ ] Estados de carga, error y vacío resueltos en toda vista con datos.
- [ ] Las decisiones ante error usan `code`, no texto.
- [ ] Ningún estado de servidor duplicado en estado local.
- [ ] Ninguna regla de negocio implementada solo en el front.
- [ ] Lo desconocido se muestra como desconocido.
- [ ] Labels, foco y navegación por teclado funcionan.
- [ ] Hay test del journey que la task toca.

## Conexiones con otros skills

| Skill | Relación |
|---|---|
| `crm-ux-quick-capture` | Cómo se piden los datos: captura mínima, progressive disclosure, UNKNOWN. |
| `aspnetcore-rest-layer` | Contrato del BFF y formato `problem+json` con `code`. |
| `oidc-keycloak-aspnetcore` | El BFF retiene los tokens; el front solo tiene cookie. |
| `multitenancy-authorization` | El front nunca envía `tenantId`: sale de la sesión. |
| `common-repo-documentation` | READMEs de cada app y convenciones compartidas. |
