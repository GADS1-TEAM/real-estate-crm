"use client";

import Link from "next/link";
import { useState, type ReactNode } from "react";
import { Avatar, Chip, Icon, type IconName } from "@/components/ui/primitives";

export interface ShellNavItem {
  href: string;
  label: string;
  icon: IconName;
  disabled?: boolean;
  disabledReason?: string;
  badge?: string;
}

export function CrmShell({
  children,
  activeHref,
  navItems,
  roleName,
  userName = "Martín Quiroga",
  offlineCount,
  onSearch,
  onQuickCreate,
  onUserMenu,
  userMenuOpen,
  userMenu,
}: {
  children: ReactNode;
  activeHref: string;
  navItems: ShellNavItem[];
  roleName: string;
  userName?: string;
  offlineCount: number;
  onSearch: () => void;
  onQuickCreate: () => void;
  onUserMenu: () => void;
  userMenuOpen: boolean;
  userMenu: ReactNode;
}) {
  const [mobileNavOpen, setMobileNavOpen] = useState(false);
  const primaryMobileItems = navItems.filter((item) => !item.disabled).slice(0, 3);
  return <div className="crm-shell">
    {mobileNavOpen && <button type="button" className="mobile-sidebar-backdrop" aria-label="Cerrar menú de navegación" onClick={() => setMobileNavOpen(false)} />}
    <aside className={`sidebar ${mobileNavOpen ? "is-mobile-open" : ""}`} aria-label="Navegación principal">
      <div className="brand-lockup">
        <span className="brand-mark">b</span>
        <span className="brand-name">brick<span>/</span>eminent</span>
        <button type="button" className="mobile-sidebar-close" aria-label="Cerrar menú" onClick={() => setMobileNavOpen(false)}><Icon name="close" size={18} /></button>
      </div>
      <div className="sidebar-context"><span className="context-label">Instalación</span><strong>Inmobiliaria Norte</strong><span className="context-status"><span className="status-pulse" />Operativa</span></div>
      <nav className="sidebar-nav">
        {navItems.map((item) => item.disabled ? <div className="nav-item nav-disabled" aria-disabled="true" aria-description={item.disabledReason ?? "Capacidad diferida para una fase posterior"} title={item.disabledReason ?? "Capacidad diferida para una fase posterior"} key={item.label}><Icon name={item.icon} size={18} /><span>{item.label}</span>{item.badge && <span className="nav-badge">{item.badge}</span>}</div> : <Link className={`nav-item ${activeHref === item.href ? "is-active" : ""}`} href={item.href} key={item.label} onClick={() => setMobileNavOpen(false)}><Icon name={item.icon} size={18} /><span>{item.label}</span>{item.badge && <span className="nav-badge">{item.badge}</span>}</Link>)}
      </nav>
      <div className="sidebar-bottom"><button className="nav-item nav-button" onClick={() => { setMobileNavOpen(false); onQuickCreate(); }}><Icon name="plus" size={18} /><span>Crear rápido</span><kbd>⌘ K</kbd></button><div className="sidebar-user"><Avatar name={userName} size="small" /><div><strong>{userName}</strong><span>{roleName}</span></div><button aria-label="Abrir menú de usuario" className="user-more" onClick={() => { setMobileNavOpen(false); onUserMenu(); }}><Icon name="more" size={16} /></button></div></div>
    </aside>
    <div className="main-column">
      <header className="topbar">
        <div className="mobile-brand">
          <button type="button" className="mobile-menu-trigger" aria-label="Abrir menú principal" onClick={() => setMobileNavOpen(true)}><Icon name="menu" size={20} /></button>
          <span className="brand-mark">b</span>
          <strong>brick/eminent</strong>
        </div>
        <button className="global-search-trigger" onClick={onSearch}><Icon name="search" size={17} /><span>Buscar en toda la instalación</span><kbd>Ctrl K</kbd></button>
        <div className="topbar-actions"><button className="topbar-icon" aria-label="Abrir ayuda" title="Ayuda"><Icon name="info" size={18} /></button>{offlineCount > 0 && <Chip tone="warning" dot>{offlineCount} pendiente{offlineCount > 1 ? "s" : ""}</Chip>}<button className="topbar-avatar" aria-label="Abrir menú de usuario" onClick={onUserMenu}><Avatar name={userName} size="small" /></button></div>
        {userMenuOpen && userMenu}
      </header>
      <main className="main-content">{children}</main>
    </div>
    <nav className="mobile-bottom-nav" aria-label="Navegación mobile">{primaryMobileItems.map((item) => <Link href={item.href} className={activeHref === item.href ? "mobile-nav-item is-active" : "mobile-nav-item"} key={item.label} onClick={() => setMobileNavOpen(false)}><Icon name={item.icon} size={19} /><span>{item.label}</span></Link>)}<button className="mobile-nav-item" onClick={() => { setMobileNavOpen(false); onQuickCreate(); }}><Icon name="plus" size={19} /><span>Registrar</span></button><button className={`mobile-nav-item ${mobileNavOpen ? "is-active" : ""}`} onClick={() => setMobileNavOpen((open) => !open)}><Icon name="menu" size={19} /><span>Más</span></button></nav>
  </div>;
}

export function UserMenu({
  roleName,
  userName = "Martín Quiroga",
  userEmail = "martin@inmobiliaria.com.ar",
  onClose,
  onPermission,
  onSwitchAccount,
  onLoginScreen,
}: {
  roleName: string;
  userName?: string;
  userEmail?: string;
  onClose: () => void;
  onPermission: () => void;
  onSwitchAccount?: (roleId: "vendedor" | "responsable") => void;
  onLoginScreen?: () => void;
}) {
  const isResponsable = roleName.toLowerCase().includes("responsable");
  return <div className="user-menu" role="menu">
    <div className="user-menu-head">
      <Avatar name={userName} />
      <div>
        <strong>{userName}</strong>
        <span>{userEmail}</span>
      </div>
    </div>
    <div className="user-role-line"><span>Rol efectivo</span><Chip tone="info">{roleName}</Chip></div>
    {onSwitchAccount && (
      <button className="user-menu-item" onClick={() => onSwitchAccount(isResponsable ? "vendedor" : "responsable")}>
        <Icon name="users" size={16} />
        <span>{isResponsable ? "Usar cuenta Martín Quiroga (Vendedor)" : "Usar cuenta Rodrigo Vergara (Métricas)"}</span>
      </button>
    )}
    <button className="user-menu-item" onClick={onPermission}><Icon name="lock" size={16} /><span>Ver permisos efectivos</span></button>
    {onLoginScreen && (
      <button className="user-menu-item" onClick={onLoginScreen}><Icon name="logout" size={16} /><span>Cambiar de cuenta / Iniciar sesión</span></button>
    )}
    <button className="user-menu-item" onClick={onClose}><Icon name="close" size={16} /><span>Cerrar menú</span></button>
    <small className="user-menu-note">Acceso personalizado según tu rol.</small>
  </div>;
}
