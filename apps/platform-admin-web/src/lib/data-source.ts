import type { ArtifactSummary, CorrectionInput, DraftUpdateInput, PlatformSnapshot, PublishInput, ReviewState } from "@/lib/types";
import { persistReviewState, readReviewState, reviewInitialState, reviewReducer } from "@/lib/review-store";

export type PlatformDataSourceKind = "bff" | "review";

export interface PlatformAdminDataSource {
  readonly kind: PlatformDataSourceKind;
  getAvailability(): { available: boolean; reason?: "BFF_URL_MISSING" };
  getSnapshot(): Promise<PlatformSnapshot>;
  saveDraft(input: DraftUpdateInput): Promise<void>;
  validateDraft(artifactId: string): Promise<PlatformSnapshot>;
  publish(input: PublishInput): Promise<void>;
  deprecate(artifactId: string, reason: string): Promise<void>;
  executeCorrection(input: CorrectionInput): Promise<void>;
}

export class PlatformDataSourceError extends Error {
  constructor(public readonly code: string, public readonly status?: number, public readonly correlationId?: string) {
    super(code);
    this.name = "PlatformDataSourceError";
  }
}

export const reviewArtifacts: ArtifactSummary[] = [
  { id: "pack-argentina", type: "pack", name: "Argentina Base Pack", description: "Defaults operativos para inmobiliarias argentinas.", status: "PUBLISHED", publishedVersion: 8, draftVersion: 9, tenantCount: 12, updatedAt: "hace 12 min", warnings: 3 },
  { id: "pack-compliance", type: "pack", name: "Argentina Compliance Pack", description: "Controles documentales y de compliance.", status: "PUBLISHED", publishedVersion: 4, tenantCount: 8, updatedAt: "ayer" },
  { id: "pack-closing", type: "pack", name: "Argentina Closing Pack", description: "Configuración de cierre y operaciones residenciales.", status: "PUBLISHED", publishedVersion: 3, draftVersion: 4, tenantCount: 5, updatedAt: "hace 2 días" },
  { id: "pack-commission", type: "pack", name: "Commission Starter", description: "Reglas iniciales de comisión.", status: "DRAFT", publishedVersion: 2, draftVersion: 3, tenantCount: 12, updatedAt: "hace 3 días" },
  { id: "pack-automation", type: "pack", name: "Automation Starter", description: "Automatizaciones iniciales con guardrails.", status: "IN_REVIEW", publishedVersion: 3, tenantCount: 3, updatedAt: "hace 5 días" },
  { id: "workflow-sale", type: "workflow", name: "Residential sale", description: "Workflow de compraventa residencial.", status: "PUBLISHED", publishedVersion: 7, tenantCount: 12, updatedAt: "hace 1 h" },
  { id: "requirement-identity", type: "document-requirement", name: "Identity document", description: "Documento de identidad vigente.", status: "PUBLISHED", publishedVersion: 6, tenantCount: 12, updatedAt: "ayer" },
  { id: "compliance-kyc", type: "compliance", name: "Compliance KYC", description: "Reglas de apertura y satisfacción de casos.", status: "PUBLISHED", publishedVersion: 4, tenantCount: 8, updatedAt: "hace 2 días" },
  { id: "commission-sale-default", type: "commission", name: "SALE_DEFAULT", description: "Política global de comisión.", status: "PUBLISHED", publishedVersion: 5, tenantCount: 12, updatedAt: "hace 3 días" },
  { id: "automation-lead-routing", type: "automation", name: "Lead routing starter", description: "Sugerencia de asignación de leads.", status: "DRAFT", publishedVersion: 3, draftVersion: 4, tenantCount: 3, updatedAt: "hace 5 días" },
  { id: "metric-close-rate", type: "metric", name: "Close rate", description: "Tasa de cierre con lineage declarado.", status: "PUBLISHED", publishedVersion: 4, tenantCount: 12, updatedAt: "hace 4 días" },
  { id: "catalog-property-types", type: "catalog", name: "Property types", description: "Catálogo base de tipos de inmueble.", status: "PUBLISHED", publishedVersion: 5, tenantCount: 12, updatedAt: "hace 1 día" },
  { id: "connector-whatsapp", type: "connector", name: "WhatsApp", description: "Canal disponible para el CRM.", status: "PUBLISHED", publishedVersion: 2, updatedAt: "hace 1 día" },
  { id: "flag-effective-config", type: "feature-flag", name: "effective_config", description: "Inspector de configuración efectiva.", status: "PUBLISHED", publishedVersion: 1, updatedAt: "hace 2 h" },
  { id: "schema-property-extensions", type: "extension-schema", name: "property.extensions", description: "Extensiones tipadas de Property.", status: "PUBLISHED", publishedVersion: 3, updatedAt: "hace 3 días" },
];

export const reviewImpact = { tenantsAffected: 12, operationsAffected: 18, blockers: 7, warnings: 1, reasons: ["Un workflow draft sigue referenciado por un pack."] };

const emptySnapshot = (state: ReviewState): PlatformSnapshot => ({ artifacts: reviewArtifacts, impact: reviewImpact, state });

export class ReviewPlatformAdminDataSource implements PlatformAdminDataSource {
  readonly kind = "review" as const;
  private state: ReviewState;

  constructor(state: ReviewState = reviewInitialState, private readonly storage?: Pick<Storage, "getItem" | "setItem" | "removeItem">) {
    this.state = storage ? readReviewState(storage) ?? state : state;
  }

  getAvailability() { return { available: true }; }
  async getSnapshot() { return emptySnapshot(this.state); }
  async saveDraft(input: DraftUpdateInput) { this.commit({ type: "draft/update", artifactId: input.artifactId, field: input.field, value: input.value }); }
  async validateDraft() { return emptySnapshot(this.state); }
  async publish(input: PublishInput) { this.commit({ type: "artifact/publish", artifactId: input.artifactId }); }
  async deprecate(artifactId: string) { this.commit({ type: "artifact/deprecate", artifactId }); }
  async executeCorrection(input: CorrectionInput) { this.commit({ type: "correction/audit", command: input.commandId, targetId: input.targetId, reason: input.reason }); }

  private commit(action: Parameters<typeof reviewReducer>[1]) {
    this.state = reviewReducer(this.state, action);
    if (this.storage) persistReviewState(this.storage, this.state);
  }
}

export class BffPlatformAdminDataSource implements PlatformAdminDataSource {
  readonly kind = "bff" as const;
  constructor(private readonly baseUrl?: string, private readonly environment = process.env.NEXT_PUBLIC_PLATFORM_ADMIN_ENVIRONMENT ?? "Development") {}

  getAvailability() { return this.baseUrl?.trim() ? { available: true } : { available: false, reason: "BFF_URL_MISSING" as const }; }

  private async request<T>(path: string, init?: RequestInit): Promise<T> {
    if (!this.baseUrl?.trim()) throw new PlatformDataSourceError("BFF_URL_MISSING");
    const correlationId = globalThis.crypto?.randomUUID?.() ?? "platform-admin-ui";
    let response: Response;
    try {
      response = await fetch(`${this.baseUrl.replace(/\/$/, "")}${path}`, { ...init, headers: { "Content-Type": "application/json", "x-correlation-id": correlationId, "x-platform-environment": this.environment, ...(init?.headers ?? {}) }, credentials: "include" });
    } catch {
      throw new PlatformDataSourceError("PLATFORM_BFF_UNAVAILABLE", undefined, correlationId);
    }
    if (!response.ok) {
      let payload: { code?: string; correlationId?: string } | undefined;
      try { payload = await response.clone().json() as { code?: string; correlationId?: string }; } catch { /* el BFF puede responder sin cuerpo */ }
      throw new PlatformDataSourceError(payload?.code ?? `PLATFORM_BFF_${response.status}`, response.status, payload?.correlationId ?? response.headers.get("x-correlation-id") ?? correlationId);
    }
    if (response.status === 204) return undefined as T;
    return response.json() as Promise<T>;
  }

  async getSnapshot() { return this.request<PlatformSnapshot>("/overview"); }
  async saveDraft(input: DraftUpdateInput) { await this.request<void>("/drafts/field", { method: "PATCH", body: JSON.stringify(input) }); }
  async validateDraft(artifactId: string) { return this.request<PlatformSnapshot>(`/drafts/${encodeURIComponent(artifactId)}/validate`, { method: "POST" }); }
  async publish(input: PublishInput) { await this.request<void>("/publish", { method: "POST", body: JSON.stringify(input) }); }
  async deprecate(artifactId: string, reason: string) { await this.request<void>("/deprecate", { method: "POST", body: JSON.stringify({ artifactId, reason }) }); }
  async executeCorrection(input: CorrectionInput) { await this.request<void>("/corrections/execute", { method: "POST", body: JSON.stringify(input) }); }
}

export function createPlatformAdminDataSource(mode = process.env.NEXT_PUBLIC_PLATFORM_ADMIN_MODE, bffUrl = process.env.NEXT_PUBLIC_PLATFORM_ADMIN_BFF_URL, environment = process.env.NEXT_PUBLIC_PLATFORM_ADMIN_ENVIRONMENT): PlatformAdminDataSource {
  return mode === "review" ? new ReviewPlatformAdminDataSource() : new BffPlatformAdminDataSource(bffUrl, environment);
}
