---
name: common-repo-documentation
description: |
  Activa cuando se pide generar, auditar o actualizar documentación estructural del
  repositorio destino: READMEs por carpeta, documentación de módulos, estrategia de
  testing. Triggers: "generá README", "falta documentación", "auditá docs", "README
  desactualizado", "documentá el módulo", "documentación por carpeta", "docs faltantes",
  "README de testing", "estrategia de tests", "qué falta documentar", "detectá
  documentación faltante", "actualizá la documentación". No activar para: documentación
  inline en código (usar skill JSDoc/TSDoc/XMLDoc/Javadoc del stack), ni para diagramas
  Mermaid (usar common-mermaid-diagrams).
---

# Documentación estructural de repositorios destino

## Objetivo

Garantizar que cada carpeta funcional del repositorio destino posea documentación local
actualizada. Los documentos viven en el repositorio destino y deben describir únicamente
el sistema al que pertenecen.

## Cuándo activar

- Se detecta una carpeta sin README.md o con README vacío/placeholder.
- Se pide auditar la documentación del repositorio destino.
- Un developer finaliza una feature y hay módulos impactados sin documentar.
- Se pide generar documentación de la estrategia de testing del repo.

## Cuándo NO activar

- Documentación inline en código → usar el skill de doc del stack (JSDoc/TSDoc/XMLDoc/Javadoc).
- Diagramas Mermaid → usar `common-mermaid-diagrams`.
- Documentación ajena al repositorio destino → no aplica este skill.

## Estructura mínima de un README de módulo

Cada README.md de carpeta funcional debe incluir:

```markdown
# <Nombre del módulo>

## Propósito

Qué hace este módulo/carpeta y por qué existe.

## Componentes

Lista de los archivos o subcomponentes principales con una línea de descripción cada uno.

## Dependencias

Qué depende de este módulo y de qué depende este módulo.

## Ejemplos de uso

Snippet mínimo de cómo se usa / instancia / llama. Sin datos sensibles reales.

## Decisiones de diseño

Decisiones no obvias y por qué se tomaron.
```

Secciones opcionales según aplique: `Configuración`, `Variables de entorno requeridas`,
`Limitaciones conocidas`.

## Paquetes publicables (npm): README consumidor vs README estructural

Si la carpeta documentada es la raíz de un **paquete publicable** (tiene su propio
`package.json` con `name`/`version` y se instala como dependencia, ej.
`@organization/package`), su `README.md` raíz es **documentación para el consumidor**:
Instalación, Uso/API pública, Configuración y variables de entorno requeridas,
Limitaciones conocidas relevantes para integrar la lib. NO debe incluir diagramas de
arquitectura interna, mapas de dependencias entre módulos internos del paquete, ni la
estrategia de testing completa.

La documentación **estructural/interna** (arquitectura interna, cómo se relacionan los
módulos/carpetas internas de `src/`, decisiones de diseño no obvias) va en un
`README.md` **por subcarpeta funcional** dentro de `src/`, siguiendo la misma estructura
mínima de este skill (Propósito, Componentes, Dependencias, Ejemplos de uso, Decisiones
de diseño).

Cuando el paquete ya tiene o va a tener una carpeta `docs/` (por la documentación de
testing), los diagramas Mermaid de arquitectura/secuencia también van por defecto en
`docs/arquitectura.md`, y el README que corresponda (raíz o estructural) solo
referencia/linkea ese archivo en vez de embeber el diagrama completo — así se evita
duplicar o inflar los README con diagramas grandes. Si terminan siendo más de un
diagrama, `docs/arquitectura.md` pasa a ser un índice que linkea a archivos individuales
bajo `docs/diagramas/` en vez de embeberlos todos. Ver `common-mermaid-diagrams` para
el detalle de ambas convenciones.

### Cuándo una subcarpeta amerita su propio README

No todas las subcarpetas de `src/` necesitan su propio `README.md`. Aplicá dos niveles:

- **Nivel 1 — siempre documentado**: las subcarpetas de primer nivel dentro de `src/`
  (los dominios/divisiones arquitectónicas principales del paquete). Cada una tiene su
  propio `README.md` sin importar su tamaño, porque representan las divisiones
  estructurales del paquete.
- **Nivel 2 — solo si amerita**: subcarpetas más profundas que tienen complejidad propia
  real. Basta con que cumpla **una** de estas señales concretas:
  - Patrón de extensibilidad/plugin (ej. un proveedor/estrategia por subcarpeta).
  - Tiene su propio `utils.ts`/módulo con reglas de negocio no triviales — no solo
    helpers puros de una línea — ej. un pipeline de varios pasos, validaciones,
    enriquecimiento/transformación de datos.
  - Maneja estado o cache propio (singleton de promise, memoización, idempotencia,
    interceptar/parchear una API global o del entorno de ejecución).
  - Describirla bien en el README del nivel 1 requeriría un bullet largo (más de
    ~6-8 líneas) que empieza a competir en detalle con tener Propósito/Dependencias/
    Decisiones de diseño propios.
    Si no cumple ninguna señal, NO se crea un README dedicado.
- **Todo lo demás** (carpetas hoja de soporte con un solo archivo, un helper puro sin
  estado, o un builder simple sin lógica propia relevante): se documenta como un ítem
  dentro de la sección "Componentes" del README estructural del **ancestro inmediato**
  en la jerarquía (nivel 1 o nivel 2 según corresponda) — NUNCA en el `README.md` raíz
  consumidor del paquete. El README raíz solo lista lo que efectivamente se exporta al
  consumidor (sección "Exportaciones"), no la composición interna de `src/`.

Regla de decisión rápida: si el contenido ayuda a alguien a **usar** la lib desde afuera
→ README raíz. Si ayuda a alguien a **modificar** el código interno → README de la
subcarpeta correspondiente.

## Estructura mínima de documentación de testing

Si el repo tiene tests, debe existir documentación de la estrategia de testing,
preferentemente **siempre en `docs/testing.md`** como archivo separado para paquetes
publicables. La sección inline en el README raíz solo es un fallback aceptable si el
repo/paquete no tiene convención de carpeta `docs/`:

```markdown
## Estrategia de testing

- **Framework:** Jest / xUnit / JUnit 5 (según stack)
- **Cómo correr los tests:** `npm test` / `dotnet test` / `mvn test`
- **Cobertura esperada:** branches alcanzables con assertions reales
- **Casos principales cubiertos:** [lista breve de los flujos críticos testeados]
- **Casos no cubiertos:** [lista con justificación]
- **Dependencias necesarias para correr los tests:** mocks, fixtures, variables de entorno
```

## Reglas obligatorias (MUST / MUST NOT)

1. MUST verificar si ya existe un README.md en la carpeta antes de crear uno nuevo.
   Si existe, actualizarlo preservando lo correcto; si no, crearlo.
2. MUST documentar solo lo que se comprende con certeza del código — no inventar comportamiento.
3. MUST mantener READMEs sincronizados: si cambia la arquitectura del módulo, el README
   se actualiza en el mismo PR.
4. MUST incluir sección de testing si el módulo o el repo tiene tests asociados.
5. MUST actualizar `docs/testing.md` (o la sección de testing del README si no existe
   `docs/`) en el mismo cambio cuando se agregan, modifican o eliminan tests: conteo de
   test suites/tests, casos cubiertos y casos no cubiertos. No dejarlo para un PR aparte.
6. MUST NOT duplicar información que ya está en el README raíz — referenciar en su lugar.
7. MUST NOT crear READMEs vacíos o con solo el título: mínimo Propósito y Componentes.
8. MUST NOT incluir datos sensibles reales (DNI, CBU, tokens, passwords) en ejemplos.
9. MUST NOT incluir en el README raíz de un paquete publicable diagramas de arquitectura
   interna, mapas de dependencias entre módulos internos, o la estrategia de testing
   completa — esos van en README por subcarpeta o en `docs/testing.md`.

## Recomendaciones (SHOULD)

- SHOULD incluir README en el root del repo con visión general del sistema.
- SHOULD incluir o referenciar diagramas Mermaid cuando aporten claridad (usar `common-mermaid-diagrams`).
- SHOULD incluir ejemplos de uso con valores representativos pero ficticios.
- SHOULD enlazar desde el README raíz hacia los READMEs de módulos relevantes.
- SHOULD incluir `docs/testing.md` si la estrategia de testing es compleja.

## Auditoría de documentación faltante

Cuando se pide auditar la documentación del repo destino:

1. Listar todas las carpetas funcionales principales (excluir `node_modules`, `.git`,
   `dist`, `build`, `target`, `.next`, `coverage`).
2. Para cada carpeta con archivos de código, verificar si existe `README.md`.
3. Para cada README existente, verificar si tiene al menos Propósito y Componentes.
4. Verificar si existe documentación de testing (en README raíz o `docs/testing.md`).
5. Reportar por carpeta: **AUSENTE** | **INCOMPLETO** | **OK**.

## Checklist antes de entregar

- [ ] Todas las carpetas funcionales principales tienen README.md.
- [ ] Cada README tiene al menos Propósito y Componentes.
- [ ] Existe documentación de testing si el repo tiene tests.
- [ ] No hay contenido inventado: solo lo comprendido con certeza.
- [ ] READMEs existentes actualizados si se modificó la estructura del módulo.
- [ ] Sin datos sensibles en ejemplos.
- [ ] Si es paquete publicable: el README raíz solo tiene contenido consumidor; lo estructural está en subcarpetas o docs/.
- [ ] Ninguna subcarpeta con señales de Nivel 2 (pipeline propio, estado/cache propio,
      `utils.ts` con lógica de negocio no trivial) quedó resumida en un bullet largo del
      README padre en vez de tener su propio README dedicado.

## Conexiones con otros skills

- `common-mermaid-diagrams` — los READMEs pueden y deben incluir diagramas Mermaid.
- `nodejs-code-documentation-jsdoc` / `frontend-code-documentation-tsdoc` / `dotnet-code-documentation-xmldoc` / `java-code-documentation-javadoc` — para documentación inline en código.
