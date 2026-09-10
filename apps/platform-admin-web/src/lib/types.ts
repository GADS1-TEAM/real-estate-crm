export type ArtifactStatus = "DRAFT" | "PUBLISHED" | "DEPRECATED" | "IN_REVIEW";
export type CapabilityState = "DISABLED" | "ENABLED" | "PILOT" | "RESTRICTED";
export type PlatformPermission =
  | "VIEW"
  | "EDIT_DRAFT"
  | "PUBLISH"
  | "DEPRECATE"
  | "MANAGE_PILOTS"
  | "PROMOTE"
  | "ADMIN_CORRECTION"
  | "VIEW_AUDIT";
export type EnvironmentName = "Development" | "Staging" | "Production";
export type Severity = "blocker" | "warning" | "info" | "ok";
export type JourneyId = "J1" | "J2" | "J3" | "J4" | "J5" | "J6" | "J7" | "J8" | "J9" | "J10";

export type ArtifactType =
  | "pack"
  | "capability"
  | "catalog"
  | "workflow"
  | "document-requirement"
  | "compliance"
  | "commission"
  | "automation"
  | "metric"
  | "connector"
  | "feature-flag"
  | "extension-schema";

export type ScreenRenderKey =
  | "overview"
  | "attention"
  | "pack-list"
  | "pack-detail"
  | "deprecate"
  | "pack-version"
  | "pack-editor"
  | "pack-diff"
  | "publish-review"
  | "impact"
  | "capabilities"
  | "capability-detail"
  | "catalogs"
  | "catalog-detail"
  | "catalog-editor"
  | "validation"
  | "artifact-list"
  | "artifact-detail"
  | "workflow-editor"
  | "workflow-preview"
  | "workflow-diff"
  | "requirements-matrix"
  | "requirement-editor"
  | "compliance-editor"
  | "compliance-simulator"
  | "commission-editor"
  | "split-editor"
  | "commission-simulator"
  | "commission-diff"
  | "automation-editor"
  | "automation-simulator"
  | "metric-editor"
  | "metric-lineage"
  | "connectors"
  | "connector-detail"
  | "feature-flags"
  | "schemas"
  | "schema-editor"
  | "schema-preview"
  | "schema-diff"
  | "pilots"
  | "pilot-detail"
  | "pilot-create"
  | "rollout"
  | "environments"
  | "environment-compare"
  | "promotion"
  | "tenants"
  | "tenant-detail"
  | "effective-inspector"
  | "audit"
  | "audit-detail"
  | "corrections"
  | "correction-flow"
  | "global-search"
  | "product-history"
  | "no-permission"
  | "version-header";

export interface ScreenDefinition {
  id: `PA-${number}`;
  title: string;
  group: string;
  entry: string;
  module: "pack" | "rules" | "release" | "system" | "transversal";
  renderKey: ScreenRenderKey;
  route: string;
  prototype: "navigable" | "pattern";
  journey?: JourneyId;
}

export interface ValidationIssue {
  code: string;
  severity: Severity;
  journey?: JourneyId;
  category?: "Estructura" | "Dependencias" | "Compatibilidad" | "Referencias" | "Reglas de negocio" | "Deploy";
  title: string;
  message: string;
  field?: string;
  fixLabel?: string;
}

export interface ArtifactSummary {
  id: string;
  type: ArtifactType;
  name: string;
  description: string;
  status: ArtifactStatus;
  publishedVersion?: number;
  draftVersion?: number;
  tenantCount?: number;
  updatedAt: string;
  warnings?: number;
}

export interface PackManifest {
  id: string;
  key: string;
  name: string;
  version: number;
  status: ArtifactStatus;
  market: string;
  capabilities: string[];
  workflows: string[];
  requirements: string[];
  policies: string[];
}

export interface Capability {
  key: string;
  name: string;
  state: CapabilityState;
  version: number;
  dependencies: string[];
  tenantCount: number | "UNKNOWN";
}

export interface BaseCatalog {
  key: string;
  name: string;
  version: number;
  entries: Array<{ code: string; label: string; active: boolean }>;
}

export interface WorkflowTemplate {
  key: string;
  name: string;
  version: number;
  stages: Array<{ key: string; label: string; order: number }>;
  tasks: Array<{ key: string; stageKey: string; label: string; dependsOn: string[] }>;
}

export interface DocumentRequirement {
  key: string;
  name: string;
  version: number;
  operationType: string;
  stage: string;
  partyRole: string;
  level: "REQUIRED" | "CONDITIONAL" | "RECOMMENDED";
}

export interface CompliancePolicy {
  key: string;
  name: string;
  version: number;
  rules: Array<{ when: string; then: string; except?: string }>;
}

export interface CommissionPolicy {
  key: string;
  name: string;
  version: number;
  minimumPercent: number;
  maximumPercent: number;
  splits: Array<{ role: string; percent: number }>;
}

export interface AutomationPolicy {
  key: string;
  name: string;
  version: number;
  autonomy: "SUGGEST_ONLY" | "REQUIRE_APPROVAL" | "AUTO_EXECUTE_WITH_GUARDRAILS" | "AUTO_EXECUTE";
  dailyVolumeLimit?: number | "UNKNOWN";
}

export interface MetricDefinition {
  key: string;
  name: string;
  version: number;
  sourceEvents: string[];
  dimensions: string[];
  exclusions: string[];
  formula: string;
}

export interface Connector {
  key: string;
  name: string;
  version: number;
  status: ArtifactStatus;
  capability: string;
}

export interface FeatureFlag {
  key: string;
  enabled: boolean | "UNKNOWN";
  environment: EnvironmentName;
  updatedAt: string;
}

export interface ExtensionSchema {
  namespace: string;
  entityType: string;
  schemaVersion: number;
  typedFields: Record<string, "string" | "number" | "boolean">;
  validationRules: string[];
}

export interface PublishedArtifact {
  id: string;
  type: ArtifactType;
  name: string;
  version: number;
  status: "PUBLISHED" | "DEPRECATED";
  publishedAt: string;
}

export interface DraftArtifact {
  id: string;
  type: ArtifactType;
  name: string;
  version: number;
  status: "DRAFT" | "IN_REVIEW";
  changedFields: string[];
  updatedAt: string;
}

export interface ImpactSummary {
  tenantsAffected: number;
  operationsAffected: number;
  blockers: number;
  warnings: number;
  reasons: string[];
}

export interface Pilot {
  id: string;
  name: string;
  capability: string;
  version: number;
  tenants: string[];
  status: "DRAFT" | "RUNNING" | "COMPLETED" | "PAUSED";
  progress: number;
}

export interface EnvironmentRelease {
  artifactId: string;
  artifactVersion: number;
  source: EnvironmentName;
  target: EnvironmentName;
  status: "READY" | "BLOCKED" | "PROMOTED";
  blockers: string[];
}

export interface AdministrativeCorrection {
  commandId: string;
  targetId: string;
  reason: string;
  actor: string;
  correlationId: string;
  status: "REQUESTED" | "EXECUTED" | "REJECTED";
}

export interface AuditEntry {
  id: string;
  action: string;
  artifact: string;
  actor: string;
  reason: string;
  timestamp: string;
  correlationId: string;
  before: unknown;
  after: unknown;
}

export interface TenantRef {
  id: string;
  name: string;
  location: string;
  capabilityState: CapabilityState;
}

export interface EffectiveConfiguration {
  context: string;
  pack: string;
  defaultValue: string;
  tenantOverride: string;
  effectiveValue: string;
  why: string;
}

export interface PlatformSession {
  userId: string;
  name: string;
  roleLabel: string;
  permissions: PlatformPermission[];
  status: "authenticated" | "expired" | "disabled";
}

export interface ReviewState {
  published: Record<string, PublishedArtifact>;
  drafts: Record<string, DraftArtifact>;
  draftValues: Record<string, Record<string, unknown>>;
  acceptedWarnings: Record<string, string>;
  focusedField: string | null;
  selectedArtifactId: string | null;
  selectedTenantId: string;
  selectedAuditId: string | null;
  featureFlags: Record<string, boolean | "UNKNOWN">;
  journeyFixes: Partial<Record<JourneyId, boolean>>;
  unknownValue: "UNKNOWN";
  selectedRole: string;
  environment: EnvironmentName;
  toast: string | null;
  toastAuditId?: string | null;
  validationOpen: boolean;
  session: PlatformSession;
  pilots: Pilot[];
  audit: AuditEntry[];
  tenantRefs: TenantRef[];
  effectiveConfiguration: EffectiveConfiguration;
}

export interface PlatformSnapshot {
  artifacts: ArtifactSummary[];
  impact: ImpactSummary;
  state: ReviewState;
}

export interface DraftUpdateInput {
  artifactId: string;
  field: string;
  value: unknown;
}

export interface PublishInput {
  artifactId: string;
  version: number;
  acceptedWarnings: string[];
  confirmation?: string;
}

export interface CorrectionInput {
  commandId: string;
  targetId: string;
  reason: string;
  note?: string;
}
