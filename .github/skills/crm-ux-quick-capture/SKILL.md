---
name: crm-ux-quick-capture
description: |
  Activa cuando se diseña o implementa una pantalla, un formulario o un flujo de
  este CRM: qué se pide, cuándo, qué bloquea y cómo se muestra lo que falta.
  Triggers: "formulario", "form", "campo obligatorio", "campo requerido",
  "validación", "required", "qué datos pedir", "alta de", "crear party",
  "cargar propiedad", "wizard", "paso a paso", "quick capture", "captura rápida",
  "progressive disclosure", "mostrar más campos", "completitud",
  "CompletenessProfile", "dato faltante", "UNKNOWN", "dato desconocido",
  "sin dato", "N/D", "vacío", "flujo del usuario", "journey", "pantalla",
  "CRUD", "listado", "detalle", "acción primaria", "mensaje de error al usuario",
  "copy", "texto de la UI", "accesibilidad", "explicar la sugerencia de IA".
  Garantiza que el usuario pueda avanzar con datos mínimos, que nada bloquee salvo
  cuando una invariante o norma lo exige, que lo desconocido se muestre como
  desconocido y que la UI siga journeys y no el modelo de dominio. NO activar para:
  arquitectura de Next.js, código backend ni reglas de dominio.
---

# UX del CRM: captura mínima y progressive disclosure

## Objetivo

El modelo de dominio de este CRM es rico a propósito: soporta estructuras jurídicas
complejas, operación rural, múltiples monedas y compliance argentino. La trampa es
traducir esa riqueza en formularios que exigen todo antes de dejar guardar.

> **El modelo rico define lo que el sistema puede representar; no lo que el usuario
> está obligado a cargar.**

Un agente inmobiliario carga datos parado en la vereda, con una mano, entre dos
visitas. Si la pantalla le pide quince campos para registrar un contacto, no los
carga: lo anota en el teléfono y el CRM queda vacío.

Fuentes: [`ARCHITECTURE.md`](../../../ARCHITECTURE.md) §3 y §14,
[`README.md`](../../../README.md) §1.1.

## Cuándo activar

- Se diseña o implementa un formulario o una pantalla de alta.
- Se decide qué campo es obligatorio.
- Se muestra un dato que puede faltar.
- Se define el flujo de un journey.
- Se escribe copy de la interfaz o mensajes de error.
- Se presenta una sugerencia automática o de IA.

## Cuándo NO activar

- Arquitectura de la app (ver `nextjs-frontend-architecture`).
- Reglas de dominio e invariantes (ver `ddd-hexagonal-architecture`).
- Código backend.

## Decisiones del proyecto

| Tema | Decisión |
|---|---|
| Orientación | La UI se diseña por **journeys y tareas**, no como CRUD del modelo |
| Captura | **Quick capture primero.** El mínimo operativo permite avanzar |
| Revelado | **Progressive disclosure**: lo avanzado aparece cuando hace falta |
| Bloqueo | Solo cuando una invariante, integración, política o norma **realmente** lo exige |
| Completitud | Es una **proyección que sugiere**, nunca una barrera |
| Desconocido | `UNKNOWN` ≠ `false` ≠ `0` ≠ lista vacía confirmada. Se muestra distinto |
| Enriquecimiento | IA y conectores intentan completar **antes** de pedirle al usuario |
| Defaults | Argentinos y precargados por pack: no se configuran de cero |
| Autosave | Donde sea seguro. Sin comandos sensibles offline |
| Sugerencias | Toda sugerencia automática **debe poder explicarse** |
| Errores | Dicen qué acción concreta resuelve el problema |

### Los tres niveles de dato

1. **Mínimo operativo** — permite avanzar con el caso de uso.
2. **Enriquecimiento recomendado** — mejora matching, analytics y automatización.
3. **Requerido por acción concreta** — bloquea *solo* esa acción, y recién cuando se
   intenta.

Confundir el nivel 2 con el 1 es el error más caro de este producto.

## Estado actual vs target

- **Estado:** no hay frontend. `FND-005` crea los shells; los formularios reales
  llegan con la Ola 1.
- **Target:** toda alta se puede completar con el mínimo operativo, y la pantalla
  muestra qué falta y qué habilita completarlo.

## Reglas obligatorias

### MUST

- **MUST** permitir crear con el **mínimo operativo** que el dominio define, sin
  exigir más.
- **MUST** hacer obligatorio en la UI solo lo que el dominio define obligatorio.
- **MUST** bloquear una acción concreta, y no la captura entera, cuando falta un dato
  que esa acción requiere.
- **MUST** explicar, al bloquear, **qué falta y qué acción lo resuelve**.
- **MUST** distinguir visualmente **desconocido** de **confirmado como ausente** y de
  **cero**.
- **MUST** mostrar la completitud como sugerencia, indicando **qué habilita**
  completar cada cosa.
- **MUST** usar el lenguaje del negocio: `captación`, `tasación`, `mandato`,
  `reserva`. Nunca los nombres técnicos del modelo.
- **MUST** poder explicar toda sugerencia automática: en qué se basó.
- **MUST** asociar cada campo con su label y permitir operar por teclado.

### MUST NOT

- **MUST NOT** convertir un dato opcional del dominio en obligatorio de la UI.
- **MUST NOT** bloquear el guardado por un dato de enriquecimiento.
- **MUST NOT** mostrar `0`, `No` o `—` para un dato que nadie cargó.
- **MUST NOT** pedir dos veces el mismo dato si puede derivarse o reutilizarse.
- **MUST NOT** diseñar la pantalla como espejo del aggregate: eso produce CRUD, no
  un journey.
- **MUST NOT** presentar un porcentaje de completitud como reproche.
- **MUST NOT** mostrar una sugerencia de IA sin decir de dónde sale.
- **MUST NOT** hacer que un formulario largo pierda lo cargado ante un error.
- **MUST NOT** exigir configuración inicial de cosas que el pack argentino ya trae.

## Recomendaciones

### SHOULD

- **SHOULD** empezar toda alta con una pantalla de una sola columna y pocos campos.
- **SHOULD** agrupar lo avanzado en secciones colapsadas, cerradas por defecto.
- **SHOULD** autosalvar borradores donde no haya efecto de negocio.
- **SHOULD** mostrar el beneficio de completar (*"con la superficie mejoran los
  matches"*), no la penalización de no hacerlo.
- **SHOULD** ofrecer la acción siguiente del journey al terminar una tarea, en vez de
  devolver a un listado.
- **SHOULD** dejar que el usuario corrija un dato inferido o sugerido, y registrar
  que lo corrigió.
- **SHOULD** validar en el borde con el mismo esquema que usa el servidor, y aun así
  confiar en la respuesta del servidor como verdad.

### SHOULD NOT

- **SHOULD NOT** usar wizards de más de tres pasos para una captura inicial.
- **SHOULD NOT** mostrar todos los tipos de propiedad y atributos rurales a una
  inmobiliaria urbana.

## Anti-patrones prohibidos

### 1. Formulario que exige todo

```tsx
// ❌ Quince campos obligatorios para registrar a alguien que llamó por teléfono.
// El agente no lo carga y el CRM queda vacío.
<Field name="fullName" required />
<Field name="taxId" required />
<Field name="birthDate" required />
<Field name="address" required />
<Field name="maritalStatus" required />
```

```tsx
// ✅ Mínimo operativo: nombre y un contacto. El resto, después.
<Field name="displayName" required label="Nombre" />
<Field name="contact" required label="Teléfono o email" />

<Collapsible title="Más datos" defaultOpen={false}>
  <Field name="taxId" label="CUIT / CUIL" />
  <Field name="address" label="Domicilio" />
</Collapsible>
```

### 2. Bloquear la captura por un dato de una acción futura

```tsx
// ❌ No deja guardar la propiedad porque falta la escritura,
//    que recién hace falta al cerrar una operación.
if (!property.deedNumber) return <Error>Falta el número de escritura.</Error>;
```

```tsx
// ✅ Se guarda igual. El dato se exige en el momento en que se necesita.
<Button disabled={!canClose} onClick={close}>Cerrar operación</Button>
{!canClose && (
  <Hint>
    Para cerrar falta el número de escritura.{" "}
    <a onClick={openDeedForm}>Cargarlo ahora</a>
  </Hint>
)}
```

### 3. Desconocido mostrado como valor

```tsx
// ❌ El gerente ve "Acepta mascotas: No" cuando nadie preguntó.
<Row label="Acepta mascotas" value={req.petsAllowed ? "Sí" : "No"} />
```

```tsx
// ✅ Tres estados distinguibles.
<Row
  label="Acepta mascotas"
  value={
    req.petsAllowed.state === "UNKNOWN"
      ? <Unknown onAsk={() => askClient("petsAllowed")} />
      : req.petsAllowed.value ? "Sí" : "No"
  }
/>
```

### 4. Pantalla que espeja el modelo

```tsx
// ❌ CRUD del aggregate: el usuario tiene que entender el dominio para usarlo.
<Tabs>
  <Tab title="Party" />
  <Tab title="ContactPoints" />
  <Tab title="PartyRelationships" />
  <Tab title="IdentityAttributes" />
</Tabs>
```

```tsx
// ✅ Organizado por lo que el agente viene a hacer.
<Tabs>
  <Tab title="Resumen" />
  <Tab title="Qué busca" />
  <Tab title="Conversaciones" />
  <Tab title="Visitas y ofertas" />
</Tabs>
```

### 5. Completitud como reproche

```tsx
// ❌ Culpa al usuario sin decirle para qué sirve completar.
<Badge variant="danger">Perfil incompleto: 40%</Badge>
```

```tsx
// ✅ Muestra el beneficio concreto de cada dato faltante.
<CompletenessHint>
  Agregando <b>zona preferida</b> y <b>presupuesto</b> podemos sugerirte
  propiedades para este cliente.
</CompletenessHint>
```

### 6. Sugerencia sin explicación

```tsx
// ❌ El agente no sabe si confiar.
<Suggestion>Este cliente podría estar interesado en PROP-4821.</Suggestion>
```

```tsx
// ✅ Se explica en qué se basó y se puede descartar con motivo.
<Suggestion
  reason="Coincide en zona (Palermo), presupuesto (USD 180k–210k) y ambientes (3)."
  confidence={0.82}
  onDismiss={(motivo) => registrarDescarte(motivo)}
/>
```

### 7. Error que no dice qué hacer

```tsx
// ❌ El usuario queda trabado.
<Error>No se pudo crear la reserva.</Error>
```

```tsx
// ✅ Dice qué falta y ofrece resolverlo.
<Error>
  Para crear la reserva falta el documento de identidad del comprador.{" "}
  <a onClick={openDocumentUpload}>Subirlo ahora</a>
</Error>
```

## Checklist antes de devolver código

- [ ] El alta se puede completar con el mínimo operativo del dominio.
- [ ] Ningún campo opcional del dominio es obligatorio en la UI.
- [ ] Lo avanzado está colapsado y cerrado por defecto.
- [ ] Los bloqueos son por acción concreta y explican qué los resuelve.
- [ ] Desconocido, ausente confirmado y cero se ven distinto.
- [ ] La completitud sugiere y dice qué habilita; no reprocha.
- [ ] La pantalla está organizada por journey, no por aggregate.
- [ ] El copy usa lenguaje del negocio.
- [ ] Toda sugerencia automática se explica y se puede descartar.
- [ ] Ningún dato se pide dos veces.
- [ ] Labels asociados, foco visible y navegación por teclado.
- [ ] Un error no hace perder lo cargado.

## Conexiones con otros skills

| Skill | Relación |
|---|---|
| `nextjs-frontend-architecture` | Cómo se implementa: componentes, formularios, estados de carga. |
| `ddd-hexagonal-architecture` | Qué es realmente obligatorio lo define el dominio, no la pantalla. |
| `cqrs-read-models-projections` | La completitud y las métricas de la vista salen de proyecciones. |
| `aspnetcore-rest-layer` | El `code` del error determina qué mensaje accionable mostrar. |
| `multitenancy-authorization` | La UI muestra solo lo que el scope del actor permite. |
