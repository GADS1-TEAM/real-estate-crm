export const COMMERCIAL_STATUS_VALUES = ["POTENTIAL", "CUSTOMER", "INACTIVE", "DO_NOT_CONTACT"] as const;
export type CommercialStatus = (typeof COMMERCIAL_STATUS_VALUES)[number];
export type IdentityStatus = "VERIFIED" | "PENDING" | "UNKNOWN";

const commercialStatusMeanings: Record<CommercialStatus, string> = {
  POTENTIAL: "Potencial: todavía no hay una relación comercial confirmada.",
  CUSTOMER: "Cliente: existe una relación comercial activa o confirmada.",
  INACTIVE: "Inactivo: se conserva el historial, pero no hay acciones comerciales activas.",
  DO_NOT_CONTACT: "No contactar: se bloquean nuevas acciones comerciales y se conserva el historial.",
};

export function getCommercialStatusMeaning(status: CommercialStatus): string {
  return commercialStatusMeanings[status];
}

export interface ContactSuggestions {
  name?: string;
  phone?: string;
  email?: string;
}

export function parseHistoricalContactText(text: string): ContactSuggestions {
  const suggestions: ContactSuggestions = {};
  const name = text.match(/(?:^|\n)\s*nombre(?:\s+y\s+apellido)?\s*[:\-]\s*([^\n]+)/i)?.[1]?.trim();
  const phone = text.match(/(?:^|\n)\s*(?:tel(?:éfono)?|cel(?:ular)?|phone)\s*[:\-]\s*([^\n]+)/i)?.[1]?.trim();
  const email = text.match(/[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}/i)?.[0]?.trim();
  if (name) suggestions.name = name;
  if (phone) suggestions.phone = phone;
  if (email) suggestions.email = email;
  return suggestions;
}

export function applyContactSuggestions<T extends { name: string; phone: string; email: string }>(contact: T, suggestions: ContactSuggestions, confirmed: boolean): T {
  if (!confirmed) return contact;
  return {
    ...contact,
    ...(suggestions.name ? { name: suggestions.name } : {}),
    ...(suggestions.phone ? { phone: suggestions.phone } : {}),
    ...(suggestions.email ? { email: suggestions.email } : {}),
  };
}

export type ValidationResult = { valid: true } | { valid: false; error: string };

export function resolveSelectedRecord<T extends { id: string }>(records: readonly T[], selectedId?: string | null): T | undefined {
  if (selectedId) return records.find((record) => record.id === selectedId);
  return records[0];
}

export function validateOpportunityStageChange(currentStage: string, nextStage: string, reason?: string | null): ValidationResult {
  if (currentStage === nextStage) return { valid: true };
  if (currentStage === "CLOSED" || currentStage === "Cerrada") return { valid: false, error: "Una oportunidad cerrada no puede reabrirse." };
  if (!reason?.trim()) return { valid: false, error: "Indica un motivo para cambiar la etapa de la oportunidad." };
  return { valid: true };
}

export type OpportunityCloseOutcome = "won" | "lost";

export function validateOpportunityClose(outcome: OpportunityCloseOutcome, closeDate?: string | null, finalValue?: number | null, reason?: string | null): ValidationResult {
  if (outcome === "won") {
    if (!closeDate?.trim()) return { valid: false, error: "Indica la fecha de cierre para una oportunidad ganada." };
    if (typeof finalValue !== "number" || !Number.isFinite(finalValue) || finalValue < 0) return { valid: false, error: "Indica un valor final válido y no negativo para una oportunidad ganada." };
    return { valid: true };
  }
  if (!closeDate?.trim()) return { valid: false, error: "Indica la fecha de cierre para una oportunidad perdida." };
  if (!reason?.trim()) return { valid: false, error: "Indica un motivo para cerrar la oportunidad como perdida." };
  return { valid: true };
}

export type ActivityValidationInput = { activityType: string; occurredAt?: string | null; actor?: string | null; relatedRecordId?: string | null };
export type ActivityValidationResult = ValidationResult & { historical?: boolean };

export function validateActivity(activity: ActivityValidationInput): ActivityValidationResult {
  if (!activity.occurredAt?.trim()) return { valid: false, error: "Indica cuándo ocurrió la actividad." };
  if (!activity.actor?.trim()) return { valid: false, error: "Indica quién registró la actividad." };
  if (!activity.relatedRecordId?.trim()) return { valid: false, error: "Indica el registro relacionado con la actividad." };
  return { valid: true, historical: ["Email", "Email histórico", "WhatsApp", "WhatsApp histórico", "Correo electrónico", "Mensaje"].includes(activity.activityType) };
}

export function validateReason(reason: string | null | undefined, subject: string): ValidationResult {
  return reason?.trim() ? { valid: true } : { valid: false, error: `Indica un motivo para ${subject}.` };
}

export function validateOperationClose(closeDate?: string | null, finalValue?: number | null): ValidationResult {
  if (!closeDate?.trim()) return { valid: false, error: "Indica la fecha de cierre de la operación." };
  if (typeof finalValue !== "number" || !Number.isFinite(finalValue) || finalValue < 0) return { valid: false, error: "Indica un valor final válido y no negativo para la operación." };
  return { valid: true };
}

export function validateOperationTransition(currentStage: string, nextStage: string): ValidationResult {
  if (currentStage === nextStage) return { valid: true };
  if (currentStage === "Cerrada" || currentStage === "Cancelada") return { valid: false, error: "Una operación cerrada o cancelada no puede avanzar." };
  return { valid: true };
}

export type CriterionContribution =
  | { status: "match"; contribution: 1 }
  | { status: "mismatch"; contribution: 0 }
  | { status: "unknown"; contribution: null };

export function criterionContribution(expected: unknown, actual: unknown): CriterionContribution {
  if (expected === undefined || expected === null || expected === "UNKNOWN" || actual === undefined || actual === null || actual === "UNKNOWN") {
    return { status: "unknown", contribution: null };
  }
  return expected === actual ? { status: "match", contribution: 1 } : { status: "mismatch", contribution: 0 };
}
