"use client";

import React, { useState, type ReactNode } from "react";
import { getRole, platformRoles } from "@/lib/permissions";
import { getScreenById, screenRegistry } from "@/lib/screen-registry";
import type { EnvironmentName, PlatformSession, ScreenDefinition } from "@/lib/types";
import { Chip, Icon, type IconName } from "@/components/ui/primitives";

interface PlatformShellProps {
  children: ReactNode;
  screenId: string;
  roleId: string;
  session: PlatformSession;
  environment: EnvironmentName;
  onNavigate: (screen: ScreenDefinition) => void;
  onOpenSearch: () => void;
  onEnvironmentChange: (environment: EnvironmentName) => void;
  onRoleChange: (roleId: string) => void;
  reviewMode?: boolean;
}

const navigation = [
  { group: "Overview", items: [["PA-001", "Inicio", "grid"], ["PA-002", "Centro de atención", "inbox"], ["PA-151", "Historial de producto", "history"]] },
  { group: "Packs", items: [["PA-010", "Packs", "package"], ["PA-020", "Capabilities", "toggle"], ["PA-030", "Catálogos base", "catalog"]] },
  { group: "Reglas operativas", items: [["PA-040", "Workflows", "workflow"], ["PA-050", "Requisitos documentales", "document"], ["PA-060", "Compliance", "shield"], ["PA-070", "Comisiones", "commission"], ["PA-080", "Automatizaciones", "automation"]] },
  { group: "Datos", items: [["PA-090", "Métricas", "chart"]] },
  { group: "Integraciones", items: [["PA-100", "Conectores", "plug"], ["PA-102", "Feature flags", "flag"], ["PA-110", "Extension Schemas", "schema"]] },
  { group: "Release", items: [["PA-120", "Impacto", "impact"], ["PA-121", "Pilotos y rollout", "pilot"], ["PA-125", "Ambientes", "environment"]] },
  { group: "Diagnóstico", items: [["PA-130", "Inmobiliarias", "tenant"], ["PA-140", "Auditoría", "audit"], ["PA-142", "Correcciones", "wrench"]] },
] as const;

const mobileNavigation: readonly [string, string, IconName][] = [
  ["PA-001", "Inicio", "grid"],
  ["PA-002", "Centro de atención", "inbox"],
  ["PA-151", "Historial de producto", "history"],
  ["PA-010", "Packs", "package"],
];

export function PlatformShell({ children, screenId, roleId, session, environment, onNavigate, onOpenSearch, onEnvironmentChange, onRoleChange, reviewMode = false }: PlatformShellProps) {
  const [userOpen, setUserOpen] = useState(false);
  const [environmentOpen, setEnvironmentOpen] = useState(false);
  const role = getRole(roleId);
  const initials = session.name.split(/\s+/).map((part) => part[0]).join("").slice(0, 2).toUpperCase();
  return <div className="platform-shell">
    <aside className="sidebar" aria-label="Navegación principal">
      <div className="brand-lockup"><span className="brand-mark">b</span><div className="brand-copy"><strong>Platform Admin</strong><span>Pack Studio</span></div></div>
      <div className="sidebar-scroll">
        {navigation.map((section) => <div className="nav-group" key={section.group}><div className="nav-group-title">{section.group}</div>{section.items.map(([id, label, icon]) => { const screen = getScreenById(id); if (!screen) return null; return <button type="button" className={`nav-item ${screen.id === screenId ? "is-active" : ""}`} key={id} onClick={() => onNavigate(screen)}><Icon name={icon} size={15} /><span>{label}</span>{id === "PA-010" && <span className="nav-count">2</span>}{id === "PA-120" && <span className="nav-count">3</span>}</button>; })}</div>)}
      </div>
      <div className="sidebar-bottom">
        <div className="environment-rail"><span className="environment-dot" /><span>Ambiente activo</span><strong>{environment}</strong></div>
        <div className="user-mini"><span className="avatar">{initials}</span><div className="user-mini-copy"><strong>{session.name}</strong><span>{role.shortName}</span></div><button type="button" className="user-menu-button" aria-label="Abrir menú de usuario" onClick={() => setUserOpen((open) => !open)}><Icon name="more" size={16} /></button></div>
      </div>
    </aside>
    <div className="main-column">
      <header className="topbar">
        <button className="topbar-search" type="button" onClick={onOpenSearch}><Icon name="search" size={15} /><span>Buscar en Platform Admin</span><kbd>Ctrl K</kbd></button>
        <div className="topbar-actions">
          <div className="environment-control"><button type="button" className="topbar-environment" aria-label={reviewMode ? "Cambiar ambiente de revisión" : "Ambiente determinado por BFF"} title={reviewMode ? "Cambiar ambiente de revisión" : "El BFF determina el ambiente activo"} disabled={!reviewMode} onClick={() => setEnvironmentOpen((open) => !open)}><span className="environment-dot" />{environment}{reviewMode && <Icon name="chevron" size={12} />}</button>{reviewMode && environmentOpen && <div className="environment-menu">{(["Development", "Staging", "Production"] as EnvironmentName[]).map((name) => <button type="button" key={name} onClick={() => { onEnvironmentChange(name); setEnvironmentOpen(false); }}>{name}{name === environment && <Icon name="check" size={13} />}</button>)}</div>}</div>
          <button className="topbar-user" type="button" onClick={() => setUserOpen((open) => !open)}><span className="avatar">{initials}</span><span>{role.shortName}</span></button>
        </div>
        {userOpen && <div className="user-menu" role="dialog" aria-label="Sesión activa"><div className="user-menu-head"><span className="avatar">{initials}</span><div><strong>{session.name}</strong><span>{role.name}</span></div></div>{reviewMode && <label className="field review-persona-field"><span className="field-label">Persona de revisión</span><select aria-label="Persona de revisión" value={roleId} onChange={(event) => onRoleChange(event.target.value)}>{platformRoles.map((item) => <option value={item.id} key={item.id}>{item.name}</option>)}</select><small className="field-hint">Selector local de revisión; no reemplaza OIDC ni los permisos del BFF.</small></label>}<div className="user-menu-line"><span>Permisos efectivos</span><div className="permission-list">{role.permissions.map((permission) => <Chip tone="info" key={permission}>{permission}</Chip>)}</div></div><small>{session.status === "authenticated" ? "Los permisos determinan qué acciones podés ejecutar. Las acciones restringidas siguen visibles." : "La sesión no está habilitada para ejecutar acciones."}</small></div>}
      </header>
      <main className="content"><div className="content-inner">{children}</div></main>
    </div>
    <nav className="mobile-bottom-nav" aria-label="Navegación mobile">
      {mobileNavigation.map(([id, label, icon]) => {
        const screen = getScreenById(id);
        if (!screen) return null;
        return <button type="button" className={`mobile-nav-item ${screen.id === screenId ? "is-active" : ""}`} key={id} onClick={() => onNavigate(screen)}><Icon name={icon} size={19} /><span>{label}</span></button>;
      })}
      <button type="button" className="mobile-nav-item" onClick={onOpenSearch}><Icon name="search" size={19} /><span>Buscar</span></button>
    </nav>
  </div>;
}

export function CommandPalette({ open, onClose, onNavigate }: { open: boolean; onClose: () => void; onNavigate: (screen: ScreenDefinition) => void }) {
  const [query, setQuery] = useState("");
  if (!open) return null;
  const results = screenRegistry.filter((screen) => `${screen.id} ${screen.title} ${screen.group}`.toLowerCase().includes(query.toLowerCase())).slice(0, 12);
  return <div className="overlay-layer"><button className="overlay-backdrop" aria-label="Cerrar búsqueda" onClick={onClose} /><div className="command-palette" role="dialog" aria-label="Buscar pantalla"><label className="search-field"><Icon name="search" size={15} /><input autoFocus value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Buscar pantalla, artefacto o sección" /></label><div className="command-results">{results.length ? results.map((screen) => <button type="button" className="command-result" key={screen.id} onClick={() => { onNavigate(screen); onClose(); }}><Icon name={screen.module === "pack" ? "package" : screen.module === "rules" ? "workflow" : "layers"} size={15} /><span><strong>{screen.title}</strong><span>{screen.id} · {screen.group}</span></span><Icon name="chevron" size={13} /></button>) : <div className="empty-state"><h3>No encontramos esa pantalla</h3><p>Probá con un nombre de artefacto o un código PA.</p></div>}</div></div></div>;
}
