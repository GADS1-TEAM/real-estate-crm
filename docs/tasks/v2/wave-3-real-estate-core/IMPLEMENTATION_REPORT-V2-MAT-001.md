# V2-MAT-001 Implementation Report

## Done:
- Added `MatchCase` aggregate with `MatchId`, `Eligibility`, `Score`, etc.
- Added Domain logic for `MatchingScoringEngine` supporting hard/soft criteria.
- Added events `MatchCalculatedV1`, `MatchPresentedV1`, `MatchSelectedV1`, `MatchDiscardedV1`, `MatchInvalidatedV1` in `contracts`.

## Remaining work:
- Implementation of the endpoints in `matching-service` (Calculate, Select, Discard, Queries).
- Persistence (Mongo) for `MatchCase` and Snapshots.
- Event Consumers (RabbitMQ) for Requirements and Listings updates.
- Operations-BFF proxies.
- Testing (Unit & Integration).
