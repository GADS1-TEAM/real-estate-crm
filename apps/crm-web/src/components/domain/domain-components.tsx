"use client";

import { useState } from "react";
import type { CriterionWeight } from "@/lib/demo-store";
import type { CurrencyCode } from "@/lib/currency";
import { formatCurrency } from "@/lib/currency";
import { Button, Card, Chip, Icon, Tag } from "@/components/ui/primitives";

export function CurrencyAmount({ value, currency, className = "" }: { value: number | null | undefined; currency: CurrencyCode; className?: string }) {
  return <span className={`currency-amount ${className}`}>{formatCurrency(value, currency)}</span>;
}

export function UnknownIndicator({ label = "Desconocido" }: { label?: string }) {
  return <span className="unknown-indicator"><span aria-hidden="true">?</span>{label}</span>;
}

export interface CriterionEvidence {
  label: string;
  status: "match" | "mismatch" | "unknown";
  contribution: number | null;
}

export function MatchScoreExplanation({ score, confidence, matches, unknowns, criterionDetails = [], invalidated = false }: { score: number; confidence: string; matches: string[]; unknowns: string[]; criterionDetails?: CriterionEvidence[]; invalidated?: boolean }) {
  return <Card className={`match-explanation ${invalidated ? "is-invalidated" : ""}`}>
    <div className="match-summary"><div className="score-ring" aria-label={`${score} puntos`}><strong>{score}</strong><span>puntos</span></div><div><span className="eyebrow">Compatibilidad explicable</span><h3>{invalidated ? "Compatibilidad invalidada" : `${confidence} confianza`}</h3><p>{invalidated ? "La publicación cambió desde el último cálculo." : "El score es una síntesis; la decisión se apoya en la evidencia de abajo."}</p></div></div>
    {criterionDetails.length > 0 ? <div className="evidence-columns"><div><span className="evidence-title"><Icon name="check" size={15} />Criterios</span>{criterionDetails.map((item) => <span className="evidence-row" key={item.label}>{item.label} · {item.status === "match" ? "coincide" : item.status === "mismatch" ? "no coincide" : "desconocido"} · aporte {item.contribution === null ? "desconocido" : item.contribution}</span>)}</div></div> : <div className="evidence-columns"><div><span className="evidence-title"><Icon name="check" size={15} />Coincide</span>{matches.map((item) => <span className="evidence-row" key={item}>{item}</span>)}</div><div><span className="evidence-title evidence-unknown"><Icon name="info" size={15} />No sabemos</span>{unknowns.map((item) => <span className="evidence-row" key={item}>{item} desconocida</span>)}</div></div>}
  </Card>;
}

const weightOptions: { id: CriterionWeight; label: string }[] = [
  { id: "must", label: "Debe" },
  { id: "nice", label: "Ojalá" },
  { id: "any", label: "Da igual" },
  { id: "unknown", label: "?" },
];

export function CriterionEditor({ label, value, weight, onChange }: { label: string; value: string; weight: CriterionWeight; onChange: (value: { value: string; weight: CriterionWeight }) => void }) {
  const [currentWeight, setCurrentWeight] = useState(weight);
  const [currentValue, setCurrentValue] = useState(value);
  const changeWeight = (next: CriterionWeight) => { setCurrentWeight(next); onChange({ value: currentValue, weight: next }); };
  const changeValue = (next: string) => { setCurrentValue(next); onChange({ value: next, weight: currentWeight }); };
  return <div className="criterion-editor"><div className="criterion-heading"><div><strong>{label}</strong><span className="field-hint">Peso vivo: {currentWeight === "must" ? "alto" : currentWeight === "nice" ? "medio" : currentWeight === "any" ? "bajo" : "sin dato"}</span></div><Tag tone={currentWeight === "must" ? "brand" : currentWeight === "unknown" ? "warning" : "neutral"}>{currentWeight === "unknown" ? "Sin dato" : "Criterio editable"}</Tag></div><input aria-label={`Valor de ${label}`} value={currentValue} onChange={(event) => changeValue(event.target.value)} /><div className="weight-toggle" role="group" aria-label={`Peso de ${label}`}>{weightOptions.map((option) => <button type="button" key={option.id} aria-pressed={currentWeight === option.id} className={currentWeight === option.id ? "weight-button is-active" : "weight-button"} onClick={() => changeWeight(option.id)}>{option.label}</button>)}</div></div>;
}

export function PipelineCard({ title, sourceType, stage, owner, fee, onOpen }: { title: string; sourceType: "REQUIREMENT" | "CAPTATION_CASE"; stage: string; owner: string; fee: React.ReactNode; onOpen?: () => void }) {
  return <Card className="pipeline-card" as="article"><div className="pipeline-card-top"><Tag tone={sourceType === "REQUIREMENT" ? "info" : "brand"}>{sourceType === "REQUIREMENT" ? "Búsqueda" : "Captación"}</Tag><Button variant="ghost" size="xsmall" icon="more" aria-label={`Más acciones para ${title}`} onClick={onOpen} /></div><button className="card-link" onClick={onOpen}><strong>{title}</strong></button><span className="pipeline-card-stage">{stage}</span><div className="pipeline-card-footer"><span><AvatarMini name={owner} />{owner}</span><strong>{fee}</strong></div></Card>;
}

function AvatarMini({ name }: { name: string }) {
  return <span className="avatar avatar-small" aria-hidden="true">{name.split(" ").slice(0, 2).map((part) => part[0]).join("").toUpperCase()}</span>;
}

export function AISuggestion({ title, body, evidence, onReview, onDismiss }: { title: string; body: string; evidence: string; onReview: () => void; onDismiss: () => void }) {
  return <Card className="ai-suggestion"><div className="ai-heading"><span className="ai-spark"><Icon name="spark" size={17} /></span><div><span className="eyebrow">Asistente revisable</span><h3>{title}</h3></div></div><p>{body}</p><div className="ai-evidence"><Icon name="info" size={15} /><span>{evidence}</span></div><div className="ai-actions"><Button variant="secondary" size="small" onClick={onReview}>Revisar sugerencia</Button><Button variant="ghost" size="small" onClick={onDismiss}>Descartar</Button></div></Card>;
}

export function PropertyPlaceholder({ title, status }: { title: string; status?: string }) {
  return <div className="property-placeholder" aria-label={`Placeholder de ${title}`}><Icon name="building" size={30} /><span>Imagen pendiente</span>{status && <Chip tone="neutral">{status}</Chip>}</div>;
}

export function StatusDot({ label, tone = "success" }: { label: string; tone?: "success" | "warning" | "error" | "info" | "neutral" }) {
  return <Chip tone={tone} dot>{label}</Chip>;
}
