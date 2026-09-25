import { createContext, createElement, useContext, useEffect, useMemo, useReducer, type Dispatch, type ReactNode } from "react";
import type { CurrencyCode } from "@/lib/currency";
import { COMMERCIAL_STATUS_VALUES, validateActivity, validateOperationClose, validateOperationTransition, validateOpportunityClose, validateOpportunityStageChange, validateReason, type CommercialStatus, type IdentityStatus } from "@/lib/workflow-rules";

export type PipelineStage = "Nuevo" | "Contacto" | "Visita" | "Negociación" | "Reserva" | "Operación" | "Cerrada";
export type OpportunityStage = PipelineStage;
export type CriterionWeight = "must" | "nice" | "any" | "unknown";

export interface DemoCriterion {
  id: string;
  label: string;
  value: string;
  weight: CriterionWeight;
  matchScore?: number;
}

export interface DemoContact {
  id: string;
  name: string;
  kind: "Persona" | "Empresa";
  phone: string;
  email: string;
  status: "Activo" | "No contactar" | "Archivado";
  owner: string;
  commercialStatus?: CommercialStatus;
  identityStatus?: IdentityStatus;
  origin?: string;
}

export interface DemoPartyRelationship {
  id: string;
  fromPartyId: string;
  toPartyId: string;
  relationshipType: "CONTACT_OF" | "REPRESENTS";
  createdAt: string;
  createdBy: string;
}

export interface DemoProperty {
  id: string;
  title: string;
  address: string;
  type: string;
  status: "Disponible" | "Reservado" | "Archivado";
  price: number | null;
  currency: CurrencyCode;
  bedrooms: string;
  geo: "Conocida" | "Desconocida";
  surfaceM2?: number | null;
  surfaceHa?: number | null;
  ownerPartyIds?: string[];
}

export interface DemoListing {
  id: string;
  title: string;
  propertyId: string;
  status: "Activa" | "Borrador" | "Pausada" | "Cerrada";
  mandate: "Firmado" | "Sin mandato";
  views: number;
  description?: string;
  price?: number | null;
  currency?: CurrencyCode;
  operationType?: string;
  statusReason?: string;
  statusChangedAt?: string;
}

export interface DemoCaptation {
  id: string;
  propertyId: string;
  owner: string;
  stage: "Nueva" | "Tasación" | "Mandato" | "Publicada" | "Cerrada";
  expectation: number;
  valuation: number | null;
  currency: CurrencyCode;
  origin?: string;
  ownerPartyId?: string;
  valuationHistory?: { id: string; value: number; currency: CurrencyCode; recordedAt: string; recordedBy: string }[];
  closeReason?: string;
  closedAt?: string;
}

export interface DemoDemand {
  id: string;
  contactId: string;
  title: string;
  status: "Activa" | "Pausada" | "Cerrada";
  criteria: DemoCriterion[];
  score: number;
  origin?: string;
  selectedListingIds?: string[];
  closeReason?: string;
  closedAt?: string;
}

export interface CommercialPipelineItem {
  id: string;
  title: string;
  sourceType: "REQUIREMENT" | "CAPTATION_CASE";
  sourceId: string;
  stage: PipelineStage;
  owner: string;
  fee: number;
  currency: CurrencyCode;
  daysInStage: number;
  origin?: string;
  outcome?: "won" | "lost";
  closedAt?: string;
  finalValue?: number;
  closeReason?: string;
}

export type AnalyticsPipelineKind = "ALL" | CommercialPipelineItem["sourceType"];
export type AnalyticsMetricValue = number | "UNKNOWN";

export interface AnalyticsFilters {
  period: "30d" | "all";
  responsible: string;
  pipelineKind: AnalyticsPipelineKind;
  origin: string;
}

export interface AnalyticsSnapshot {
  opportunities: CommercialPipelineItem[];
  activities: DemoActivity[];
  openOpportunities: number;
  conversionRate: AnalyticsMetricValue;
  averageStageDays: AnalyticsMetricValue;
  estimatedFees: AnalyticsMetricValue;
  activeListings: number;
  activeDemands: number;
  activeMatches: number;
}

export type DemoActivityType =
  | "Nota"
  | "Llamada"
  | "Email"
  | "Email histórico"
  | "WhatsApp"
  | "WhatsApp histórico"
  | "Visita"
  | "Correo electrónico"
  | "Mensaje"
  | "Reunión presencial"
  | "Reunión virtual"
  | "Demostración"
  | "Envío de propuesta"
  | "Nota interna"
  | "Otro";

export interface DemoActivity {
  id: string;
  type: DemoActivityType;
  subject: string;
  body: string;
  actor: string;
  createdAt: string;
  occurredAt?: string;
  recordedByUserId?: string;
  relatedRecordId?: string;
  relatedRecordType?: "Party" | "Property" | "Listing" | "Demand" | "Opportunity" | "Captation" | "Reservation" | "Operation";
  propertyId?: string;
  listingId?: string;
  pipelineItemId?: string;
  historical?: boolean;
}

export interface DemoReservation {
  id: string;
  opportunityId: string;
  propertyTitle: string;
  deposit: number | null;
  currency: CurrencyCode;
  status: "Activa" | "Cancelada" | "Convertida";
  listingId?: string;
  acceptedProposalId?: string;
  validFrom?: string;
  expiresAt?: string;
  conditions?: string;
  cancellationReason?: string;
  cancelledAt?: string;
}

export interface DemoOperation {
  id: string;
  reservationId: string;
  propertyTitle: string;
  operationType?: string;
  propertyId?: string;
  listingId?: string;
  agreedTerms?: string;
  stage: "Documentación" | "Escrituración" | "Cerrada" | "Cancelada";
  outcome?: "OPEN" | "CLOSED" | "CANCELLED";
  closeDate?: string;
  finalValue?: number;
  cancellationReason?: string;
  closedAt?: string;
}

export interface DemoProposal {
  outcome: "pending" | "accepted" | "rejected";
  reason: string;
}

export interface DemoProposalRecord extends DemoProposal {
  id: string;
  opportunityId: string;
  sequence: number;
  kind: "PROPUESTA" | "CONTRAPROPUESTA";
  proposedBy: string;
  proposedTo?: string;
  amount: number | null;
  currency: CurrencyCode;
  validity?: string;
  conditions: string;
  createdAt: string;
  respondedAt?: string;
}

export interface OfflineQueueItem {
  id: string;
  label: string;
  createdAt: string;
  kind?: "activity" | "visit" | "photo";
}

export interface DemoMatchAction {
  id: string;
  demandId: string;
  propertyId: string;
  status: "new" | "favorite" | "dismissed" | "presented" | "selected";
  reason?: string;
}

export interface DemoAISuggestion {
  id: string;
  title: string;
  body: string;
  evidence: string;
  status: "pending" | "reviewed" | "dismissed";
  updatedAt?: string;
}

export type CatalogFamily = "pipeline-stage" | "activity-type" | "origin" | "loss-reason" | "operation-type" | "property-type";

export interface CatalogFamilyDefinition {
  id: CatalogFamily;
  label: string;
  description: string;
  screenId: string;
}

export const crmCatalogFamilies: CatalogFamilyDefinition[] = [
  { id: "pipeline-stage", label: "Etapas del embudo", description: "Estados comerciales con semántica OPEN/WON/LOST protegida.", screenId: "ADM-06" },
  { id: "activity-type", label: "Tipos de actividad", description: "Hechos ocurridos que pueden quedar en el historial.", screenId: "ADM-07" },
  { id: "origin", label: "Orígenes", description: "Procedencia declarada de una Party, búsqueda o captación.", screenId: "ADM-08" },
  { id: "loss-reason", label: "Motivos de pérdida", description: "Motivos versionados para cerrar una oportunidad como perdida.", screenId: "ADM-09" },
  { id: "operation-type", label: "Tipos de operación", description: "Tipo comercial de una publicación u oportunidad.", screenId: "ADM-10" },
  { id: "property-type", label: "Tipos de inmueble", description: "Taxonomía de inmuebles usada por la operación.", screenId: "ADM-11" },
];

export interface DemoCatalogEntry {
  id: string;
  label: string;
  status: "Activo" | "Inactivo" | "Cierre comercial";
  catalogType?: CatalogFamily;
  code?: string;
  order?: number;
  pipelineKind?: "DEMAND" | "SUPPLY" | "BOTH";
  semanticState?: "OPEN" | "WON" | "LOST";
  protected?: boolean;
}

export interface DemoState {
  contacts: DemoContact[];
  relationships: DemoPartyRelationship[];
  properties: DemoProperty[];
  listings: DemoListing[];
  captations: DemoCaptation[];
  demands: DemoDemand[];
  opportunities: CommercialPipelineItem[];
  activities: DemoActivity[];
  reservations: DemoReservation[];
  operations: DemoOperation[];
  proposals: DemoProposalRecord[];
  proposal: DemoProposal;
  stageHistory: { id: string; opportunityId: string; from: PipelineStage; to: PipelineStage; actor: string; at: string; reason?: string; outcome?: "won" | "lost"; closeDate?: string; finalValue?: number }[];
  offlineQueue: OfflineQueueItem[];
  matchActions: DemoMatchAction[];
  aiSuggestions: DemoAISuggestion[];
  catalogEntries: DemoCatalogEntry[];
  users: { id: string; name: string; email: string; role: "Vendedor" | "Responsable comercial" | "Dirección" | "Administradora"; status: "Habilitado" | "Pendiente" }[];
  catalogVersion: number;
}

const now = "2026-09-09T12:00:00.000Z";

export const demoInitialState: DemoState = {
  contacts: [
    { id: "contact-1", name: "Ana Suárez", kind: "Persona", phone: "+54 9 11 5555 0101", email: "ana@ejemplo.com", status: "Activo", owner: "Martín Quiroga", commercialStatus: "CUSTOMER", identityStatus: "VERIFIED", origin: "WhatsApp histórico" },
    { id: "contact-2", name: "Carla Benítez", kind: "Persona", phone: "+54 9 11 5555 0142", email: "carla@ejemplo.com", status: "Activo", owner: "Martín Quiroga", commercialStatus: "POTENTIAL", identityStatus: "PENDING", origin: "Carga manual" },
    { id: "contact-3", name: "Diego Ibarra", kind: "Persona", phone: "+54 9 11 5555 0212", email: "diego@ejemplo.com", status: "Activo", owner: "Lucía Ferrari", commercialStatus: "CUSTOMER", identityStatus: "UNKNOWN", origin: "Referido" },
    { id: "contact-4", name: "Estudio Norte SA", kind: "Empresa", phone: "+54 11 4555 9090", email: "contacto@estudionorte.com", status: "Activo", owner: "Sofía Rendón", commercialStatus: "CUSTOMER", identityStatus: "VERIFIED", origin: "Carga manual" },
  ],
  relationships: [{ id: "relationship-1", fromPartyId: "contact-1", toPartyId: "contact-4", relationshipType: "CONTACT_OF", createdAt: now, createdBy: "user-1" }],
  properties: [
    { id: "property-1", title: "Casa en Villa Crespo", address: "Thames 1720", type: "Casa", status: "Disponible", price: 235000, currency: "USD", bedrooms: "4 ambientes", geo: "Conocida", ownerPartyIds: ["contact-4"] },
    { id: "property-2", title: "PH en Guardia Vieja 3355", address: "Guardia Vieja 3355", type: "PH", status: "Disponible", price: 178000, currency: "USD", bedrooms: "3 ambientes", geo: "Desconocida", ownerPartyIds: ["contact-3"] },
    { id: "property-3", title: "Local en Corrientes al 4500", address: "Av. Corrientes 4520", type: "Local", status: "Reservado", price: 1240580.5, currency: "ARS", bedrooms: "Planta libre", geo: "Conocida", ownerPartyIds: ["contact-4"] },
    { id: "property-4", title: "Casa en Villa Devoto", address: "José Cubas 3900", type: "Casa", status: "Disponible", price: null, currency: "USD", bedrooms: "5 ambientes", geo: "Conocida", ownerPartyIds: ["contact-3"] },
  ],
  listings: [
    { id: "listing-1", title: "Casa luminosa con patio", propertyId: "property-1", status: "Activa", mandate: "Firmado", views: 184, description: "Casa con patio y buena luz natural.", price: 235000, currency: "USD", operationType: "Venta" },
    { id: "listing-2", title: "PH reciclado cerca del subte", propertyId: "property-2", status: "Activa", mandate: "Sin mandato", views: 96, description: "PH de tres ambientes cerca del subte.", price: 178000, currency: "USD", operationType: "Venta" },
    { id: "listing-3", title: "Local sobre avenida", propertyId: "property-3", status: "Pausada", mandate: "Firmado", views: 42, description: "Local comercial sobre avenida.", price: 1240580.5, currency: "ARS", operationType: "Alquiler" },
  ],
  captations: [
    { id: "captation-1", propertyId: "property-1", owner: "Lucía Ferrari", stage: "Mandato", expectation: 245000, valuation: 235000, currency: "USD", origin: "Referido", ownerPartyId: "contact-4", valuationHistory: [{ id: "valuation-1", value: 235000, currency: "USD", recordedAt: now, recordedBy: "Lucía Ferrari" }] },
    { id: "captation-2", propertyId: "property-4", owner: "Lucía Ferrari", stage: "Tasación", expectation: 410000, valuation: null, currency: "USD", origin: "Carga manual", ownerPartyId: "contact-3", valuationHistory: [] },
  ],
  demands: [
    {
      id: "demand-1",
      contactId: "contact-1",
      title: "Ana busca casa en Villa Crespo",
      status: "Activa",
      criteria: [
        { id: "rooms", label: "Ambientes", value: "3", weight: "must", matchScore: 0.86 },
        { id: "neighborhood", label: "Barrio", value: "Villa Crespo", weight: "nice", matchScore: 0.76 },
        { id: "surface", label: "Superficie cubierta", value: "UNKNOWN", weight: "unknown" },
      ],
      score: 82,
      origin: "WhatsApp histórico",
      selectedListingIds: ["listing-1"],
    },
    { id: "demand-2", contactId: "contact-2", title: "Carla busca PH con terraza", status: "Activa", criteria: [{ id: "rooms", label: "Ambientes", value: "4", weight: "must" }, { id: "outdoor", label: "Terraza", value: "Sí", weight: "nice" }], score: 74, origin: "Referido", selectedListingIds: [] },
  ],
    opportunities: [
    { id: "opp-1", title: "Ana Suárez · Casa en Villa Crespo", sourceType: "REQUIREMENT", sourceId: "demand-1", stage: "Visita", owner: "Martín Quiroga", fee: 7050000, currency: "ARS", daysInStage: 4 },
    { id: "opp-2", title: "Carla Benítez · PH en Guardia Vieja", sourceType: "REQUIREMENT", sourceId: "demand-2", stage: "Negociación", owner: "Martín Quiroga", fee: 5340000, currency: "ARS", daysInStage: 9 },
    { id: "opp-3", title: "Captación Casa Villa Crespo", sourceType: "CAPTATION_CASE", sourceId: "captation-1", stage: "Contacto", owner: "Lucía Ferrari", fee: 8700000, currency: "ARS", daysInStage: 3 },
    { id: "opp-4", title: "Captación Casa Villa Devoto", sourceType: "CAPTATION_CASE", sourceId: "captation-2", stage: "Nuevo", owner: "Lucía Ferrari", fee: 0, currency: "USD", daysInStage: 1 },
  ],
  activities: [
    { id: "activity-1", type: "WhatsApp", subject: "Consulta pegada desde WhatsApp", body: "Busca 3 ambientes por Villa Crespo. Prefiere luz y balcón.", actor: "Martín Quiroga", recordedByUserId: "user-1", occurredAt: "2026-09-09T09:42:00.000Z", relatedRecordId: "contact-1", relatedRecordType: "Party", historical: true, createdAt: "Hoy, 09:42" },
    { id: "activity-2", type: "Visita", subject: "Visita registrada", body: "Casa en Villa Crespo · hecho ocurrido", actor: "Martín Quiroga", recordedByUserId: "user-1", occurredAt: "2026-09-08T16:20:00.000Z", relatedRecordId: "property-1", relatedRecordType: "Property", createdAt: "Ayer, 16:20" },
    { id: "activity-3", type: "Email", subject: "Email histórico", body: "Se envió fuera del CRM; aquí queda solo el registro.", actor: "Lucía Ferrari", recordedByUserId: "user-2", occurredAt: "2026-09-05T11:05:00.000Z", relatedRecordId: "contact-1", relatedRecordType: "Party", historical: true, createdAt: "05 sep, 11:05" },
  ],
  reservations: [{ id: "reservation-1", opportunityId: "opp-2", propertyTitle: "PH en Guardia Vieja 3355", deposit: 1500000, currency: "ARS", status: "Activa" }],
  operations: [{ id: "operation-1", reservationId: "reservation-1", propertyTitle: "PH en Guardia Vieja 3355", propertyId: "property-2", listingId: "listing-2", operationType: "Venta", stage: "Documentación", outcome: "OPEN" }],
  proposals: [],
  proposal: { outcome: "pending", reason: "" },
  stageHistory: [{ id: "history-1", opportunityId: "opp-2", from: "Visita", to: "Negociación", actor: "Martín Quiroga", at: "Ayer, 16:30" }],
  offlineQueue: [],
  matchActions: [],
  aiSuggestions: [
    { id: "ai-surface", title: "Completar criterio de superficie", body: "Ana mencionó que busca ambientes, pero la superficie cubierta todavía es desconocida.", evidence: "WhatsApp histórico · confianza media", status: "pending" },
    { id: "ai-match", title: "Presentar Casa en Villa Crespo", body: "La publicación coincide con barrio y ambientes de una búsqueda activa.", evidence: "2 coincidencias · 2 datos desconocidos", status: "pending" },
  ],
  catalogEntries: [
    { id: "stage-new", code: "NEW", label: "Nuevo", order: 1, pipelineKind: "BOTH", status: "Activo", catalogType: "pipeline-stage", semanticState: "OPEN", protected: true },
    { id: "stage-contact", code: "CONTACT", label: "Contacto", order: 2, pipelineKind: "BOTH", status: "Activo", catalogType: "pipeline-stage", semanticState: "OPEN", protected: true },
    { id: "stage-visit", code: "VISIT", label: "Visita", order: 3, pipelineKind: "BOTH", status: "Activo", catalogType: "pipeline-stage", semanticState: "OPEN", protected: true },
    { id: "stage-negotiation", code: "NEGOTIATION", label: "Negociación", order: 4, pipelineKind: "BOTH", status: "Activo", catalogType: "pipeline-stage", semanticState: "OPEN", protected: true },
    { id: "stage-reservation", code: "RESERVATION", label: "Reserva", order: 5, pipelineKind: "BOTH", status: "Activo", catalogType: "pipeline-stage", semanticState: "OPEN", protected: true },
    { id: "stage-operation", code: "OPERATION_WON", label: "Operación", order: 6, pipelineKind: "BOTH", status: "Cierre comercial", catalogType: "pipeline-stage", semanticState: "WON", protected: true },
    { id: "stage-closed", code: "CLOSED_LOST", label: "Cerrada", order: 7, pipelineKind: "BOTH", status: "Cierre comercial", catalogType: "pipeline-stage", semanticState: "LOST", protected: true },
    { id: "demand-stage-received", code: "DEMAND_RECEIVED", label: "Consulta recibida", order: 1, pipelineKind: "DEMAND", status: "Activo", catalogType: "pipeline-stage", semanticState: "OPEN", protected: true },
    { id: "demand-stage-qualified", code: "DEMAND_QUALIFIED", label: "Necesidad relevada", order: 2, pipelineKind: "DEMAND", status: "Activo", catalogType: "pipeline-stage", semanticState: "OPEN", protected: true },
    { id: "demand-stage-selected", code: "DEMAND_SELECTED", label: "Propiedades seleccionadas", order: 3, pipelineKind: "DEMAND", status: "Activo", catalogType: "pipeline-stage", semanticState: "OPEN", protected: true },
    { id: "demand-stage-visited", code: "DEMAND_VISITED", label: "Visita realizada", order: 4, pipelineKind: "DEMAND", status: "Activo", catalogType: "pipeline-stage", semanticState: "OPEN", protected: true },
    { id: "demand-stage-won", code: "DEMAND_WON", label: "Operación concretada", order: 7, pipelineKind: "DEMAND", status: "Cierre comercial", catalogType: "pipeline-stage", semanticState: "WON", protected: true },
    { id: "demand-stage-lost", code: "DEMAND_LOST", label: "Operación perdida", order: 8, pipelineKind: "DEMAND", status: "Cierre comercial", catalogType: "pipeline-stage", semanticState: "LOST", protected: true },
    { id: "supply-stage-owner", code: "SUPPLY_OWNER_CONTACT", label: "Contacto propietario", order: 1, pipelineKind: "SUPPLY", status: "Activo", catalogType: "pipeline-stage", semanticState: "OPEN", protected: true },
    { id: "supply-stage-valuation", code: "SUPPLY_VALUATION", label: "Tasación", order: 2, pipelineKind: "SUPPLY", status: "Activo", catalogType: "pipeline-stage", semanticState: "OPEN", protected: true },
    { id: "supply-stage-mandate", code: "SUPPLY_MANDATE", label: "Mandato", order: 3, pipelineKind: "SUPPLY", status: "Activo", catalogType: "pipeline-stage", semanticState: "OPEN", protected: true },
    { id: "supply-stage-published", code: "SUPPLY_PUBLISHED", label: "Publicación", order: 4, pipelineKind: "SUPPLY", status: "Activo", catalogType: "pipeline-stage", semanticState: "OPEN", protected: true },
    { id: "supply-stage-won", code: "SUPPLY_WON", label: "Captación concretada", order: 5, pipelineKind: "SUPPLY", status: "Cierre comercial", catalogType: "pipeline-stage", semanticState: "WON", protected: true },
    { id: "supply-stage-lost", code: "SUPPLY_LOST", label: "Captación perdida", order: 6, pipelineKind: "SUPPLY", status: "Cierre comercial", catalogType: "pipeline-stage", semanticState: "LOST", protected: true },
    { id: "activity-note", label: "Nota", status: "Activo", catalogType: "activity-type" },
    { id: "activity-call", label: "Llamada", status: "Activo", catalogType: "activity-type" },
    { id: "activity-visit", label: "Visita", status: "Activo", catalogType: "activity-type" },
    { id: "activity-email", label: "Email histórico", status: "Activo", catalogType: "activity-type" },
    { id: "activity-whatsapp", label: "WhatsApp histórico", status: "Activo", catalogType: "activity-type" },
    { id: "activity-email-current", label: "Correo electrónico", status: "Activo", catalogType: "activity-type" },
    { id: "activity-message", label: "Mensaje", status: "Activo", catalogType: "activity-type" },
    { id: "activity-meeting", label: "Reunión presencial", status: "Activo", catalogType: "activity-type" },
    { id: "activity-video", label: "Reunión virtual", status: "Activo", catalogType: "activity-type" },
    { id: "activity-demo", label: "Demostración", status: "Activo", catalogType: "activity-type" },
    { id: "activity-proposal", label: "Envío de propuesta", status: "Activo", catalogType: "activity-type" },
    { id: "activity-internal", label: "Nota interna", status: "Activo", catalogType: "activity-type" },
    { id: "activity-other", label: "Otro", status: "Activo", catalogType: "activity-type" },
    { id: "origin-manual", label: "Carga manual", status: "Activo", catalogType: "origin" },
    { id: "origin-referral", label: "Referido", status: "Activo", catalogType: "origin" },
    { id: "origin-whatsapp", label: "WhatsApp histórico", status: "Activo", catalogType: "origin" },
    { id: "origin-website", label: "Sitio web", status: "Activo", catalogType: "origin" },
    { id: "origin-social", label: "Redes sociales", status: "Activo", catalogType: "origin" },
    { id: "origin-advertising", label: "Publicidad", status: "Activo", catalogType: "origin" },
    { id: "origin-recommendation", label: "Recomendación", status: "Activo", catalogType: "origin" },
    { id: "origin-event", label: "Evento", status: "Activo", catalogType: "origin" },
    { id: "origin-prospecting", label: "Prospección", status: "Activo", catalogType: "origin" },
    { id: "origin-existing", label: "Cliente existente", status: "Activo", catalogType: "origin" },
    { id: "origin-portal", label: "Portal inmobiliario", status: "Activo", catalogType: "origin" },
    { id: "loss-price", label: "Precio fuera de mercado", status: "Activo", catalogType: "loss-reason" },
    { id: "loss-withdrawn", label: "Cliente desistió", status: "Activo", catalogType: "loss-reason" },
    { id: "loss-budget", label: "Falta de presupuesto", status: "Activo", catalogType: "loss-reason" },
    { id: "loss-competitor", label: "Competidor", status: "Activo", catalogType: "loss-reason" },
    { id: "loss-property", label: "Inmueble inadecuado", status: "Activo", catalogType: "loss-reason" },
    { id: "loss-no-response", label: "Falta de respuesta", status: "Activo", catalogType: "loss-reason" },
    { id: "loss-postponed", label: "Decisión postergada", status: "Activo", catalogType: "loss-reason" },
    { id: "loss-unavailable", label: "Inmueble no disponible", status: "Activo", catalogType: "loss-reason" },
    { id: "loss-financing", label: "Financiación", status: "Activo", catalogType: "loss-reason" },
    { id: "loss-other", label: "Otro", status: "Activo", catalogType: "loss-reason" },
    { id: "operation-sale", label: "Venta", status: "Activo", catalogType: "operation-type" },
    { id: "operation-rental", label: "Alquiler", status: "Activo", catalogType: "operation-type" },
    { id: "property-house", label: "Casa", status: "Activo", catalogType: "property-type" },
    { id: "property-apartment", label: "Departamento", status: "Activo", catalogType: "property-type" },
    { id: "property-ph", label: "PH", status: "Activo", catalogType: "property-type" },
    { id: "property-local", label: "Local", status: "Activo", catalogType: "property-type" },
    { id: "property-rural", label: "Rural", status: "Activo", catalogType: "property-type" },
  ],
  users: [
    { id: "user-1", name: "Martín Quiroga", email: "martin@inmobiliaria.com.ar", role: "Vendedor", status: "Habilitado" },
    { id: "user-2", name: "Lucía Ferrari", email: "lucia@inmobiliaria.com.ar", role: "Vendedor", status: "Habilitado" },
    { id: "user-3", name: "Rodrigo Vergara", email: "rodrigo@inmobiliaria.com.ar", role: "Responsable comercial", status: "Habilitado" },
    { id: "user-5", name: "Elena Vergara", email: "elena@inmobiliaria.com.ar", role: "Dirección", status: "Habilitado" },
    { id: "user-4", name: "Sofía Rendón", email: "sofia@inmobiliaria.com.ar", role: "Administradora", status: "Habilitado" },
  ],
  catalogVersion: 2,
};

export function criterionWeightValue(weight: CriterionWeight): number {
  return weight === "must" ? 1 : weight === "nice" ? 0.8 : weight === "any" ? 0.4 : 0;
}

export function criterionMatchValue(criterion: DemoCriterion): number {
  if (criterion.value === "UNKNOWN" || criterion.weight === "unknown") return 0;
  if (criterion.matchScore !== undefined) return Math.max(0, Math.min(1, criterion.matchScore));
  const value = criterion.value.trim().toLowerCase();
  if (criterion.label.toLowerCase().includes("ambiente")) return value === "3" ? 0.86 : 0.72;
  if (criterion.label.toLowerCase().includes("barrio") && value.includes("villa crespo")) return 0.76;
  return 1;
}

export function calculateDemandScore(criteria: DemoCriterion[]): number {
  const weightedCriteria = criteria.filter((criterion) => criterion.weight !== "unknown");
  const possible = weightedCriteria.reduce((sum, criterion) => sum + criterionWeightValue(criterion.weight), 0);
  if (!possible) return 0;
  const weightedMatch = weightedCriteria.reduce((sum, criterion) => sum + criterionWeightValue(criterion.weight) * criterionMatchValue(criterion), 0);
  return Math.round((weightedMatch / possible) * 100);
}

function analyticsOrigin(state: DemoState, opportunity: CommercialPipelineItem): string {
  if (opportunity.origin) return opportunity.origin;
  if (opportunity.sourceType === "REQUIREMENT") {
    return state.demands.find((demand) => demand.id === opportunity.sourceId)?.origin ?? "Origen desconocido";
  }
  return state.captations.find((captation) => captation.id === opportunity.sourceId)?.origin ?? "Origen desconocido";
}

function activityInPeriod(activity: DemoActivity, period: AnalyticsFilters["period"]): boolean {
  if (period === "all") return true;
  const occurredAt = Date.parse(activity.occurredAt ?? activity.createdAt);
  if (!Number.isFinite(occurredAt)) return false;
  const periodStart = Date.parse("2026-08-11T00:00:00.000Z");
  return occurredAt >= periodStart && occurredAt <= Date.parse("2026-09-30T23:59:59.999Z");
}

export function getAnalyticsSnapshot(state: DemoState, filters: AnalyticsFilters): AnalyticsSnapshot {
  const opportunities = state.opportunities.filter((opportunity) => {
    if (filters.responsible !== "ALL" && opportunity.owner !== filters.responsible) return false;
    if (filters.pipelineKind !== "ALL" && opportunity.sourceType !== filters.pipelineKind) return false;
    if (filters.origin !== "ALL" && analyticsOrigin(state, opportunity) !== filters.origin) return false;
    return true;
  });
  const activities = state.activities.filter((activity) => activityInPeriod(activity, filters.period));
  const visits = activities.filter((activity) => activity.type === "Visita").length;
  const reservations = state.reservations.filter((reservation) => reservation.status !== "Cancelada").length;
  const estimatedFees = opportunities.length ? opportunities.reduce((sum, opportunity) => sum + opportunity.fee, 0) : "UNKNOWN";
  return {
    opportunities,
    activities,
    openOpportunities: opportunities.filter((opportunity) => opportunity.stage !== "Cerrada").length,
    conversionRate: opportunities.length && visits ? Math.round((reservations / visits) * 100) : "UNKNOWN",
    averageStageDays: opportunities.length ? Math.round((opportunities.reduce((sum, opportunity) => sum + opportunity.daysInStage, 0) / opportunities.length) * 10) / 10 : "UNKNOWN",
    estimatedFees,
    activeListings: state.listings.filter((listing) => listing.status === "Activa").length,
    activeDemands: state.demands.filter((demand) => demand.status === "Activa").length,
    activeMatches: state.matchActions.filter((match) => match.status !== "dismissed").length,
  };
}

export type DemoAction =
  | { type: "contact/create"; item: DemoContact }
  | { type: "contact/update"; id: string; changes: Partial<DemoContact> }
  | { type: "party/relate"; contactId: string; companyId: string }
  | { type: "property/create"; item: DemoProperty }
  | { type: "property/update"; id: string; changes: Partial<DemoProperty> }
  | { type: "listing/create"; item: DemoListing }
  | { type: "listing/update"; id: string; changes: Partial<DemoListing> }
  | { type: "captation/create"; item: DemoCaptation }
  | { type: "captation/update-stage"; id: string; stage: DemoCaptation["stage"] }
  | { type: "captation/update-valuation"; id: string; valuation: number | null }
  | { type: "captation/issue-valuation"; id: string; value: number; currency: CurrencyCode; recordedBy: string }
  | { type: "captation/close"; id: string; reason: string }
  | { type: "captation/reactivate"; id: string }
  | { type: "opportunity/change-stage"; id: string; stage: PipelineStage; reason?: string }
  | { type: "opportunity/create"; item: CommercialPipelineItem }
  | { type: "opportunity/reassign"; id: string; owner: string }
  | { type: "opportunity/close"; id: string; outcome: "won" | "lost"; closeDate?: string; finalValue?: number; reason?: string }
  | { type: "demand/create"; item: DemoDemand }
  | { type: "demand/update-criterion"; demandId: string; criterionId: string; value: string; weight: CriterionWeight }
  | { type: "demand/update-status"; id: string; status: DemoDemand["status"]; reason?: string }
  | { type: "match/set-status"; demandId: string; propertyId: string; status: DemoMatchAction["status"]; reason?: string }
  | { type: "ai/review"; id: string }
  | { type: "ai/dismiss"; id: string }
  | { type: "offline/enqueue"; item: OfflineQueueItem }
  | { type: "offline/flush" }
  | { type: "activity/add"; item: DemoActivity }
  | { type: "activity/update"; id: string; changes: Partial<DemoActivity> }
  | { type: "reservation/create"; item: DemoReservation }
  | { type: "reservation/cancel"; id: string; reason?: string }
  | { type: "proposal/submit"; item: DemoProposalRecord }
  | { type: "proposal/resolve"; proposalId?: string; outcome: "accepted" | "rejected"; reason: string }
  | { type: "operation/create"; item: DemoOperation }
  | { type: "operation/advance"; id: string; stage: DemoOperation["stage"] }
  | { type: "operation/cancel"; id: string; reason?: string }
  | { type: "operation/close"; id: string; closeDate?: string; finalValue?: number }
  | { type: "user/invite"; item: DemoState["users"][number] }
  | { type: "user/update"; id: string; changes: Partial<DemoState["users"][number]> }
  | { type: "catalog/update-entry"; id: string; label: string; status: DemoCatalogEntry["status"] }
  | { type: "hydrate"; state: DemoState };

export function demoReducer(state: DemoState, action: DemoAction): DemoState {
  switch (action.type) {
    case "hydrate":
      return normalizeDemoState(action.state);
    case "contact/create":
      return { ...state, contacts: [...state.contacts, action.item] };
    case "contact/update":
      return { ...state, contacts: state.contacts.map((contact) => contact.id === action.id ? { ...contact, ...action.changes } : contact) };
    case "party/relate": {
      const contact = state.contacts.find((party) => party.id === action.contactId && party.kind === "Persona");
      const company = state.contacts.find((party) => party.id === action.companyId && party.kind === "Empresa");
      if (!contact || !company || state.relationships.some((relationship) => relationship.fromPartyId === contact.id && relationship.toPartyId === company.id && relationship.relationshipType === "CONTACT_OF")) return state;
      return { ...state, relationships: [...state.relationships, { id: `relationship-${Date.now()}`, fromPartyId: contact.id, toPartyId: company.id, relationshipType: "CONTACT_OF", createdAt: now, createdBy: "user-1" }] };
    }
    case "property/create":
      return { ...state, properties: [...state.properties, action.item] };
    case "property/update":
      return { ...state, properties: state.properties.map((property) => property.id === action.id ? { ...property, ...action.changes } : property) };
    case "listing/create":
      return { ...state, listings: [...state.listings, action.item] };
    case "listing/update":
      return { ...state, listings: state.listings.map((listing) => listing.id === action.id ? { ...listing, ...action.changes } : listing) };
    case "captation/create":
      return { ...state, captations: [...state.captations, action.item] };
    case "captation/update-stage":
      return { ...state, captations: state.captations.map((captation) => captation.id === action.id ? { ...captation, stage: action.stage } : captation) };
    case "captation/update-valuation":
      return { ...state, captations: state.captations.map((captation) => captation.id === action.id ? { ...captation, valuation: action.valuation } : captation) };
    case "captation/issue-valuation": {
      const current = state.captations.find((captation) => captation.id === action.id);
      if (!current || !Number.isFinite(action.value) || action.value < 0) return state;
      const historyEntry = { id: `valuation-${Date.now()}`, value: action.value, currency: action.currency, recordedAt: now, recordedBy: action.recordedBy };
      return { ...state, captations: state.captations.map((captation) => captation.id === action.id ? { ...captation, valuation: action.value, currency: action.currency, valuationHistory: [...(captation.valuationHistory ?? []), historyEntry], stage: captation.stage === "Nueva" || captation.stage === "Tasación" ? "Tasación" : captation.stage } : captation) };
    }
    case "captation/close": {
      const current = state.captations.find((captation) => captation.id === action.id);
      if (!current || current.stage === "Cerrada" || !validateReason(action.reason, "cerrar la captación").valid) return state;
      return { ...state, captations: state.captations.map((captation) => captation.id === action.id ? { ...captation, stage: "Cerrada", closeReason: action.reason.trim(), closedAt: now } : captation) };
    }
    case "captation/reactivate":
      return { ...state, captations: state.captations.map((captation) => captation.id === action.id && captation.stage === "Cerrada" ? { ...captation, stage: "Nueva", closeReason: undefined, closedAt: undefined } : captation) };
    case "opportunity/change-stage": {
      const current = state.opportunities.find((item) => item.id === action.id);
      if (!current || !validateOpportunityStageChange(current.stage, action.stage, action.reason).valid) return state;
      return {
        ...state,
        opportunities: state.opportunities.map((item) => item.id === action.id ? { ...item, stage: action.stage, daysInStage: 0 } : item),
        stageHistory: [...state.stageHistory, { id: `history-${Date.now()}`, opportunityId: action.id, from: current.stage, to: action.stage, actor: "Martín Quiroga", at: now, reason: action.reason }],
      };
    }
    case "opportunity/create":
      return { ...state, opportunities: [...state.opportunities, action.item] };
    case "opportunity/reassign":
      return { ...state, opportunities: state.opportunities.map((item) => item.id === action.id ? { ...item, owner: action.owner } : item) };
    case "opportunity/close": {
      const current = state.opportunities.find((item) => item.id === action.id);
      const validation = validateOpportunityClose(action.outcome, action.closeDate, action.finalValue, action.reason);
      if (!current || !validation.valid) return state;
      const closeDate = action.closeDate ?? now;
      return {
        ...state,
        opportunities: state.opportunities.map((item) => item.id === action.id ? { ...item, stage: "Cerrada", outcome: action.outcome, closedAt: closeDate, finalValue: action.finalValue, closeReason: action.reason, daysInStage: 0 } : item),
        stageHistory: [...state.stageHistory, { id: `history-${Date.now()}`, opportunityId: action.id, from: current.stage, to: "Cerrada", actor: "Martín Quiroga", at: now, reason: action.outcome === "won" ? "Cierre ganado" : action.reason, outcome: action.outcome, closeDate, finalValue: action.finalValue }],
      };
    }
    case "demand/create":
      return { ...state, demands: [...state.demands, { ...action.item, score: calculateDemandScore(action.item.criteria) }] };
    case "demand/update-criterion": {
      const demands = state.demands.map((demand) => {
        if (demand.id !== action.demandId) return demand;
        const criteria = demand.criteria.map((criterion) => criterion.id === action.criterionId ? { ...criterion, value: action.value, weight: action.weight } : criterion);
        return { ...demand, criteria, score: calculateDemandScore(criteria) };
      });
      return { ...state, demands };
    }
    case "demand/update-status": {
      const current = state.demands.find((demand) => demand.id === action.id);
      if (!current || (action.status === "Cerrada" && !validateReason(action.reason, "cerrar la búsqueda").valid)) return state;
      return { ...state, demands: state.demands.map((demand) => demand.id === action.id ? { ...demand, status: action.status, closeReason: action.reason?.trim(), closedAt: action.status === "Cerrada" ? now : undefined } : demand) };
    }
    case "match/set-status": {
      const existing = state.matchActions.find((item) => item.demandId === action.demandId && item.propertyId === action.propertyId);
      const next = { id: existing?.id ?? `match-${action.demandId}-${action.propertyId}`, demandId: action.demandId, propertyId: action.propertyId, status: action.status, reason: action.reason };
      const demands = state.demands.map((demand) => {
        if (demand.id !== action.demandId) return demand;
        const selectedListingIds = new Set(demand.selectedListingIds ?? []);
        if (action.status === "selected") selectedListingIds.add(action.propertyId);
        if (action.status === "dismissed") selectedListingIds.delete(action.propertyId);
        return { ...demand, selectedListingIds: [...selectedListingIds] };
      });
      return { ...state, demands, matchActions: existing ? state.matchActions.map((item) => item.id === existing.id ? next : item) : [...state.matchActions, next] };
    }
    case "ai/review":
      return { ...state, aiSuggestions: state.aiSuggestions.map((suggestion) => suggestion.id === action.id ? { ...suggestion, status: "reviewed", updatedAt: now } : suggestion) };
    case "ai/dismiss":
      return { ...state, aiSuggestions: state.aiSuggestions.map((suggestion) => suggestion.id === action.id ? { ...suggestion, status: "dismissed", updatedAt: now } : suggestion) };
    case "offline/enqueue":
      return { ...state, offlineQueue: [...state.offlineQueue, action.item] };
    case "offline/flush":
      return { ...state, offlineQueue: [] };
    case "activity/add": {
      const historical = isHistoricalActivityType(action.item.type);
      if (!validateActivity({ activityType: action.item.type, occurredAt: action.item.occurredAt, actor: action.item.actor, relatedRecordId: action.item.relatedRecordId }).valid || !action.item.subject.trim() || !action.item.body.trim() || !activityRelationExists(state, action.item)) return state;
      return { ...state, activities: [{ ...action.item, historical }, ...state.activities] };
    }
    case "activity/update": {
      const current = state.activities.find((activity) => activity.id === action.id);
      if (!current) return state;
      const next = { ...current, ...action.changes };
      if (!validateActivity({ activityType: next.type, occurredAt: next.occurredAt, actor: next.actor, relatedRecordId: next.relatedRecordId }).valid || !next.subject.trim() || !next.body.trim() || !activityRelationExists(state, next)) return state;
      return { ...state, activities: state.activities.map((activity) => activity.id === action.id ? { ...next, historical: isHistoricalActivityType(next.type) } : activity) };
    }
    case "reservation/create":
      return { ...state, reservations: [action.item, ...state.reservations] };
    case "reservation/cancel": {
      const current = state.reservations.find((reservation) => reservation.id === action.id);
      if (!current || current.status !== "Activa" || !validateReason(action.reason, "cancelar la reserva").valid) return state;
      return { ...state, reservations: state.reservations.map((reservation) => reservation.id === action.id ? { ...reservation, status: "Cancelada", cancellationReason: action.reason?.trim(), cancelledAt: now } : reservation) };
    }
    case "proposal/submit": {
      const item = action.item;
      if (!item.id || !item.opportunityId || !Number.isInteger(item.sequence) || item.sequence < 1 || state.proposals.some((proposal) => proposal.id === item.id)) return state;
      return { ...state, proposals: [...state.proposals, { ...item, conditions: item.conditions.trim() }], proposal: { outcome: "pending", reason: "" } };
    }
    case "proposal/resolve": {
      if (!validateReason(action.reason, "resolver la propuesta").valid) return state;
      const current = action.proposalId ? state.proposals.find((proposal) => proposal.id === action.proposalId) : state.proposals.at(-1);
      if (!current) {
        if (state.proposal.outcome !== "pending") return state;
        return { ...state, proposal: { outcome: action.outcome, reason: action.reason.trim() } };
      }
      if (current.outcome !== "pending") return state;
      const resolved = { ...current, outcome: action.outcome, reason: action.reason.trim(), respondedAt: now } satisfies DemoProposalRecord;
      return {
        ...state,
        proposals: state.proposals.map((proposal) => proposal.id === current.id ? resolved : proposal),
        proposal: { outcome: action.outcome, reason: action.reason.trim() },
      };
    }
    case "operation/create":
      return { ...state, operations: [...state.operations, action.item] };
    case "operation/advance": {
      const current = state.operations.find((operation) => operation.id === action.id);
      if (!current || !validateOperationTransition(current.stage, action.stage).valid) return state;
      return { ...state, operations: state.operations.map((operation) => operation.id === action.id ? { ...operation, stage: action.stage } : operation) };
    }
    case "operation/cancel": {
      const current = state.operations.find((operation) => operation.id === action.id);
      if (!current || current.stage === "Cerrada" || current.stage === "Cancelada" || !validateReason(action.reason, "cancelar la operación").valid) return state;
      return { ...state, operations: state.operations.map((operation) => operation.id === action.id ? { ...operation, stage: "Cancelada", outcome: "CANCELLED", cancellationReason: action.reason?.trim(), closedAt: now } : operation) };
    }
    case "operation/close": {
      const current = state.operations.find((operation) => operation.id === action.id);
      if (!current || !validateOperationClose(action.closeDate, action.finalValue).valid || current.stage === "Cerrada" || current.stage === "Cancelada") return state;
      return { ...state, operations: state.operations.map((operation) => operation.id === action.id ? { ...operation, stage: "Cerrada", outcome: "CLOSED", closeDate: action.closeDate, finalValue: action.finalValue, closedAt: now } : operation) };
    }
    case "user/invite":
      return { ...state, users: [...state.users, action.item] };
    case "user/update":
      return { ...state, users: state.users.map((user) => user.id === action.id ? { ...user, ...action.changes } : user) };
    case "catalog/update-entry":
      return { ...state, catalogEntries: state.catalogEntries.map((entry) => entry.id === action.id ? { ...entry, label: action.label, status: action.status } : entry), catalogVersion: state.catalogVersion + 1 };
    default:
      return state;
  }
}

const DemoStoreContext = createContext<{ state: DemoState; dispatch: Dispatch<DemoAction> } | null>(null);
export const demoStorageKey = "crm-web:demo-store:v1";

function arrayOrDefault<T>(value: unknown, fallback: T[]): T[] {
  return Array.isArray(value) ? value as T[] : fallback;
}

function normalizeCommercialStatus(contact: Partial<DemoContact>): CommercialStatus {
  const candidate = contact.commercialStatus;
  if (typeof candidate === "string" && (COMMERCIAL_STATUS_VALUES as readonly string[]).includes(candidate)) return candidate as CommercialStatus;
  if (contact.status === "No contactar") return "DO_NOT_CONTACT";
  if (contact.status === "Archivado") return "INACTIVE";
  return "CUSTOMER";
}

function normalizeContact(contact: DemoContact): DemoContact {
  const commercialStatus = normalizeCommercialStatus(contact);
  const identityStatus = contact.identityStatus === "VERIFIED" || contact.identityStatus === "PENDING" || contact.identityStatus === "UNKNOWN" ? contact.identityStatus : "UNKNOWN";
  const status: DemoContact["status"] = commercialStatus === "DO_NOT_CONTACT" ? "No contactar" : commercialStatus === "INACTIVE" ? "Archivado" : "Activo";
  return { ...contact, status, commercialStatus, identityStatus, origin: contact.origin ?? "Origen desconocido" };
}

function normalizeActivity(activity: DemoActivity): DemoActivity {
  return {
    ...activity,
    occurredAt: activity.occurredAt ?? activity.createdAt,
    historical: activity.historical ?? isHistoricalActivityType(activity.type),
  };
}

function isHistoricalActivityType(type: DemoActivityType): boolean {
  return ["Email", "Email histórico", "WhatsApp", "WhatsApp histórico", "Correo electrónico", "Mensaje"].includes(type);
}

function catalogTypeFromLegacyId(id: string): CatalogFamily {
  if (id.startsWith("stage-")) return "pipeline-stage";
  if (id.startsWith("activity-")) return "activity-type";
  if (id.startsWith("origin-")) return "origin";
  if (id.startsWith("loss-")) return "loss-reason";
  if (id.startsWith("operation-")) return "operation-type";
  return "property-type";
}

function activityRelationExists(state: DemoState, activity: DemoActivity): boolean {
  const relationId = activity.relatedRecordId?.trim();
  if (!relationId) return false;
  let primaryExists: boolean;
  if (!activity.relatedRecordType) {
    primaryExists = [
      ...state.contacts,
      ...state.properties,
      ...state.listings,
      ...state.captations,
      ...state.demands,
      ...state.opportunities,
      ...state.reservations,
      ...state.operations,
    ].some((record) => record.id === relationId);
  }
  else {
    const recordsByType: Record<NonNullable<DemoActivity["relatedRecordType"]>, readonly { id: string }[]> = {
      Party: state.contacts,
      Property: state.properties,
      Listing: state.listings,
      Demand: state.demands,
      Opportunity: state.opportunities,
      Captation: state.captations,
      Reservation: state.reservations,
      Operation: state.operations,
    };
    primaryExists = recordsByType[activity.relatedRecordType].some((record) => record.id === relationId);
  }
  if (!primaryExists) return false;
  if (activity.propertyId && !state.properties.some((property) => property.id === activity.propertyId)) return false;
  if (activity.listingId && !state.listings.some((listing) => listing.id === activity.listingId)) return false;
  if (activity.pipelineItemId && !state.opportunities.some((opportunity) => opportunity.id === activity.pipelineItemId)) return false;
  return true;
}

function normalizeOwnerName(owner: string | undefined): string {
  if (!owner || owner === "Martin Quiroga") return "Martín Quiroga";
  return owner;
}

function normalizeCatalogEntries(rawEntries: unknown): DemoState["catalogEntries"] {
  if (!Array.isArray(rawEntries) || rawEntries.length === 0) {
    return demoInitialState.catalogEntries;
  }
  const mapped: DemoState["catalogEntries"] = rawEntries.map((raw: Record<string, unknown>, idx: number) => {
    const id = String(raw.id ?? raw.entryId ?? `cat-${idx}`);
    const rawType = String(raw.catalogType ?? "");
    const catalogType: CatalogFamily =
      rawType === "commercial-stage" || rawType === "pipeline-stage" ? "pipeline-stage" :
      rawType === "commercial-origin" || rawType === "origin" ? "origin" :
      rawType === "activity-type" ? "activity-type" :
      rawType === "loss-reason" ? "loss-reason" :
      rawType === "operation-type" ? "operation-type" :
      rawType === "property-type" ? "property-type" :
      catalogTypeFromLegacyId(id);
    const label = String(raw.label ?? raw.name ?? raw.code ?? id);
    const status: DemoCatalogEntry["status"] =
      raw.status === "Cierre comercial" ? "Cierre comercial" :
      raw.status === "Inactivo" || raw.isActive === false ? "Inactivo" : "Activo";
    return {
      id,
      catalogType,
      label,
      status,
      semanticState: raw.semanticState as DemoState["catalogEntries"][number]["semanticState"],
    };
  });
  const families: CatalogFamily[] = ["pipeline-stage", "activity-type", "origin", "loss-reason", "operation-type", "property-type"];
  const missingDefaults = demoInitialState.catalogEntries.filter(
    (def) => !families.some((fam) => fam === def.catalogType && mapped.some((m) => m.catalogType === fam && m.status === "Activo"))
  );
  return [...mapped, ...missingDefaults];
}

const knownPropertyMetadata: Record<string, { title: string; address: string; type: DemoProperty["type"] }> = {
  "PROP-101": { title: "Departamento en Gorriti 4800 · Palermo Soho", address: "Gorriti 4800, Palermo Soho", type: "Departamento" },
  "PROP-102": { title: "Casa en Av. del Libertador 16200 · San Isidro", address: "Av. del Libertador 16200, Las Lomas", type: "Casa" },
  "PROP-103": { title: "Oficina en Av. Leandro N. Alem 850 · Retiro", address: "Av. Leandro N. Alem 850, Retiro", type: "Local" },
  "PROP-104": { title: "Departamento en Juana Manso 1100 · Puerto Madero", address: "Juana Manso 1100, Puerto Madero", type: "Departamento" },
  "PROP-105": { title: "Local comercial en Zapiola 2100 · Belgrano R", address: "Zapiola 2100, Belgrano R", type: "Local" },
  "PROP-106": { title: "Departamento en Av. Las Heras 2300 · Recoleta", address: "Av. Las Heras 2300, Recoleta", type: "Departamento" },
  "PROP-107": { title: "Lote en Av. de los Lagos 500 · Nordelta", address: "Av. de los Lagos 500, Nordelta", type: "Terreno" },
  "PROP-108": { title: "Casa en Maipú 1400 · Olivos", address: "Maipú 1400, Olivos", type: "Casa" },
  "PROP-109": { title: "Departamento en Av. Pedro Goyena 900 · Caballito", address: "Av. Pedro Goyena 900, Caballito", type: "Departamento" },
  "PROP-110": { title: "Casa en Triunvirato 4500 · Villa Urquiza", address: "Triunvirato 4500, Villa Urquiza", type: "Casa" },
};

function normalizeProperty(property: DemoProperty): DemoProperty {
  const known = knownPropertyMetadata[property.id];
  const rawTitle = (property.title ?? "").trim();
  const isGenericTitle = !rawTitle || rawTitle.toLowerCase() === "inmueble";
  const rawAddress = (property.address ?? "").trim();
  const isGenericAddress = !rawAddress || rawAddress.toLowerCase() === "capital federal";
  const normalizedType: DemoProperty["type"] =
    (property.type as string) === "APARTMENT" ? "Departamento" :
    (property.type as string) === "HOUSE" ? "Casa" :
    (property.type as string) === "COMMERCIAL" ? "Local" :
    (property.type as string) === "LAND" ? "Terreno" :
    property.type ?? known?.type ?? "Departamento";
  const address = isGenericAddress && known ? known.address : (rawAddress || "Capital Federal");
  const title = isGenericTitle
    ? (known?.title ?? (address && address !== "Capital Federal" ? `${normalizedType} en ${address}` : `${normalizedType} · ${property.id}`))
    : rawTitle;
  return {
    ...property,
    title,
    address,
    type: normalizedType,
    ownerPartyIds: property.ownerPartyIds ?? [],
  };
}

export function normalizeDemoState(snapshot: unknown): DemoState {
  if (!snapshot || typeof snapshot !== "object") return demoInitialState;
  const candidate = snapshot as Partial<DemoState>;
  return {
    ...demoInitialState,
    ...candidate,
    contacts: arrayOrDefault(candidate.contacts, demoInitialState.contacts).map(normalizeContact),
    relationships: arrayOrDefault(candidate.relationships, demoInitialState.relationships),
    properties: arrayOrDefault(candidate.properties, demoInitialState.properties).map(normalizeProperty),
    listings: arrayOrDefault(candidate.listings, demoInitialState.listings),
    captations: arrayOrDefault(candidate.captations, demoInitialState.captations).map((captation) => ({ ...captation, owner: normalizeOwnerName(captation.owner), origin: captation.origin ?? "Origen desconocido", valuationHistory: captation.valuationHistory ?? [] })),
    demands: arrayOrDefault(candidate.demands, demoInitialState.demands).map((demand) => ({ ...demand, criteria: Array.isArray(demand.criteria) ? demand.criteria : [], origin: demand.origin ?? "Origen desconocido", selectedListingIds: demand.selectedListingIds ?? [] })),
    opportunities: arrayOrDefault(candidate.opportunities, demoInitialState.opportunities).map((opp) => ({ ...opp, owner: normalizeOwnerName(opp.owner) })),
    activities: arrayOrDefault(candidate.activities, demoInitialState.activities).map(normalizeActivity),
    reservations: arrayOrDefault(candidate.reservations, demoInitialState.reservations),
    operations: arrayOrDefault(candidate.operations, demoInitialState.operations),
    proposals: arrayOrDefault(candidate.proposals, demoInitialState.proposals),
    proposal: candidate.proposal ?? demoInitialState.proposal,
    stageHistory: arrayOrDefault(candidate.stageHistory, demoInitialState.stageHistory),
    offlineQueue: arrayOrDefault(candidate.offlineQueue, demoInitialState.offlineQueue),
    matchActions: arrayOrDefault(candidate.matchActions, demoInitialState.matchActions),
    aiSuggestions: arrayOrDefault(candidate.aiSuggestions, demoInitialState.aiSuggestions),
    catalogEntries: normalizeCatalogEntries(candidate.catalogEntries),
    users: arrayOrDefault(candidate.users, demoInitialState.users).map((user: Record<string, unknown>) => ({
      id: String(user.id ?? user.userId ?? `usr-${Math.random()}`),
      name: String(user.name ?? user.displayName ?? "Usuario"),
      email: String(user.email ?? "usuario@inmobiliaria.com"),
      role: (user.role as DemoState["users"][number]["role"]) ?? "Vendedor",
      status: (user.status as DemoState["users"][number]["status"]) ?? "Habilitado",
    })),
  };
}

export function readDemoState(storage: Pick<Storage, "getItem" | "removeItem">): DemoState | null {
  const stored = storage.getItem(demoStorageKey);
  if (!stored) return null;
  try {
    return normalizeDemoState(JSON.parse(stored));
  } catch {
    storage.removeItem(demoStorageKey);
    return null;
  }
}

export function persistDemoState(storage: Pick<Storage, "setItem">, state: DemoState): void {
  storage.setItem(demoStorageKey, JSON.stringify(state));
}

export function DemoStoreProvider({ active = true, children }: { active?: boolean; children: ReactNode }) {
  const [state, dispatch] = useReducer(
    demoReducer,
    demoInitialState,
    (initial) => {
      if (typeof window !== "undefined") {
        try {
          const stored = readDemoState(window.localStorage);
          if (stored) return stored;
        } catch {
          // ignore
        }
      }
      return initial;
    }
  );

  useEffect(() => {
    if (typeof window === "undefined") return;
    try {
      persistDemoState(window.localStorage, state);
    } catch {
      // ignore
    }
  }, [state]);

  const value = useMemo(() => ({ state, dispatch }), [state]);
  return createElement(DemoStoreContext.Provider, { value }, children);
}

export function useDemoStore(): { state: DemoState; dispatch: Dispatch<DemoAction> } {
  const context = useContext(DemoStoreContext);
  if (!context) throw new Error("useDemoStore must be used inside DemoStoreProvider");
  return context;
}
