# W1-PRP-01 — Property mínima urbana/rural, enriquecimiento y búsqueda geográfica

**Dependencias:** `FND-005`, `FND-006`  
**Casos de uso:** `PRP-001`, `PRP-002`, `PRP-003`, `PRP-013`

## Objetivo

Registrar inmuebles urbanos y rurales con datos mínimos, permitir enriquecimiento progresivo y búsquedas geográficas sin reducir rural a un “terreno genérico”.

## Casos de uso

- `PRP-001` Registrar Property mínima: tipo + ubicación mínima; `property-service`; evento `PropertyRegistered`.
- `PRP-002` Enriquecer atributos físicos: superficies, ambientes/características según tipo; evento `PropertyChanged`.
- `PRP-003` Registrar inmueble rural: unidad física y atributos productivos relevantes como primera clase; evento `PropertyRegistered`.
- `PRP-013` Buscar por mapa/zona: geo, radio, polígono, zona/texto; POC con índices MongoDB `2dsphere`, sin `search-service` desplegado.

## Reglas

- `Property` representa el activo físico, no su comercialización.
- Lo rural tiene atributos especializados y extensibles.
- La captura mínima no exige datos registrales/documentales completos.
- Enriquecimiento posterior conserva historia significativa.
- Búsqueda respeta tenant/scope.

## Criterios de aceptación

- [ ] Crear urbano y rural con información mínima.
- [ ] Enriquecer sin reemplazar el aggregate ni perder identidad.
- [ ] Buscar por ubicación mediante MongoDB POC.
- [ ] Modelo no obliga a todos los tipos de Property a tener los mismos atributos.
