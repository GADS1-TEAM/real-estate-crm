# Handoff: CRM Inmobiliario · alcance V2

El CRM que usan las inmobiliarias. Es el producto operativo: consultas entrantes, contactos, inmuebles, captaciones, búsquedas, compatibilidades, oportunidades, cierre, métricas y administración.

Repo: `GADS1-TEAM/real-estate-crm` · branch `main` · task de diseño **V2-UX-001**

> Existe un **segundo** paquete de handoff, `design_handoff_platform_admin`, para el backoffice interno de plataforma. Son dos aplicaciones distintas: este CRM lo usan las inmobiliarias; Platform Admin lo usa el equipo de producto para configurarlo. No los mezcles.

---

## 1. Antes de escribir código: leé esto

### Los archivos de `designs/` son **referencias de diseño**, no código de producción

Son prototipos en HTML que muestran el aspecto y el comportamiento buscados. **La tarea es recrear estos diseños en el entorno del codebase** (framework, librería de componentes y patrones que ya existan en el repo), no copiar el HTML. Si todavía no hay frontend definido, elegí el stack apropiado para el proyecto e implementá los diseños ahí.

Cada archivo es un **HTML autocontenido**: se abre con doble click en cualquier navegador, sin servidor y sin internet. Todo está embebido (el design system Brick, las fuentes, el runtime del prototipo). Abrilos para ver y clickear el diseño; leé su código fuente para extraer valores exactos.

Son grandes (~900 KB cada uno) justamente porque llevan Brick adentro. Ese peso es del prototipo, no del diseño: no lo tomes como referencia de nada.

### Fidelidad: **alta (hifi)**

Colores, tipografía, espaciado, estados e interacciones son finales. Los textos de la interfaz también: están en español de Argentina con voseo y se pueden usar tal cual.

### El alcance sale de ADR-001

- **Una sola instalación por inmobiliaria. Sin multi-tenancy.** No hay selector de organización, ni `tenantId` en la UI, ni nada que sugiera que la app sirve a varias inmobiliarias a la vez.
- **17 tasks en el board V2.** Todo lo que está fuera de esas tasks es **diferido**, y el diseño lo marca explícitamente en lugar de omitirlo.
- **Sin sistema de tareas ni agenda en el V2.** Esto tiene una consecuencia de diseño grande, ver más abajo.

De **172 pantallas inventariadas**, **137 son del alcance V2** y **35 quedan diferidas**. Las diferidas están en el inventario a propósito: sirven para no diseñar el V2 en una dirección que las haga imposibles después.

---

## 2. Las cinco decisiones de producto ya tomadas

Estas cinco preguntas estaban abiertas y **el cliente ya las respondió**. El diseño las implementa. No las vuelvas a abrir.

| # | Pregunta | Decisión | Consecuencia en el código |
|---|---|---|---|
| 1 | ¿Cómo le avisa el sistema al agente que algo se está enfriando, si no hay tareas ni agenda? | **El sistema deduce solo.** Nada que el usuario marque a mano | "Requiere atención" (`INI-02`) se calcula de **señales derivadas de los datos**: días sin actividad, propuesta por vencer, reserva por vencer. No hay entidad Tarea, ni recordatorios, ni fecha de próximo contacto |
| 2 | ¿Se puede publicar un inmueble sin la autorización firmada del dueño? | **Sí, solo advierte** | El mandato de comercialización **no bloquea** la publicación. Se muestra un aviso persistente, y publicar queda registrado |
| 3 | ¿La seña del comprador se anota, aunque el sistema no maneje plata? | **Sí, como dato informativo** | La reserva (`COM-09`) tiene monto de seña como campo. **No** genera asientos, ni cuenta corriente, ni cobranza |
| 4 | ¿En los reportes se pueden comparar sucursales? | **Solo vendedores y equipos** | Las métricas agrupan por vendedor y por equipo. **No** hay dimensión sucursal en el V2 |
| 5 | ¿Mantenimiento de propiedades entra? | **Diferido** | El módulo `MTN` está en el inventario como diferido, con la superficie mínima para que se pueda agregar sin rediseñar |

### Y una decisión de qué NO mostrar

Se descartó el **valor de pipeline ponderado por probabilidad** (sumar el precio de cada oportunidad multiplicado por su probabilidad de cierre). El cliente fue explícito: confunde y no significa nada accionable.

Lo que se muestra en su lugar: **la comisión estimada pendiente** — lo que la inmobiliaria va a cobrar si esas operaciones cierran, no el precio de los inmuebles. Si aparece un pedido de "valor del pipeline", ese es el número correcto.

---

## 3. Arquitectura de información

### Navegación híbrida

Sidebar de secciones + topbar con búsqueda global (`Ctrl+K`), creación rápida y menú de usuario. El contenido cambia sin recargar la página.

```
Inicio            Qué requiere atención · tu embudo
Contactos         Personas y empresas
Inmuebles         Fichas, valuaciones, captaciones
Publicaciones     Avisos y su estado
Búsquedas         Criterios de demanda
Oportunidades     Embudo comercial
Actividad         Timeline transversal
Métricas          Panel del equipo o de dirección, según rol
Agenda · pronto   Fuera del V2. Se muestra desactivada, a propósito
Administración    Usuarios, roles, catálogos, primer uso
```

**"Agenda · pronto" se muestra desactivada intencionalmente.** No es un bug: el equipo tiene que saber que viene, y que hoy no está. Mismo criterio para otras superficies diferidas.

### Los cinco roles

El prototipo trae un **selector de rol** en la topbar (tocá el nombre de usuario) para recorrer la app como cada uno. Cada rol arranca en una pantalla distinta:

| Persona | Rol | Arranca en |
|---|---|---|
| Martín Quiroga | Vendedor | Inicio del agente (`INI-01`) |
| Lucía Ferrari | Vendedora · captaciones | Inmuebles (`PRP-01`) |
| Rodrigo Vergara | Responsable comercial | Panel del equipo (`INI-03`) |
| Elena Vergara | Dirección | Panel de dirección (`ANA-05`) |
| Sofía Rendón | Administradora | Usuarios y roles (`ADM-01`) |

Los permisos se resuelven por **rol + scope**, y la UI **explica el permiso efectivo y por qué** (`ADM-01`, `SHL-05`): un rol con scope ampliado se señala como tal. Una acción sin permiso se ve deshabilitada con el motivo, no desaparece.

---

## 4. Los 25 módulos

| Código | Módulo | Alcance |
|---|---|---|
| `SHL` | Shell y navegación | V2 · 7 pantallas |
| `AUT` | Acceso (Keycloak) | V2 · 4 |
| `INI` | Inicio | V2 · 3 |
| `PTY` | Empresas y contactos | V2 · 14 |
| `PRP` | Inmuebles | V2 · 13 |
| `LST` | Publicaciones | V2 · 10 |
| `CAP` | Captaciones | V2 · 8 |
| `DEM` | Búsquedas | V2 · 7 |
| `MAT` | Compatibilidades | V2 · 6 |
| `OPP` | Oportunidades | V2 · 11 |
| `COM` | Progresión comercial | V2 · 14 |
| `ACT` | Actividades | V2 · 6 |
| `ANA` | Métricas | V2 · 7 |
| `IA` | Asistente | V2 |
| `ADM` | Administración | V2 · 13 |
| `GLB` | Estados globales | V2 · 12 |
| `AGD` | Agenda | **Diferido** · 3 |
| `OMN` | Omnicanalidad | **Diferido** · 4 |
| `SYN` | Portales | **Diferido** · 2 |
| `DOC` | Documentos | **Diferido** · 4 |
| `CMS` | Comisiones | **Diferido** · 2 |
| `RNT` | Alquileres | **Diferido** · 6 |
| `MTN` | Mantenimiento | **Diferido** · 2 |
| `IDR` | Duplicados | **Diferido** · 3 |
| `PAD` | Platform Admin | **Diferido acá** · 6 · (su propio handoff) |

El inventario completo — con propósito, patrón, usuario, entradas, acciones, estados, comportamiento mobile y task trazada por pantalla — está en `designs/01 - Product Map y Screen Inventory.html`, filtrable por módulo y por alcance.

---

## 5. Design tokens

**Todo sale del design system Brick, tema `eminent`.** No inventes valores: usá los tokens. Los prototipos referencian todo por `var(--*)`, con los tokens definidos en el mismo archivo.

| Token | Valor |
|---|---|
| Radios | Solo `4px` y `8px` (pill/círculo para chips y avatares). Nada más redondeado |
| Espaciado | Grilla de 4px: 4·8·12·16·24·32·40·48·96. Padding horizontal de layout: 24px |
| Elevación | `--shadow-1` en cards + borde 1px `--border-subtle`. Superficies flotantes suben de escalón |
| Motion | 150/170/200ms `ease-in-out` para cambios de estado · 350ms `ease-out` para entradas |
| z-index | dropdown 1 · topbar 2 · drawer 3 · modal 4 |
| Hit target mobile | Nunca menos de 44px. Los flujos de campo usan 44–52px |

### Tipografía

Inter (`interregular` 400 / `interSemiBold` 500 — Brick llama "bold" al 500). Escala GUI de Brick: `.brk-title-*`, `.brk-text-body`, `.brk-number-*` para montos.

### Plata

Formato argentino: `$` + miles con punto y decimales con coma (`$ 1.240.580,50`). USD como `US$`. **Siempre tabular y alineado a la derecha en listas.** El componente `CurrencyAmount` lo resuelve; usalo en todos lados en lugar de formatear a mano.

### Copy

Español de Argentina, **voseo**: *"Ingresá"*, *"Verificá los datos"*, *"Volvé a intentarlo"*. Nunca *usted*. Tono llano y tranquilizador, frases cortas, la plata explicada con claridad.

Los labels de botón en Brick se normalizan por CSS a minúscula con primera letra capitalizada — escribilos naturales y el componente los ajusta.

### Sin emoji

Los íconos cargan el significado: los 120 SVG de Brick (24×24, `#2B2B2B`).

---

## 6. Componentes de dominio

Además de los de Brick (`Button`, `Table`, `Card`, `Chip`, `Alert`, `Avatar`, `Modal`, `SideDrawer`, `Tabs`, `Timeline`, `Stepper`, `Select`, `TextInput`, `Switch`, `Checkbox`, `Slider`, `ProgressBar`, `Breadcrumbs`), el diseño define cinco componentes propios que Brick no cubre. Están especificados en `designs/02 - Design System.html`.

**`MatchScoreExplanation`** — el puntaje de compatibilidad **siempre explicado**, criterio por criterio: cuánto aportó cada uno y por qué. Un número solo no sirve; el agente tiene que poder defender la recomendación frente al cliente. Nunca muestres el score sin acceso a su desglose.

**`CriterionEditor`** — editor de criterios de búsqueda con peso de tres niveles: **Debe / Ojalá / Da igual**. El peso se cambia en vivo y las compatibilidades se recalculan a la vista, para que el agente entienda el efecto de cada criterio.

**`CurrencyAmount`** — montos en formato argentino, tabulares y alineados. Maneja ARS y USD.

**`PipelineCard`** — tarjeta de oportunidad en el embudo: etapa, contacto, inmueble, antigüedad y las señales derivadas que la ponen en "requiere atención".

**`AISuggestion`** — sugerencia del asistente, **siempre distinguible de un dato del sistema** y siempre descartable. Nunca se presenta como un hecho ni ejecuta sola.

---

## 7. Estados obligatorios en toda pantalla

El inventario documenta los estados de cada pantalla una por una. Los transversales:

| Estado | Tratamiento |
|---|---|
| Vacío declarado | Dice qué no hay y qué acción lo crea |
| Cargando | Skeleton de filas. Nunca spinner a pantalla completa |
| Error | Qué falló y qué hacer |
| Sin permiso | Qué permiso falta y quién lo tiene. La acción se ve deshabilitada, no desaparece |
| Solo lectura | Campos como texto |
| Guardando / guardado | La edición inline (`PTY-08`) muestra *"Guardando…"* → *"Guardado"* |
| **Conflicto de edición** | Dos personas editaron el mismo campo: se muestra el conflicto, no se pisa en silencio |
| Sesión expirada | `AUT-02` recupera la sesión **sin perder lo escrito**: avisa del borrador antes de redirigir |
| Sin habilitación | `AUT-03` explica por qué no puede entrar y a quién pedirle |

El módulo `GLB` (12 pantallas) son justamente los estados globales: vacíos, errores, offline, sin permiso.

---

## 8. Los cinco flujos diseñados

Todos navegables de punta a punta en los prototipos.

### Journey 1 · De la consulta a la operación cerrada (`designs/03`)

Consulta entrante → Contacto 360 → criterios de búsqueda → compatibilidades explicadas → propuesta → negociación → embudo del agente.

Pantallas: Inicio (`INI-01`/`INI-02`) · Contactos (`PTY-01`) · Contacto 360 (`PTY-05`) · Criterios (`DEM-03`/`DEM-04`) · Compatibilidades (`MAT-01`) · Oportunidad · Negociación.

Detalles que importan:
- El alta de contacto acepta **pegar texto de WhatsApp** para prellenar.
- En Criterios, cambiar el peso de un criterio **recalcula las compatibilidades en vivo**.
- En Compatibilidades, cada fila abre **el porqué del puntaje**. Proponer visita hace avanzar la oportunidad; descartar **pide motivo** (del catálogo versionado).

### Journey 2 · De la captación a la publicación (`designs/04`)

Alta de inmueble → valuación → mandato de comercialización → publicación del aviso.

Recordá la decisión 2: **el mandato advierte, no bloquea.**

### Journey 3 · Equipo y métricas (`designs/05`)

Panel del responsable comercial con alertas → drill-down a oportunidades reales → reasignación. Y el panel de dirección.

Detalles: el drill-down **llega a la oportunidad concreta**, no a un gráfico agregado. Las métricas agrupan por vendedor y equipo (decisión 4), y el pipeline se expresa en **comisión estimada pendiente**, no en valor ponderado.

### Cierre y administración (`designs/06`)

Reserva (`COM-09`, con seña como dato) → Operación (`COM-12`, expediente completo) → cierre. Más Usuarios y roles (`ADM-01`, con permisos efectivos explicados), Catálogos (`ADM-06`, etapas del embudo versionadas) y Primer uso (`AUT-04`).

### Mobile · trabajo de campo (`designs/07`)

Los tres flujos que se hacen fuera de la oficina: **visita in situ**, **registrar actividad** y **mi día**. Bottom bar bajo 768px (`SHL-06`). Hit targets de 44–52px, probados para uso con una mano en la calle.

Mobile **no** es la app completa reducida: son esos flujos. El resto de las pantallas se degrada a una columna y sigue siendo consultable.

---

## 9. Trazabilidad

Las 137 pantallas del V2 están trazadas a **62 casos de uso** y a las **17 tasks del board V2**. La matriz completa está en `designs/01 - Product Map y Screen Inventory.html` (columna Task por pantalla) y `designs/00 - Mapa de Navegacion.html` resume cómo se llega a cada superficie.

---

## 10. Archivos de este bundle

| Archivo | Contenido |
|---|---|
| `designs/00 - Mapa de Navegacion.html` | **Empezá acá.** Los 5 roles y dónde arranca cada uno, qué hace cada ítem del menú, y cómo se llega a cada pantalla |
| `designs/01 - Product Map y Screen Inventory.html` | Sitemaps, journeys, matriz de roles e inventario de las 172 pantallas, filtrable por módulo y alcance |
| `designs/02 - Design System.html` | Foundations de Brick + los 5 componentes de dominio con sus estados |
| `designs/03 - Journey 1 - Consulta a operacion.html` | Vendedor: de la consulta entrante a la operación |
| `designs/04 - Journey 2 - Captacion a publicacion.html` | Captación: inmueble, valuación, mandato, publicación |
| `designs/05 - Journey 3 - Equipo y metricas.html` | Responsable comercial y dirección |
| `designs/06 - Cierre y Administracion.html` | Reserva, operación, cierre, usuarios, catálogos, primer uso |
| `designs/07 - Mobile.html` | Visita in situ, registrar actividad, mi día |
| `designs/08 - Decisiones de Producto.html` | Las cinco decisiones explicadas en lenguaje no técnico, con sus alternativas |

**Cómo usarlos:** doble click y listo — cada uno es autocontenido y funciona offline. Tardan un segundo en aparecer la primera vez (están desempaquetando Brick). Los links entre archivos funcionan **si los nueve quedan en la misma carpeta**. Usá el selector de rol de la topbar para recorrer la app como cada persona.

Para valores exactos leé el código fuente: los estilos están inline y referencian tokens de Brick por `var(--*)`.

## 11. Assets

No hay imágenes ni ilustraciones propias. Los íconos son los 120 SVG de Brick (`assets/icons/`, 24×24, `#2B2B2B`). Donde el diseño muestra fotos de inmuebles hay **placeholders**: hacen falta imágenes reales del cliente. **Sin emoji** en ninguna superficie.
