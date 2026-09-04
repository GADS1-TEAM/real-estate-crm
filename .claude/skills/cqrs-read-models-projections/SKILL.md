---
name: cqrs-read-models-projections
description: |
  Activa cuando se construye una vista compuesta, un dashboard o una métrica de este
  CRM a partir de eventos: read models, projectors, checkpoints, rebuild, consistencia
  eventual y trazabilidad de las cifras. Triggers: "CQRS", "read model", "modelo de
  lectura", "proyección", "projection", "projector", "vista 360", "Party360",
  "Property360", "AgentToday", "ManagerCockpit", "ListingPerformance", "dashboard",
  "embudo", "funnel", "métrica", "KPI", "indicador", "reporte", "exportar métricas",
  "consulta compuesta", "datos de varios servicios", "checkpoint", "rebuild",
  "reconstruir la proyección", "replay", "consistencia eventual", "eventual
  consistency", "está desactualizado", "lineage", "de dónde sale este número",
  "UNKNOWN vs cero", "dato desconocido en una métrica", "denominador".
  Garantiza que los read models sean derivados y reconstruibles, que los projectors
  sean idempotentes, que ningún dato desconocido se cuente como cero y que toda cifra
  pueda explicarse. NO activar para: writes y aggregates, forma de los documentos de
  negocio, transporte de mensajes ni capa REST.
---

# CQRS, Read Models y Proyecciones

## Objetivo

Este CRM usa CQRS **selectivamente**. La mayoría de las lecturas se resuelven
consultando el servicio propietario. Los read models existen solo para lo que no
puede resolverse así: vistas que componen datos de varios contextos, dashboards y
métricas.

Hay dos formas de arruinarlo:

- **Usar CQRS de más.** Una proyección para algo que una query directa resolvía
  duplica el dato, introduce desfasaje y agrega un projector que mantener.
- **Tratar el read model como verdad.** En cuanto algo le escribe directamente, deja
  de ser reconstruible y se convierte en una segunda fuente de datos que
  eventualmente contradice a la primera.

Y hay un tercer riesgo, propio de este dominio: **contar lo desconocido como cero**.
Una inmobiliaria que no cargó la superficie de 30 propiedades no tiene 30
propiedades de 0 m².

Fuentes: [`ARCHITECTURE.md`](../../../ARCHITECTURE.md) §3 y §9,
[`README.md`](../../../README.md) §1.4.

## Cuándo activar

- Una vista necesita datos de más de un bounded context.
- Se construye un dashboard, un embudo o una métrica.
- Se escribe o modifica un projector.
- Hay que reconstruir una proyección o migrar su forma.
- Aparece una cifra que nadie sabe explicar.
- Se discute si algo debería ser read model o una query directa.

## Cuándo NO activar

- Writes, aggregates e invariantes (ver `ddd-hexagonal-architecture`).
- Forma de los documentos de negocio (ver `mongodb-document-modeling`).
- Outbox, inbox e idempotencia de consumo (ver `event-driven-outbox-inbox`).
- Transporte, colas y DLQ (ver `rabbitmq-dotnet`).

## Decisiones del proyecto

| Tema | Decisión |
|---|---|
| Alcance | CQRS **selectivo**. Writes al servicio propietario; read models solo para vistas compuestas |
| Naturaleza | Los read models son **derivados y reconstruibles**. Nunca fuente de verdad |
| Alimentación | Por **eventos de integración**. Nunca leyendo colecciones de otro servicio |
| Owner | El servicio que proyecta es dueño de su colección de lectura |
| Persistencia | Proyecciones en MongoDB. Sin warehouse ni motor columnar en la POC |
| Idempotencia | Todo projector es un consumidor: aplican inbox y las reglas de `event-driven-outbox-inbox` |
| Checkpoint | Cada projector guarda su posición para poder reanudar y reconstruir |
| Consistencia | **Eventual y explícita**. La UI dice cuándo un dato puede estar desfasado |
| Tenancy | `tenantId` en todo documento de proyección y en todos sus índices |
| Desconocido | `UNKNOWN` **nunca** se cuenta como `0`, `false` ni lista vacía |
| Métricas | Definidas y versionadas en `platform-config-service`, no inventadas por dashboard |

### Read models previstos

`Party360`, `Property360`, `AgentToday`, `ManagerCockpit`, `ListingPerformance`,
métricas de alquileres y las vistas del copiloto.

## Estado actual vs target

- **Estado:** no hay proyecciones. `W2-ANA-01` crea los primeros read models
  operativos y `W2-ANA-02` agrega lineage y el tratamiento de `UNKNOWN`.
- **Target:** cada read model tiene projector con checkpoint, se reconstruye desde
  cero sin intervención manual, y cada métrica declara de qué eventos sale.

## Reglas obligatorias

### MUST

- **MUST** poder **reconstruir** todo read model desde los eventos, sin
  intervención manual y sin pérdida.
- **MUST** alimentar las proyecciones únicamente con eventos de integración.
- **MUST** hacer los projectors **idempotentes**: reprocesar un evento ya aplicado
  no cambia el resultado.
- **MUST** guardar un **checkpoint** por projector, con la posición procesada y su
  timestamp.
- **MUST** incluir `tenantId` en todo documento de proyección y como primer campo de
  sus índices.
- **MUST** distinguir en la proyección los tres estados: valor confirmado, ausencia
  confirmada y **desconocido**.
- **MUST** exponer, junto a toda métrica agregada, el **denominador** y la cantidad
  de casos `UNKNOWN` excluidos.
- **MUST** declarar el **lineage** de cada métrica: de qué eventos y campos sale, y
  desde cuándo.
- **MUST** exponer la frescura de la proyección (`lastUpdatedAt`, retraso estimado)
  cuando la vista se usa para decidir.
- **MUST** versionar la forma de la proyección y reconstruir cuando cambia de forma
  incompatible.

### MUST NOT

- **MUST NOT** escribir en una colección de read model desde lógica de negocio: solo
  el projector escribe.
- **MUST NOT** enviar comandos de negocio a un read model.
- **MUST NOT** usar un read model como fuente para validar una invariante: eso se
  hace contra el aggregate.
- **MUST NOT** construir una proyección leyendo colecciones de otro servicio.
- **MUST NOT** contar `UNKNOWN` como `0`, `false` o lista vacía en ninguna métrica,
  promedio, porcentaje ni gráfico.
- **MUST NOT** crear un read model para una consulta que el servicio propietario ya
  resuelve.
- **MUST NOT** reconstruir en caliente sobre la colección en uso sin una estrategia
  que evite dejarla a medio llenar.
- **MUST NOT** publicar una métrica sin definición: un número sin lineage no es un
  indicador, es una opinión.

## Recomendaciones

### SHOULD

- **SHOULD** empezar sin read model. Se agrega cuando una query concreta demuestra
  que no puede resolverse en el servicio propietario.
- **SHOULD** reconstruir en una colección nueva y cambiar el puntero al terminar, en
  vez de vaciar y rellenar la que se está usando.
- **SHOULD** guardar en la proyección el `eventId` o la versión del aggregate que la
  dejó en ese estado: permite diagnosticar desfasajes.
- **SHOULD** hacer la consistencia eventual **visible** en la UI cuando importa
  ("actualizado hace 2 minutos"), en vez de fingir que es instantánea.
- **SHOULD** tratar cada projector como una unidad independiente: uno atrasado no
  debe frenar a los demás.
- **SHOULD** alertar cuando el retraso de un projector supera su umbral: es la señal
  temprana de que un consumidor se rompió.
- **SHOULD** testear el rebuild, no solo el camino incremental: casi siempre el bug
  aparece ahí.

### SHOULD NOT

- **SHOULD NOT** acumular lógica de negocio en el projector: transforma y agrega, no
  decide.
- **SHOULD NOT** proyectar PII que la vista no necesita.
- **SHOULD NOT** mantener una proyección que nadie consulta.

## Anti-patrones prohibidos

### 1. Escribir el read model desde la lógica de negocio

```csharp
// ❌ Deja de ser derivado: el rebuild lo pisa y aparecen dos verdades.
await _transactions.SaveAsync(tx, ct);
await _managerCockpit.IncrementClosedDealsAsync(tx.AgentId, ct);
```

```csharp
// ✅ Solo el projector escribe, reaccionando al evento.
public async Task On(TransactionClosed e, CancellationToken ct)
    => await _managerCockpit.ApplyAsync(e, ct);
```

### 2. Projector no idempotente

```csharp
// ❌ At-least-once: una redelivery infla la métrica para siempre.
public async Task On(VisitCompleted e, CancellationToken ct)
    => await _agentToday.IncrementVisitsAsync(e.AgentId, ct);
```

```csharp
// ✅ El inbox descarta el duplicado y la escritura es idempotente por evento.
public async Task On(VisitCompleted e, CancellationToken ct)
{
    await _projections.ApplyOnceAsync(
        eventId: e.EventId,
        tenantId: e.TenantId,
        apply: p => p.AddVisit(e.AgentId, e.VisitId, e.OccurredAt),
        ct);
}
```

### 3. Desconocido contado como cero

```csharp
// ❌ 30 propiedades sin superficie cargada arrastran el promedio a la mitad,
//    y el gerente toma una decisión con un número inventado.
var avgSurface = properties.Average(p => p.SurfaceM2 ?? 0);
```

```csharp
// ✅ Se promedia lo conocido y se informa cuántos quedaron afuera.
var known = properties.Where(p => p.Surface.IsConfirmed).ToList();

var metric = new SurfaceMetric(
    Average: known.Count > 0 ? known.Average(p => p.Surface.Value) : null,
    Denominator: known.Count,
    UnknownCount: properties.Count - known.Count);
```

### 4. Proyección construida leyendo otro servicio

```csharp
// ❌ Acopla analytics a las colecciones de property-service y rompe el ownership.
var props = await _mongo.GetDatabase("crm")
    .GetCollection<BsonDocument>("property_properties")
    .Find(filter).ToListAsync(ct);
```

```csharp
// ✅ Se alimenta de los eventos que property-service publica.
public async Task On(PropertyRegistered e, CancellationToken ct) { /* ... */ }
public async Task On(PropertyEnriched e, CancellationToken ct) { /* ... */ }
```

### 5. Rebuild sin checkpoint

```csharp
// ❌ Si el rebuild se corta a la mitad, la colección queda incompleta y
//    nadie sabe desde dónde retomar.
await _collection.DeleteManyAsync(FilterDefinition<T>.Empty, ct);
await ReplayAllAsync(ct);
```

```csharp
// ✅ Se construye en una colección nueva y se cambia el puntero al terminar.
var target = $"analytics_manager_cockpit_v{newVersion}";
await ReplayIntoAsync(target, fromCheckpoint: null, ct);
await _projectionRegistry.PromoteAsync("manager_cockpit", target, ct);
```

### 6. Read model para lo que ya resuelve el owner

```csharp
// ❌ Una proyección entera para listar los inmuebles de un agente, que
//    property-service resuelve con un índice.
```

```csharp
// ✅ Query directa al servicio propietario. El read model se justifica cuando
//    la vista compone Party + Property + Listing + Visit + Negotiation.
```

### 7. Métrica sin lineage

```csharp
// ❌ "Tasa de conversión: 34%". ¿De qué eventos sale? ¿Desde cuándo?
//    ¿Qué pasa con los casos sin desenlace registrado?
```

```csharp
// ✅ La definición vive en platform-config-service, versionada, y la métrica
//    la referencia.
new MetricResult(
    DefinitionId: "conversion_rate",
    DefinitionVersion: 3,
    Value: 0.34m,
    Denominator: 118,
    UnknownExcluded: 12,
    ComputedFrom: ["RequirementCreated", "TransactionClosed", "CaptationCaseLost"],
    AsOf: lastProcessedAt);
```

## Checklist antes de devolver código

- [ ] El read model se reconstruye desde cero sin pasos manuales.
- [ ] Solo el projector escribe en la colección de lectura.
- [ ] El projector es idempotente y tiene inbox.
- [ ] Hay checkpoint con posición y timestamp.
- [ ] `tenantId` en el documento y en todos los índices.
- [ ] `UNKNOWN` no se degradó a `0`, `false` ni lista vacía en ninguna cifra.
- [ ] Toda métrica agregada expone denominador y cantidad de `UNKNOWN` excluidos.
- [ ] Cada métrica declara su lineage y su versión de definición.
- [ ] La vista expone su frescura cuando se usa para decidir.
- [ ] Ninguna proyección lee colecciones de otro servicio.
- [ ] Hay test del rebuild, no solo del camino incremental.
- [ ] Ninguna invariante de negocio se valida contra el read model.

## Conexiones con otros skills

| Skill | Relación |
|---|---|
| `ddd-hexagonal-architecture` | Los read models son derivados; la verdad vive en los aggregates. |
| `event-driven-outbox-inbox` | Los projectors son consumidores: mismo inbox, misma idempotencia. |
| `mongodb-document-modeling` | Las colecciones de proyección siguen las mismas reglas de índices y tenancy. |
| `mongodb-dotnet-driver` | Escrituras idempotentes, bulk y bootstrap de índices de las proyecciones. |
| `multitenancy-authorization` | Un dashboard filtra por tenant y scope igual que un endpoint. |
| `rabbitmq-dotnet` | Transporte de los eventos que alimentan los projectors. |
| `aspnetcore-error-and-observability` | Métricas de retraso del projector y alertas de proyección atrasada. |
