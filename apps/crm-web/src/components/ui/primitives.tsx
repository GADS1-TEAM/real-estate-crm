"use client";

import { useEffect, useState } from "react";
import type { ButtonHTMLAttributes, InputHTMLAttributes, ReactNode, SelectHTMLAttributes, TextareaHTMLAttributes } from "react";

export type IconName =
  | "home" | "search" | "plus" | "users" | "building" | "target" | "pipeline" | "activity"
  | "chart" | "settings" | "calendar" | "chevron" | "more" | "close" | "arrow" | "lock"
  | "alert" | "check" | "map" | "phone" | "mail" | "menu" | "spark" | "filter" | "external"
  | "refresh" | "camera" | "briefcase" | "clock" | "info" | "sliders" | "download"
  | "archive" | "edit" | "person" | "logout" | "database" | "wifi";

const paths: Record<IconName, string> = {
  home: "M3 10.8 12 3l9 7.8v8.7a1.5 1.5 0 0 1-1.5 1.5h-15A1.5 1.5 0 0 1 3 19.5v-8.7ZM8.5 21v-6.2h7V21",
  search: "m21 21-4.35-4.35m2.1-5.4a7.5 7.5 0 1 1-15 0 7.5 7.5 0 0 1 15 0Z",
  plus: "M12 5v14M5 12h14",
  users: "M16 20v-1.5a4.5 4.5 0 0 0-4.5-4.5h-3A4.5 4.5 0 0 0 4 18.5V20m6-10a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7Zm6.5 0a3 3 0 1 0 0-6m1 12h4v-1.2a4 4 0 0 0-3.2-3.9",
  building: "M4 21V4.5A1.5 1.5 0 0 1 5.5 3h9A1.5 1.5 0 0 1 16 4.5V21M8 7h1m3 0h1M8 11h1m3 0h1M8 15h1m3 0h1M2 21h20M9 21v-3h4v3",
  target: "M12 3v3m0 12v3M3 12h3m12 0h3m-3.2-6.8-2.1 2.1m-5.4 5.4-2.1 2.1m0-9.6 2.1 2.1m5.4 5.4 2.1 2.1M16.5 12a4.5 4.5 0 1 1-9 0 4.5 4.5 0 0 1 9 0Z",
  pipeline: "M4 5h5v4H4zM15 5h5v4h-5zM9.5 16h5v4h-5zM9 7h6m-3 2v7",
  activity: "M4 13h3l2-7 4 12 2-5h5",
  chart: "M4 19V5m0 14h17M8 16v-4m4 4V8m4 8v-7m4 7V5",
  settings: "M12 15.2a3.2 3.2 0 1 0 0-6.4 3.2 3.2 0 0 0 0 6.4Zm0-12.2v2m0 14v2M4.8 4.8l1.4 1.4m11.6 11.6 1.4 1.4M2 12h2m16 0h2M4.8 19.2l1.4-1.4m11.6-11.6 1.4-1.4",
  calendar: "M5 4h14a2 2 0 0 1 2 2v13H3V6a2 2 0 0 1 2-2Zm-2 5h18M8 2v4m8-4v4",
  chevron: "m8 10 4 4 4-4",
  more: "M5 12h.01M12 12h.01M19 12h.01",
  close: "m6 6 12 12M18 6 6 18",
  arrow: "M5 12h14m-6-6 6 6-6 6",
  lock: "M6 10V7a6 6 0 0 1 12 0v3m-13 0h14v10H5V10Z",
  alert: "M12 3 2.8 20h18.4L12 3Zm0 6v5m0 3h.01",
  check: "m5 12 4 4L19 6",
  map: "m3 6 6-3 6 3 6-3v15l-6 3-6-3-6 3V6Zm6-3v15m6-12v15",
  phone: "M6.5 3.5 9 4l1.3 4-2 1.6a15 15 0 0 0 6.1 6.1l1.6-2 4 1.3.5 2.5a2 2 0 0 1-2.2 2.4C10.5 18.9 5.1 13.5 4.1 5.7A2 2 0 0 1 6.5 3.5Z",
  mail: "M3 5h18v14H3V5Zm1 1 8 6 8-6",
  menu: "M4 7h16M4 12h16M4 17h16",
  spark: "m12 3 1.4 5.6L19 10l-5.6 1.4L12 17l-1.4-5.6L5 10l5.6-1.4L12 3Zm6 12 .6 2.4L21 18l-2.4.6L18 21l-.6-2.4L15 18l2.4-.6L18 15Z",
  filter: "M4 5h16M7 12h10m-6 7h4",
  external: "M14 4h6v6m-1-5-8 8M18 13v5a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h5",
  refresh: "M20 11a8 8 0 0 0-14.9-3M4 5v4h4m-4 2a8 8 0 0 0 14.9 3M20 19v-4h-4",
  camera: "M4 7h3l1.4-2h7.2L17 7h3v12H4V7Zm8 8.5a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7Z",
  briefcase: "M4 7h16v13H4V7Zm4 0V5a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2M4 12h16m-9 0v2h2v-2",
  clock: "M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18Zm0-14v5l3 2",
  info: "M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18Zm0-10v5m0-8h.01",
  sliders: "M4 7h16M4 17h16M8 4v6m8 4v6",
  download: "M12 3v12m0 0 4-4m-4 4-4-4M4 21h16",
  archive: "M4 5h16v4H4V5Zm2 4h12v10H6V9Zm3 4h6",
  edit: "m4 16 9.5-9.5 4 4L8 20H4v-4Zm8.5-8.5 2-2a1.4 1.4 0 0 1 2 0l.8.8a1.4 1.4 0 0 1 0 2l-2 2",
  person: "M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8Zm-7 9a7 7 0 0 1 14 0",
  logout: "M10 5H5v14h5m5-4 4-3-4-3m4 3H9",
  database: "M4 6c0 1.7 3.6 3 8 3s8-1.3 8-3-3.6-3-8-3-8 1.3-8 3Zm0 0v6c0 1.7 3.6 3 8 3s8-1.3 8-3V6m-16 6v6c0 1.7 3.6 3 8 3s8-1.3 8-3v-6",
  wifi: "M3 9a14 14 0 0 1 18 0M6 13a9 9 0 0 1 12 0m-9 4a4 4 0 0 1 6 0M12 20h.01",
};

export function Icon({ name, size = 18, className }: { name: IconName; size?: number; className?: string }) {
  return (
    <svg suppressHydrationWarning aria-hidden="true" className={className} width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <path d={paths[name]} />
    </svg>
  );
}

export function Button({ variant = "primary", size = "regular", icon, children, className = "", fullWidth = false, ...props }: ButtonHTMLAttributes<HTMLButtonElement> & { variant?: "primary" | "secondary" | "outline" | "tertiary" | "ghost"; size?: "regular" | "small" | "xsmall"; icon?: IconName; fullWidth?: boolean }) {
  return <button className={`button button-${variant} button-${size} ${fullWidth ? "button-full" : ""} ${className}`} {...props}>{icon && <Icon name={icon} size={size === "xsmall" ? 14 : 17} />}{children}</button>;
}

export function Card({ children, className = "", as: Component = "section" }: { children: ReactNode; className?: string; as?: "section" | "div" | "article" }) {
  return <Component className={`card ${className}`}>{children}</Component>;
}

export function Chip({ children, tone = "neutral", dot = false }: { children: ReactNode; tone?: "success" | "info" | "neutral" | "error" | "warning" | "brand"; dot?: boolean }) {
  return <span className={`chip chip-${tone}`}>{dot && <span className="chip-dot" aria-hidden="true" />}{children}</span>;
}

export function Tag({ children, tone = "neutral" }: { children: ReactNode; tone?: "neutral" | "brand" | "success" | "warning" | "error" | "info" }) {
  return <span className={`tag tag-${tone}`}>{children}</span>;
}

export function Alert({ children, tone = "info", title, action }: { children: ReactNode; tone?: "info" | "warning" | "error" | "success"; title?: string; action?: ReactNode }) {
  return <div className={`alert alert-${tone}`} role={tone === "error" ? "alert" : "status"}><Icon name={tone === "success" ? "check" : tone === "warning" || tone === "error" ? "alert" : "info"} size={18} /><div className="alert-copy">{title && <strong>{title}</strong>}<span>{children}</span></div>{action}</div>;
}

export function Avatar({ name, size = "regular" }: { name: string; size?: "small" | "regular" | "large" }) {
  const initials = name.split(" ").slice(0, 2).map((part) => part[0]).join("").toUpperCase();
  return <span className={`avatar avatar-${size}`} aria-label={name}>{initials}</span>;
}

export function Skeleton({ width = "100%", height = 16, className = "" }: { width?: string | number; height?: number; className?: string }) {
  return <span className={`skeleton ${className}`} style={{ width, height }} aria-hidden="true" />;
}

export function Tabs({ tabs, active, onChange }: { tabs: { id: string; label: string; count?: number }[]; active: string; onChange: (id: string) => void }) {
  const [selected, setSelected] = useState(active);
  useEffect(() => setSelected(active), [active]);
  return <div className="tabs" role="tablist">{tabs.map((tab) => <button type="button" role="tab" aria-selected={selected === tab.id} className={selected === tab.id ? "tab is-active" : "tab"} key={tab.id} onClick={() => { setSelected(tab.id); onChange(tab.id); }}>{tab.label}{tab.count !== undefined && <span className="tab-count">{tab.count}</span>}</button>)}</div>;
}

export function Pagination({ page = 1, pages = 4, onChange }: { page?: number; pages?: number; onChange?: (page: number) => void }) {
  return <nav className="pagination" aria-label="Paginación"><Button variant="ghost" size="xsmall" icon="chevron" aria-label="Página anterior" disabled={page <= 1} onClick={() => onChange?.(page - 1)} /><span>Página <strong>{page}</strong> de {pages}</span><Button variant="ghost" size="xsmall" icon="chevron" aria-label="Página siguiente" disabled={page >= pages} onClick={() => onChange?.(page + 1)} /></nav>;
}

export function Drawer({ open, title, eyebrow, children, onClose, footer }: { open: boolean; title: string; eyebrow?: string; children: ReactNode; onClose: () => void; footer?: ReactNode }) {
  if (!open) return null;
  return <div className="overlay-layer"><button className="overlay-backdrop" aria-label="Cerrar panel" onClick={onClose} /><aside className="drawer" role="dialog" aria-modal="true" aria-label={title}><header className="drawer-header">{eyebrow && <span className="eyebrow">{eyebrow}</span>}<div className="drawer-heading"><h2>{title}</h2><Button variant="ghost" size="xsmall" icon="close" aria-label="Cerrar" onClick={onClose} /></div></header><div className="drawer-body">{children}</div>{footer && <footer className="drawer-footer">{footer}</footer>}</aside></div>;
}

export function Modal({ open, title, children, onClose, footer }: { open: boolean; title: string; children: ReactNode; onClose: () => void; footer?: ReactNode }) {
  if (!open) return null;
  return <div className="overlay-layer"><button className="overlay-backdrop" aria-label="Cerrar modal" onClick={onClose} /><div className="modal" role="dialog" aria-modal="true" aria-label={title}><header className="modal-header"><h2>{title}</h2><Button variant="ghost" size="xsmall" icon="close" aria-label="Cerrar" onClick={onClose} /></header><div className="modal-body">{children}</div>{footer && <footer className="modal-footer">{footer}</footer>}</div></div>;
}

export function EmptyState({ icon = "database", title, description, action }: { icon?: IconName; title: string; description: string; action?: ReactNode }) {
  return <div className="empty-state"><span className="empty-icon"><Icon name={icon} size={24} /></span><h3>{title}</h3><p>{description}</p>{action}</div>;
}

export function Field({ label, hint, ...props }: InputHTMLAttributes<HTMLInputElement> & { label: string; hint?: string }) {
  return <label className="field"><span className="field-label">{label}</span><input {...props} />{hint && <span className="field-hint">{hint}</span>}</label>;
}

export function TextAreaField({ label, hint, ...props }: TextareaHTMLAttributes<HTMLTextAreaElement> & { label: string; hint?: string }) {
  return <label className="field"><span className="field-label">{label}</span><textarea {...props} />{hint && <span className="field-hint">{hint}</span>}</label>;
}

export function SelectField({ label, children, ...props }: SelectHTMLAttributes<HTMLSelectElement> & { label: string }) {
  return <label className="field"><span className="field-label">{label}</span><select {...props}>{children}</select></label>;
}

export function Toast({ message, tone = "success", onClose }: { message: string; tone?: "success" | "info" | "warning"; onClose: () => void }) {
  return <div className={`toast toast-${tone}`} role="status"><Icon name={tone === "success" ? "check" : tone === "warning" ? "alert" : "info"} size={16} /><span>{message}</span><button aria-label="Cerrar notificación" onClick={onClose}><Icon name="close" size={14} /></button></div>;
}

export function StatusDot({ label, tone = "success" }: { label: string; tone?: "success" | "warning" | "error" | "info" | "neutral" }) {
  return <Chip tone={tone} dot>{label}</Chip>;
}
