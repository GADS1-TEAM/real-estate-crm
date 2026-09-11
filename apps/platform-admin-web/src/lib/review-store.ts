import type { Dispatch } from "react";
import { buildAuditEvidence } from "@/lib/audit-rules";
import { getRole } from "@/lib/permissions";
import type { AuditEntry, ReviewState } from "@/lib/types";

export const reviewStorageKey = "platform-admin:review-store:v1";

export type ReviewAction =
  | { type: "state/hydrate"; state: ReviewState }
  | { type: "draft/create"; artifactId: string; baseVersion: number }
  | { type: "draft/update"; artifactId: string; field: string; value?: unknown }
  | { type: "draft/discard"; artifactId: string }
  | { type: "artifact/review"; artifactId: string }
  | { type: "artifact/publish"; artifactId: string }
  | { type: "artifact/deprecate"; artifactId: string }
  | { type: "artifact/select"; artifactId: string }
  | { type: "tenant/select"; tenantId: string }
  | { type: "audit/select"; auditId: string }
  | { type: "flag/toggle"; key: string }
  | { type: "pilot/create"; pilot: Omit<import("@/lib/types").Pilot, "id" | "status" | "progress"> }
  | { type: "pilot/advance"; pilotId: string; progress: number }
  | { type: "journey/fix"; journey: keyof ReviewState["journeyFixes"] }
  | { type: "validation/accept-warning"; code: string; reason: string }
  | { type: "validation/focus"; field: string | null }
  | { type: "role/select"; roleId: string }
  | { type: "environment/select"; environment: ReviewState["environment"] }
  | { type: "toast/show"; message: string }
  | { type: "toast/clear" }
  | { type: "validation/toggle" }
  | { type: "correction/audit"; command: string; targetId: string; reason: string };

export const reviewInitialState: ReviewState = {
  published: {
    "pack-argentina": { id: "pack-argentina", type: "pack", name: "Argentina Base Pack", version: 8, status: "PUBLISHED", publishedAt: "7 sep 2026, 16:20" },
    "workflow-sale": { id: "workflow-sale", type: "workflow", name: "Residential sale", version: 7, status: "PUBLISHED", publishedAt: "5 sep 2026, 11:05" },
  },
  drafts: {
    "pack-argentina": { id: "pack-argentina", type: "pack", name: "Argentina Base Pack", version: 9, status: "DRAFT", changedFields: ["workflowTemplate", "commissionPolicy"], updatedAt: "hace 12 min" },
    "pack-automation": { id: "pack-automation", type: "pack", name: "Automation Starter", version: 4, status: "IN_REVIEW", changedFields: ["guardrails"], updatedAt: "hace 5 días" },
  },
  draftValues: {
    "pack-argentina": {
      name: "Argentina Base Pack",
      market: "Argentina",
      description: "Defaults operativos para inmobiliarias argentinas.",
      publicationStatus: "DRAFT",
    },
  },
  acceptedWarnings: {},
  focusedField: null,
  selectedArtifactId: "pack-argentina",
  selectedTenantId: "norte",
  selectedAuditId: "audit-1",
  featureFlags: { effective_config: true, new_workflow: false, automation_guardrails: true, lineage_panel: false },
  journeyFixes: {},
  unknownValue: "UNKNOWN",
  selectedRole: "release-manager",
  environment: "Development",
  toast: null,
  toastAuditId: null,
  validationOpen: false,
  session: { userId: "rodrigo-vergara", name: "Rodrigo Vergara", roleLabel: "Release manager", permissions: ["VIEW", "EDIT_DRAFT", "PUBLISH", "DEPRECATE", "MANAGE_PILOTS", "PROMOTE"], status: "authenticated" },
  pilots: [{ id: "pilot-rental", name: "Administración de alquileres", capability: "RENTAL_ADMINISTRATION", version: 3, tenants: ["Inmobiliaria Norte", "Pepito Propiedades", "Vergara & Asociados"], status: "RUNNING", progress: 62 }],
  audit: [{ id: "audit-1", action: "Publicó versión", artifact: "Argentina Base Pack v8", actor: "Rodrigo Vergara", reason: "Cierre de la versión inicial", timestamp: "7 sep 2026, 16:20", correlationId: "corr-8b12", before: { version: 7, status: "IN_REVIEW" }, after: { version: 8, status: "PUBLISHED" } }],
  tenantRefs: [
    { id: "pepito", name: "Pepito Propiedades", location: "Palermo", capabilityState: "PILOT" },
    { id: "vergara", name: "Vergara & Asociados", location: "Belgrano", capabilityState: "ENABLED" },
    { id: "norte", name: "Inmobiliaria Norte", location: "Villa Crespo", capabilityState: "ENABLED" },
  ],
  effectiveConfiguration: { context: "Inmobiliaria Norte · Palermo · Compraventa", pack: "Argentina Base Pack v8", defaultValue: "4 %", tenantOverride: "3 %", effectiveValue: "3 %", why: "El override de la inmobiliaria gana dentro del mínimo y máximo publicados para esta política." },
};

export function reviewReducer(state: ReviewState, action: ReviewAction): ReviewState {
  switch (action.type) {
    case "state/hydrate":
      return mergeReviewState(action.state);
    case "draft/create": {
      const published = state.published[action.artifactId];
      const currentDraft = state.drafts[action.artifactId];
      if (currentDraft) return state;
      return {
        ...state,
        drafts: {
          ...state.drafts,
          [action.artifactId]: {
            id: action.artifactId,
            type: published?.type ?? "pack",
            name: published?.name ?? action.artifactId,
            version: action.baseVersion + 1,
            status: "DRAFT",
            changedFields: [],
            updatedAt: "ahora",
          },
        },
        draftValues: { ...state.draftValues, [action.artifactId]: state.draftValues[action.artifactId] ?? {} },
        toast: "Se creó un nuevo borrador. La versión publicada no cambió.",
        toastAuditId: null,
      };
    }
    case "draft/update": {
      const draft = state.drafts[action.artifactId];
      if (!draft) return state;
      return {
        ...state,
        drafts: { ...state.drafts, [action.artifactId]: { ...draft, changedFields: [...new Set([...draft.changedFields, action.field])], updatedAt: "ahora" } },
        draftValues: { ...state.draftValues, [action.artifactId]: { ...(state.draftValues[action.artifactId] ?? {}), [action.field]: action.value } },
        toast: "Borrador guardado.",
        toastAuditId: null,
      };
    }
    case "draft/discard": {
      const drafts = { ...state.drafts };
      const draftValues = { ...state.draftValues };
      delete drafts[action.artifactId];
      delete draftValues[action.artifactId];
      return { ...state, drafts, draftValues, toast: "Cambios descartados. La versión publicada no cambió.", toastAuditId: null };
    }
    case "artifact/review": {
      const draft = state.drafts[action.artifactId];
      if (!draft || draft.status === "IN_REVIEW") return state;
      const nextDraft = { ...draft, status: "IN_REVIEW" as const, updatedAt: "ahora" };
      const next = { ...state, drafts: { ...state.drafts, [action.artifactId]: nextDraft } };
      return withAudit(next, { action: "Envió a revisión", artifact: draft.name, reason: "Revisión solicitada desde el editor", before: draft, after: nextDraft, toast: "Borrador enviado a revisión." });
    }
    case "artifact/publish": {
      const draft = state.drafts[action.artifactId];
      if (!draft) return state;
      const previous = state.published[action.artifactId] ?? null;
      const publishedArtifact = { id: draft.id, type: draft.type, name: draft.name, version: draft.version, status: "PUBLISHED" as const, publishedAt: "ahora" };
      const draftValues = { ...state.draftValues };
      delete draftValues[action.artifactId];
      const next = {
        ...state,
        published: { ...state.published, [action.artifactId]: publishedArtifact },
        drafts: Object.fromEntries(Object.entries(state.drafts).filter(([id]) => id !== action.artifactId)),
        draftValues,
        toast: "Versión publicada. Las instancias históricas no cambiaron.",
      };
      return withAudit(next, { action: "Publicó versión", artifact: publishedArtifact.name, reason: "Publicación confirmada desde revisión", before: previous, after: publishedArtifact });
    }
    case "artifact/deprecate": {
      const published = state.published[action.artifactId];
      if (!published) return state;
      const deprecated = { ...published, status: "DEPRECATED" as const };
      const next = { ...state, published: { ...state.published, [action.artifactId]: deprecated }, toast: "Artefacto deprecado. Sus instancias históricas se conservan." };
      return withAudit(next, { action: "Deprecó artefacto", artifact: published.name, reason: "Deprecación confirmada desde Platform Admin", before: published, after: deprecated });
    }
    case "artifact/select":
      return { ...state, selectedArtifactId: action.artifactId };
    case "tenant/select":
      return { ...state, selectedTenantId: action.tenantId };
    case "audit/select":
      return { ...state, selectedAuditId: action.auditId };
    case "flag/toggle": {
      const current = state.featureFlags[action.key];
      if (typeof current !== "boolean") return state;
      return { ...state, featureFlags: { ...state.featureFlags, [action.key]: !current }, toast: `Flag ${action.key} ${current ? "desactivada" : "activada"}.`, toastAuditId: null };
    }
    case "pilot/create":
      return {
        ...state,
        pilots: [{ ...action.pilot, id: `pilot-${state.pilots.length + 1}`, status: "DRAFT", progress: 0 }, ...state.pilots],
        toast: "Piloto creado como borrador.",
        toastAuditId: null,
      };
    case "pilot/advance":
      return {
        ...state,
        pilots: state.pilots.map((pilot) => pilot.id !== action.pilotId ? pilot : { ...pilot, progress: action.progress, status: action.progress >= 100 ? "COMPLETED" : "RUNNING" }),
        toast: "Rollout actualizado.",
        toastAuditId: null,
      };
    case "journey/fix":
      return { ...state, journeyFixes: { ...state.journeyFixes, [action.journey]: true }, toast: `Se resolvió el bloqueo de ${action.journey}.`, toastAuditId: null };
    case "validation/accept-warning":
      return { ...state, acceptedWarnings: { ...state.acceptedWarnings, [action.code]: action.reason }, toast: "Warning aceptado con motivo.", toastAuditId: null };
    case "validation/focus":
      return { ...state, focusedField: action.field };
    case "role/select": {
      const role = getRole(action.roleId);
      return { ...state, selectedRole: role.id, session: { ...state.session, roleLabel: role.name, permissions: role.permissions }, toast: null, toastAuditId: null };
    }
    case "environment/select":
      return { ...state, environment: action.environment, toast: `Ambiente activo: ${action.environment}.`, toastAuditId: null };
    case "toast/show":
      return { ...state, toast: action.message, toastAuditId: null };
    case "toast/clear":
      return { ...state, toast: null, toastAuditId: null };
    case "validation/toggle":
      return { ...state, validationOpen: !state.validationOpen };
    case "correction/audit":
      return withAudit(state, { action: action.command, artifact: action.targetId, reason: action.reason, before: { targetId: action.targetId, status: "unchanged" }, after: { targetId: action.targetId, status: "correction-requested" }, toast: "Corrección registrada y enviada al servicio owner." });
    default:
      return state;
  }
}

function withAudit(state: ReviewState, input: { action: string; artifact: string; reason: string; before: unknown; after: unknown; toast?: string }): ReviewState {
  const evidence = buildAuditEvidence({ action: input.action, actor: state.session.name, reason: input.reason, correlationId: `corr-${state.audit.length + 1}`, before: input.before, after: input.after });
  const entry: AuditEntry = { id: `audit-${state.audit.length + 1}`, ...evidence, artifact: input.artifact, timestamp: "ahora" };
  return { ...state, audit: [entry, ...state.audit], selectedAuditId: entry.id, toastAuditId: entry.id, toast: input.toast ?? state.toast };
}

export function readReviewState(storage: Pick<Storage, "getItem" | "removeItem">): ReviewState | null {
  const raw = storage.getItem(reviewStorageKey);
  if (!raw) return null;
  try {
    const parsed = JSON.parse(raw) as Partial<ReviewState>;
    if (!parsed || typeof parsed !== "object" || !parsed.published || !parsed.drafts) throw new Error("invalid snapshot");
    return mergeReviewState(parsed);
  } catch {
    storage.removeItem(reviewStorageKey);
    return null;
  }
}

function mergeReviewState(snapshot: Partial<ReviewState>): ReviewState {
  return {
    ...reviewInitialState,
    ...snapshot,
    published: { ...reviewInitialState.published, ...(snapshot.published ?? {}) },
    drafts: { ...reviewInitialState.drafts, ...(snapshot.drafts ?? {}) },
    journeyFixes: { ...reviewInitialState.journeyFixes, ...(snapshot.journeyFixes ?? {}) },
    session: { ...reviewInitialState.session, ...(snapshot.session ?? {}) },
    pilots: snapshot.pilots ?? reviewInitialState.pilots,
    audit: (snapshot.audit ?? reviewInitialState.audit).map((entry) => ({ ...entry, before: entry.before ?? "UNKNOWN", after: entry.after ?? "UNKNOWN" })),
    tenantRefs: snapshot.tenantRefs ?? reviewInitialState.tenantRefs,
    effectiveConfiguration: { ...reviewInitialState.effectiveConfiguration, ...(snapshot.effectiveConfiguration ?? {}) },
    draftValues: snapshot.draftValues ?? reviewInitialState.draftValues,
    acceptedWarnings: snapshot.acceptedWarnings ?? reviewInitialState.acceptedWarnings,
    focusedField: snapshot.focusedField ?? reviewInitialState.focusedField,
    selectedArtifactId: snapshot.selectedArtifactId ?? reviewInitialState.selectedArtifactId,
    selectedTenantId: snapshot.selectedTenantId ?? reviewInitialState.selectedTenantId,
    selectedAuditId: snapshot.selectedAuditId ?? reviewInitialState.selectedAuditId,
    featureFlags: snapshot.featureFlags ?? reviewInitialState.featureFlags,
    toastAuditId: snapshot.toastAuditId ?? reviewInitialState.toastAuditId,
  };
}

export function persistReviewState(storage: Pick<Storage, "setItem">, state: ReviewState): void {
  storage.setItem(reviewStorageKey, JSON.stringify(state));
}

export type ReviewDispatch = Dispatch<ReviewAction>;
