"use client";

import React, { useEffect, useRef, type ButtonHTMLAttributes, type InputHTMLAttributes, type ReactNode, type SelectHTMLAttributes, type TextareaHTMLAttributes } from "react";

export type IconName =
  | "grid" | "inbox" | "history" | "package" | "toggle" | "catalog" | "workflow" | "document" | "shield"
  | "commission" | "automation" | "chart" | "plug" | "flag" | "schema" | "impact" | "pilot" | "environment"
  | "tenant" | "audit" | "wrench" | "search" | "info" | "plus" | "chevron" | "arrow" | "close" | "more"
  | "filter" | "download" | "refresh" | "check" | "warning" | "lock" | "edit" | "external" | "copy"
  | "layers" | "play" | "pause" | "dots" | "menu" | "command" | "calendar" | "compare" | "minus";

const paths: Record<IconName, ReactNode> = {
  grid: <><rect x="4" y="4" width="6" height="6" rx="1" /><rect x="14" y="4" width="6" height="6" rx="1" /><rect x="4" y="14" width="6" height="6" rx="1" /><rect x="14" y="14" width="6" height="6" rx="1" /></>,
  inbox: <><path d="M4 5.5h16v13H4z" /><path d="M4 14h4l1.5 2h5L16 14h4" /></>,
  history: <><path d="M4 11a8 8 0 1 0 2.4-5.7" /><path d="M4 5v5h5" /><path d="M12 7v5l3 2" /></>,
  package: <><path d="m4 8 8-4 8 4-8 4-8-4Z" /><path d="M4 8v8l8 4 8-4V8M12 12v8" /></>,
  toggle: <><rect x="3" y="7" width="18" height="10" rx="5" /><circle cx="16" cy="12" r="3" /></>,
  catalog: <><path d="M6 4h12v16H6z" /><path d="M9 8h6M9 12h6M9 16h4" /></>,
  workflow: <><rect x="3" y="4" width="6" height="5" rx="1" /><rect x="15" y="15" width="6" height="5" rx="1" /><rect x="3" y="15" width="6" height="5" rx="1" /><path d="M9 6.5h3a3 3 0 0 1 3 3v8M9 17.5h3" /></>,
  document: <><path d="M6 3h9l3 3v15H6z" /><path d="M15 3v4h4M9 12h6M9 16h5" /></>,
  shield: <><path d="m12 3 7 3v5c0 5-3 8-7 10-4-2-7-5-7-10V6l7-3Z" /><path d="m9 12 2 2 4-4" /></>,
  commission: <><circle cx="12" cy="12" r="8" /><path d="M8 9.5c.8-1 1.7-1.5 2.8-1.5 1.5 0 2.7.8 2.7 2s-1.2 2-2.7 2-2.8.8-2.8 2 1.2 2 2.8 2c1 0 2-.5 2.8-1.5M12 5.5v13" /></>,
  automation: <><path d="M7 8V5h10v3M5 8h14v8H5z" /><path d="M9 12h6M12 16v4" /><circle cx="12" cy="20" r="1" /></>,
  chart: <><path d="M4 19V5M4 19h17" /><path d="m7 15 3-4 3 2 5-6" /></>,
  plug: <><path d="M9 3v5M15 3v5M6 8h12v2a6 6 0 0 1-12 0V8ZM12 16v5" /></>,
  flag: <><path d="M5 21V4M5 5c4-3 7 3 14 0v9c-7 3-10-3-14 0" /></>,
  schema: <><path d="M5 5h5v5H5zM14 14h5v5h-5zM5 14h5v5H5z" /><path d="M10 7.5h3a2 2 0 0 1 2 2V14M10 16.5h4" /></>,
  impact: <><path d="M12 3v18M3 12h18" /><circle cx="12" cy="12" r="8" /><path d="M12 8v4l3 2" /></>,
  pilot: <><path d="m5 20 7-17 7 17-7-3-7 3Z" /><path d="M12 3v5" /></>,
  environment: <><path d="M4 5h16v14H4z" /><path d="M4 9h16M8 5v4M12 9v10M16 5v4" /></>,
  tenant: <><path d="M4 20V6l8-3 8 3v14" /><path d="M8 9h2M14 9h2M8 13h2M14 13h2M10 20v-4h4v4" /></>,
  audit: <><path d="M6 3h12v18H6z" /><path d="M9 7h6M9 11h6M9 15h4" /></>,
  wrench: <><path d="M14 6a5 5 0 0 0-6 6l-5 5a2 2 0 1 0 3 3l5-5a5 5 0 0 0 6-6l-3 3-3-3 3-3Z" /></>,
  search: <><circle cx="10.5" cy="10.5" r="6" /><path d="m15 15 5 5" /></>,
  info: <><circle cx="12" cy="12" r="8" /><path d="M12 11v5M12 8h.01" /></>,
  plus: <><path d="M12 5v14M5 12h14" /></>,
  chevron: <path d="m9 6 6 6-6 6" />,
  arrow: <><path d="M4 12h15M14 7l5 5-5 5" /></>,
  close: <><path d="m6 6 12 12M18 6 6 18" /></>,
  more: <><circle cx="5" cy="12" r="1" fill="currentColor" stroke="none" /><circle cx="12" cy="12" r="1" fill="currentColor" stroke="none" /><circle cx="19" cy="12" r="1" fill="currentColor" stroke="none" /></>,
  filter: <><path d="M4 6h16M7 12h10M10 18h4" /></>,
  download: <><path d="M12 4v11M8 11l4 4 4-4M5 20h14" /></>,
  refresh: <><path d="M20 11a8 8 0 0 0-14-4L4 9M4 5v4h4M4 13a8 8 0 0 0 14 4l2-2M20 19v-4h-4" /></>,
  check: <path d="m5 12 4 4L19 6" />,
  warning: <><path d="m12 4 9 16H3L12 4Z" /><path d="M12 9v5M12 17h.01" /></>,
  lock: <><rect x="5" y="10" width="14" height="10" rx="1" /><path d="M8 10V7a4 4 0 0 1 8 0v3" /></>,
  edit: <><path d="m5 16-1 4 4-1 10-10-3-3L5 16Z" /><path d="m14 7 3 3" /></>,
  external: <><path d="M14 4h6v6M20 4l-9 9" /><path d="M18 13v6H4V5h6" /></>,
  copy: <><rect x="8" y="8" width="11" height="12" rx="1" /><path d="M16 8V5H5v12h3" /></>,
  layers: <><path d="m12 3 9 5-9 5-9-5 9-5ZM3 12l9 5 9-5M3 16l9 5 9-5" /></>,
  play: <path d="m8 5 11 7-11 7V5Z" />,
  pause: <><path d="M8 5v14M16 5v14" /></>,
  dots: <><path d="M5 12h.01M12 12h.01M19 12h.01" /></>,
  menu: <><path d="M4 7h16M4 12h16M4 17h16" /></>,
  command: <><path d="M9 9V7a3 3 0 1 0-3 3h2v5a3 3 0 1 0 3 3v-2h5a3 3 0 1 0 3-3h-2V8a3 3 0 1 0-3-3v2H9" /></>,
  calendar: <><rect x="4" y="5" width="16" height="15" rx="1" /><path d="M8 3v4M16 3v4M4 10h16" /></>,
  compare: <><path d="M7 5h11M7 12h11M7 19h11" /><path d="M4 5h.01M4 12h.01M4 19h.01" /></>,
  minus: <path d="M5 12h14" />,
};

export function Icon({ name, size = 17, className }: { name: IconName; size?: number; className?: string }) {
  return <svg aria-hidden="true" className={className} width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round">{paths[name]}</svg>;
}

export function Button({ variant = "primary", size = "regular", icon, children, className = "", ...props }: ButtonHTMLAttributes<HTMLButtonElement> & { variant?: "primary" | "secondary" | "outline" | "tertiary" | "ghost" | "danger"; size?: "regular" | "small" | "xsmall"; icon?: IconName }) {
  return <button className={`button button-${variant} button-${size} ${className}`} {...props}>{icon && <Icon name={icon} size={size === "xsmall" ? 13 : 16} />}{children}</button>;
}

export function Card({ children, className = "", as: Component = "section" }: { children: ReactNode; className?: string; as?: "section" | "div" | "article" }) {
  return <Component className={`card ${className}`}>{children}</Component>;
}

export function Chip({ children, tone = "neutral", icon }: { children: ReactNode; tone?: "neutral" | "brand" | "success" | "warning" | "error" | "info"; icon?: IconName }) {
  return <span className={`chip chip-${tone}`}>{icon && <Icon name={icon} size={13} />}{children}</span>;
}

export function StatusBadge({ status, label }: { status: "published" | "draft" | "deprecated" | "pilot" | "review" | "warning" | "error"; label?: string }) {
  const labels = { published: "Publicado", draft: "Borrador", deprecated: "Deprecado", pilot: "Piloto", review: "En revisión", warning: "Atención", error: "Bloqueado" };
  return <span className={`status-badge status-${status}`}><span className="status-dot" aria-hidden="true" />{label ?? labels[status]}</span>;
}

export function ArtifactTypeBadge({ type }: { type: string }) {
  const labels: Record<string, string> = { pack: "PACK", capability: "CAPABILITY", catalog: "CATÁLOGO", workflow: "WORKFLOW", "document-requirement": "DOCUMENTO", compliance: "COMPLIANCE", commission: "COMISIÓN", automation: "AUTOMATIZACIÓN", metric: "MÉTRICA", connector: "CONECTOR", "feature-flag": "FLAG", "extension-schema": "SCHEMA" };
  return <span className="type-badge">{labels[type] ?? type.toUpperCase()}</span>;
}

export function Alert({ children, tone = "info", title, action }: { children: ReactNode; tone?: "info" | "warning" | "error" | "success"; title?: string; action?: ReactNode }) {
  const icon = tone === "success" ? "check" : tone === "warning" || tone === "error" ? "warning" : "info";
  return <div className={`alert alert-${tone}`} role={tone === "error" ? "alert" : "status"}><Icon name={icon} size={17} /><div className="alert-copy">{title && <strong>{title}</strong>}<span>{children}</span></div>{action}</div>;
}

export function Drawer({ open, title, eyebrow, children, onClose, width = "validation", footer }: { open: boolean; title: string; eyebrow?: string; children: ReactNode; onClose: () => void; width?: "validation" | "property"; footer?: ReactNode }) {
  useOverlayBehavior(open, onClose);
  if (!open) return null;
  return <div className="overlay-layer"><button className="overlay-backdrop" aria-label="Cerrar panel" onClick={onClose} /><aside className={`drawer drawer-${width}`} role="dialog" aria-modal="true" aria-label={title}><header className="drawer-header">{eyebrow && <span className="overline">{eyebrow}</span>}<div className="drawer-heading"><h2>{title}</h2><Button variant="ghost" size="xsmall" icon="close" aria-label="Cerrar" onClick={onClose} /></div></header><div className="drawer-body">{children}</div>{footer && <footer className="drawer-footer">{footer}</footer>}</aside></div>;
}

export function Modal({ open, title, children, onClose, footer }: { open: boolean; title: string; children: ReactNode; onClose: () => void; footer?: ReactNode }) {
  useOverlayBehavior(open, onClose);
  if (!open) return null;
  return <div className="overlay-layer"><button className="overlay-backdrop" aria-label="Cerrar modal" onClick={onClose} /><div className="modal" role="dialog" aria-modal="true" aria-label={title}><header className="modal-header"><h2>{title}</h2><Button variant="ghost" size="xsmall" icon="close" aria-label="Cerrar" onClick={onClose} /></header><div className="modal-body">{children}</div>{footer && <footer className="modal-footer">{footer}</footer>}</div></div>;
}

function useOverlayBehavior(open: boolean, onClose: () => void) {
  const previousFocus = useRef<HTMLElement | null>(null);
  useEffect(() => {
    if (!open) return;
    previousFocus.current = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    const handleKeyDown = (event: KeyboardEvent) => { if (event.key === "Escape") onClose(); };
    document.addEventListener("keydown", handleKeyDown);
    const focusTarget = document.querySelector<HTMLElement>('[role="dialog"] button, [role="dialog"] input, [role="dialog"] select, [role="dialog"] textarea');
    focusTarget?.focus();
    return () => { document.removeEventListener("keydown", handleKeyDown); previousFocus.current?.focus(); };
  }, [open, onClose]);
}

export function Tabs({ tabs, active, onChange }: { tabs: { id: string; label: string; count?: number }[]; active: string; onChange: (id: string) => void }) {
  return <div className="tabs" role="tablist">{tabs.map((tab) => <button type="button" role="tab" id={`tab-${tab.id}`} aria-controls={`tabpanel-${tab.id}`} aria-selected={active === tab.id} tabIndex={active === tab.id ? 0 : -1} className={active === tab.id ? "tab is-active" : "tab"} key={tab.id} onClick={() => onChange(tab.id)}>{tab.label}{tab.count !== undefined && <span className="tab-count">{tab.count}</span>}</button>)}</div>;
}

export function Field({ label, hint, ...props }: InputHTMLAttributes<HTMLInputElement> & { label: string; hint?: string }) {
  return <label className="field"><span className="field-label">{label}</span><input aria-label={label} {...props} />{hint && <span className="field-hint">{hint}</span>}</label>;
}

export function SelectField({ label, children, ...props }: SelectHTMLAttributes<HTMLSelectElement> & { label: string }) {
  return <label className="field"><span className="field-label">{label}</span><select aria-label={label} {...props}>{children}</select></label>;
}

export function TextAreaField({ label, hint, ...props }: TextareaHTMLAttributes<HTMLTextAreaElement> & { label: string; hint?: string }) {
  return <label className="field"><span className="field-label">{label}</span><textarea aria-label={label} {...props} />{hint && <span className="field-hint">{hint}</span>}</label>;
}

export function SectionHeading({ eyebrow, title, description, action }: { eyebrow?: string; title: string; description?: string; action?: ReactNode }) {
  return <div className="section-heading"><div>{eyebrow && <span className="overline">{eyebrow}</span>}<h2>{title}</h2>{description && <p>{description}</p>}</div>{action && <div className="section-heading-action">{action}</div>}</div>;
}

export function Kpi({ label, value, detail, tone = "blue" }: { label: string; value: string; detail: string; tone?: "blue" | "orange" | "green" | "purple" }) {
  return <div className={`kpi kpi-${tone}`}><span>{label}</span><strong>{value}</strong><small>{detail}</small></div>;
}

export function Toast({ message, action, onClose }: { message: string | null; action?: ReactNode; onClose: () => void }) {
  if (!message) return null;
  return <div className="toast" role="status"><Icon name="check" size={16} /><span>{message}</span>{action}<button aria-label="Cerrar confirmación" onClick={onClose}><Icon name="close" size={14} /></button></div>;
}

export function Skeleton({ width = "100%", height = 16, className = "" }: { width?: string | number; height?: number; className?: string }) {
  return <span className={`skeleton ${className}`} style={{ width, height }} aria-hidden="true" />;
}

export function EmptyState({ icon = "inbox", title, description, action }: { icon?: IconName; title: string; description: string; action?: ReactNode }) {
  return <div className="empty-state"><span className="empty-icon"><Icon name={icon} size={23} /></span><h3>{title}</h3><p>{description}</p>{action}</div>;
}

export function Pagination({ page = 1, pages = 4, onChange }: { page?: number; pages?: number; onChange?: (page: number) => void }) {
  return <nav className="pagination" aria-label="Paginación"><Button variant="ghost" size="xsmall" icon="chevron" aria-label="Página anterior" disabled={page <= 1} onClick={() => onChange?.(page - 1)} /><span>Página <strong>{page}</strong> de {pages}</span><Button variant="ghost" size="xsmall" icon="chevron" aria-label="Página siguiente" disabled={page >= pages} onClick={() => onChange?.(page + 1)} /></nav>;
}
