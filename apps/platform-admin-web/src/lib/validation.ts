import type { ReviewState, ValidationIssue } from "@/lib/types";

export function validateReviewState(state: ReviewState): ValidationIssue[] {
  const issues: ValidationIssue[] = [];
  if (!state.journeyFixes.J1) issues.push({ code: "PACK_DRAFT_REFERENCE", severity: "blocker", journey: "J1", category: "Referencias", title: "El pack apunta a un draft", message: "Argentina Base Pack v9 referencia un WorkflowTemplate que todavía no está publicado.", field: "workflowTemplate", fixLabel: "Actualizar referencia" });
  if (!state.journeyFixes.J2) issues.push({ code: "SPLITS_TOTAL", severity: "blocker", journey: "J2", category: "Reglas de negocio", title: "Los splits no suman 100 %", message: "La distribución actual suma 94 %. Corregila antes de guardar la política.", field: "splits", fixLabel: "Revisar splits" });
  if (!state.journeyFixes.J3) issues.push({ code: "LATER_STAGE_DEPENDENCY", severity: "blocker", journey: "J3", category: "Dependencias", title: "Dependencia hacia una etapa posterior", message: "La tarea Informe de dominio depende de una etapa que ocurre después.", field: "taskDependency", fixLabel: "Corregir dependencia" });
  if (!state.journeyFixes.J4) issues.push({ code: "OVERLAPPING_DOCUMENT_RULES", severity: "blocker", journey: "J4", category: "Reglas de negocio", title: "Reglas documentales superpuestas", message: "Hay dos reglas para el mismo rol y etapa. También falta una regla para la parte compradora.", field: "requirements", fixLabel: "Resolver matriz" });
  if (!state.journeyFixes.J5) issues.push({ code: "AUTOMATION_VOLUME_LIMIT", severity: "blocker", journey: "J5", category: "Reglas de negocio", title: "Falta un límite de volumen", message: "AUTO_EXECUTE_WITH_GUARDRAILS necesita un límite diario antes de poder publicarse.", field: "guardrail", fixLabel: "Agregar guardrail" });
  if (!state.journeyFixes.J6) issues.push({ code: "REQUIRED_CAPABILITY", severity: "warning", journey: "J6", category: "Deploy", title: "Capability requerida por tenants activos", message: "RENTAL_ADMINISTRATION está activa en 3 inmobiliarias. Deprecarla requiere revisar el impacto.", field: "capability", fixLabel: "Revisar impacto" });
  if (!state.journeyFixes.J7) issues.push({ code: "PILOT_DEPENDENCY", severity: "blocker", journey: "J7", category: "Deploy", title: "El piloto tiene una dependencia faltante", message: "Pepito Propiedades no tiene habilitada la capability requerida por esta versión.", field: "pilot", fixLabel: "Agregar dependencia" });
  issues.push({ code: "EFFECTIVE_CONFIGURATION_EXPLAINED", severity: "ok", journey: "J8", category: "Compatibilidad", title: "Configuración efectiva explicable", message: "El inspector muestra pack, default, override y precedencia.", field: "effectiveConfiguration" });
  if (!state.journeyFixes.J9) issues.push({ code: "PROMOTION_DEPENDENCY", severity: "blocker", journey: "J9", category: "Deploy", title: "La promoción no está lista", message: "Staging no tiene la capability base requerida por el artefacto seleccionado.", field: "promotion", fixLabel: "Resolver destino" });
  if (!state.journeyFixes.J10) issues.push({ code: "CORRECTION_PERMISSION", severity: "warning", journey: "J10", category: "Estructura", title: "Corrección restringida", message: "La corrección requiere ADMIN_CORRECTION y un motivo auditable.", field: "correction", fixLabel: "Ver permisos" });
  issues.push({ code: "UNKNOWN_PRESERVED", severity: "ok", title: "UNKNOWN se conserva", message: `El valor ${state.unknownValue} no se convierte en falso, cero ni lista vacía.` });
  return issues;
}

export function validationCounts(issues: ValidationIssue[]): { blockers: number; warnings: number; ok: number } {
  return issues.reduce((counts, issue) => ({ ...counts, ...(issue.severity === "blocker" ? { blockers: counts.blockers + 1 } : issue.severity === "warning" ? { warnings: counts.warnings + 1 } : issue.severity === "ok" ? { ok: counts.ok + 1 } : {}) }), { blockers: 0, warnings: 0, ok: 0 });
}
