# Implementation Report: V2-ACT-001 (Activity Service Task 14)

## Context
Implemented Task 14 for `activity-service` establishing the Domain, Application, Infrastructure, and API layers to handle manual logging of activities.

## Implementation Details

### 1. Domain
- Created `Activity` aggregate root containing all required fields: `ActivityId`, `ActivityTypeCode`, `OccurredAt`, `RecordedByUserId`, `PrimaryPartyId`, `RelatedCompanyId`, `RelatedContactId`, `PipelineItemId`, `PropertyId`, `ListingId`, `Description`, `Result`, and `CreatedAt`.
- Implemented invariants: enforced requirements on `OccurredAt`, `RecordedByUserId`, `Description` and ensured that at least a `PrimaryPartyId` or `PipelineItemId` is provided.

### 2. Application
- Implemented `RecordActivityHandler` to process `RecordActivityRequestV1` and return `ActivityResponseV1`.
- Implemented `TimelineQueryHandler` to resolve queries for Party, Company, Contact, and Commercial pipelines mapping directly into `TimelineEntryV1`. Note: Stage changes are currently out of scope for the simple aggregate query, but the queries exist and combine existing activities correctly.

### 3. Infrastructure
- Implemented `MongoActivityRepository` implementing `IActivityRepository` and targeting the `crm_activity` MongoDB collection natively via MongoDB.Driver.

### 4. API
- Created `ActivitiesController` providing the following endpoints:
  - `POST /api/v1/activities`
  - `GET /api/v1/activities/timeline?partyId=...&companyId=...&contactId=...&pipelineItemId=...`

## Contratos publicados
- The contracts correspond to those existing in `RealEstateCrm.Contracts.Activities`.
