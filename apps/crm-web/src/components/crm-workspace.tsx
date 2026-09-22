"use client";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import { CrmShell, UserMenu, type ShellNavItem } from "@/components/crm-shell";
import { Alert, Avatar, Button, Card, Chip, Drawer, EmptyState, Field, Icon, Modal, Pagination, SelectField, Skeleton, StatusDot, Tabs, Tag, TextAreaField, Toast, } from "@/components/ui/primitives";
import { AISuggestion, CriterionEditor, CurrencyAmount, MatchScoreExplanation, PipelineCard, PropertyPlaceholder, UnknownIndicator, type CriterionEvidence } from "@/components/domain/domain-components";
import { crmCatalogFamilies, DemoStoreProvider, getAnalyticsSnapshot, useDemoStore, type AnalyticsFilters, type AnalyticsMetricValue, type CatalogFamily, type DemoAction, type DemoActivity, type DemoDemand, type DemoListing, normalizeDemoState, type DemoState, type OpportunityStage, } from "@/lib/demo-store";
import { createCrmDataSource, type CrmDataSource } from "@/lib/data-source";
import { crmPersonas, getRole, hasPermission, permissionLabel, permissionReason, type Permission, type RoleId } from "@/lib/permissions";
import { getScreenById, screenRegistry, type ScreenDefinition } from "@/lib/screen-registry";
import { buildCrmScreenUrl } from "@/lib/navigation";
import { applyContactSuggestions, criterionContribution, getCommercialStatusMeaning, parseHistoricalContactText, resolveSelectedRecord, validateActivity, validateOperationClose, validateOpportunityClose, validateOpportunityStageChange, validateReason, type CommercialStatus } from "@/lib/workflow-rules";
const mode = process.env.NEXT_PUBLIC_CRM_WEB_MODE ?? "bff";
const navItems: ShellNavItem[] = [
    { href: "/inicio", label: "Inicio", icon: "home" },
    { href: "/contactos", label: "Contactos", icon: "users" },
    { href: "/inmuebles", label: "Inmuebles", icon: "building" },
    { href: "/publicaciones", label: "Publicaciones", icon: "external" },
    { href: "/captaciones", label: "Captaciones", icon: "briefcase" },
    { href: "/busquedas", label: "Búsquedas", icon: "target" },
    { href: "/compatibilidades", label: "Compatibilidades", icon: "spark" },
    { href: "/oportunidades", label: "Oportunidades", icon: "pipeline" },
    { href: "/actividad", label: "Actividad", icon: "activity" },
    { href: "/agenda", label: "Agenda · pronto", icon: "calendar", disabled: true, disabledReason: "La agenda está planificada para una fase posterior y todavía no está disponible." },
    { href: "/metricas", label: "Métricas", icon: "chart" },
    { href: "/administracion", label: "Administración", icon: "settings" },
];
const defaultScreens: Record<string, string> = {
    inicio: "INI-01",
    contactos: "PTY-01",
    inmuebles: "PRP-01",
    publicaciones: "LST-01",
    captaciones: "CAP-01",
    busquedas: "DEM-01",
    compatibilidades: "MAT-01",
    oportunidades: "OPP-01",
    actividad: "ACT-05",
    metricas: "ANA-01",
    asistente: "IA-01",
    administracion: "ADM-01",
};
const routeByModule: Record<string, string> = {
    SHL: "/inicio", AUT: "/login", INI: "/inicio", PTY: "/contactos", PRP: "/inmuebles", LST: "/publicaciones",
    CAP: "/captaciones", DEM: "/busquedas", MAT: "/compatibilidades", OPP: "/oportunidades", COM: "/oportunidades",
    ACT: "/actividad", ANA: "/metricas", IA: "/asistente", ADM: "/administracion", GLB: "/inicio", AGD: "/agenda",
    OMN: "/actividad", SYN: "/publicaciones", DOC: "/oportunidades", CMS: "/administracion", RNT: "/administracion",
    MTN: "/administracion", IDR: "/contactos", PAD: "/administracion",
};
const sectionTitles: Record<string, string> = {
    inicio: "Inicio", contactos: "Empresas y contactos", inmuebles: "Inmuebles", publicaciones: "Publicaciones",
    captaciones: "Captaciones", busquedas: "Búsquedas", compatibilidades: "Compatibilidades", oportunidades: "Oportunidades",
    actividad: "Actividad", metricas: "Métricas", asistente: "Asistente", administracion: "Administración",
};
const stageOrder: OpportunityStage[] = ["Nuevo", "Contacto", "Visita", "Negociación", "Reserva", "Operación"];
type ScreenNavigator = (id: string, entityId?: string) => void;

function contactCommercialStatus(contact: { commercialStatus?: CommercialStatus; status: string }): CommercialStatus {
    if (contact.commercialStatus) return contact.commercialStatus;
    if (contact.status === "No contactar") return "DO_NOT_CONTACT";
    if (contact.status === "Archivado") return "INACTIVE";
    return "CUSTOMER";
}

function commercialStatusTone(status: CommercialStatus): "success" | "warning" | "neutral" {
    return status === "CUSTOMER" ? "success" : status === "DO_NOT_CONTACT" ? "warning" : "neutral";
}

function propertyCriterionValue(criterion: { label: string }, property: { title: string; address: string; bedrooms: string }): string {
    const label = criterion.label.toLowerCase();
    if (label.includes("ambiente")) return property.bedrooms.match(/\d+/)?.[0] ?? "UNKNOWN";
    if (label.includes("barrio")) return `${property.title} ${property.address}`.toLowerCase().includes("villa crespo") ? "Villa Crespo" : "UNKNOWN";
    return "UNKNOWN";
}

function parseOptionalMoney(value: string): number | null | undefined {
    if (!value.trim()) return null;
    const parsed = Number(value.replace(/\./g, "").replace(",", "."));
    return Number.isFinite(parsed) && parsed >= 0 ? parsed : undefined;
}

function selectedRecord<T extends { id: string }>(records: readonly T[], entityId?: string | null): T | undefined {
    return entityId === "" ? undefined : resolveSelectedRecord(records, entityId);
}

function UnavailableRecord() {
    return <Card><EmptyState icon="search" title="Registro no disponible" description="El registro solicitado no existe o ya no está disponible."/></Card>;
}

function ScreenTabs({ screen, entityId, tabs, onNavigate }: {
    screen: ScreenDefinition;
    entityId?: string | null;
    tabs: { id: string; label: string; count?: number; entityId?: string }[];
    onNavigate: ScreenNavigator;
}) {
    const active = tabs.some((tab) => tab.id === screen.id) ? screen.id : tabs[0].id;
    const screenModule = screen.id.slice(0, 3);
    return <Tabs tabs={tabs} active={active} onChange={(id) => {
        const target = tabs.find((tab) => tab.id === id);
        onNavigate(id, target?.entityId ?? (id.slice(0, 3) === screenModule ? entityId ?? undefined : undefined));
    }}/>;
}

function useLocalPagination<T>(records: readonly T[], query: string, pageSize = 25) {
    const [page, setPage] = useState(1);
    useEffect(() => setPage(1), [query]);
    const pages = Math.max(1, Math.ceil(records.length / pageSize));
    const safePage = Math.min(page, pages);
    return { page: safePage, pages, items: records.slice((safePage - 1) * pageSize, safePage * pageSize), setPage };
}
export function CrmApp({ initialSection, catalogOnly = false }: {
    initialSection: string;
    catalogOnly?: boolean;
}) {
    return <DemoStoreProvider active={mode === "demo"}>
<CrmWorkspace initialSection={initialSection} catalogOnly={catalogOnly}/>
</DemoStoreProvider>;
}
function saveActionMutation(dataSource: CrmDataSource, action: DemoAction) {
    try {
        switch (action.type) {
            case "contact/create":
                dataSource.saveMutation("createContact", action.item);
                break;
            case "contact/update":
                dataSource.saveMutation("updateContact", { partyId: action.id, ...action.changes });
                break;
            case "party/relate":
                dataSource.saveMutation("relateContactToCompany", action);
                break;
            case "property/create":
                dataSource.saveMutation("createProperty", action.item);
                break;
            case "property/update":
                dataSource.saveMutation("updateProperty", { propertyId: action.id, ...action.changes });
                break;
            case "listing/create":
                dataSource.saveMutation("createListing", action.item);
                break;
            case "listing/update":
                dataSource.saveMutation("updateListing", { listingId: action.id, ...action.changes });
                break;
            case "demand/create":
                dataSource.saveMutation("createRequirement", action.item);
                break;
            case "activity/add":
                dataSource.saveMutation("recordActivity", action.item);
                break;
            case "user/invite":
                dataSource.saveMutation("createUser", action.item);
                break;
            case "user/update":
                dataSource.saveMutation("updateUser", { userId: action.id, ...action.changes });
                break;
            case "catalog/update-entry":
                dataSource.saveMutation("updateCatalogEntry", { entryId: action.id, label: action.label, status: action.status });
                break;
            default:
                break;
        }
    } catch (e) {
        console.error("Error sending mutation to BFF:", e);
    }
}

function CrmWorkspace({ initialSection, catalogOnly }: {
    initialSection: string;
    catalogOnly: boolean;
}) {
    const router = useRouter();
    const pathname = usePathname();
    const searchParams = useSearchParams();
    const { state, dispatch: rawDispatch } = useDemoStore();
    const [roleId, setRoleId] = useState<RoleId>("vendedor");
    const [searchOpen, setSearchOpen] = useState(false);
    const [quickCreateOpen, setQuickCreateOpen] = useState(false);
    const [userMenuOpen, setUserMenuOpen] = useState(false);
    const [permissionOpen, setPermissionOpen] = useState(false);
    const [offlineOpen, setOfflineOpen] = useState(false);
    const [sourceStatus, setSourceStatus] = useState<"idle" | "loading" | "ready" | "error">(mode === "demo" ? "ready" : "idle");
    const [sourceError, setSourceError] = useState<string | null>(null);
    const [sourceAttempt, setSourceAttempt] = useState(0);
    const [remoteState, setRemoteState] = useState<DemoState | null>(null);
    const dataSource = useMemo(() => createCrmDataSource(mode), []);
    const dispatch = useCallback((action: DemoAction) => {
        rawDispatch(action);
        if (mode !== "demo") {
            saveActionMutation(dataSource, action);
        }
    }, [rawDispatch, dataSource]);
    const [toast, setToast] = useState<{
        message: string;
        tone?: "success" | "info" | "warning";
    } | null>(null);
    const role = getRole(roleId);
    useEffect(() => {
        const handler = (event: KeyboardEvent) => {
            if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "k") {
                event.preventDefault();
                setSearchOpen(true);
            }
        };
        window.addEventListener("keydown", handler);
        return () => window.removeEventListener("keydown", handler);
    }, []);
    useEffect(() => {
        if (!toast)
            return;
        const timer = window.setTimeout(() => setToast(null), 3500);
        return () => window.clearTimeout(timer);
    }, [toast]);
    useEffect(() => {
        if (mode === "demo")
            return;
        let active = true;
        setSourceStatus("loading");
        setSourceError(null);
        dataSource.getScreenData(screenForRequest(searchParams.get("screen"), initialSection)).then((payload) => {
            if (!active)
                return;
            const candidate = getStateCandidate(payload);
            if (isDemoStateSnapshot(candidate))
                setRemoteState(normalizeDemoState(candidate));
            setSourceStatus("ready");
        }).catch((error: unknown) => {
            if (!active)
                return;
            setSourceStatus("error");
            setSourceError(error instanceof Error ? error.message : "CRM_BFF_UNKNOWN");
        });
        return () => { active = false; };
    }, [dataSource, initialSection, searchParams, sourceAttempt]);
    const entityId = searchParams.get("entity");
    const requestedScreen = searchParams.get("screen") ?? defaultScreens[initialSection] ?? "INI-01";
    const screen = getScreenById(requestedScreen) ?? getScreenById(defaultScreens[initialSection] ?? "INI-01") ?? screenRegistry[0];
    const activeHref = navItems.find((item) => item.href === pathname)?.href ?? `/${initialSection}`;
    const navigateToScreen: ScreenNavigator = (screenId, entityId) => {
        const target = getScreenById(screenId);
        if (!target)
            return;
        router.push(buildCrmScreenUrl(routeByModule[target.module] ?? "/inicio", target.id, entityId));
        setQuickCreateOpen(false);
        setSearchOpen(false);
    };
    const showToast = (message: string, tone: "success" | "info" | "warning" = "success") => setToast({ message, tone });
    if (catalogOnly)
        return <DesignCatalog />;
    return <CrmShell activeHref={activeHref} navItems={navItems} roleName={role.name} offlineCount={state.offlineQueue.length} onSearch={() => setSearchOpen(true)} onQuickCreate={() => setQuickCreateOpen(true)} onUserMenu={() => setUserMenuOpen((value) => !value)} userMenuOpen={userMenuOpen} userMenu={<UserMenu roleName={role.name} onClose={() => setUserMenuOpen(false)} onPermission={() => { setUserMenuOpen(false); setPermissionOpen(true); }}/>}>
    <PageHeader screen={screen} section={initialSection} onQuickCreate={() => setQuickCreateOpen(true)} onNavigate={navigateToScreen}/>
    {mode !== "demo" ? sourceStatus === "loading" ? <BffLoadingState /> : sourceStatus === "error" ? <BffUnavailableState error={sourceError} onRetry={() => setSourceAttempt((attempt) => attempt + 1)}/> : remoteState ? <FeatureView key={`${screen.id}:${entityId ?? ""}`} entityId={entityId} screen={screen} state={remoteState} roleId={roleId} onRoleChange={setRoleId} onNavigate={navigateToScreen} onToast={showToast} dispatch={dispatch}/> : <BffResponseState /> : <FeatureView key={`${screen.id}:${entityId ?? ""}`} entityId={entityId} screen={screen} state={state} roleId={roleId} onRoleChange={setRoleId} onNavigate={navigateToScreen} onToast={showToast} dispatch={dispatch}/>}
    {mode === "demo" && state.offlineQueue.length > 0 && <div className="offline-banner">
<Icon name="wifi" size={16}/>
<span>
<strong>{state.offlineQueue.length} acción{state.offlineQueue.length > 1 ? "es" : ""} guardada{state.offlineQueue.length > 1 ? "s" : ""} sin conexión.</strong> Se sincronizan cuando vuelva la red.</span>
<Button variant="ghost" size="xsmall" onClick={() => setOfflineOpen(true)}>Ver cola</Button>
</div>}
    <QuickCreateDrawer open={quickCreateOpen} onClose={() => setQuickCreateOpen(false)} onNavigate={navigateToScreen} roleId={roleId}/>
    <PermissionModal open={permissionOpen} roleId={roleId} onClose={() => setPermissionOpen(false)} onRoleChange={(next) => { setRoleId(next); setPermissionOpen(false); showToast(`Vista cambiada a ${getRole(next).name}.`, "info"); }} onPersonaChange={(persona) => { setRoleId(persona.roleId); setPermissionOpen(false); showToast(`Vista de ${persona.name} cargada para revisión.`, "info"); navigateToScreen(persona.startScreen); }}/>
    <OfflineQueueDrawer open={offlineOpen} state={state} onClose={() => setOfflineOpen(false)} onFlush={() => { dispatch({ type: "offline/flush" }); setOfflineOpen(false); showToast("Cola sincronizada.", "info"); }}/>
    {searchOpen && <GlobalSearch state={state} onClose={() => setSearchOpen(false)} onNavigate={navigateToScreen}/>}
    {toast && <Toast message={toast.message} tone={toast.tone} onClose={() => setToast(null)}/>}
  </CrmShell>;
}
function PageHeader({ screen, section, onQuickCreate, onNavigate }: {
    screen: ScreenDefinition;
    section: string;
    onQuickCreate: () => void;
    onNavigate: ScreenNavigator;
}) {
    const title = sectionTitles[section] ?? "CRM";
    const isDefault = defaultScreens[section] === screen.id;
    return <div className="page-header">
<div>
<div className="breadcrumb">
<span>CRM</span>
<Icon name="chevron" size={13}/>
<span>{title}</span>{!isDefault && <>
<Icon name="chevron" size={13}/>
<span>{screen.title}</span>
</>}</div>
<div className="page-title-row">
<h1>{isDefault ? title : screen.title}</h1>
</div>
<p>{isDefault ? pageDescription(section) : screenDescription(screen)}</p>
</div>
<div className="page-actions">{isDefault && <Button variant="outline" icon="plus" onClick={onQuickCreate}>Crear rápido</Button>}{!isDefault && <Button variant="ghost" icon="arrow" onClick={() => onNavigate(defaultScreens[section] ?? "INI-01")}>Volver a {title}</Button>}</div>
</div>;
}
function screenDescription(screen: ScreenDefinition): string {
    return screen.deferred ? "Esta sección estará disponible más adelante." : "Gestioná esta parte de la operación desde un solo lugar.";
}
function pageDescription(section: string): string {
    const descriptions: Record<string, string> = {
        inicio: "Tu día comercial, con señales accionables y sin inventar tareas.", contactos: "Personas y empresas con contexto, relaciones e historial.", inmuebles: "Inventario de propiedades con datos desconocidos explícitos.", publicaciones: "Contenido comercial, términos y estado de cada publicación.", captaciones: "Del primer contacto al mandato y la publicación.", busquedas: "Demanda activa y criterios con peso visible.", compatibilidades: "Coincidencias explicables entre demanda y oferta.", oportunidades: "El proceso comercial visible por etapa y fuente.", actividad: "Hechos ocurridos, notas y trazabilidad de la operación.", metricas: "Indicadores por vendedor y equipo, con drill-down.", asistente: "Sugerencias revisables con evidencia y acción previsualizada.", administracion: "Usuarios, permisos y catálogos de esta instalación.",
    };
    return descriptions[section] ?? "Gestión comercial inmobiliaria.";
}
function screenForRequest(requested: string | null, section: string): string {
    return requested ?? defaultScreens[section] ?? "INI-01";
}
function getStateCandidate(payload: unknown): unknown {
    if (!payload || typeof payload !== "object")
        return payload;
    const record = payload as Record<string, unknown>;
    return record.state ?? record.snapshot ?? payload;
}
function isDemoStateSnapshot(value: unknown): value is DemoState {
    if (!value || typeof value !== "object")
        return false;
    const record = value as Record<string, unknown>;
    return Array.isArray(record.contacts) && Array.isArray(record.properties);
}
function BffLoadingState() {
    return <Card className="blocking-state">
<div className="blocking-icon">
<Icon name="refresh" size={28}/>
</div>
<div>
<span className="eyebrow">Fuente de datos</span>
<h2>Cargando información</h2>
<p>Estamos consultando la sección solicitada. En unos instantes vas a ver la información disponible.</p>
<div className="skeleton-state">
<Skeleton width="78%"/>
<Skeleton width="48%"/>
</div>
</div>
</Card>;
}
function BffUnavailableState({ error, onRetry }: {
    error?: string | null;
    onRetry?: () => void;
}) {
    const notConfigured = !error || error === "CRM_BFF_NOT_CONFIGURED";
    return <Card className="blocking-state">
<div className="blocking-icon">
<Icon name="database" size={28}/>
</div>
<div>
<span className="eyebrow">Fuente de datos</span>
<h2>{notConfigured ? "Conexión pendiente" : "No pudimos cargar la información"}</h2>
<p>{notConfigured ? "La conexión de datos todavía no está configurada para esta instalación." : "Se produjo un problema al consultar la información. Revisá la conexión y volvé a intentar."}</p>
<Alert tone="info" title="Datos no disponibles">La interfaz sigue disponible y va a mostrar la información cuando la conexión esté lista.</Alert>{onRetry && <div className="state-actions">
<Button variant="outline" icon="refresh" onClick={onRetry}>Reintentar</Button>
</div>}</div>
</Card>;
}
function BffResponseState() {
    return <Card className="blocking-state">
<div className="blocking-icon">
<Icon name="database" size={28}/>
</div>
<div>
<span className="eyebrow">Fuente de datos</span>
<h2>Información recibida</h2>
<p>La conexión respondió, pero el formato de esta sección todavía necesita una adaptación para mostrarse correctamente.</p>
<Alert tone="info" title="Preparando datos">La sección va a estar disponible cuando termine la adaptación de la información.</Alert>
</div>
</Card>;
}
function QuickCreateDrawer({ open, onClose, onNavigate, roleId }: {
    open: boolean;
    onClose: () => void;
    onNavigate: ScreenNavigator;
    roleId: RoleId;
}) {
    const options = [
        { id: "PTY-02", label: "Contacto", detail: "Persona o empresa", icon: "person" as const, permission: "party.write" as Permission },
        { id: "PRP-03", label: "Inmueble", detail: "Alta rápida con placeholder", icon: "building" as const, permission: "property.write" as Permission },
        { id: "DEM-02", label: "Búsqueda", detail: "Necesidad de un contacto", icon: "target" as const, permission: "commercial.write" as Permission },
        { id: "CAP-02", label: "Captación", detail: "Propietario y propiedad", icon: "briefcase" as const, permission: "commercial.write" as Permission },
        { id: "OPP-03", label: "Oportunidad", detail: "Búsqueda o captación", icon: "pipeline" as const, permission: "commercial.write" as Permission },
        { id: "ACT-01", label: "Actividad", detail: "Hecho ocurrido", icon: "activity" as const, permission: "party.write" as Permission },
    ];
    return <Drawer open={open} onClose={onClose} title="Crear rápido" eyebrow="Acción contextual" footer={<div className="drawer-footer-note">
<Icon name="info" size={15}/>Las acciones no habilitadas muestran el motivo del permiso.</div>}>
<div className="quick-create-list">{options.map((option) => { const allowed = hasPermission(roleId, option.permission); return <button type="button" className={`quick-create-option ${!allowed ? "is-disabled" : ""}`} key={option.id} disabled={!allowed} onClick={() => onNavigate(option.id)}>
<span className="quick-create-icon">
<Icon name={option.icon} size={19}/>
</span>
<span>
<strong>{option.label}</strong>
<small>{option.detail}</small>{!allowed && <em>{permissionReason(roleId, option.permission)}</em>}</span>
<Icon name={allowed ? "arrow" : "lock"} size={17}/>
</button>; })}</div>
</Drawer>;
}
function PermissionModal({ open, roleId, onClose, onRoleChange, onPersonaChange }: {
    open: boolean;
    roleId: RoleId;
    onClose: () => void;
    onRoleChange: (roleId: RoleId) => void;
    onPersonaChange: (persona: typeof crmPersonas[number]) => void;
}) {
    const activePersona = crmPersonas.find((persona) => persona.roleId === roleId) ?? crmPersonas[0];
    return <Modal open={open} title="Permisos efectivos" onClose={onClose} footer={<Button variant="primary" onClick={onClose}>Cerrar</Button>}>
<div className="permission-summary">
<Avatar name={activePersona.name}/>
<div>
<strong>{activePersona.name}</strong>
<span>La autorización efectiva combina tu rol con el alcance de la instalación.</span>
</div>
</div>
<div className="role-switcher">
<span className="field-label">Revisar persona documentada</span>
<div className="role-options">{crmPersonas.map((persona) => <button type="button" key={persona.name} className={activePersona.name === persona.name ? "role-option is-active" : "role-option"} onClick={() => onPersonaChange(persona)}>
<strong>{persona.name}</strong>
<span>{persona.roleLabel} · {persona.scope}</span>
</button>)}</div>
</div>
<div className="role-switcher">
<span className="field-label">Revisar como</span>
<div className="role-options">{(["vendedor", "responsable", "direccion", "administradora"] as RoleId[]).map((id) => <button type="button" key={id} className={roleId === id ? "role-option is-active" : "role-option"} onClick={() => onRoleChange(id)}>
<strong>{getRole(id).name}</strong>
<span>{getRole(id).scope}</span>
</button>)}</div>
</div>
<div className="permission-list">{["party.write", "property.write", "commercial.write", "analytics.read", "admin.write"].map((permission) => <div className="permission-row" key={permission}>
<span>{permissionLabel(permission as Permission)}</span>
<Chip tone={hasPermission(roleId, permission as Permission) ? "success" : "neutral"}>{hasPermission(roleId, permission as Permission) ? "Permitido" : "No habilitado"}</Chip>
</div>)}</div>
</Modal>;
}
function OfflineQueueDrawer({ open, state, onClose, onFlush }: {
    open: boolean;
    state: DemoState;
    onClose: () => void;
    onFlush: () => void;
}) {
    return <Drawer open={open} onClose={onClose} title="Cola offline" eyebrow="Captura en terreno" footer={<div className="form-footer">
<Button variant="ghost" onClick={onClose}>Cerrar</Button>
<Button icon="refresh" onClick={onFlush}>Sincronizar cola</Button>
</div>}>
<Alert tone="warning" title="Sin conexión confirmada">Estas acciones se guardaron en el dispositivo y se sincronizarán cuando vuelva la conexión.</Alert>
<div className="offline-queue-list">{state.offlineQueue.map((item) => <div className="offline-queue-row" key={item.id}>
<span className="activity-icon">
<Icon name={item.kind === "photo" ? "camera" : "wifi"} size={16}/>
</span>
<div>
<strong>{item.label}</strong>
<small>{item.createdAt}</small>
</div>
<Tag tone="warning">Pendiente</Tag>
</div>)}</div>
</Drawer>;
}
function GlobalSearch({ state, onClose, onNavigate }: {
    state: DemoState;
    onClose: () => void;
    onNavigate: ScreenNavigator;
}) {
    const [query, setQuery] = useState("");
    const results = useMemo(() => {
        const normalized = query.trim().toLowerCase();
        const screens = screenRegistry.filter((screen) => !screen.deferred).filter((screen) => `${screen.id} ${screen.title}`.toLowerCase().includes(normalized || "__recent__")).slice(0, normalized ? 5 : 4).map((screen) => ({ id: screen.id, title: screen.title, detail: "Acceso directo", module: screen.module, entityId: undefined as string | undefined }));
        if (!normalized)
            return screens.concat(state.contacts.slice(0, 2).map((contact) => ({ id: contact.kind === "Empresa" ? "PTY-06" : "PTY-05", entityId: contact.id, title: contact.name, detail: `Contacto · ${contact.phone}`, module: "PTY" })));
        const contacts = state.contacts.filter((contact) => `${contact.name} ${contact.phone} ${contact.email}`.toLowerCase().includes(normalized)).slice(0, 3).map((contact) => ({ id: contact.kind === "Empresa" ? "PTY-06" : "PTY-05", entityId: contact.id, title: contact.name, detail: `Contacto · ${contact.email}`, module: "PTY" }));
        const properties = state.properties.filter((property) => `${property.title} ${property.address} ${property.type}`.toLowerCase().includes(normalized)).slice(0, 3).map((property) => ({ id: "PRP-06", entityId: property.id, title: property.title, detail: `Inmueble · ${property.address}`, module: "PRP" }));
        const listings = state.listings.filter((listing) => listing.title.toLowerCase().includes(normalized)).slice(0, 2).map((listing) => ({ id: "LST-04", entityId: listing.id, title: listing.title, detail: "Publicación", module: "LST" }));
        const captations = state.captations.filter((item) => `${item.owner} ${state.properties.find((property) => property.id === item.propertyId)?.title ?? ""}`.toLowerCase().includes(normalized)).slice(0, 2).map((item) => ({ id: "CAP-03", entityId: item.id, title: state.properties.find((property) => property.id === item.propertyId)?.title ?? item.id, detail: "Captación", module: "CAP" }));
        const demands = state.demands.filter((item) => item.title.toLowerCase().includes(normalized)).slice(0, 2).map((item) => ({ id: "DEM-04", entityId: item.id, title: item.title, detail: "Búsqueda", module: "DEM" }));
        const opportunities = state.opportunities.filter((item) => item.title.toLowerCase().includes(normalized)).slice(0, 2).map((item) => ({ id: "OPP-04", entityId: item.id, title: item.title, detail: "Oportunidad", module: "OPP" }));
        const reservations = state.reservations.filter((item) => item.propertyTitle.toLowerCase().includes(normalized)).slice(0, 2).map((item) => ({ id: "COM-09", entityId: item.id, title: item.propertyTitle, detail: "Reserva", module: "COM" }));
        const operations = state.operations.filter((item) => item.propertyTitle.toLowerCase().includes(normalized)).slice(0, 2).map((item) => ({ id: "COM-12", entityId: item.id, title: item.propertyTitle, detail: "Operación", module: "COM" }));
        return [...contacts, ...properties, ...listings, ...captations, ...demands, ...opportunities, ...reservations, ...operations, ...screens];
    }, [query, state]);
    return <div className="search-layer">
<button className="overlay-backdrop" aria-label="Cerrar búsqueda" onClick={onClose}/>
<div className="search-panel" role="dialog" aria-label="Búsqueda global">
<div className="search-input-wrap">
<Icon name="search" size={20}/>
<input autoFocus value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Buscar contactos, inmuebles, publicaciones..."/>
<kbd>ESC</kbd>
<button aria-label="Cerrar" onClick={onClose}>
<Icon name="close" size={18}/>
</button>
</div>
<div className="search-hint">
<span>{query ? "Resultados de registros y pantallas" : "Accesos recientes"}</span>
<span>También podés usar Ctrl K</span>
</div>
<div className="search-results">{results.length ? results.map((result, index) => <button type="button" className="search-result" key={`${result.id}-${result.title}-${index}`} onClick={() => onNavigate(result.id, result.entityId)}>
<span className="search-result-icon">
<Icon name={iconForModule(result.module)} size={17}/>
</span>
<span>
<strong>{result.title}</strong>
<small>{result.detail}</small>
</span>
<Icon name="arrow" size={16}/>
</button>) : <EmptyState icon="search" title="Sin resultados" description="Probá con un nombre, una dirección o una acción."/>}</div>
<div className="search-footer">
<span>
<kbd>↑↓</kbd> navegar</span>
<span>
<kbd>↵</kbd> abrir</span>
<span>
<kbd>ESC</kbd> cerrar</span>
</div>
</div>
</div>;
}
function iconForModule(module: string): "users" | "building" | "target" | "pipeline" | "activity" | "chart" | "settings" | "spark" | "briefcase" {
    if (module === "PTY")
        return "users";
    if (["PRP", "LST"].includes(module))
        return "building";
    if (["DEM", "MAT"].includes(module))
        return "target";
    if (module === "OPP")
        return "pipeline";
    if (module === "ACT" || module === "COM")
        return "activity";
    if (module === "ANA")
        return "chart";
    if (module === "ADM")
        return "settings";
    if (module === "IA")
        return "spark";
    return "briefcase";
}
function FeatureView({ screen, state, entityId, roleId, onRoleChange, onNavigate, onToast, dispatch }: {
    screen: ScreenDefinition;
    state: DemoState;
    entityId: string | null;
    roleId: RoleId;
    onRoleChange: (role: RoleId) => void;
    onNavigate: ScreenNavigator;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    dispatch: React.Dispatch<DemoAction>;
}) {
    if (screen.deferred)
        return <DeferredSurface screen={screen}/>;
    if (screen.renderKey === "auth")
        return <AuthSurface screen={screen} onNavigate={onNavigate}/>;
    if (screen.renderKey === "global")
        return <GlobalStateSurface screen={screen} onToast={onToast} onNavigate={onNavigate}/>;
    switch (screen.renderKey) {
        case "inicio": return <HomeView state={state} onNavigate={onNavigate} onToast={onToast} dispatch={dispatch}/>;
        case "party": return <PartyView entityId={entityId} screen={screen} state={state} onNavigate={onNavigate} onToast={onToast} dispatch={dispatch}/>;
        case "property": return <PropertyView entityId={entityId} screen={screen} state={state} onNavigate={onNavigate} onToast={onToast} dispatch={dispatch}/>;
        case "listing": return <ListingView entityId={entityId} screen={screen} state={state} onNavigate={onNavigate} onToast={onToast} dispatch={dispatch}/>;
        case "captation": return <CaptationView entityId={entityId} screen={screen} state={state} onNavigate={onNavigate} onToast={onToast} dispatch={dispatch}/>;
        case "demand": return screen.id === "DEM-05" ? <PartyView entityId={entityId} screen={screen} state={state} onNavigate={onNavigate} onToast={onToast} dispatch={dispatch}/> : <DemandView entityId={entityId} screen={screen} state={state} onNavigate={onNavigate} onToast={onToast} dispatch={dispatch}/>;
        case "matching": return <MatchingView entityId={entityId} screen={screen} state={state} onNavigate={onNavigate} onToast={onToast} dispatch={dispatch}/>;
        case "pipeline": return <PipelineView entityId={entityId} screen={screen} state={state} onNavigate={onNavigate} onToast={onToast} dispatch={dispatch}/>;
        case "commercial": return <CommercialView entityId={entityId} screen={screen} state={state} onNavigate={onNavigate} onToast={onToast} dispatch={dispatch}/>;
        case "activity": return <ActivityView entityId={entityId} screen={screen} state={state} onNavigate={onNavigate} onToast={onToast} dispatch={dispatch}/>;
        case "analytics": return <AnalyticsView screen={screen} state={state} onNavigate={onNavigate}/>;
        case "assistant": return <AssistantView screen={screen} state={state} onNavigate={onNavigate} onToast={onToast} dispatch={dispatch}/>;
        case "admin": return <AdminView screen={screen} state={state} roleId={roleId} onRoleChange={onRoleChange} onNavigate={onNavigate} onToast={onToast} dispatch={dispatch}/>;
        default: return <HomeView state={state} onNavigate={onNavigate} onToast={onToast} dispatch={dispatch}/>;
    }
}
function HomeView({ state, onNavigate, onToast, dispatch }: {
    state: DemoState;
    onNavigate: ScreenNavigator;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const attention = state.opportunities.filter((item) => item.daysInStage >= 4).slice(0, 3);
    const grossFees = state.opportunities.reduce((total, opportunity) => total + opportunity.fee, 0);
    const activeContacts = state.contacts.filter((contact) => contactCommercialStatus(contact) !== "INACTIVE" && contactCommercialStatus(contact) !== "DO_NOT_CONTACT").length;
    const activeDemands = state.demands.filter((demand) => demand.status === "Activa").length;
    const visits = state.activities.filter((activity) => activity.type === "Visita").length;
    const listingsWithoutMandate = state.listings.filter((listing) => listing.status === "Activa" && listing.mandate !== "Firmado").length;
    return <div className="feature-stack">
    <Alert tone="warning" title={`${attention.length} oportunidades requieren atención`}>La señal se deriva de la antigüedad de la etapa. No crea una tarea ni un recordatorio.</Alert>
    <div className="hero-grid">
<Card className="home-hero">
<div className="hero-overline">
<span className="eyebrow">Mi día · miércoles 9 de septiembre</span>
<Chip tone="success" dot>Instalación operativa</Chip>
</div>
<h2>Buen día, Martín.</h2>
<p>Tenés <strong>{attention.length} oportunidades</strong> que conviene mirar antes de las 12:00.</p>
<div className="hero-actions">
<Button icon="arrow" onClick={() => onNavigate("INI-02")}>Ver requiere atención</Button>
<Button variant="secondary" icon="plus" onClick={() => onNavigate("ACT-01")}>Registrar actividad</Button>
</div>
</Card>
<Card className="hero-side">
<span className="eyebrow">Honorarios abiertos</span>
<strong className="hero-number">
<CurrencyAmount value={grossFees} currency="ARS"/>
</strong>
<span className="muted">Suma cruda · no ponderada</span>
<div className="hero-side-foot">
<span>
<Icon name="chart" size={15}/> {state.opportunities.length} oportunidades</span>
<button onClick={() => onNavigate("ANA-01")}>Ver métricas <Icon name="arrow" size={14}/>
</button>
</div>
</Card>
</div>
    <div className="kpi-grid">
<Kpi title="Contactos activos" value={String(activeContacts)} detail="Excluye archivados y no contactar" tone="blue"/>
<Kpi title="Búsquedas activas" value={String(activeDemands)} detail={`${state.demands.filter((demand) => demand.criteria.some((criterion) => criterion.value === "UNKNOWN")).length} con criterios incompletos`} tone="orange"/>
<Kpi title="Visitas ocurridas" value={String(visits)} detail="Hechos registrados" tone="green"/>
<Kpi title="Publicaciones activas" value={String(state.listings.filter((item) => item.status === "Activa").length)} detail={`${listingsWithoutMandate} sin mandato firmado`} tone="purple"/>
</div>
    <div className="content-grid content-grid-home">
<Card>
<SectionHeading eyebrow="Requiere atención" title="Lo que pide una mirada" action={<Button variant="ghost" size="small" onClick={() => onNavigate("INI-02")}>Ver todo <Icon name="arrow" size={14}/>
</Button>}/>
<div className="attention-list">{attention.map((opportunity) => <button className="attention-row" key={opportunity.id} onClick={() => onNavigate("OPP-04", opportunity.id)}>
<span className="attention-icon">
<Icon name={opportunity.daysInStage >= 8 ? "clock" : "alert"} size={17}/>
</span>
<span>
<strong>{opportunity.title}</strong>
<small>{opportunity.stage} · {opportunity.daysInStage} días sin cambio · {opportunity.owner}</small>
</span>
<Icon name="arrow" size={15}/>
</button>)}</div>
</Card>
<Card>
<SectionHeading eyebrow="Embudo" title="Proceso comercial" action={<Button variant="ghost" size="small" onClick={() => onNavigate("OPP-01")}>Abrir tablero <Icon name="arrow" size={14}/>
</Button>}/>
<div className="mini-funnel">{stageOrder.slice(0, 5).map((stage) => { const count = state.opportunities.filter((opportunity) => opportunity.stage === stage).length; const max = Math.max(1, ...stageOrder.map((item) => state.opportunities.filter((opportunity) => opportunity.stage === item).length)); return <div className="mini-funnel-row" key={stage}>
<span>{stage}</span>
<span className="mini-bar">
<i style={{ width: `${Math.max(8, Math.round((count / max) * 100))}%` }}/>
</span>
<strong>{count}</strong>
</div>; })}</div>
<div className="funnel-note">
<Icon name="info" size={15}/>Cada tarjeta conserva su fuente: Búsqueda o Captación.</div>
</Card>
{state.aiSuggestions.find((suggestion) => suggestion.id === "ai-surface")?.status === "pending" && <AISuggestion title="Falta una superficie" body="La búsqueda de Ana todavía no tiene superficie cubierta. Podés dejarla como desconocida y seguir comparando." evidence="Origen: conversación histórica · confianza media" onReview={() => { dispatch({ type: "ai/review", id: "ai-surface" }); onNavigate("DEM-03", state.demands[0]?.id); onToast("Sugerencia abierta para revisar; no se modificó la búsqueda.", "info"); }} onDismiss={() => { dispatch({ type: "ai/dismiss", id: "ai-surface" }); onToast("Sugerencia descartada; no se modificó ningún dato.", "info"); }}/>} 
</div>
    <div className="mobile-day-card">
<SectionHeading eyebrow="En la calle" title="Mi día mobile"/>
<div className="quick-mobile-actions">
<button onClick={() => onNavigate("COM-01")}>
<Icon name="calendar" size={20}/>
<span>Registrar visita</span>
</button>
<button onClick={() => onNavigate("ACT-01")}>
<Icon name="activity" size={20}/>
<span>Nota rápida</span>
</button>
<button onClick={() => onNavigate("PTY-05")}>
<Icon name="users" size={20}/>
<span>Abrir contacto</span>
</button>
</div>
</div>
  </div>;
}
function Kpi({ title, value, detail, tone }: {
    title: string;
    value: string;
    detail: string;
    tone: "blue" | "orange" | "green" | "purple";
}) {
    return <Card className="kpi-card">
<span className={`kpi-mark kpi-${tone}`}/>
<span className="kpi-label">{title}</span>
<strong>{value}</strong>
<span className="kpi-detail">{detail}</span>
</Card>;
}
function SectionHeading({ eyebrow, title, action }: {
    eyebrow?: string;
    title: string;
    action?: ReactNode;
}) {
    return <div className="section-heading">{eyebrow && <span className="eyebrow">{eyebrow}</span>}<div>
<h2>{title}</h2>{action}</div>
</div>;
}
function TableToolbar({ searchPlaceholder, count, onAdd, addLabel = "Nuevo", filterLabel = "Filtrar", value = "", onSearch, onFilter }: {
    searchPlaceholder: string;
    count: number;
    onAdd?: () => void;
    addLabel?: string;
    filterLabel?: string;
    value?: string;
    onSearch?: (value: string) => void;
    onFilter?: () => void;
}) {
    return <div className="table-toolbar">
<div className="inline-search">
<Icon name="search" size={16}/>
<input aria-label={searchPlaceholder} placeholder={searchPlaceholder} value={value} onChange={(event) => onSearch?.(event.target.value)}/>
</div>
{onFilter && <Button variant="outline" size="small" icon="filter" onClick={onFilter}>{filterLabel}</Button>}
<span className="toolbar-count">{count} registros</span>{onAdd && <Button size="small" icon="plus" onClick={onAdd}>{addLabel}</Button>}</div>;
}
function PartyView({ screen, state, entityId, onNavigate, onToast, dispatch }: {
    screen: ScreenDefinition;
    state: DemoState;
    entityId?: string | null;
    onNavigate: ScreenNavigator;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const [query, setQuery] = useState("");
    const contacts = state.contacts.filter((contact) => `${contact.name} ${contact.phone} ${contact.email} ${contact.owner}`.toLowerCase().includes(query.toLowerCase()));
    const pagination = useLocalPagination(contacts, query, 25);
    const detail = screen.id !== "PTY-01" && screen.id !== "PTY-02" && screen.id !== "PTY-03" && screen.id !== "PTY-04";
    if (screen.id === "PTY-02" || screen.id === "PTY-03" || screen.id === "PTY-04")
        return <QuickPartyForm screen={screen} onToast={onToast} onNavigate={onNavigate} dispatch={dispatch}/>;
    if (detail)
        return <PartyDetail entityId={entityId} screen={screen} state={state} onNavigate={onNavigate} onToast={onToast} dispatch={dispatch}/>;
    return <div className="feature-stack">
<Card>
<TableToolbar searchPlaceholder="Buscar por nombre, teléfono o email" count={contacts.length} value={query} onSearch={setQuery} onAdd={() => onNavigate("PTY-02")} addLabel="Nuevo contacto"/>
<div className="table-wrap">{contacts.length ? <table className="data-table">
<thead>
<tr>
<th>Nombre</th>
<th>Tipo</th>
<th>Contacto</th>
<th>Responsable</th>
<th>Estado</th>
<th />
</tr>
</thead>
<tbody>{pagination.items.map((contact) => <tr key={contact.id} onClick={() => onNavigate(contact.kind === "Empresa" ? "PTY-06" : "PTY-05", contact.id)}>
<td>
<div className="table-person">
<Avatar name={contact.name} size="small"/>
<strong>{contact.name}</strong>
</div>
</td>
<td>{contact.kind}</td>
<td>
<span>{contact.phone}</span>
<small>{contact.email}</small>
</td>
<td>{contact.owner}</td>
<td>
<StatusDot label={contactCommercialStatus(contact)} tone={commercialStatusTone(contactCommercialStatus(contact))}/>
</td>
<td>
<Button variant="ghost" size="xsmall" icon="chevron" aria-label={`Abrir ${contact.name}`} onClick={(event) => { event.stopPropagation(); onNavigate(contact.kind === "Empresa" ? "PTY-06" : "PTY-05", contact.id); }}/>
</td>
</tr>)}</tbody>
</table> : <EmptyState icon="search" title="No hay contactos para este filtro" description="Probá con otro nombre, teléfono o email."/>}</div>
<Pagination page={pagination.page} pages={pagination.pages} onChange={pagination.setPage}/>
</Card>
<div className="split-hint">
<Icon name="info" size={16}/>
<span>Seleccioná una fila para abrir el Contacto 360 con actividades, búsquedas y oportunidades relacionadas.</span>
</div>
</div>;
}
function QuickPartyForm({ screen, onToast, onNavigate, dispatch }: {
    screen: ScreenDefinition;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    onNavigate: ScreenNavigator;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const isCompany = screen.id === "PTY-03";
    const [name, setName] = useState("");
    const [phone, setPhone] = useState("");
    const [email, setEmail] = useState("");
    const [kind, setKind] = useState<"Persona" | "Empresa">(isCompany ? "Empresa" : "Persona");
    const [historicalText, setHistoricalText] = useState("");
    const [reviewedSuggestions, setReviewedSuggestions] = useState(false);
    const suggestions = reviewedSuggestions ? parseHistoricalContactText(historicalText) : {};
    const save = () => {
        if (!name.trim()) {
            onToast(`Falta el nombre ${kind === "Empresa" ? "de la empresa" : "del contacto"} para guardar.`, "warning");
            return;
        }
        dispatch({ type: "contact/create", item: { id: `contact-${Date.now()}`, name: name.trim(), kind, phone: phone.trim(), email: email.trim(), status: "Activo", commercialStatus: "POTENTIAL", identityStatus: "UNKNOWN", origin: historicalText.trim() ? "WhatsApp histórico" : "Carga manual", owner: "Martín Quiroga" } });
        onToast(`${kind === "Empresa" ? "Empresa" : "Contacto"} guardado.`);
        onNavigate("PTY-01");
    };
    const applySuggestions = () => {
        const applied = applyContactSuggestions({ name, phone, email }, suggestions, true);
        setName(applied.name);
        setPhone(applied.phone);
        setEmail(applied.email);
        onToast("Sugerencias aplicadas para revisión; todavía no se guardaron.", "info");
    };
    return <Card className="form-card">
<div className="form-card-heading">
<div>
<span className="eyebrow">{screen.id === "PTY-04" ? "Enrichment progresivo" : "Alta rápida"}</span>
<h2>{screen.title}</h2>
<p>{kind === "Empresa" ? "Capturá la empresa primero; sus relaciones y datos técnicos se enriquecen después." : "Capturá lo mínimo primero. El resto puede enriquecerse después sin bloquear la conversación."}</p>
</div>
<Tag tone="info">Alta flexible</Tag>
</div>
<div className="form-grid">
<Field label={kind === "Empresa" ? "Nombre de la empresa" : "Nombre y apellido"} placeholder={kind === "Empresa" ? "Ej. Estudio Norte SA" : "Ej. Ana Suárez"} autoFocus value={name} onChange={(event) => setName(event.target.value)}/>
<Field label="Teléfono" placeholder="+54 9 11 ..." value={phone} onChange={(event) => setPhone(event.target.value)}/>
<Field label="Email" placeholder="nombre@correo.com" value={email} onChange={(event) => setEmail(event.target.value)}/>
<SelectField label="Tipo" value={kind} onChange={(event) => setKind(event.target.value as "Persona" | "Empresa")}>
<option>Persona</option>
<option>Empresa</option>
</SelectField>
</div>
<TextAreaField label="Texto histórico de WhatsApp" placeholder="Pegá el texto histórico para revisar posibles datos..." rows={4} value={historicalText} onChange={(event) => { setHistoricalText(event.target.value); setReviewedSuggestions(false); }}/>
<div className="form-footer form-footer-left">
<Button variant="outline" onClick={() => setReviewedSuggestions(true)} disabled={!historicalText.trim()}>Revisar sugerencias</Button>
{reviewedSuggestions && <Button variant="secondary" onClick={applySuggestions} disabled={!Object.keys(suggestions).length}>Aplicar sugerencias</Button>}
</div>
{reviewedSuggestions && <Alert tone="info" title="Revisión antes de aplicar">No se modificó ningún campo automáticamente. {Object.keys(suggestions).length ? "Revisá los datos detectados y elegí aplicarlos." : "No detectamos nombre, teléfono o email en el texto."}</Alert>}
{reviewedSuggestions && Object.keys(suggestions).length > 0 && <div className="criterion-chip-list"><Chip tone="info">Nombre: {suggestions.name ?? "No detectado"}</Chip><Chip tone="info">Teléfono: {suggestions.phone ?? "No detectado"}</Chip><Chip tone="info">Email: {suggestions.email ?? "No detectado"}</Chip></div>}
<div className="form-footer">
<Button variant="ghost" onClick={() => onNavigate("PTY-01")}>Cancelar</Button>
<Button onClick={save}>Guardar {kind === "Empresa" ? "empresa" : "contacto"}</Button>
</div>
</Card>;
}
function PartyDetail({ screen, state, entityId, onNavigate, onToast, dispatch }: {
    screen: ScreenDefinition;
    state: DemoState;
    entityId?: string | null;
    onNavigate: ScreenNavigator;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const [confirmNoContact, setConfirmNoContact] = useState(false);
    const [confirmInactive, setConfirmInactive] = useState(false);
    const [confirmReactivate, setConfirmReactivate] = useState(false);
    const contact = selectedRecord(state.contacts, entityId);
    const [editName, setEditName] = useState(contact?.name ?? "");
    const [editPhone, setEditPhone] = useState(contact?.phone ?? "");
    const [editEmail, setEditEmail] = useState(contact?.email ?? "");
    const [companyId, setCompanyId] = useState(state.contacts.find((party) => party.kind === "Empresa" && party.id !== contact?.id)?.id ?? "");
    if (!contact) return <UnavailableRecord />;
    const commercialStatus = contactCommercialStatus(contact);
    const relatedDemand = state.demands.find((demand) => demand.contactId === contact.id);
    const relatedActivities = state.activities.filter((activity) => activity.relatedRecordId === contact.id);
    const relatedParties = state.relationships
        .filter((relationship) => relationship.fromPartyId === contact.id || relationship.toPartyId === contact.id)
        .map((relationship) => state.contacts.find((party) => party.id === (relationship.fromPartyId === contact.id ? relationship.toPartyId : relationship.fromPartyId)))
        .filter((party): party is DemoState["contacts"][number] => Boolean(party));
    const recordChange = (subject: string, body: string) => dispatch({ type: "activity/add", item: { id: `activity-${Date.now()}`, type: "Nota", subject, body, actor: "Martín Quiroga", recordedByUserId: "user-1", occurredAt: new Date().toISOString(), relatedRecordId: contact.id, relatedRecordType: "Party", createdAt: "Ahora" } });
    const updateCommercialStatus = (next: CommercialStatus) => {
        dispatch({ type: "contact/update", id: contact.id, changes: { commercialStatus: next, status: next === "DO_NOT_CONTACT" ? "No contactar" : next === "INACTIVE" ? "Archivado" : "Activo" } });
        recordChange("Estado comercial actualizado", `${commercialStatus} → ${next}`);
        onToast(`Estado comercial actualizado a ${next}.`, "info");
    };
    const requestCommercialStatus = (next: CommercialStatus) => {
        if (next === "DO_NOT_CONTACT") return setConfirmNoContact(true);
        if (next === "INACTIVE") return setConfirmInactive(true);
        updateCommercialStatus(next);
    };
    const saveEdit = () => {
        if (!editName.trim()) {
            onToast("El nombre no puede quedar vacío.", "warning");
            return;
        }
        dispatch({ type: "contact/update", id: contact.id, changes: { name: editName.trim(), phone: editPhone.trim(), email: editEmail.trim() } });
        recordChange("Ficha actualizada", "Se conservaron los datos anteriores en el historial.");
        onToast("Ficha actualizada.");
        onNavigate(contact.kind === "Empresa" ? "PTY-06" : "PTY-05", contact.id);
    };
    const saveRelationship = () => {
        if (contact.kind !== "Persona" || !companyId) {
            onToast("Elegí una empresa para relacionar el contacto.", "warning");
            return;
        }
        dispatch({ type: "party/relate", contactId: contact.id, companyId });
        onToast("Relación guardada en la ficha.");
    };
    return <div className="feature-stack">
<Card className="detail-hero">
<div className="detail-identity">
<Avatar name={contact.name} size="large"/>
<div>
<span className="eyebrow">{contact.kind === "Empresa" ? "Empresa 360" : "Contacto 360"}</span>
<h2>{contact.name}</h2>
<div className="identity-meta">
<StatusDot label={contactCommercialStatus(contact)} tone={commercialStatusTone(contactCommercialStatus(contact))}/>
<span>{contact.kind}</span>
<span>Responsable: {contact.owner}</span>
<span>Origen: {contact.origin ?? "Origen desconocido"}</span>
<span>Identidad: {contact.identityStatus ?? "UNKNOWN"}</span>
</div>
</div>
</div>
<div className="detail-actions">
<Button variant="outline" icon="edit" onClick={() => onNavigate("PTY-08", contact.id)}>Editar</Button>
<Button icon="activity" onClick={() => onNavigate("ACT-01", contact.id)}>Registrar actividad</Button>
<Button variant="ghost" icon="more" aria-label="Más acciones" onClick={() => commercialStatus === "DO_NOT_CONTACT" || commercialStatus === "INACTIVE" ? setConfirmReactivate(true) : setConfirmNoContact(true)}/>
</div>
</Card>
<ScreenTabs screen={screen} entityId={entityId} onNavigate={onNavigate} tabs={[{ id: contact.kind === "Empresa" ? "PTY-06" : "PTY-05", label: "Resumen" }, { id: "DEM-01", label: "Búsquedas", count: state.demands.length }, { id: "PTY-12", label: "Historial", count: state.activities.length }, { id: "PTY-07", label: "Relaciones" }]}/>
<div className="detail-grid">
<Card>
<SectionHeading eyebrow="Datos de contacto" title="Ficha"/>
<div className="definition-grid">
<Definition label="Teléfono" value={contact.phone}/>
<Definition label="Email" value={contact.email}/>
<Definition label="Estado comercial" value={<StatusDot label={commercialStatus} tone={commercialStatusTone(commercialStatus)}/>}/>
<Definition label="Qué significa" value={getCommercialStatusMeaning(commercialStatus)}/>
<Definition label="Identidad técnica" value={contact.identityStatus ?? "UNKNOWN"}/>
<Definition label="Identificador" value="Registrado por el sistema · solo lectura"/>
</div>{screen.id === "PTY-09" && <div className="inline-stage-editor"><SelectField label="Estado comercial" value={commercialStatus} onChange={(event) => requestCommercialStatus(event.target.value as CommercialStatus)}>{(["POTENTIAL", "CUSTOMER", "INACTIVE", "DO_NOT_CONTACT"] as CommercialStatus[]).map((status) => <option key={status} value={status}>{status}</option>)}</SelectField></div>}{screen.id === "PTY-08" && <div className="form-grid"><Field label={contact.kind === "Empresa" ? "Nombre de la empresa" : "Nombre y apellido"} value={editName} onChange={(event) => setEditName(event.target.value)}/><Field label="Teléfono" value={editPhone} onChange={(event) => setEditPhone(event.target.value)}/><Field label="Email" type="email" value={editEmail} onChange={(event) => setEditEmail(event.target.value)}/><div className="form-footer"><Button onClick={saveEdit}>Guardar cambios</Button></div></div>}{screen.id === "PTY-10" && <Alert tone="warning" title="No contactar">Esta marca bloquea acciones comerciales futuras, pero conserva el historial.</Alert>}{screen.id === "PTY-11" && <Alert tone={commercialStatus === "INACTIVE" ? "warning" : "info"} title={commercialStatus === "INACTIVE" ? "Baja lógica aplicada" : "Baja lógica disponible"}>{commercialStatus === "INACTIVE" ? "El registro conserva relaciones e historial. Podés reactivarlo con una acción explícita." : "La baja lógica conserva relaciones e historial y no elimina el registro."}</Alert>}{screen.id === "PTY-13" && <Alert tone="info" title="Panel de solo lectura">Los identificadores de integración no se editan desde el CRM.</Alert>}</Card>
<Card>
<SectionHeading eyebrow="Actividad reciente" title="Últimos hechos" action={<Button variant="ghost" size="small" onClick={() => onNavigate("ACT-02", contact.id)}>Ver timeline <Icon name="arrow" size={14}/>
</Button>}/>
{relatedActivities.length ? <ActivityList activities={relatedActivities.slice(0, 3)} onEdit={(activityId) => onNavigate("ACT-06", activityId)}/> : <EmptyState icon="activity" title="Sin actividad relacionada" description="Los hechos de este registro van a aparecer acá cuando se registren."/>}
</Card>
</div>
<Card>
<SectionHeading eyebrow={contact.kind === "Empresa" ? "Contactos relacionados" : "Relaciones"} title={contact.kind === "Empresa" ? `${relatedParties.length} contacto${relatedParties.length === 1 ? "" : "s"}` : relatedParties[0]?.name ?? "Contacto individual"} action={relatedDemand ? <Button variant="ghost" size="small" onClick={() => onNavigate("DEM-04", relatedDemand.id)}>Abrir búsqueda <Icon name="arrow" size={14}/>
</Button> : undefined}/>
{screen.id === "PTY-07" && contact.kind === "Persona" && <div className="form-grid"><SelectField label="Relacionar con empresa" value={companyId} onChange={(event) => setCompanyId(event.target.value)}>{state.contacts.filter((party) => party.kind === "Empresa").map((company) => <option key={company.id} value={company.id}>{company.name}</option>)}</SelectField><Button size="small" onClick={saveRelationship}>Guardar relación</Button></div>}
{relatedParties.length > 0 && <div className="criterion-chip-list">{relatedParties.map((party) => <Chip key={party.id} tone="info">{party.kind === "Empresa" ? "Empresa" : "Contacto"}: {party.name}</Chip>)}</div>}
{relatedDemand && <div className="criterion-chip-list"><Chip tone="brand">Búsqueda: {relatedDemand.title}</Chip></div>}
<div className="criterion-chip-list">{relatedDemand?.criteria.map((criterion) => <Chip key={criterion.id} tone={criterion.weight === "must" ? "brand" : criterion.weight === "unknown" ? "warning" : "neutral"}>{criterion.label}: {criterion.value === "UNKNOWN" ? "Desconocido" : criterion.value}</Chip>)}</div>
</Card>
<Modal open={confirmNoContact} title="Marcar como no contactar" onClose={() => setConfirmNoContact(false)} footer={<><Button variant="ghost" onClick={() => setConfirmNoContact(false)}>Cancelar</Button><Button variant="tertiary" onClick={() => { updateCommercialStatus("DO_NOT_CONTACT"); setConfirmNoContact(false); }}>Confirmar no contactar</Button></>}>
<p>Se bloquearán nuevas acciones comerciales para {contact.name}. El historial y los datos existentes se conservan.</p>
</Modal>
<Modal open={confirmInactive} title="Aplicar baja lógica" onClose={() => setConfirmInactive(false)} footer={<><Button variant="ghost" onClick={() => setConfirmInactive(false)}>Cancelar</Button><Button variant="tertiary" onClick={() => { updateCommercialStatus("INACTIVE"); setConfirmInactive(false); }}>Confirmar baja lógica</Button></>}><p>La ficha quedará inactiva sin borrar relaciones, actividades ni oportunidades.</p></Modal>
<Modal open={confirmReactivate} title="Reactivar ficha" onClose={() => setConfirmReactivate(false)} footer={<><Button variant="ghost" onClick={() => setConfirmReactivate(false)}>Cancelar</Button><Button onClick={() => { updateCommercialStatus("CUSTOMER"); setConfirmReactivate(false); }}>Confirmar reactivación</Button></>}><p>La ficha volverá a estar disponible para nuevas acciones comerciales.</p></Modal>
</div>;
}
function Definition({ label, value }: {
    label: string;
    value: ReactNode;
}) { return <div className="definition">
<span>{label}</span>
<strong>{value}</strong>
</div>; }
function ActivityList({ activities, onEdit }: {
    activities: DemoActivity[];
    onEdit?: (activityId: string) => void;
}) {
    const orderedActivities = [...activities].sort((left, right) => (Date.parse(right.occurredAt ?? right.createdAt) || 0) - (Date.parse(left.occurredAt ?? left.createdAt) || 0));
    return <div className="activity-list">{orderedActivities.map((activity) => <div className="activity-row" key={activity.id}>
<span className="activity-icon">
<Icon name={activity.type === "Visita" ? "calendar" : ["Email", "Email histórico", "Correo electrónico"].includes(activity.type) ? "mail" : ["WhatsApp", "WhatsApp histórico", "Mensaje", "Llamada"].includes(activity.type) ? "phone" : "activity"} size={16}/>
</span>
<div>
<strong>{activity.subject}</strong>
<p>{activity.body}</p>
<small>{activity.actor} · {activity.occurredAt ?? activity.createdAt}{activity.historical ? " · Histórico" : ""}</small>
{onEdit && <Button variant="ghost" size="xsmall" icon="edit" aria-label={`Corregir ${activity.subject}`} onClick={() => onEdit(activity.id)}>Corregir</Button>}
</div>
</div>)}</div>;
}
function PropertyView({ screen, state, entityId, onNavigate, onToast, dispatch }: {
    screen: ScreenDefinition;
    state: DemoState;
    entityId?: string | null;
    onNavigate: ScreenNavigator;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const [query, setQuery] = useState("");
    const properties = state.properties.filter((property) => `${property.title} ${property.address} ${property.type}`.toLowerCase().includes(query.toLowerCase()));
    const pagination = useLocalPagination(properties, query);
    if (["PRP-03", "PRP-04", "PRP-05"].includes(screen.id))
        return <PropertyForm screen={screen} state={state} entityId={entityId} onToast={onToast} onNavigate={onNavigate} dispatch={dispatch}/>;
    if (screen.id !== "PRP-01" && screen.id !== "PRP-02")
        return <PropertyDetail entityId={entityId} screen={screen} state={state} onNavigate={onNavigate} onToast={onToast} dispatch={dispatch}/>;
    return <div className="feature-stack">
<Card>
<TableToolbar searchPlaceholder="Buscar por dirección, tipo o código" count={properties.length} value={query} onSearch={setQuery} onAdd={() => onNavigate("PRP-03")} addLabel="Nuevo inmueble"/>
<div className="table-wrap">{properties.length ? <table className="data-table">
<thead>
<tr>
<th>Inmueble</th>
<th>Tipo</th>
<th>Precio</th>
<th>Estado</th>
<th>Geo</th>
<th />
</tr>
</thead>
<tbody>{pagination.items.map((property) => <tr key={property.id} onClick={() => onNavigate("PRP-06", property.id)}>
<td>
<div className="table-property">
<PropertyPlaceholder title={property.title}/>
<span>
<strong>{property.title}</strong>
<small>{property.address}</small>
</span>
</div>
</td>
<td>{property.type}</td>
<td>
<CurrencyAmount value={property.price} currency={property.currency}/>
</td>
<td>
<StatusDot label={property.status} tone={property.status === "Disponible" ? "success" : property.status === "Reservado" ? "warning" : "neutral"}/>
</td>
<td>{property.geo === "Conocida" ? <Chip tone="success" dot>Conocida</Chip> : <UnknownIndicator label="Geo desconocida"/>}</td>
<td>
<Button variant="ghost" size="xsmall" icon="chevron" aria-label={`Abrir ${property.title}`} onClick={(event) => { event.stopPropagation(); onNavigate("PRP-06", property.id); }}/>
</td>
</tr>)}</tbody>
</table> : <EmptyState icon="search" title="No hay inmuebles para este filtro" description="Probá con otra dirección, tipo o código."/>}</div>
<Pagination page={pagination.page} pages={pagination.pages} onChange={pagination.setPage}/>
</Card>{screen.id === "PRP-02" && <Card className="map-card">
<div className="map-surface">
<div className="map-grid-lines"/>
<span className="map-pin pin-one">
<Icon name="building" size={14}/>
</span>
<span className="map-pin pin-two">
<Icon name="building" size={14}/>
</span>
<span className="map-pin pin-three">
<Icon name="building" size={14}/>
</span>
<div className="map-label">
<Icon name="map" size={17}/>
<span>Mapa como forma de descubrir, no como requisito</span>
</div>
</div>
</Card>}</div>;
}
function PropertyForm({ screen, state, entityId, onToast, onNavigate, dispatch }: {
    screen: ScreenDefinition;
    state: DemoState;
    entityId?: string | null;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    onNavigate: ScreenNavigator;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const existingProperty = entityId ? selectedRecord(state.properties, entityId) : undefined;
    const [title, setTitle] = useState(existingProperty?.title ?? "");
    const [address, setAddress] = useState(existingProperty?.address ?? "");
    const [type, setType] = useState(existingProperty?.type ?? "Casa");
    const [price, setPrice] = useState(existingProperty?.price == null ? "" : String(existingProperty.price));
    const [currency, setCurrency] = useState<"USD" | "ARS">(existingProperty?.currency ?? "USD");
    const [bedrooms, setBedrooms] = useState(existingProperty?.bedrooms === "Desconocido" ? "" : existingProperty?.bedrooms ?? "");
    const [surfaceHa, setSurfaceHa] = useState(existingProperty?.surfaceHa == null ? "" : String(existingProperty.surfaceHa));
    const propertyTypes = Array.from(new Set([existingProperty?.type ?? "Casa", ...state.catalogEntries.filter((entry) => entry.catalogType === "property-type" && entry.status === "Activo").map((entry) => entry.label)]));
    const save = () => {
        if (!title.trim() || !address.trim()) {
            onToast("Completá el título y la dirección para guardar el inmueble.", "warning");
            return;
        }
        const parsedPrice = parseOptionalMoney(price);
        const parsedSurfaceHa = parseOptionalMoney(surfaceHa);
        if (parsedPrice === undefined || parsedSurfaceHa === undefined) {
            onToast("Cargá importes válidos y no negativos.", "warning");
            return;
        }
        if (existingProperty) {
            dispatch({ type: "property/update", id: existingProperty.id, changes: { title: title.trim(), address: address.trim(), type, price: parsedPrice, currency, bedrooms: bedrooms.trim() || "Desconocido", surfaceHa: type === "Rural" ? parsedSurfaceHa : undefined } });
            onToast("Inmueble actualizado.");
            onNavigate("PRP-06", existingProperty.id);
        }
        else {
            dispatch({ type: "property/create", item: { id: `property-${Date.now()}`, title: title.trim(), address: address.trim(), type, status: "Disponible", price: parsedPrice, currency, bedrooms: bedrooms.trim() || "Desconocido", geo: "Desconocida", surfaceHa: type === "Rural" ? parsedSurfaceHa : undefined, ownerPartyIds: [] } });
            onToast("Inmueble guardado.");
            onNavigate("PRP-01");
        }
    };
    return <Card className="form-card">
<div className="form-card-heading">
<div>
<span className="eyebrow">{existingProperty ? "Edición de inmueble" : "Alta de inmueble"}</span>
<h2>{existingProperty ? "Editar inmueble" : screen.title}</h2>
<p>Los datos de inmueble se cargan por bloques; una geo desconocida no impide continuar.</p>
</div>
<PropertyPlaceholder title="Nuevo inmueble"/>
</div>
<div className="form-grid">
<Field label="Título interno" placeholder="Ej. Casa en Villa Crespo" value={title} onChange={(event) => setTitle(event.target.value)} autoFocus/>
<Field label="Dirección" placeholder="Calle y altura" value={address} onChange={(event) => setAddress(event.target.value)}/>
<SelectField label="Tipo de inmueble" value={type} onChange={(event) => setType(event.target.value)}>
{propertyTypes.map((propertyType) => <option key={propertyType}>{propertyType}</option>)}
</SelectField>
<Field label="Precio" placeholder="0,00" inputMode="decimal" value={price} onChange={(event) => setPrice(event.target.value)}/>
<SelectField label="Moneda" value={currency} onChange={(event) => setCurrency(event.target.value as "USD" | "ARS")}>
<option>USD</option>
<option>ARS</option>
</SelectField>
<Field label="Ambientes" placeholder="Desconocido" value={bedrooms} onChange={(event) => setBedrooms(event.target.value)}/>
{type === "Rural" && <Field label="Superficie rural (ha)" placeholder="Desconocido" value={surfaceHa} onChange={(event) => setSurfaceHa(event.target.value)}/>} 
</div>
<Alert tone="info" title="Imágenes pendientes">El placeholder queda explícito hasta recibir multimedia.</Alert>
<div className="form-footer">
<Button variant="ghost" onClick={() => onNavigate(existingProperty ? "PRP-06" : "PRP-01", existingProperty?.id)}>Cancelar</Button>
<Button onClick={save}>{existingProperty ? "Guardar cambios" : "Guardar inmueble"}</Button>
</div>
</Card>;
}
function PropertyDetail({ screen, state, entityId, onNavigate, onToast, dispatch }: {
    screen: ScreenDefinition;
    state: DemoState;
    entityId?: string | null;
    onNavigate: ScreenNavigator;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const property = selectedRecord(state.properties, entityId);
    const [confirmArchive, setConfirmArchive] = useState(false);
    if (!property) return <UnavailableRecord />;
    const listing = state.listings.find((item) => item.propertyId === property.id);
    const owners = (property.ownerPartyIds ?? []).map((ownerId) => state.contacts.find((contact) => contact.id === ownerId)).filter((owner): owner is DemoState["contacts"][number] => Boolean(owner));
    const relatedActivities = state.activities.filter((activity) => activity.relatedRecordId === property.id || activity.propertyId === property.id);
    const archive = () => {
        dispatch({ type: "property/update", id: property.id, changes: { status: "Archivado" } });
        setConfirmArchive(false);
        onToast("Inmueble archivado; el historial se conserva.", "info");
        onNavigate("PRP-06", property.id);
    };
    return <div className="feature-stack">
<Card className="detail-hero property-detail-hero">
<div className="detail-identity">
<PropertyPlaceholder title={property.title} status={property.status}/>
<div>
<span className="eyebrow">Ficha de inmueble</span>
<h2>{property.title}</h2>
<div className="identity-meta">
<span>{property.address}</span>
<span>{property.type} · {property.bedrooms}</span>
<CurrencyAmount value={property.price} currency={property.currency}/>
</div>
</div>
</div>
<div className="detail-actions">
<Button variant="outline" icon="edit" onClick={() => onNavigate("PRP-03", property.id)}>Editar</Button>
<Button icon="external" onClick={() => onNavigate("LST-02", property.id)}>Crear publicación</Button>
<Button variant="ghost" icon="archive" aria-label="Archivar inmueble" onClick={() => setConfirmArchive(true)} disabled={property.status === "Archivado"}/>
</div>
</Card>
<ScreenTabs screen={screen} entityId={entityId} onNavigate={onNavigate} tabs={[{ id: "PRP-06", label: "Resumen" }, { id: "PRP-07", label: "Ubicación" }, { id: "PRP-08", label: "Características" }, { id: "PRP-09", label: "Multimedia" }, { id: "PRP-10", label: "Propietarios" }, { id: "PRP-11", label: "Historia" }]}/>
<div className="detail-grid">
<Card>
<SectionHeading eyebrow="Características" title="Lo que sabemos"/>
<div className="definition-grid">
<Definition label="Tipo" value={property.type}/>
<Definition label="Ambientes" value={property.bedrooms}/>
<Definition label="Superficie cubierta" value={<UnknownIndicator />}/>
<Definition label="Geo" value={property.geo === "Conocida" ? "Ubicación registrada" : <UnknownIndicator label="No cargada"/>}/>
</div>{screen.id === "PRP-08" && <div className="criterion-chip-list">
<Chip tone="info">{property.type}</Chip>
<Chip tone="info">{property.bedrooms}</Chip>
<Chip tone={property.surfaceM2 || property.surfaceHa ? "success" : "neutral"}>{property.surfaceM2 ? `${property.surfaceM2} m²` : property.surfaceHa ? `${property.surfaceHa} ha` : "Superficie desconocida"}</Chip>
</div>}{screen.id === "PRP-09" && <div className="placeholder-grid">
<PropertyPlaceholder title={property.title}/>
<PropertyPlaceholder title="Plano pendiente"/>
<PropertyPlaceholder title="Video pendiente"/>
</div>}{screen.id === "PRP-13" && <Alert tone="warning" title="Archivar inmueble">Al archivar se conserva el historial y se detienen nuevas publicaciones.</Alert>}</Card>
<Card>
<SectionHeading eyebrow="Propietarios e intereses" title={owners.length ? `${owners.length} titular${owners.length === 1 ? "" : "es"}` : "Sin titular registrado"}/>
{owners.length ? <div className="criterion-chip-list">{owners.map((owner) => <Chip key={owner.id} tone="info">{owner.kind}: {owner.name}</Chip>)}</div> : <EmptyState icon="users" title="Sin propietarios vinculados" description="La relación con titulares todavía no fue registrada."/>}
<div className="listing-summary"><strong>Oferta comercial</strong>{listing ? <>
<PropertyPlaceholder title={listing.title}/>
<div>
<StatusDot label={listing.status} tone={listing.status === "Activa" ? "success" : "warning"}/>
<p>{listing.mandate === "Firmado" ? "Mandato firmado" : "Sin mandato firmado · no bloquea publicar"}</p>
<small>{listing.views} visualizaciones registradas</small>
</div>
</> : <EmptyState icon="external" title="Todavía no hay publicación" description="Podés crearla desde este inmueble." action={<Button size="small" onClick={() => onNavigate("LST-02", property.id)}>Crear publicación</Button>}/>}</div>{listing && <Button variant="ghost" size="small" onClick={() => onNavigate("LST-04", listing.id)}>Abrir publicación <Icon name="arrow" size={14}/></Button>}</Card>
</div>
<Card>
<SectionHeading eyebrow="Historial" title="Últimos cambios" action={<Button variant="ghost" size="small" onClick={() => onNavigate("PRP-11", property.id)}>Ver historia <Icon name="arrow" size={14}/>
</Button>}/>
{relatedActivities.length ? <ActivityList activities={relatedActivities} onEdit={(activityId) => onNavigate("ACT-06", activityId)}/> : <Timeline items={["Inmueble cargado en la instalación", "Tasación pendiente de confirmar", "Mandato y publicación visibles"]}/>} 
</Card>
<Modal open={confirmArchive} title="Archivar inmueble" onClose={() => setConfirmArchive(false)} footer={<><Button variant="ghost" onClick={() => setConfirmArchive(false)}>Cancelar</Button><Button variant="tertiary" onClick={archive}>Confirmar archivo</Button></>}><p>El inmueble dejará de estar disponible para nuevas publicaciones, pero conserva propietarios, listings e historial.</p></Modal>
</div>;
}
function ListingView({ screen, state, entityId, onNavigate, onToast, dispatch }: {
    screen: ScreenDefinition;
    state: DemoState;
    entityId?: string | null;
    onNavigate: ScreenNavigator;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const [query, setQuery] = useState("");
    const listings = state.listings.filter((listing) => { const property = state.properties.find((item) => item.id === listing.propertyId); return `${listing.title} ${property?.address ?? ""}`.toLowerCase().includes(query.toLowerCase()); });
    const pagination = useLocalPagination(listings, query);
    if (screen.id === "LST-02" || screen.id === "LST-03")
        return <ListingForm entityId={entityId} screen={screen} state={state} onToast={onToast} onNavigate={onNavigate} dispatch={dispatch}/>;
    if (screen.id !== "LST-01")
        return <ListingDetail entityId={entityId} screen={screen} state={state} onNavigate={onNavigate} onToast={onToast} dispatch={dispatch}/>;
    return <div className="feature-stack">
<Card>
<TableToolbar searchPlaceholder="Buscar publicación o dirección" count={listings.length} value={query} onSearch={setQuery} onAdd={() => onNavigate("LST-02")} addLabel="Nueva publicación"/>
<div className="table-wrap">{listings.length ? <table className="data-table">
<thead>
<tr>
<th>Publicación</th>
<th>Inmueble</th>
<th>Estado</th>
<th>Mandato</th>
<th>Rendimiento</th>
<th />
</tr>
</thead>
<tbody>{pagination.items.map((listing) => { const property = state.properties.find((item) => item.id === listing.propertyId); return <tr key={listing.id} onClick={() => onNavigate("LST-04", listing.id)}>
<td>
<div className="table-person">
<PropertyPlaceholder title={listing.title}/>
<strong>{listing.title}</strong>
</div>
</td>
<td>{property?.address}</td>
<td>
<StatusDot label={listing.status} tone={listing.status === "Activa" ? "success" : listing.status === "Pausada" ? "warning" : "neutral"}/>
</td>
<td>{listing.mandate === "Firmado" ? <Chip tone="success">Firmado</Chip> : <Chip tone="warning">Sin mandato</Chip>}</td>
<td>{listing.views} vistas</td>
<td>
<Button variant="ghost" size="xsmall" icon="chevron" aria-label={`Abrir ${listing.title}`} onClick={(event) => { event.stopPropagation(); onNavigate("LST-04", listing.id); }}/>
</td>
</tr>; })}</tbody>
</table> : <EmptyState icon="search" title="No hay publicaciones para este filtro" description="Probá con otro nombre o dirección."/>}</div>
<Pagination page={pagination.page} pages={pagination.pages} onChange={pagination.setPage}/>
</Card>
<Alert tone="info" title="El mandato advierte, no bloquea">Una publicación sin mandato queda marcada en la lista, pero no se bloquea la acción.</Alert>
</div>;
}
function ListingForm({ screen, state, entityId, onToast, onNavigate, dispatch }: {
    screen: ScreenDefinition;
    state: DemoState;
    entityId?: string | null;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    onNavigate: ScreenNavigator;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const existingListing = entityId ? selectedRecord(state.listings, entityId) : undefined;
    const sourceCaptation = !existingListing && entityId ? selectedRecord(state.captations, entityId) : undefined;
    const [propertyId, setPropertyId] = useState(existingListing?.propertyId ?? sourceCaptation?.propertyId ?? (entityId && state.properties.some((property) => property.id === entityId) ? entityId : state.properties[0]?.id ?? ""));
    const [title, setTitle] = useState(existingListing?.title ?? "");
    const [description, setDescription] = useState(existingListing?.description ?? "");
    const [price, setPrice] = useState(existingListing?.price == null ? "" : String(existingListing.price));
    const [currency, setCurrency] = useState<"USD" | "ARS">(existingListing?.currency ?? state.properties.find((property) => property.id === propertyId)?.currency ?? "USD");
    const [operationType, setOperationType] = useState(existingListing?.operationType ?? "Venta");
    const operationTypes = Array.from(new Set([existingListing?.operationType ?? "Venta", ...state.catalogEntries.filter((entry) => entry.catalogType === "operation-type" && entry.status === "Activo").map((entry) => entry.label)]));
    const save = () => {
        if (!propertyId || !title.trim()) {
            onToast("Elegí un inmueble y completá el título comercial.", "warning");
            return;
        }
        const parsedPrice = parseOptionalMoney(price);
        if (parsedPrice === undefined) {
            onToast("Cargá un precio válido y no negativo, o dejalo desconocido.", "warning");
            return;
        }
        if (existingListing) {
            dispatch({ type: "listing/update", id: existingListing.id, changes: { title: title.trim(), propertyId, description: description.trim() || undefined, price: parsedPrice, currency, operationType } });
            onToast("Publicación actualizada.");
            onNavigate("LST-04", existingListing.id);
        }
        else {
            dispatch({ type: "listing/create", item: { id: `listing-${Date.now()}`, title: title.trim(), propertyId, status: "Borrador", mandate: "Sin mandato", views: 0, description: description.trim() || undefined, price: parsedPrice, currency, operationType } });
            onToast("Publicación preparada.");
            onNavigate("LST-01");
        }
    };
    return <Card className="form-card">
<div className="form-card-heading">
<div>
<span className="eyebrow">{existingListing ? "Edición de publicación" : "Nueva publicación"}</span>
<h2>{existingListing ? "Editar publicación" : screen.title}</h2>
<p>La publicación hereda propietario, inmueble y expectativa de la fuente seleccionada.</p>
</div>
<Tag tone="info">Paso 1 de 2</Tag>
</div>
<div className="form-grid">
<SelectField label="Fuente" value={propertyId} onChange={(event) => setPropertyId(event.target.value)}>{state.properties.map((property) => <option key={property.id} value={property.id}>{property.title} · {property.address}</option>)}</SelectField>
<Field label="Título comercial" placeholder="Casa luminosa con patio" value={title} onChange={(event) => setTitle(event.target.value)} autoFocus/>
<SelectField label="Tipo de operación" value={operationType} onChange={(event) => setOperationType(event.target.value)}>{operationTypes.map((operation) => <option key={operation}>{operation}</option>)}</SelectField>
<SelectField label="Moneda" value={currency} onChange={(event) => setCurrency(event.target.value as typeof currency)}><option>USD</option><option>ARS</option></SelectField>
<TextAreaField label="Descripción" placeholder="Escribí una descripción clara y verificable." rows={4} value={description} onChange={(event) => setDescription(event.target.value)}/>
<Field label="Precio publicado" placeholder="Desconocido" value={price} onChange={(event) => setPrice(event.target.value)}/>
</div>
<Alert tone="warning" title="Sin mandato firmado">La publicación puede continuar. Queda señalada como pendiente para la administradora.</Alert>
<div className="form-footer">
<Button variant="ghost" onClick={() => onNavigate(existingListing ? "LST-04" : "LST-01", existingListing?.id)}>Cancelar</Button>
<Button onClick={save}>{existingListing ? "Guardar cambios" : "Guardar publicación"}</Button>
</div>
</Card>;
}
function ListingDetail({ screen, state, entityId, onNavigate, onToast, dispatch }: {
    screen: ScreenDefinition;
    state: DemoState;
    entityId?: string | null;
    onNavigate: ScreenNavigator;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const listing = selectedRecord(state.listings, entityId);
    const [statusReason, setStatusReason] = useState("");
    if (!listing) return <UnavailableRecord />;
    const property = resolveSelectedRecord(state.properties, listing.propertyId);
    if (!property) return <UnavailableRecord />;
    const listingLabels = [listing.operationType, property.type, property.bedrooms].filter((label): label is string => Boolean(label && label !== "Desconocido"));
    const relatedDemand = state.demands
        .filter((demand) => demand.status === "Activa" && demand.selectedListingIds?.includes(listing.id))
        .sort((left, right) => right.score - left.score)[0];
    const relatedDemandContact = relatedDemand ? state.contacts.find((contact) => contact.id === relatedDemand.contactId) : undefined;
    const relatedDemandUnknowns = relatedDemand?.criteria.filter((criterion) => criterion.value === "UNKNOWN").length ?? 0;
    return <div className="feature-stack">
<Card className="detail-hero">
<div className="detail-identity">
<PropertyPlaceholder title={listing.title}/>
<div>
<span className="eyebrow">Publicación</span>
<h2>{listing.title}</h2>
<div className="identity-meta">
<StatusDot label={listing.status} tone={listing.status === "Activa" ? "success" : "warning"}/>
<span>{property.address}</span>
<span>{listing.views} visualizaciones</span>
</div>
</div>
</div>
<div className="detail-actions">
<Button variant="outline" icon="edit" onClick={() => onNavigate("LST-02", listing.id)}>Editar contenido</Button>
<Button icon="target" onClick={() => onNavigate("MAT-03", listing.id)}>Ver demanda compatible</Button>
</div>
</Card>
<ScreenTabs screen={screen} entityId={entityId} onNavigate={onNavigate} tabs={[{ id: "LST-04", label: "Resumen" }, { id: "LST-06", label: "Presentación" }, { id: "LST-05", label: "Términos" }, { id: "LST-07", label: "Estado" }, { id: "LST-08", label: "Rendimiento" }]}/>
<div className="detail-grid">
<Card>
<SectionHeading eyebrow="Contenido" title={listing.title}/>
<PropertyPlaceholder title={listing.title}/>
<p className="body-copy">{listing.description ?? "Una descripción comercial clara, con evidencia de lo que se conoce del inmueble. Las imágenes reales se incorporarán cuando estén disponibles."}</p>
<div className="criterion-chip-list">{listingLabels.map((label) => <Chip tone="info" key={label}>{label}</Chip>)}{!listingLabels.length && <Chip tone="neutral">Datos comerciales desconocidos</Chip>}</div>
</Card>
<Card>
<SectionHeading eyebrow="Términos comerciales" title="Condiciones visibles"/>
<div className="definition-grid">
<Definition label="Precio" value={<CurrencyAmount value={listing.price ?? property.price} currency={listing.currency ?? property.currency}/>}/>
<Definition label="Mandato" value={listing.mandate}/>
<Definition label="Comisión" value={<UnknownIndicator />}/>
<Definition label="Estado" value={<StatusDot label={listing.status} tone={listing.status === "Activa" ? "success" : "warning"}/>}/>
</div>{screen.id === "LST-05" && <Alert tone="info" title="Comisión pendiente">La comisión se mantiene como dato desconocido hasta contar con la información necesaria.</Alert>}{screen.id === "LST-07" && <div className="inline-stage-editor"><SelectField label="Estado de publicación" value={listing.status} onChange={(event) => { const next = event.target.value as DemoListing["status"]; if (next === "Cerrada") { const validation = validateReason(statusReason, "cerrar la publicación"); if (!validation.valid) { onToast(validation.error, "warning"); return; } } dispatch({ type: "listing/update", id: listing.id, changes: { status: next, statusReason: statusReason.trim() || undefined, statusChangedAt: new Date().toISOString() } }); onToast(`Publicación marcada como ${next.toLowerCase()}.`, "info"); }}>{(["Borrador", "Activa", "Pausada", "Cerrada"] as DemoListing["status"][]).map((status) => <option key={status}>{status}</option>)}</SelectField>{listing.status !== "Cerrada" && <TextAreaField label="Motivo de cambio" placeholder="Qué ocurrió con la publicación..." rows={3} value={statusReason} onChange={(event) => setStatusReason(event.target.value)}/>}</div>}</Card>
</div>
<Card>
<SectionHeading eyebrow="Compatibilidad" title="Demanda relacionada" action={<Button variant="ghost" size="small" onClick={() => onNavigate("MAT-03", listing.id)}>Explorar <Icon name="arrow" size={14}/>
</Button>}/>
{relatedDemand ? <div className="match-inline">
<span className="score-bubble">{relatedDemand.score}</span>
<div>
<strong>{relatedDemand.title}</strong>
<p>{relatedDemandContact?.name ?? "Party desconocida"} · {relatedDemand.criteria.length - relatedDemandUnknowns} criterios conocidos · {relatedDemandUnknowns} desconocidos.</p>
</div>
<Chip tone={relatedDemand.score >= 80 ? "success" : "info"}>{relatedDemand.score >= 80 ? "Alta" : "Media"}</Chip>
</div> : <EmptyState icon="target" title="Sin demanda seleccionada" description="Todavía no hay una compatibilidad seleccionada para esta publicación."/>}
</Card>
</div>;
}
function CaptationView({ screen, state, entityId, onNavigate, onToast, dispatch }: {
    screen: ScreenDefinition;
    state: DemoState;
    entityId?: string | null;
    onNavigate: ScreenNavigator;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const [query, setQuery] = useState("");
    const captations = state.captations.filter((captation) => { const property = state.properties.find((item) => item.id === captation.propertyId); return `${property?.title ?? ""} ${captation.owner}`.toLowerCase().includes(query.toLowerCase()); });
    const pagination = useLocalPagination(captations, query);
    if (screen.id === "CAP-02")
        return <CaptationForm state={state} onToast={onToast} onNavigate={onNavigate} dispatch={dispatch}/>;
    if (screen.id !== "CAP-01")
        return <CaptationDetail entityId={entityId} screen={screen} state={state} onNavigate={onNavigate} onToast={onToast} dispatch={dispatch}/>;
    const stages = ["Nueva", "Tasación", "Mandato", "Publicada", "Cerrada"];
    return <div className="feature-stack">
<Card>
<TableToolbar searchPlaceholder="Buscar captación, propietario o inmueble" count={captations.length} value={query} onSearch={setQuery} onAdd={() => onNavigate("CAP-02")} addLabel="Abrir captación"/>
<div className="kanban-board">{stages.map((stage) => <div className="kanban-column" key={stage}>
<div className="kanban-column-head">
<span>{stage}</span>
<span className="column-count">{captations.filter((item) => item.stage === stage).length}</span>
</div>{pagination.items.filter((item) => item.stage === stage).map((captation) => { const property = state.properties.find((item) => item.id === captation.propertyId); return <button className="captation-card" key={captation.id} onClick={() => onNavigate("CAP-03", captation.id)}>
<strong>{property?.title}</strong>
<span>{captation.owner}</span>
<small>Expectativa <CurrencyAmount value={captation.expectation} currency={captation.currency}/>
</small>
</button>; })}{!pagination.items.some((item) => item.stage === stage) && <span className="column-empty">Sin casos</span>}</div>)}</div>
<Pagination page={pagination.page} pages={pagination.pages} onChange={pagination.setPage}/>
</Card>
<Alert tone="info" title="Del propietario a publicar">La tasación y el mandato aparecen como pasos visibles. Un mandato sin firma advierte, pero no bloquea la publicación.</Alert>
</div>;
}
function CaptationForm({ state, onToast, onNavigate, dispatch }: {
    state: DemoState;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    onNavigate: ScreenNavigator;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const [propertyId, setPropertyId] = useState(state.properties[0]?.id ?? "");
    const [expectation, setExpectation] = useState("");
    const [owner, setOwner] = useState("Lucía Ferrari");
    const originOptions = state.catalogEntries.filter((entry) => entry.catalogType === "origin" && entry.status === "Activo");
    const [origin, setOrigin] = useState(originOptions[0]?.label ?? "Origen desconocido");
    const save = () => {
        const amount = Number(expectation.replace(/\./g, "").replace(",", "."));
        if (!propertyId || !Number.isFinite(amount) || amount <= 0) {
            onToast("Elegí un inmueble y cargá una expectativa válida.", "warning");
            return;
        }
        dispatch({ type: "captation/create", item: { id: `captation-${Date.now()}`, propertyId, owner, stage: "Nueva", expectation: amount, valuation: null, currency: "USD", origin } });
        onToast("Captación abierta.");
        onNavigate("CAP-01");
    };
    return <Card className="form-card">
<div className="form-card-heading">
<div>
<span className="eyebrow">Alta rápida</span>
<h2>Abrir captación</h2>
<p>Registrá el caso del propietario y seguí con tasación y mandato como pasos explícitos.</p>
</div>
<Tag tone="info">Mandato no bloqueante</Tag>
</div>
<div className="form-grid">
<SelectField label="Inmueble" value={propertyId} onChange={(event) => setPropertyId(event.target.value)}>{state.properties.map((property) => <option key={property.id} value={property.id}>{property.title}</option>)}</SelectField>
<Field label="Expectativa" placeholder="235.000" value={expectation} onChange={(event) => setExpectation(event.target.value)}/>
<SelectField label="Responsable" value={owner} onChange={(event) => setOwner(event.target.value)}>
<option>Lucía Ferrari</option>
<option>Martín Quiroga</option>
<option>Rodrigo Vergara</option>
</SelectField>
<SelectField label="Origen" value={origin} onChange={(event) => setOrigin(event.target.value)}>{originOptions.map((entry) => <option key={entry.id}>{entry.label}</option>)}</SelectField>
</div>
<Alert tone="info" title="Datos desconocidos permitidos">La valuación se puede registrar después; no inventamos una cifra para destrabar el alta.</Alert>
<div className="form-footer">
<Button variant="ghost" onClick={() => onNavigate("CAP-01")}>Cancelar</Button>
<Button onClick={save}>Abrir captación</Button>
</div>
</Card>;
}
function CaptationDetail({ screen, state, entityId, onNavigate, onToast, dispatch }: {
    screen: ScreenDefinition;
    state: DemoState;
    entityId?: string | null;
    onNavigate: ScreenNavigator;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const captation = selectedRecord(state.captations, entityId);
    const [valuationDraft, setValuationDraft] = useState(captation?.valuation == null ? "" : String(captation.valuation));
    const [closeReason, setCloseReason] = useState("");
    if (!captation) return <UnavailableRecord />;
    const property = resolveSelectedRecord(state.properties, captation.propertyId);
    if (!property) return <UnavailableRecord />;
    const owner = captation.ownerPartyId ? state.contacts.find((contact) => contact.id === captation.ownerPartyId) : undefined;
    const listing = state.listings.find((item) => item.propertyId === property.id);
    const historyItems = [
        `Captación abierta por ${captation.owner}`,
        captation.valuation === null ? "Tasación pendiente de confirmar" : `Valuación registrada · ${captation.valuation.toLocaleString("es-AR")} ${captation.currency}`,
        captation.stage === "Mandato" || captation.stage === "Publicada" || captation.stage === "Cerrada" ? "Mandato preparado" : "Mandato pendiente",
        listing ? `Publicación derivada · ${listing.title}` : "Publicación pendiente",
    ];
    const gap = captation.valuation === null ? null : captation.expectation - captation.valuation;
    const saveValuation = () => {
        const value = parseOptionalMoney(valuationDraft);
        if (value === undefined || value === null) {
            onToast("Cargá una valuación válida y no negativa.", "warning");
            return;
        }
        dispatch({ type: "captation/issue-valuation", id: captation.id, value, currency: captation.currency, recordedBy: captation.owner });
        onToast("Nueva valuación registrada; la anterior queda en el historial.");
    };
    const closeCaptation = () => {
        const validation = validateReason(closeReason, "cerrar la captación");
        if (!validation.valid) {
            onToast(validation.error, "warning");
            return;
        }
        dispatch({ type: "captation/close", id: captation.id, reason: closeReason.trim() });
        onToast("Captación cerrada; la historia se conserva.", "warning");
    };
    return <div className="feature-stack">
<Card className="detail-hero">
<div className="detail-identity">
<Avatar name={captation.owner} size="large"/>
<div>
<span className="eyebrow">Captación 360</span>
<h2>{property.title}</h2>
<div className="identity-meta">
<StatusDot label={captation.stage} tone={captation.stage === "Mandato" ? "info" : "warning"}/>
<span>Propietario: {owner?.name ?? "Desconocido"}</span>
<span>Responsable: {captation.owner}</span>
</div>
</div>
</div>
<div className="detail-actions">
<Button variant="outline" icon="chart" onClick={() => onNavigate("CAP-04")}>Tasación</Button>
<Button icon="external" onClick={() => onNavigate("LST-03", captation.id)} disabled={captation.stage === "Cerrada"}>Crear publicación</Button>
<Button variant="ghost" icon="more" aria-label="Más acciones" onClick={() => { if (captation.stage !== "Cerrada") { dispatch({ type: "captation/update-stage", id: captation.id, stage: "Mandato" }); onToast("Mandato marcado para revisión.", "info"); } }} disabled={captation.stage === "Cerrada"}/>
</div>
</Card>
<ScreenTabs screen={screen} entityId={entityId} onNavigate={onNavigate} tabs={[{ id: "CAP-03", label: "Resumen" }, { id: "CAP-04", label: "Tasación" }, { id: "CAP-06", label: "Mandato" }, { id: "CAP-07", label: "Cierre" }, { id: "CAP-08", label: "Reactivación" }]}/>
<div className="detail-grid">
<Card>
<SectionHeading eyebrow="Expectativa vs valoración" title="Brecha de precio"/>
<div className="valuation-compare">
<div>
<span>Expectativa del propietario</span>
<strong>
<CurrencyAmount value={captation.expectation} currency={captation.currency}/>
</strong>
</div>
<div className="valuation-arrow">
<Icon name="arrow" size={20}/>
</div>
<div>
<span>Valuación registrada</span>
<strong>{captation.valuation === null ? <UnknownIndicator /> : <CurrencyAmount value={captation.valuation} currency={captation.currency}/>}</strong>
</div>
</div>{screen.id === "CAP-04" && <div className="inline-stage-editor">
<Field label="Valuación" placeholder="235.000" value={valuationDraft} onChange={(event) => setValuationDraft(event.target.value)}/><Button size="small" onClick={saveValuation}>Registrar nueva valuación</Button>
</div>}{captation.valuationHistory && captation.valuationHistory.length > 0 && <div className="definition-grid">{captation.valuationHistory.map((valuation) => <Definition key={valuation.id} label={valuation.recordedAt} value={<CurrencyAmount value={valuation.value} currency={valuation.currency}/>} />)}</div>}{gap !== null && <Alert tone={gap > 0 ? "warning" : "success"} title={gap > 0 ? "Hay una diferencia para conversar" : "Expectativa alineada"}>
<CurrencyAmount value={Math.abs(gap)} currency={captation.currency}/> de diferencia informativa; no modifica el precio automáticamente.</Alert>}{gap === null && <Alert tone="info" title="Valuación desconocida">Todavía no hay una valuación registrada; la expectativa del propietario se conserva sin inventar un valor.</Alert>}</Card>
<Card>
<SectionHeading eyebrow="Mandato" title="Comercialización"/>
<div className="mandate-status">
<span className="mandate-icon">
<Icon name="briefcase" size={20}/>
</span>
<div>
<strong>{captation.stage === "Mandato" ? "Mandato en revisión" : "Todavía no iniciado"}</strong>
<p>La firma queda registrada como estado. Publicar no se bloquea por este dato.</p>
</div>
</div>
<Button variant="outline" size="small" onClick={() => { dispatch({ type: "captation/update-stage", id: captation.id, stage: "Mandato" }); onToast("Borrador de mandato preparado.", "info"); }}>Abrir mandato</Button>
</Card>
</div>
<Card>
<SectionHeading eyebrow="Historia" title="Pasos del caso"/>
<Timeline items={historyItems}/>
</Card>
{screen.id === "CAP-07" && <Card className="form-card"><SectionHeading eyebrow="Cierre" title="Cerrar captación"/><p className="body-copy">El cierre conserva la propiedad, valuaciones, mandato e historial.</p>{captation.stage === "Cerrada" ? <Alert tone="warning" title="Captación cerrada">{captation.closeReason ?? "Se conserva el motivo en la ficha."}</Alert> : <><TextAreaField label="Motivo de cierre" placeholder="Qué ocurrió con el propietario..." rows={4} value={closeReason} onChange={(event) => setCloseReason(event.target.value)}/><Button variant="tertiary" onClick={closeCaptation}>Confirmar cierre</Button></>}</Card>}
{screen.id === "CAP-08" && <Card className="form-card"><SectionHeading eyebrow="Reactivación" title="Reactivar captación"/>{captation.stage === "Cerrada" ? <><p className="body-copy">La reactivación crea un nuevo paso comercial sin borrar el cierre anterior.</p><Button onClick={() => { dispatch({ type: "captation/reactivate", id: captation.id }); onToast("Captación reactivada."); }}>Confirmar reactivación</Button></> : <Alert tone="info" title="Captación activa">Solo se pueden reactivar captaciones cerradas.</Alert>}</Card>}
</div>;
}
function DemandView({ screen, state, entityId, onNavigate, onToast, dispatch }: {
    screen: ScreenDefinition;
    state: DemoState;
    entityId?: string | null;
    onNavigate: ScreenNavigator;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const [query, setQuery] = useState("");
    const demands = state.demands.filter((demandItem) => { const contact = state.contacts.find((item) => item.id === demandItem.contactId); return `${demandItem.title} ${contact?.name ?? ""} ${demandItem.criteria.map((criterion) => `${criterion.label} ${criterion.value}`).join(" ")}`.toLowerCase().includes(query.toLowerCase()); });
    const pagination = useLocalPagination(demands, query);
    if (["DEM-02"].includes(screen.id))
        return <DemandForm state={state} onToast={onToast} onNavigate={onNavigate} dispatch={dispatch}/>;
    const demand = selectedRecord(state.demands, entityId);
    if (screen.id !== "DEM-01" && screen.id !== "DEM-02")
        return demand ? <DemandDetail screen={screen} state={state} demand={demand} onNavigate={onNavigate} onToast={onToast} dispatch={dispatch}/> : <UnavailableRecord />;
    return <div className="feature-stack">
<Card>
<TableToolbar searchPlaceholder="Buscar por contacto, barrio o criterio" count={demands.length} value={query} onSearch={setQuery} onAdd={() => onNavigate("DEM-02")} addLabel="Nueva búsqueda"/>
<div className="table-wrap">{demands.length ? <table className="data-table">
<thead>
<tr>
<th>Búsqueda</th>
<th>Contacto</th>
<th>Criterios</th>
<th>Score</th>
<th>Estado</th>
<th />
</tr>
</thead>
<tbody>{pagination.items.map((demandItem) => { const contact = state.contacts.find((item) => item.id === demandItem.contactId); return <tr key={demandItem.id} onClick={() => onNavigate("DEM-04", demandItem.id)}>
<td>
<strong>{demandItem.title}</strong>
<small>Solicitud activa</small>
</td>
<td>{contact?.name}</td>
<td>
<div className="table-chips">{demandItem.criteria.slice(0, 2).map((criterion) => <Chip tone={criterion.weight === "must" ? "brand" : "neutral"} key={criterion.id}>{criterion.label}: {criterion.value}</Chip>)}</div>
</td>
<td>
<span className="score-inline">{demandItem.score}</span>
</td>
<td>
<StatusDot label={demandItem.status} tone="success"/>
</td>
<td>
<Button variant="ghost" size="xsmall" icon="chevron" aria-label={`Abrir ${demandItem.title}`} onClick={(event) => { event.stopPropagation(); onNavigate("DEM-04", demandItem.id); }}/>
</td>
</tr>; })}</tbody>
</table> : <EmptyState icon="search" title="No hay búsquedas para este filtro" description="Probá con otro contacto, barrio o criterio."/>}</div>
<Pagination page={pagination.page} pages={pagination.pages} onChange={pagination.setPage}/>
</Card>
<Alert tone="info" title="Criterios con peso">Debe, Ojalá, Da igual y Desconocido son visibles y editables. El score se recalcula en vivo.</Alert>
</div>;
}
function DemandForm({ state, onToast, onNavigate, dispatch }: {
    state: DemoState;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    onNavigate: ScreenNavigator;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const [contactId, setContactId] = useState(state.contacts[0]?.id ?? "");
    const originOptions = state.catalogEntries.filter((entry) => entry.catalogType === "origin" && entry.status === "Activo");
    const [origin, setOrigin] = useState(originOptions[0]?.label ?? "Origen desconocido");
    const [rooms, setRooms] = useState("");
    const [neighborhood, setNeighborhood] = useState("");
    const save = () => { const contact = state.contacts.find((item) => item.id === contactId); if (!contact || !rooms.trim()) {
        onToast("Elegí un contacto y cargá al menos los ambientes.", "warning");
        return;
    } dispatch({ type: "demand/create", item: { id: `demand-${Date.now()}`, contactId, title: `${contact.name} busca ${rooms.trim()} ambientes${neighborhood.trim() ? ` en ${neighborhood.trim()}` : ""}`, status: "Activa", origin, criteria: [{ id: "rooms", label: "Ambientes", value: rooms.trim(), weight: "must" }, { id: "neighborhood", label: "Barrio", value: neighborhood.trim() || "UNKNOWN", weight: neighborhood.trim() ? "nice" : "unknown" }, { id: "surface", label: "Superficie cubierta", value: "UNKNOWN", weight: "unknown" }], score: 0, selectedListingIds: [] } }); onToast("Búsqueda guardada."); onNavigate("DEM-01"); };
    return <Card className="form-card">
<div className="form-card-heading">
<div>
<span className="eyebrow">Alta asistida</span>
<h2>Nueva búsqueda</h2>
<p>Podés partir de una conversación pegada y revisar cada dato deducido antes de guardarlo.</p>
</div>
<Tag tone="info">No es una integración</Tag>
</div>
<div className="form-grid">
<SelectField label="Contacto" value={contactId} onChange={(event) => setContactId(event.target.value)}>{state.contacts.map((contact) => <option key={contact.id} value={contact.id}>{contact.name}</option>)}</SelectField>
<SelectField label="Origen" value={origin} onChange={(event) => setOrigin(event.target.value)}>
{originOptions.map((entry) => <option key={entry.id}>{entry.label}</option>)}
</SelectField>
<Field label="Ambientes" placeholder="3" value={rooms} onChange={(event) => setRooms(event.target.value)}/>
<Field label="Barrio sugerido" placeholder="Villa Crespo" value={neighborhood} onChange={(event) => setNeighborhood(event.target.value)}/>
<TextAreaField label="Texto de la consulta" placeholder="Pegá acá el texto de WhatsApp..." rows={5}/>
</div>
<Alert tone="info" title="Revisá lo deducido">Los datos sugeridos quedan editables y marcados antes de crear la búsqueda.</Alert>
<div className="form-footer">
<Button variant="ghost" onClick={() => onNavigate("DEM-01")}>Cancelar</Button>
<Button onClick={save}>Guardar búsqueda</Button>
</div>
</Card>;
}
function DemandDetail({ screen, state, demand, onNavigate, onToast, dispatch }: {
    screen: ScreenDefinition;
    state: DemoState;
    demand: DemoDemand;
    onNavigate: ScreenNavigator;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const [confirmClose, setConfirmClose] = useState(false);
    const [closeReason, setCloseReason] = useState("");
    const contact = state.contacts.find((party) => party.id === demand.contactId);
    const matchListing = state.listings.find((listing) => (demand.selectedListingIds ?? []).includes(listing.id)) ?? state.listings.find((listing) => listing.status === "Activa");
    const matchProperty = matchListing ? state.properties.find((property) => property.id === matchListing.propertyId) : undefined;
    const surfaceSuggestion = demand.id === "demand-1" ? state.aiSuggestions.find((suggestion) => suggestion.id === "ai-surface") : undefined;
    const updateStatus = (status: DemoDemand["status"]) => {
        dispatch({ type: "demand/update-status", id: demand.id, status });
        onToast(status === "Pausada" ? "Búsqueda pausada." : "Búsqueda reactivada.", "info");
    };
    const closeDemand = () => {
        const validation = validateReason(closeReason, "cerrar la búsqueda");
        if (!validation.valid) {
            onToast(validation.error, "warning");
            return;
        }
        dispatch({ type: "demand/update-status", id: demand.id, status: "Cerrada", reason: closeReason.trim() });
        setConfirmClose(false);
        onToast("Búsqueda cerrada; el historial se conserva.", "warning");
    };
    return <div className="feature-stack">
<Card className="detail-hero">
<div className="detail-identity">
<Avatar name={contact?.name ?? "Party desconocida"} size="large"/>
<div>
<span className="eyebrow">Búsqueda 360</span>
<h2>{demand.title}</h2>
<div className="identity-meta">
<StatusDot label={demand.status}/>
<span>Origen: {demand.origin ?? "Origen desconocido"}</span>
<span>Score actual: {demand.score}</span>
</div>
</div>
</div>
<div className="detail-actions">
<Button variant="outline" icon="edit" onClick={() => onNavigate("DEM-03", demand.id)}>Editar criterios</Button>
<Button icon="target" onClick={() => onNavigate("MAT-01", demand.id)}>Ver compatibilidades</Button>
</div>
</Card>
<div className="detail-grid">
<Card>
<SectionHeading eyebrow="Criterios" title="Qué busca" action={<Chip tone="info">{demand.score} puntos</Chip>}/>
<div className="criteria-stack">{demand.criteria.map((criterion) => <CriterionEditor key={criterion.id} label={criterion.label} value={criterion.value} weight={criterion.weight} onChange={({ value, weight }) => dispatch({ type: "demand/update-criterion", demandId: demand.id, criterionId: criterion.id, value, weight })}/>)}</div>{screen.id === "DEM-03" && <Alert tone="success" title="Score recalculado">Los cambios en los criterios se reflejan en esta misma pantalla.</Alert>}</Card>
<Card>
<SectionHeading eyebrow="Asistente" title="Información faltante"/>
{surfaceSuggestion?.status === "pending" ? <AISuggestion title={surfaceSuggestion.title} body={surfaceSuggestion.body} evidence={surfaceSuggestion.evidence} onReview={() => { dispatch({ type: "ai/review", id: surfaceSuggestion.id }); onToast("Sugerencia revisada; la búsqueda no cambió automáticamente.", "info"); }} onDismiss={() => { dispatch({ type: "ai/dismiss", id: surfaceSuggestion.id }); onToast("Sugerencia descartada; la búsqueda no cambió.", "info"); }}/> : <Alert tone={surfaceSuggestion?.status === "dismissed" ? "warning" : "success"} title={surfaceSuggestion?.status === "dismissed" ? "Sugerencia descartada" : "Sugerencia revisada"}>La decisión queda en el historial del asistente y no modificó los criterios automáticamente.</Alert>}
</Card>
</div>
<Card>
<SectionHeading eyebrow="Compatibilidades" title="Oferta que encaja" action={<Button variant="ghost" size="small" onClick={() => onNavigate("MAT-01", demand.id)}>Ver todas <Icon name="arrow" size={14}/>
</Button>}/>
<div className="match-inline">{matchListing && matchProperty ? <><span className="score-bubble">{demand.score}</span><div><strong>{matchProperty.title}</strong><p>{matchListing.title} · coincidencias explicables, con datos desconocidos señalados.</p></div><Chip tone={demand.score >= 80 ? "success" : "warning"}>{demand.score >= 80 ? "Alta" : "Media"} confianza</Chip></> : <EmptyState icon="target" title="Sin compatibilidades visibles" description="Todavía no hay una publicación activa relacionada con esta búsqueda." />}</div>
</Card>
{screen.id === "DEM-06" && <Card className="form-card"><SectionHeading eyebrow="Estado de la búsqueda" title="Pausar o cerrar"/><p className="body-copy">Pausar conserva la búsqueda para retomarla. Cerrar exige un motivo y no borra sus criterios ni la historia.</p><div className="form-footer form-footer-left">{demand.status === "Activa" ? <Button variant="outline" onClick={() => updateStatus("Pausada")}>Pausar búsqueda</Button> : demand.status === "Pausada" ? <Button variant="outline" onClick={() => updateStatus("Activa")}>Reactivar búsqueda</Button> : <Tag tone="warning">Búsqueda cerrada</Tag>} {demand.status !== "Cerrada" && <Button variant="tertiary" onClick={() => setConfirmClose(true)}>Cerrar búsqueda</Button>}</div></Card>}
<Modal open={confirmClose} title="Cerrar búsqueda" onClose={() => setConfirmClose(false)} footer={<><Button variant="ghost" onClick={() => setConfirmClose(false)}>Cancelar</Button><Button variant="tertiary" onClick={closeDemand}>Confirmar cierre</Button></>}><TextAreaField label="Motivo de cierre" placeholder="Qué ocurrió con esta necesidad..." rows={4} value={closeReason} onChange={(event) => setCloseReason(event.target.value)}/></Modal>
</div>;
}
function MatchingView({ screen, state, entityId, onNavigate, onToast, dispatch }: {
    screen: ScreenDefinition;
    state: DemoState;
    entityId?: string | null;
    onNavigate: ScreenNavigator;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const [showMatchFilters, setShowMatchFilters] = useState(false);
    const [minimumMatchScore, setMinimumMatchScore] = useState<"ALL" | "60" | "80">("ALL");
    const selectedListing = screen.id === "MAT-03" ? selectedRecord(state.listings, entityId) : undefined;
    const property = selectedListing
        ? selectedRecord(state.properties, selectedListing.propertyId)
        : screen.id === "MAT-03"
            ? entityId === undefined || entityId === null ? state.properties[0] : selectedRecord(state.properties, entityId)
            : state.properties[0];
    const demand = screen.id === "MAT-03"
        ? selectedListing
            ? state.demands.find((item) => item.status === "Activa" && item.selectedListingIds?.includes(selectedListing.id))
            : state.demands[0]
        : entityId === undefined || entityId === null ? state.demands[0] : selectedRecord(state.demands, entityId);
    if (!property || !demand) return <UnavailableRecord />;
    const contact = state.contacts.find((party) => party.id === demand.contactId);
    const secondaryListing = state.listings.find((listing) => listing.status === "Activa" && listing.id !== selectedListing?.id && listing.propertyId !== property.id);
    const secondaryProperty = secondaryListing ? state.properties.find((item) => item.id === secondaryListing.propertyId) : undefined;
    const primaryMatch = state.matchActions.find((item) => item.demandId === demand.id && item.propertyId === property.id);
    const score = demand.score;
    const minimumScore = minimumMatchScore === "ALL" ? 0 : Number(minimumMatchScore);
    const primaryVisible = score >= minimumScore;
    const criterionDetails: CriterionEvidence[] = demand.criteria.map((criterion) => {
        const contribution = criterionContribution(criterion.value, propertyCriterionValue(criterion, property));
        return { label: criterion.label, status: contribution.status, contribution: contribution.contribution };
    });
    const setMatchStatus = (status: "favorite" | "dismissed" | "presented", reason?: string) => {
        dispatch({ type: "match/set-status", demandId: demand.id, propertyId: property.id, status, reason });
        onToast(status === "favorite" ? "Inmueble marcado como favorito." : status === "dismissed" ? "Compatibilidad descartada; queda trazabilidad." : "Presentación preparada para revisar.", "info");
    };
    return <div className="feature-stack">
<Card>
<div className="match-toolbar">
<div>
<span className="eyebrow">{screen.id === "MAT-03" ? "Desde una publicación" : "Desde una búsqueda"}</span>
<h2>{screen.id === "MAT-03" ? property.title : demand.title}</h2>
<p>La compatibilidad se puede explicar, revisar y descartar. Nunca se muestra solo el número.</p>
</div>
<div className="match-toolbar-actions">
<Button variant="outline" size="small" icon="filter" aria-expanded={showMatchFilters} onClick={() => setShowMatchFilters((open) => !open)}>Filtros</Button>
{showMatchFilters && <SelectField label="Confianza mínima" value={minimumMatchScore} onChange={(event) => setMinimumMatchScore(event.target.value as typeof minimumMatchScore)}><option value="ALL">Todas</option><option value="60">Media o más</option><option value="80">Alta</option></SelectField>}
<Button size="small" icon="external" onClick={() => onNavigate(screen.id === "MAT-03" ? "LST-04" : "DEM-04", screen.id === "MAT-03" ? selectedListing?.id : demand.id)}>Abrir fuente</Button>
</div>
</div>
<div className="match-list">
{primaryVisible ? <div className="match-result">
<PropertyPlaceholder title={property.title}/>
<div className="match-result-main">
<div className="match-result-head">
<div>
<Tag tone="info">Búsqueda</Tag>
<h3>{demand.title}</h3>
<p>{contact?.name ?? "Party desconocida"} · {propertyCriterionValue(demand.criteria.find((criterion) => criterion.label.toLowerCase().includes("barrio")) ?? { label: "Barrio" }, property)} · {demand.criteria.find((criterion) => criterion.label.toLowerCase().includes("ambiente"))?.value ?? "UNKNOWN"} ambientes</p>
</div>
<span className="score-bubble score-bubble-large">{score}</span>
</div>
<div className="match-meter">
<span>
<i style={{ width: `${score}%` }}/>
</span>
<small>{score >= 80 ? "Alta" : score >= 60 ? "Media" : "Baja"} confianza</small>
</div>
<div className="match-chips">
<Chip tone="success">{property.bedrooms}</Chip>
<Chip tone={propertyCriterionValue({ label: "Barrio" }, property) === "UNKNOWN" ? "warning" : "success"}>{propertyCriterionValue({ label: "Barrio" }, property)}</Chip>
<Chip tone="warning">Superficie desconocida</Chip>
</div>
<div className="match-result-actions">
<Button variant="secondary" size="small" onClick={() => onNavigate("MAT-02", screen.id === "MAT-03" ? selectedListing?.id : demand.id)}>Ver explicación</Button>
<Button variant="ghost" size="small" onClick={() => setMatchStatus("favorite")} disabled={primaryMatch?.status === "favorite"}>Favorito</Button>
<Button variant="ghost" size="small" onClick={() => setMatchStatus("dismissed", "Descartada desde compatibilidades")}>Descartar</Button>
</div>{primaryMatch && <Alert tone={primaryMatch.status === "dismissed" ? "warning" : "success"} title="Estado de la compatibilidad">{primaryMatch.status === "favorite" ? "Marcada como favorita para revisión." : primaryMatch.status === "dismissed" ? "Descartada con trazabilidad." : "Presentación preparada para revisar."}</Alert>}</div>
</div> : <EmptyState icon="filter" title="No hay compatibilidades con este filtro" description="Probá con una confianza mínima diferente." />}
{secondaryListing && secondaryProperty && demand.score - 28 >= minimumScore && <div className="match-result match-result-muted">
<PropertyPlaceholder title={secondaryProperty.title}/>
<div className="match-result-main">
<div className="match-result-head">
<div>
<Tag tone="neutral">Búsqueda</Tag>
<h3>{secondaryProperty.title}</h3>
<p>Coincidencia parcial · {secondaryProperty.bedrooms} · {secondaryListing.title}</p>
</div>
<span className="score-bubble score-bubble-muted">{Math.max(0, demand.score - 28)}</span>
</div>
<div className="match-result-actions">
<Button variant="ghost" size="small" onClick={() => onNavigate("MAT-02", demand.id)}>Ver explicación</Button>
</div>
</div>
</div>}
</div>
</Card>{screen.id === "MAT-02" && <MatchScoreExplanation score={score} confidence={score >= 80 ? "Alta" : "Media"} matches={[]} unknowns={[]} criterionDetails={criterionDetails}/>}{screen.id === "MAT-06" && <Alert tone="warning" title="Compatibilidad invalidada">La publicación cambió desde el cálculo. Revisá la explicación antes de presentar el inmueble.</Alert>}<Card>
<SectionHeading eyebrow="Siguiente paso" title="Presentar sin enviar" action={<Button size="small" icon="arrow" onClick={() => { setMatchStatus("presented"); onNavigate("MAT-04", screen.id === "MAT-03" ? selectedListing?.id : demand.id); }}>Preparar presentación</Button>}/>
<p className="body-copy">El CRM registra la actividad histórica de presentación; no envía WhatsApp ni correo.</p>
</Card>
</div>;
}
function PipelineView({ screen, state, entityId, onNavigate, onToast, dispatch }: {
    screen: ScreenDefinition;
    state: DemoState;
    entityId?: string | null;
    onNavigate: ScreenNavigator;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const [showPipelineFilters, setShowPipelineFilters] = useState(false);
    const [pipelineKindFilter, setPipelineKindFilter] = useState<"ALL" | "REQUIREMENT" | "CAPTATION_CASE">("ALL");
    const visibleOpportunities = state.opportunities.filter((opportunity) => pipelineKindFilter === "ALL" || opportunity.sourceType === pipelineKindFilter);
    if (screen.id === "OPP-03")
        return <OpportunityForm state={state} onToast={onToast} onNavigate={onNavigate} dispatch={dispatch}/>;
    if (screen.id === "OPP-02" || screen.id === "OPP-10")
        return <OpportunityList screen={screen} state={state} onNavigate={onNavigate} onToast={onToast} dispatch={dispatch}/>;
    if (screen.id !== "OPP-01" && screen.id !== "OPP-02" && screen.id !== "OPP-10")
        return <OpportunityDetail entityId={entityId} screen={screen} state={state} onNavigate={onNavigate} onToast={onToast} dispatch={dispatch}/>;
    return <div className="feature-stack">
<Card>
<div className="board-toolbar">
<div>
<span className="eyebrow">Seguimiento comercial</span>
<h2>Tablero de oportunidades</h2>
<p>Cada tarjeta conserva el origen de la oportunidad para que puedas seguir el contexto completo.</p>
</div>
<div className="board-actions">
<Button variant="outline" size="small" icon="filter" aria-expanded={showPipelineFilters} onClick={() => setShowPipelineFilters((open) => !open)}>Filtros</Button>
{showPipelineFilters && <SelectField label="Tipo de pipeline" value={pipelineKindFilter} onChange={(event) => setPipelineKindFilter(event.target.value as typeof pipelineKindFilter)}><option value="ALL">Todos</option><option value="REQUIREMENT">Demanda</option><option value="CAPTATION_CASE">Captación</option></SelectField>}
<Button size="small" icon="plus" onClick={() => onNavigate("OPP-03")}>Crear oportunidad</Button>
</div>
</div>
<div className="pipeline-board">{stageOrder.map((stage) => <div className="pipeline-column" key={stage}>
<div className="pipeline-column-head">
<span>{stage}</span>
<span className="column-count">{visibleOpportunities.filter((item) => item.stage === stage).length}</span>
</div>{visibleOpportunities.filter((item) => item.stage === stage).map((opportunity) => <div className="pipeline-card-wrap" key={opportunity.id}>
<PipelineCard title={opportunity.title} sourceType={opportunity.sourceType} stage={opportunity.stage} owner={opportunity.owner} fee={<CurrencyAmount value={opportunity.fee} currency={opportunity.currency}/>} onOpen={() => onNavigate("OPP-04", opportunity.id)}/>
<Button variant="outline" size="xsmall" onClick={() => onNavigate("OPP-05", opportunity.id)}>Cambiar etapa</Button>
</div>)}</div>)}</div>
</Card>
<div className="pipeline-footnote">
<Icon name="info" size={16}/>
<span>No hay valor ponderado del embudo: el monto mostrado son honorarios estimados en crudo, no el precio de los inmuebles.</span>
</div>
</div>;
}
function OpportunityList({ screen, state, onNavigate, onToast, dispatch }: {
    screen: ScreenDefinition;
    state: DemoState;
    onNavigate: ScreenNavigator;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const [query, setQuery] = useState("");
    const opportunities = state.opportunities.filter((opportunity) => `${opportunity.title} ${opportunity.sourceId} ${opportunity.owner} ${opportunity.sourceType}`.toLowerCase().includes(query.toLowerCase()));
    const pagination = useLocalPagination(opportunities, query);
    return <div className="feature-stack">
<Card>
<TableToolbar searchPlaceholder="Buscar oportunidad, Party o fuente" count={opportunities.length} value={query} onSearch={setQuery} onAdd={() => onNavigate("OPP-03")} addLabel="Nueva oportunidad" filterLabel={screen.id === "OPP-10" ? "Filtros guardados" : "Filtrar"}/>
<div className="table-wrap">{opportunities.length ? <table className="data-table">
<thead>
<tr>
<th>Oportunidad</th>
<th>Fuente real</th>
<th>Etapa</th>
<th>Responsable</th>
<th>Honorarios</th>
<th />
</tr>
</thead>
<tbody>{pagination.items.map((opportunity) => <tr key={opportunity.id} onClick={() => onNavigate("OPP-04", opportunity.id)}>
<td>
<strong>{opportunity.title}</strong>
<small>{opportunity.daysInStage} días en etapa{opportunity.outcome ? ` · ${opportunity.outcome === "won" ? "Ganada" : "Perdida"}` : ""}</small>
</td>
<td>
<Tag tone={opportunity.sourceType === "REQUIREMENT" ? "info" : "brand"}>{opportunity.sourceType === "REQUIREMENT" ? "Búsqueda" : "Captación"}</Tag>
<small>Origen registrado</small>
</td>
<td>
<Button variant="outline" size="xsmall" onClick={(event) => { event.stopPropagation(); onNavigate("OPP-05", opportunity.id); }}>Etapa: {opportunity.stage}</Button>
</td>
<td>
<select aria-label={`Responsable de ${opportunity.title}`} value={opportunity.owner} onClick={(event) => event.stopPropagation()} onChange={(event) => { dispatch({ type: "opportunity/reassign", id: opportunity.id, owner: event.target.value }); onToast("Responsable actualizado.", "info"); }}>
<option>Martín Quiroga</option>
<option>Lucía Ferrari</option>
<option>Rodrigo Vergara</option>
<option>Elena Vergara</option>
</select>
</td>
<td>
<CurrencyAmount value={opportunity.fee} currency={opportunity.currency}/>
</td>
<td>
<Button variant="ghost" size="xsmall" icon="chevron" aria-label={`Abrir ${opportunity.title}`} onClick={(event) => { event.stopPropagation(); onNavigate("OPP-04", opportunity.id); }}/>
</td>
</tr>)}</tbody>
</table> : <EmptyState icon="search" title="No hay oportunidades para este filtro" description="Probá con otra fuente, responsable o etapa."/>}</div>
<Pagination page={pagination.page} pages={pagination.pages} onChange={pagination.setPage}/>
</Card>{screen.id === "OPP-10" && <Alert tone="info" title="Filtros guardados">Los filtros se guardan como preferencia de revisión; no crean una nueva entidad de negocio.</Alert>}</div>;
}
function OpportunityForm({ state, onToast, onNavigate, dispatch }: {
    state: DemoState;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    onNavigate: ScreenNavigator;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const [title, setTitle] = useState("");
    const [sourceType, setSourceType] = useState<"REQUIREMENT" | "CAPTATION_CASE">("REQUIREMENT");
    const [sourceId, setSourceId] = useState(state.demands[0]?.id ?? "");
    const [owner, setOwner] = useState("Martín Quiroga");
    const [origin, setOrigin] = useState("Carga manual");
    const sourceOptions = sourceType === "REQUIREMENT" ? state.demands.map((demand) => ({ id: demand.id, title: demand.title })) : state.captations.map((captation) => ({ id: captation.id, title: state.properties.find((property) => property.id === captation.propertyId)?.title ?? captation.id }));
    const originOptions = state.catalogEntries.filter((entry) => entry.catalogType === "origin" && entry.status === "Activo");
    const save = () => { if (!title.trim() || !sourceId || !origin.trim()) {
        onToast("Completá el nombre y la fuente real de la oportunidad.", "warning");
        return;
    } dispatch({ type: "opportunity/create", item: { id: `opp-${Date.now()}`, title: title.trim(), sourceType, sourceId, origin, stage: "Nuevo", owner, fee: 0, currency: "ARS", daysInStage: 0 } }); onToast("Oportunidad creada."); onNavigate("OPP-02"); };
    return <Card className="form-card">
<div className="form-card-heading">
<div>
<span className="eyebrow">Alta comercial</span>
<h2>Nueva oportunidad</h2>
<p>La oportunidad reúne el seguimiento comercial y conserva el origen de la operación.</p>
</div>
<Tag tone="info">Origen trazable</Tag>
</div>
<div className="form-grid">
<Field label="Nombre de la oportunidad" placeholder="Ej. Ana · Casa Villa Crespo" value={title} onChange={(event) => setTitle(event.target.value)} autoFocus/>
<SelectField label="Tipo de fuente" value={sourceType} onChange={(event) => { const next = event.target.value as typeof sourceType; setSourceType(next); setSourceId(next === "REQUIREMENT" ? state.demands[0]?.id ?? "" : state.captations[0]?.id ?? ""); }}>
<option value="REQUIREMENT">Búsqueda</option>
<option value="CAPTATION_CASE">Captación</option>
</SelectField>
<SelectField label="Fuente real" value={sourceId} onChange={(event) => setSourceId(event.target.value)}>{sourceOptions.map((source) => <option key={source.id} value={source.id}>{source.title}</option>)}</SelectField>
<SelectField label="Origen comercial" value={origin} onChange={(event) => setOrigin(event.target.value)}>{originOptions.map((entry) => <option value={entry.label} key={entry.id}>{entry.label}</option>)}</SelectField>
<SelectField label="Responsable" value={owner} onChange={(event) => setOwner(event.target.value)}>
<option>Martín Quiroga</option>
<option>Lucía Ferrari</option>
<option>Rodrigo Vergara</option>
<option>Elena Vergara</option>
</SelectField>
</div>
<Alert tone="info" title="Sin valor ponderado">El tablero muestra honorarios crudos y no convierte el precio del inmueble en valor ponderado.</Alert>
<div className="form-footer">
<Button variant="ghost" onClick={() => onNavigate("OPP-02")}>Cancelar</Button>
<Button onClick={save}>Crear oportunidad</Button>
</div>
</Card>;
}
function OpportunityDetail({ screen, state, entityId, onNavigate, onToast, dispatch }: {
    screen: ScreenDefinition;
    state: DemoState;
    entityId?: string | null;
    onNavigate: ScreenNavigator;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const [stageDraft, setStageDraft] = useState<OpportunityStage>("Nuevo");
    const [stageReason, setStageReason] = useState("");
    const [closeDate, setCloseDate] = useState("");
    const [finalValue, setFinalValue] = useState("");
    const [closeReason, setCloseReason] = useState("");
    const opportunity = selectedRecord(state.opportunities, entityId);
    useEffect(() => { if (opportunity) setStageDraft(opportunity.stage); }, [opportunity]);
    if (!opportunity) return <UnavailableRecord />;
    const sourceTitle = opportunity.sourceType === "REQUIREMENT" ? state.demands.find((demand) => demand.id === opportunity.sourceId)?.title : state.captations.find((captation) => captation.id === opportunity.sourceId)?.propertyId ? state.properties.find((property) => property.id === state.captations.find((captation) => captation.id === opportunity.sourceId)?.propertyId)?.title : undefined;
    const lossReasons = state.catalogEntries.filter((entry) => entry.catalogType === "loss-reason" && entry.status === "Activo");
    const saveStage = () => {
        const validation = validateOpportunityStageChange(opportunity.stage, stageDraft, stageReason);
        if (!validation.valid) {
            onToast(validation.error, "warning");
            return;
        }
        dispatch({ type: "opportunity/change-stage", id: opportunity.id, stage: stageDraft, reason: stageReason.trim() });
        onToast("Cambio de etapa guardado.");
    };
    const saveClose = (outcome: "won" | "lost") => {
        const value = finalValue.trim() ? Number(finalValue.replace(/\./g, "").replace(",", ".")) : undefined;
        const validation = validateOpportunityClose(outcome, closeDate, value, closeReason);
        if (!validation.valid) {
            onToast(validation.error, "warning");
            return;
        }
        dispatch({ type: "opportunity/close", id: opportunity.id, outcome, closeDate, finalValue: value, reason: closeReason.trim() || undefined });
        onToast(outcome === "won" ? "Oportunidad marcada como ganada." : "Oportunidad cerrada como perdida.", outcome === "won" ? "success" : "warning");
    };
    return <div className="feature-stack">
<Card className="detail-hero">
<div className="detail-identity">
<Avatar name={opportunity.owner} size="large"/>
<div>
<span className="eyebrow">Oportunidad</span>
<h2>{opportunity.title}</h2>
<div className="identity-meta">
<StatusDot label={opportunity.stage} tone={opportunity.stage === "Cerrada" ? "neutral" : "info"}/>
<Tag tone={opportunity.sourceType === "REQUIREMENT" ? "info" : "brand"}>{opportunity.sourceType === "REQUIREMENT" ? "Búsqueda" : "Captación"}</Tag>
<span>Responsable: {opportunity.owner}</span>
</div>
</div>
</div>
<div className="detail-actions">
<Button variant="outline" icon="edit" onClick={() => onNavigate("OPP-05", opportunity.id)} disabled={opportunity.stage === "Cerrada"}>Cambiar etapa</Button>
<Button icon="briefcase" onClick={() => onNavigate("COM-08", opportunity.id)} disabled={opportunity.stage === "Cerrada"}>Crear reserva</Button>
<Button variant="ghost" icon="activity" aria-label="Ver trazabilidad" onClick={() => onNavigate("OPP-11", opportunity.id)}/>
</div>
</Card>
<ScreenTabs screen={screen} entityId={entityId} onNavigate={onNavigate} tabs={[{ id: "OPP-04", label: "Resumen" }, { id: "OPP-06", label: "Historial" }, { id: "OPP-11", label: "Trazabilidad" }, { id: "OPP-07", label: "Cerrar ganada" }, { id: "OPP-08", label: "Cerrar perdida" }]}/>
<div className="detail-grid">
<Card>
<SectionHeading eyebrow="Estado actual" title={opportunity.stage}/>
<div className="stage-progress">{stageOrder.slice(0, 6).map((stage, index) => <div className={stage === opportunity.stage ? "stage-step is-active" : index < stageOrder.indexOf(opportunity.stage) ? "stage-step is-complete" : "stage-step"} key={stage}>
<span>{index + 1}</span>
<small>{stage}</small>
</div>)}</div>{opportunity.outcome && <Alert tone={opportunity.outcome === "won" ? "success" : "warning"} title={opportunity.outcome === "won" ? "Oportunidad ganada" : "Oportunidad perdida"}>El motivo y el historial quedan asociados a la fuente {opportunity.sourceType === "REQUIREMENT" ? "de la búsqueda" : "de la captación"}.</Alert>}{screen.id === "OPP-05" && <div className="inline-stage-editor">
<SelectField label="Nueva etapa" value={stageDraft} onChange={(event) => setStageDraft(event.target.value as OpportunityStage)}>
{state.catalogEntries.filter((entry) => entry.catalogType === "pipeline-stage" && entry.status === "Activo" && entry.semanticState !== "LOST").map((entry) => <option key={entry.id} value={entry.label}>{entry.label}</option>)}
</SelectField>
<TextAreaField label="Motivo del cambio" placeholder="Qué hecho ocurrió o qué se confirmó..." rows={3} value={stageReason} onChange={(event) => setStageReason(event.target.value)}/>
<Button onClick={saveStage}>Guardar cambio de etapa</Button>
<Alert tone="warning" title="Confirmá el motivo">El cambio queda en el historial y nunca sobrescribe silenciosamente el estado anterior.</Alert>
</div>}</Card>
<Card>
<SectionHeading eyebrow="Fuente real" title="Trazabilidad"/>
<div className="trace-panel">
<Tag tone={opportunity.sourceType === "REQUIREMENT" ? "info" : "brand"}>{opportunity.sourceType === "REQUIREMENT" ? "Búsqueda" : "Captación"}</Tag>
<strong>{sourceTitle ?? "Fuente no disponible"}</strong>
<span>Origen: {opportunity.origin ?? "Origen desconocido"}</span>
<Button variant="ghost" size="small" onClick={() => onNavigate(opportunity.sourceType === "REQUIREMENT" ? "DEM-04" : "CAP-03", opportunity.sourceId)}>Abrir fuente <Icon name="arrow" size={14}/>
</Button>
</div>
</Card>
</div>
<Card>
<SectionHeading eyebrow="Historial de etapas" title="Cambios trazables"/>
<Timeline items={state.stageHistory.filter((item) => item.opportunityId === opportunity.id).map((item) => `${item.from} → ${item.to} · ${item.actor} · ${item.at}${item.reason ? ` · ${item.reason}` : ""}`)}/>
</Card>
{screen.id === "OPP-07" || screen.id === "OPP-08" ? <Card className="form-card">
<div className="form-card-heading"><div><span className="eyebrow">Confirmación</span><h2>{screen.id === "OPP-07" ? "Cerrar oportunidad como ganada" : "Cerrar oportunidad como perdida"}</h2><p>El cierre queda asociado a la oportunidad y a su fuente real.</p></div><Tag tone="warning">Requiere evidencia</Tag></div>
<Field label="Fecha de cierre" type="date" value={closeDate} onChange={(event) => setCloseDate(event.target.value)}/>
{screen.id === "OPP-07" ? <Field label="Valor final" placeholder="7.050.000" inputMode="decimal" value={finalValue} onChange={(event) => setFinalValue(event.target.value)}/> : <SelectField label="Motivo de pérdida" value={closeReason} onChange={(event) => setCloseReason(event.target.value)}><option value="">Elegí un motivo</option>{lossReasons.map((entry) => <option value={entry.label} key={entry.id}>{entry.label}</option>)}</SelectField>} 
<div className="form-footer"><Button variant="ghost" onClick={() => onNavigate("OPP-04", opportunity.id)}>Volver</Button><Button variant={screen.id === "OPP-07" ? "primary" : "tertiary"} onClick={() => saveClose(screen.id === "OPP-07" ? "won" : "lost")}>Confirmar cierre</Button></div>
</Card> : <div className="form-footer form-footer-left">
<Button variant="outline" disabled={opportunity.stage === "Cerrada"} onClick={() => onNavigate("OPP-07", opportunity.id)}>Cerrar ganada</Button>
<Button variant="ghost" disabled={opportunity.stage === "Cerrada"} onClick={() => onNavigate("OPP-08", opportunity.id)}>Cerrar perdida</Button>
</div>}
</div>;
}
function CommercialView({ screen, state, entityId, onNavigate, onToast, dispatch }: {
    screen: ScreenDefinition;
    state: DemoState;
    entityId?: string | null;
    onNavigate: ScreenNavigator;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const requiresReservation = screen.id === "COM-09" || screen.id === "COM-10";
    const requiresOperation = screen.id === "COM-12" || screen.id === "COM-13" || screen.id === "COM-14";
    const reservation = entityId === undefined || entityId === null ? state.reservations[0] : selectedRecord(state.reservations, entityId);
    const operation = entityId === undefined || entityId === null ? state.operations[0] : selectedRecord(state.operations, entityId);
    const opportunity = selectedRecord(state.opportunities, entityId)
        ?? (reservation ? state.opportunities.find((item) => item.id === reservation.opportunityId) : undefined)
        ?? (operation ? state.opportunities.find((item) => state.reservations.find((item) => item.id === operation.reservationId)?.opportunityId === item.id) : undefined)
        ?? (entityId === undefined || entityId === null ? state.opportunities[0] : undefined);
    const sourceDemand = opportunity?.sourceType === "REQUIREMENT" ? state.demands.find((demand) => demand.id === opportunity.sourceId) : undefined;
    const sourceCaptation = opportunity?.sourceType === "CAPTATION_CASE" ? state.captations.find((captation) => captation.id === opportunity.sourceId) : undefined;
    const sourceListingId = sourceDemand?.selectedListingIds?.[0];
    const sourceListing = sourceListingId ? state.listings.find((listing) => listing.id === sourceListingId) : undefined;
    const commercialProperty = state.properties.find((property) => property.id === sourceCaptation?.propertyId || property.id === sourceListing?.propertyId)
        ?? state.properties.find((property) => property.title === reservation?.propertyTitle || property.title === operation?.propertyTitle);
    const commercialProposals = opportunity
        ? state.proposals.filter((proposal) => proposal.opportunityId === opportunity.id).sort((left, right) => left.sequence - right.sequence)
        : [];
    const latestProposal = commercialProposals.at(-1);
    const commercialActivities = opportunity
        ? state.activities.filter((activity) => activity.pipelineItemId === opportunity.id || activity.relatedRecordId === opportunity.id || activity.relatedRecordId === opportunity.sourceId)
        : [];
    const commercialTimeline = [...commercialActivities.map((activity) => ({
        at: Date.parse(activity.occurredAt ?? activity.createdAt) || 0,
        text: `${activity.subject} · ${activity.actor} · ${activity.occurredAt ?? activity.createdAt}`,
    })), ...state.stageHistory.filter((item) => item.opportunityId === opportunity?.id).map((item) => ({
        at: Date.parse(item.at) || 0,
        text: `${item.from} → ${item.to} · ${item.actor} · ${item.at}${item.reason ? ` · ${item.reason}` : ""}`,
    })), ...commercialProposals.map((proposal) => ({
        at: Date.parse(proposal.respondedAt ?? proposal.createdAt) || 0,
        text: `${proposal.kind === "CONTRAPROPUESTA" ? "Contrapropuesta" : "Propuesta"} #${proposal.sequence} · ${proposal.proposedBy} · ${proposal.amount === null ? "monto UNKNOWN" : `${proposal.amount} ${proposal.currency}`}${proposal.outcome !== "pending" ? ` · ${proposal.outcome === "accepted" ? "Aceptada" : "Rechazada"}` : " · Pendiente"}`,
    }))].sort((left, right) => right.at - left.at).map((item) => item.text);
    const displayTitle = reservation?.propertyTitle ?? operation?.propertyTitle ?? opportunity?.title ?? "Progresión comercial";
    const displayCurrency = commercialProperty?.currency ?? opportunity?.currency ?? "ARS";
    const displayPrice = latestProposal ? latestProposal.amount : commercialProperty?.price ?? opportunity?.finalValue;
    const activityTargetId = opportunity?.id ?? reservation?.id ?? operation?.id;
    if ((requiresReservation && !reservation) || (requiresOperation && !operation) || (["COM-04", "COM-05", "COM-06", "COM-07"].includes(screen.id) && !opportunity)) return <UnavailableRecord />;
    if (screen.id === "COM-01")
        return <VisitForm state={state} onNavigate={onNavigate} onToast={onToast} dispatch={dispatch}/>;
    if (screen.id === "COM-04")
        return <NegotiationForm state={state} entityId={entityId} onToast={onToast} onNavigate={onNavigate} dispatch={dispatch}/>;
    if (screen.id === "COM-06")
        return <ProposalForm state={state} entityId={entityId} onToast={onToast} onNavigate={onNavigate} dispatch={dispatch}/>;
    if (screen.id === "COM-07")
        return <ProposalResolutionForm proposal={latestProposal} legacyProposal={state.proposal} entityId={opportunity?.id} onToast={onToast} onNavigate={onNavigate} dispatch={dispatch}/>;
    if (screen.id === "COM-08")
        return <ReservationForm entityId={entityId} state={state} onToast={onToast} onNavigate={onNavigate} dispatch={dispatch}/>;
    if (screen.id === "COM-10")
        return <ReservationCancelForm entityId={entityId} state={state} onToast={onToast} onNavigate={onNavigate} dispatch={dispatch}/>;
    if (screen.id === "COM-11")
        return <OperationForm entityId={entityId} state={state} onToast={onToast} onNavigate={onNavigate} dispatch={dispatch}/>;
    if (screen.id === "COM-14")
        return <OperationCancelForm entityId={entityId} state={state} onToast={onToast} onNavigate={onNavigate} dispatch={dispatch}/>;
    if (screen.id === "COM-13")
        return <CloseOperation entityId={entityId} state={state} onToast={onToast} onNavigate={onNavigate} dispatch={dispatch}/>;
    return <div className="feature-stack">
<Card className="detail-hero">
<div className="detail-identity">
<span className="commercial-icon">
<Icon name={screen.id.includes("RES") ? "briefcase" : "activity"} size={25}/>
</span>
<div>
<span className="eyebrow">Progresión comercial</span>
<h2>{displayTitle}</h2>
<div className="identity-meta">
<StatusDot label={screen.id === "COM-09" ? reservation?.status ?? "Sin reserva" : screen.id === "COM-12" ? operation?.stage ?? "Sin operación" : "Activa"} tone="info"/>
<span>Hecho ocurrido, no agenda</span>
<span>Sin pagos ni cobranza</span>
</div>
</div>
</div>
<div className="detail-actions">
<Button variant="outline" icon="activity" onClick={() => onNavigate("ACT-01", activityTargetId)}>Registrar actividad</Button>
<Button icon="briefcase" onClick={() => onNavigate(screen.id === "COM-09" ? "COM-11" : "COM-08", screen.id === "COM-09" ? reservation?.id : opportunity?.id)}>{screen.id === "COM-09" ? "Crear operación" : "Crear reserva"}</Button>
</div>
</Card>
<ScreenTabs screen={screen} entityId={entityId} onNavigate={onNavigate} tabs={[{ id: "COM-05", label: "Timeline" }, { id: "COM-06", label: "Propuesta" }, { id: "COM-09", label: "Reserva" }, { id: "COM-12", label: "Operación" }]}/>
<div className="detail-grid">
<Card>
<SectionHeading eyebrow="Negociación" title="Hechos registrados"/>
{commercialTimeline.length ? <Timeline items={commercialTimeline}/> : <EmptyState icon="activity" title="Sin hechos registrados" description="Los hechos de esta progresión van a aparecer acá cuando se registren."/>}
</Card>
<Card>
<SectionHeading eyebrow="Propuesta" title="Condiciones visibles"/>
<div className="definition-grid">
<Definition label="Precio" value={displayPrice !== undefined && displayPrice !== null ? <CurrencyAmount value={displayPrice} currency={displayCurrency}/> : <UnknownIndicator/>}/>
<Definition label="Estado" value={(latestProposal?.outcome ?? state.proposal.outcome) === "accepted" ? "Aceptada" : (latestProposal?.outcome ?? state.proposal.outcome) === "rejected" ? "Rechazada" : "Pendiente de resolución"}/>
<Definition label="Secuencia" value={latestProposal ? `${latestProposal.kind === "CONTRAPROPUESTA" ? "Contrapropuesta" : "Propuesta"} #${latestProposal.sequence}` : <UnknownIndicator label="No registrada"/>}/>
<Definition label="Vencimiento" value={latestProposal?.validity ?? <UnknownIndicator label="No registrada"/>}/>
<Definition label="Seña" value={reservation?.deposit !== null && reservation?.deposit !== undefined ? <CurrencyAmount value={reservation.deposit} currency={reservation.currency}/> : <UnknownIndicator label="No registrada"/>}/>
{screen.id === "COM-09" && <><Definition label="Válida desde" value={reservation?.validFrom ?? <UnknownIndicator label="No registrada"/>}/><Definition label="Vencimiento de reserva" value={reservation?.expiresAt ?? <UnknownIndicator label="No registrado"/>}/></>}
{screen.id === "COM-12" && <><Definition label="Tipo de operación" value={operation?.operationType ?? <UnknownIndicator label="No registrado"/>}/><Definition label="Términos" value={operation?.agreedTerms ?? <UnknownIndicator label="No registrados"/>}/><Definition label="Resultado" value={operation?.outcome ?? "OPEN"}/></>}
</div>{commercialProposals.length > 0 && <div className="criterion-chip-list">{commercialProposals.map((proposal) => <Chip key={proposal.id} tone={proposal.outcome === "accepted" ? "success" : proposal.outcome === "rejected" ? "warning" : "info"}>{proposal.kind === "CONTRAPROPUESTA" ? "Contrapropuesta" : "Propuesta"} #{proposal.sequence} · {proposal.proposedBy} · Inmutable</Chip>)}</div>}{screen.id === "COM-07" && <Alert tone="warning" title="Resolver propuesta">Elegí aceptada o rechazada y dejá el motivo. El sistema conserva el historial; una nueva contrapropuesta crea otra secuencia.</Alert>}</Card>
</div>
<Card>
<SectionHeading eyebrow="Reserva" title="Siguiente paso" action={<Button size="small" onClick={() => onNavigate("COM-08", opportunity?.id)}>Crear reserva</Button>}/>
<p className="body-copy">La seña se puede registrar como dato informativo con monto y moneda. No hay cobranza ni recibo dentro del CRM.</p>
</Card>
</div>;
}
function NegotiationForm({ state, entityId, onToast, onNavigate, dispatch }: {
    state: DemoState;
    entityId?: string | null;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    onNavigate: ScreenNavigator;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const [notes, setNotes] = useState("");
    const opportunity = entityId === undefined || entityId === null ? state.opportunities[0] : selectedRecord(state.opportunities, entityId);
    if (entityId !== undefined && entityId !== null && !opportunity) return <UnavailableRecord />;
    const save = () => {
        if (!opportunity) {
            onToast("No hay una oportunidad válida para abrir la negociación.", "warning");
            return;
        }
        dispatch({ type: "opportunity/change-stage", id: opportunity.id, stage: "Negociación", reason: "Negociación abierta desde el flujo comercial" });
        dispatch({ type: "activity/add", item: { id: `activity-${Date.now()}`, type: "Nota", subject: "Negociación abierta", body: notes.trim() || "Negociación abierta desde el flujo comercial.", actor: "Martín Quiroga", recordedByUserId: "user-1", occurredAt: new Date().toISOString(), relatedRecordId: opportunity.id, relatedRecordType: "Opportunity", createdAt: "Ahora" } });
        onToast("Negociación abierta y registrada.");
        onNavigate("COM-05", opportunity.id);
    };
    return <Card className="form-card">
<div className="form-card-heading">
<div>
<span className="eyebrow">Progresión comercial</span>
<h2>Abrir negociación</h2>
<p>La negociación nace de un hecho comercial y conserva la oportunidad de origen.</p>
</div>
<Tag tone="info">Sin tareas</Tag>
</div>
<div className="form-grid">
<Field label="Oportunidad" value={opportunity?.title ?? "Sin oportunidad"} readOnly/>
<TextAreaField label="Contexto inicial" placeholder="Qué cambió después de la visita..." rows={5} value={notes} onChange={(event) => setNotes(event.target.value)}/>
</div>
<Alert tone="info" title="Hecho ocurrido">Abrir la negociación cambia la etapa y agrega un registro histórico; no agenda recordatorios.</Alert>
<div className="form-footer">
<Button variant="ghost" onClick={() => onNavigate("COM-05")}>Cancelar</Button>
<Button onClick={save}>Abrir negociación</Button>
</div>
</Card>;
}
function ProposalForm({ state, entityId, onToast, onNavigate, dispatch }: {
    state: DemoState;
    entityId?: string | null;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    onNavigate: ScreenNavigator;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const opportunity = entityId === undefined || entityId === null ? state.opportunities[0] : selectedRecord(state.opportunities, entityId);
    const relatedProposals = opportunity ? state.proposals.filter((proposal) => proposal.opportunityId === opportunity.id).sort((left, right) => left.sequence - right.sequence) : [];
    const sourceDemand = opportunity?.sourceType === "REQUIREMENT" ? state.demands.find((demand) => demand.id === opportunity.sourceId) : undefined;
    const sourceCaptation = opportunity?.sourceType === "CAPTATION_CASE" ? state.captations.find((captation) => captation.id === opportunity.sourceId) : undefined;
    const defaultParticipant = sourceDemand ? state.contacts.find((contact) => contact.id === sourceDemand.contactId)?.name : sourceCaptation?.ownerPartyId ? state.contacts.find((contact) => contact.id === sourceCaptation.ownerPartyId)?.name : undefined;
    const participants = Array.from(new Set([...state.users.map((user) => user.name), ...state.contacts.map((contact) => contact.name)]));
    const [kind, setKind] = useState<"PROPUESTA" | "CONTRAPROPUESTA">("PROPUESTA");
    const [proposedBy, setProposedBy] = useState("Martín Quiroga");
    const [proposedTo, setProposedTo] = useState(defaultParticipant ?? "");
    const [amount, setAmount] = useState("");
    const [currency, setCurrency] = useState<"USD" | "ARS">("USD");
    const [validity, setValidity] = useState("");
    const [notes, setNotes] = useState("");
    if (!opportunity) return <UnavailableRecord />;
    const save = () => {
        const parsedAmount = amount.trim() ? parseOptionalMoney(amount) : null;
        if (parsedAmount === undefined) {
            onToast("Cargá un monto válido y no negativo, o dejalo desconocido.", "warning");
            return;
        }
        const sequence = relatedProposals.length + 1;
        const item = { id: `proposal-${Date.now()}`, opportunityId: opportunity.id, sequence, kind, proposedBy, proposedTo: proposedTo.trim() || undefined, amount: parsedAmount, currency, validity: validity || undefined, conditions: notes.trim(), createdAt: new Date().toISOString(), outcome: "pending" as const, reason: "" };
        dispatch({ type: "proposal/submit", item });
        dispatch({ type: "activity/add", item: { id: `activity-${Date.now()}`, type: "Envío de propuesta", subject: `${kind === "CONTRAPROPUESTA" ? "Contrapropuesta" : "Propuesta"} #${sequence} registrada`, body: `${parsedAmount === null ? "Monto UNKNOWN" : `${parsedAmount} ${currency}`}${notes.trim() ? ` · ${notes.trim()}` : ""}`, actor: proposedBy, recordedByUserId: state.users.find((user) => user.name === proposedBy)?.id ?? "user-1", occurredAt: new Date().toISOString(), relatedRecordId: opportunity.id, relatedRecordType: "Opportunity", pipelineItemId: opportunity.id, createdAt: "Ahora" } });
        onToast(`${kind === "CONTRAPROPUESTA" ? "Contrapropuesta" : "Propuesta"} registrada como secuencia ${sequence}.`);
        onNavigate("COM-05", opportunity.id);
    };
    return <Card className="form-card">
<div className="form-card-heading">
<div>
<span className="eyebrow">Negociación · {opportunity.title}</span>
<h2>{kind === "CONTRAPROPUESTA" ? "Registrar contrapropuesta" : "Registrar propuesta"}</h2>
<p>Las condiciones quedan congeladas como un hecho ocurrido; una nueva respuesta crea otra secuencia.</p>
</div>
<Tag tone="info">Secuencia {relatedProposals.length + 1}</Tag>
</div>
<div className="form-grid">
<SelectField label="Tipo de propuesta" value={kind} onChange={(event) => setKind(event.target.value as typeof kind)}>
<option value="PROPUESTA">Propuesta</option>
<option value="CONTRAPROPUESTA">Contrapropuesta</option>
</SelectField>
<SelectField label="Propuesto por" value={proposedBy} onChange={(event) => setProposedBy(event.target.value)}>{participants.map((participant) => <option key={participant}>{participant}</option>)}</SelectField>
<SelectField label="Propuesto a" value={proposedTo} onChange={(event) => setProposedTo(event.target.value)}><option value="">Sin participante informado</option>{participants.map((participant) => <option key={participant}>{participant}</option>)}</SelectField>
<Field label="Monto propuesto" placeholder="235.000" value={amount} onChange={(event) => setAmount(event.target.value)} autoFocus/>
<SelectField label="Moneda" value={currency} onChange={(event) => setCurrency(event.target.value as typeof currency)}>
<option>USD</option>
<option>ARS</option>
</SelectField>
<Field label="Vigencia" type="date" value={validity} onChange={(event) => setValidity(event.target.value)}/>
<TextAreaField label="Condiciones" placeholder="Condiciones visibles de la propuesta..." rows={4} value={notes} onChange={(event) => setNotes(event.target.value)}/>
</div>
{relatedProposals.length > 0 && <Card><SectionHeading eyebrow="Historial inmutable" title="Propuestas emitidas"/>{relatedProposals.map((proposal) => <div className="activity-row" key={proposal.id}><span className="activity-icon"><Icon name="lock" size={16}/></span><div><strong>{proposal.kind === "CONTRAPROPUESTA" ? "Contrapropuesta" : "Propuesta"} #{proposal.sequence} · {proposal.amount === null ? "Monto UNKNOWN" : `${proposal.amount} ${proposal.currency}`}</strong><p>{proposal.conditions || "Sin condiciones adicionales."}</p><small>{proposal.proposedBy} · {proposal.createdAt} · Inmutable</small></div></div>)}</Card>}
<Alert tone="info" title="Sin cobro">El CRM registra la propuesta, la contrapropuesta y su resultado; no procesa pagos ni emite recibos.</Alert>
<div className="form-footer">
<Button variant="ghost" onClick={() => onNavigate("COM-05", opportunity.id)}>Cancelar</Button>
<Button onClick={save}>{kind === "CONTRAPROPUESTA" ? "Registrar contrapropuesta" : "Registrar propuesta"}</Button>
</div>
</Card>;
}
function ProposalResolutionForm({ proposal, legacyProposal, entityId, onToast, onNavigate, dispatch }: {
    proposal?: DemoState["proposals"][number];
    legacyProposal: DemoState["proposal"];
    entityId?: string;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    onNavigate: ScreenNavigator;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const currentOutcome = proposal?.outcome ?? legacyProposal.outcome;
    const [outcome, setOutcome] = useState<"accepted" | "rejected">(currentOutcome === "rejected" ? "rejected" : "accepted");
    const [reason, setReason] = useState(proposal?.reason ?? legacyProposal.reason);
    const save = () => {
        const validation = validateReason(reason, "resolver la propuesta");
        if (!validation.valid) {
            onToast(validation.error, "warning");
            return;
        }
        dispatch({ type: "proposal/resolve", proposalId: proposal?.id, outcome, reason: reason.trim() });
        onToast(outcome === "accepted" ? "Propuesta aceptada." : "Propuesta rechazada.", outcome === "accepted" ? "success" : "warning");
        onNavigate("COM-05", entityId);
    };
    if (!proposal && currentOutcome === "pending") return <Card className="form-card"><Alert tone="info" title="Todavía no hay una propuesta">Registrá una propuesta o contrapropuesta antes de resolverla.</Alert><div className="form-footer"><Button variant="ghost" onClick={() => onNavigate("COM-05", entityId)}>Volver</Button><Button onClick={() => onNavigate("COM-06", entityId)}>Registrar propuesta</Button></div></Card>;
    if (currentOutcome !== "pending") return <Card className="form-card"><Alert tone="info" title="Propuesta ya resuelta">La propuesta emitida es inmutable y su resultado ya quedó registrado.</Alert><div className="form-footer"><Button onClick={() => onNavigate("COM-05", entityId)}>Volver al timeline</Button></div></Card>;
    return <Card className="form-card">
<div className="form-card-heading">
<div>
<span className="eyebrow">Confirmación · {proposal?.kind === "CONTRAPROPUESTA" ? "Contrapropuesta" : "Propuesta"} #{proposal?.sequence ?? ""}</span>
<h2>Resolver propuesta</h2>
<p>Elegí un resultado y dejá el motivo para que el historial sea entendible.</p>
</div>
<Tag tone="warning">Requiere confirmación</Tag>
</div>
<div className="activity-type-grid">
<button type="button" className={outcome === "accepted" ? "activity-type is-active" : "activity-type"} onClick={() => setOutcome("accepted")}>
<Icon name="check" size={19}/>
<span>Aceptada</span>
</button>
<button type="button" className={outcome === "rejected" ? "activity-type is-active" : "activity-type"} onClick={() => setOutcome("rejected")}>
<Icon name="close" size={19}/>
<span>Rechazada</span>
</button>
</div>
<TextAreaField label="Motivo" placeholder="Qué se confirmó o por qué se rechazó..." rows={4} value={reason} onChange={(event) => setReason(event.target.value)}/>
<div className="form-footer">
<Button variant="ghost" onClick={() => onNavigate("COM-05", entityId)}>Cancelar</Button>
<Button onClick={save}>Guardar resolución</Button>
</div>
</Card>;
}
function ReservationCancelForm({ state, entityId, onToast, onNavigate, dispatch }: {
    state: DemoState;
    entityId?: string | null;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    onNavigate: ScreenNavigator;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const [reason, setReason] = useState("");
    const reservation = entityId === undefined || entityId === null ? state.reservations[0] : selectedRecord(state.reservations, entityId);
    if (entityId !== undefined && entityId !== null && !reservation) return <UnavailableRecord />;
    const save = () => { if (!reservation) {
        onToast("No hay una reserva para cancelar.", "warning");
        return;
    } const validation = validateReason(reason, "cancelar la reserva"); if (!validation.valid) {
        onToast(validation.error, "warning");
        return;
    } dispatch({ type: "reservation/cancel", id: reservation.id, reason: reason.trim() }); dispatch({ type: "activity/add", item: { id: `activity-${Date.now()}`, type: "Nota", subject: "Reserva cancelada", body: reason.trim(), actor: "Martín Quiroga", recordedByUserId: "user-1", occurredAt: new Date().toISOString(), relatedRecordId: reservation.id, relatedRecordType: "Reservation", createdAt: "Ahora" } }); onToast("Reserva cancelada; el historial se conserva.", "warning"); onNavigate("COM-09", reservation.id); };
    return <Card className="form-card">
<div className="form-card-heading">
<div>
<span className="eyebrow">Reserva</span>
<h2>Cancelar reserva</h2>
<p>La cancelación cambia el estado y conserva la información histórica.</p>
</div>
<Tag tone="warning">Acción reversible solo por nuevo hecho</Tag>
</div>
<div className="close-summary">
<span className="commercial-icon">
<Icon name="alert" size={27}/>
</span>
<div>
<strong>{reservation?.propertyTitle ?? "Sin reserva activa"}</strong>
<span>Estado actual: {reservation?.status ?? "Desconocida"}</span>
</div>
</div>
<TextAreaField label="Motivo de cancelación" placeholder="Motivo visible para el equipo..." rows={4} value={reason} onChange={(event) => setReason(event.target.value)}/>
<div className="form-footer">
<Button variant="ghost" onClick={() => onNavigate("COM-09", reservation?.id)}>Volver</Button>
<Button variant="tertiary" onClick={save}>Cancelar reserva</Button>
</div>
</Card>;
}
function OperationCancelForm({ state, entityId, onToast, onNavigate, dispatch }: {
    state: DemoState;
    entityId?: string | null;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    onNavigate: ScreenNavigator;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const [reason, setReason] = useState("");
    const operation = entityId === undefined || entityId === null ? state.operations[0] : selectedRecord(state.operations, entityId);
    if (entityId !== undefined && entityId !== null && !operation) return <UnavailableRecord />;
    const save = () => { if (!operation) {
        onToast("No hay una operación para cancelar.", "warning");
        return;
    } const validation = validateReason(reason, "cancelar la operación"); if (!validation.valid) {
        onToast(validation.error, "warning");
        return;
    } dispatch({ type: "operation/cancel", id: operation.id, reason: reason.trim() }); dispatch({ type: "activity/add", item: { id: `activity-${Date.now()}`, type: "Nota", subject: "Operación cancelada", body: reason.trim(), actor: "Martín Quiroga", recordedByUserId: "user-1", occurredAt: new Date().toISOString(), relatedRecordId: operation.id, relatedRecordType: "Operation", createdAt: "Ahora" } }); onToast("Operación cancelada; el historial se conserva.", "warning"); onNavigate("COM-12", operation.id); };
    return <Card className="form-card">
<div className="form-card-heading">
<div>
<span className="eyebrow">Operación</span>
<h2>Cancelar operación</h2>
<p>La operación queda cancelada sin borrar documentación ni hechos previos.</p>
</div>
<Tag tone="warning">Requiere motivo</Tag>
</div>
<div className="close-summary">
<span className="commercial-icon">
<Icon name="alert" size={27}/>
</span>
<div>
<strong>{operation?.propertyTitle ?? "Sin operación"}</strong>
<span>Etapa actual: {operation?.stage ?? "Desconocida"}</span>
</div>
</div>
<TextAreaField label="Motivo de cancelación" placeholder="Motivo visible para el equipo..." rows={4} value={reason} onChange={(event) => setReason(event.target.value)}/>
<div className="form-footer">
<Button variant="ghost" onClick={() => onNavigate("COM-12", operation?.id)}>Volver</Button>
<Button variant="tertiary" onClick={save}>Cancelar operación</Button>
</div>
</Card>;
}
function VisitForm({ state, onNavigate, onToast, dispatch }: {
    state: DemoState;
    onNavigate: ScreenNavigator;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const [note, setNote] = useState("");
    const [photoNames, setPhotoNames] = useState<string[]>([]);
    const [propertyId, setPropertyId] = useState(state.properties[0]?.id ?? "");
    const [listingId, setListingId] = useState(state.listings.find((listing) => listing.propertyId === state.properties[0]?.id)?.id ?? "");
    const [contactId, setContactId] = useState(state.contacts[0]?.id ?? "");
    const [occurredAt, setOccurredAt] = useState("");
    const saveVisit = () => {
        const selectedProperty = state.properties.find((property) => property.id === propertyId);
        const attachments = photoNames.length ? `${photoNames.length} foto${photoNames.length > 1 ? "s" : ""}` : "";
        const validation = validateActivity({ activityType: "Visita", occurredAt, actor: "Martín Quiroga", relatedRecordId: contactId });
        if (!validation.valid || !selectedProperty) {
            onToast(validation.valid ? "Elegí un inmueble para registrar la visita." : validation.error, "warning");
            return;
        }
        const activity: DemoActivity = { id: `activity-${Date.now()}`, type: "Visita", subject: "Visita registrada", body: `${note.trim() || `${selectedProperty.title} · hecho ocurrido`}${attachments ? ` · Adjuntos: ${attachments}` : ""}`, actor: "Martín Quiroga", recordedByUserId: "user-1", occurredAt, relatedRecordId: contactId, relatedRecordType: "Party", propertyId, listingId: listingId || undefined, createdAt: "Ahora" };
        dispatch({ type: "activity/add", item: activity });
        if (typeof navigator !== "undefined" && (!navigator.onLine || photoNames.length > 0)) {
            dispatch({ type: "offline/enqueue", item: { id: `offline-${Date.now()}`, label: `Visita guardada${attachments ? ` con ${attachments}` : ""}`, createdAt: new Date().toISOString(), kind: photoNames.length ? "photo" : "visit" } });
            onToast(navigator.onLine ? "Visita registrada; sus adjuntos quedan en la cola." : "Visita guardada sin conexión; queda en cola.", "warning");
        }
        else
            onToast("Visita registrada como hecho ocurrido.");
        onNavigate("ACT-05");
    };
    return <Card className="form-card mobile-action-card">
<div className="form-card-heading">
<div>
<span className="eyebrow">En la calle</span>
<h2>Registrar visita ocurrida</h2>
<p>No se agenda ni se consulta disponibilidad: registrá lo que efectivamente pasó.</p>
</div>
<span className="mobile-action-icon">
<Icon name="calendar" size={27}/>
</span>
</div>
<div className="form-grid">
<SelectField label="Inmueble" value={propertyId} onChange={(event) => { const nextPropertyId = event.target.value; setPropertyId(nextPropertyId); setListingId(state.listings.find((listing) => listing.propertyId === nextPropertyId)?.id ?? ""); }}>
{state.properties.map((property) => <option key={property.id} value={property.id}>{property.title} · {property.address}</option>)}
</SelectField>
<SelectField label="Publicación relacionada" value={listingId} onChange={(event) => setListingId(event.target.value)}>
<option value="">Sin publicación</option>
{state.listings.filter((listing) => listing.propertyId === propertyId).map((listing) => <option key={listing.id} value={listing.id}>{listing.title}</option>)}
</SelectField>
<SelectField label="Contacto" value={contactId} onChange={(event) => setContactId(event.target.value)}>
{state.contacts.map((contact) => <option key={contact.id} value={contact.id}>{contact.name}</option>)}
</SelectField>
<Field label="Cuándo ocurrió" type="datetime-local" value={occurredAt} onChange={(event) => setOccurredAt(event.target.value)}/>
<TextAreaField label="Nota de visita" placeholder="Qué observó la persona, preferencias y próximos datos..." rows={5} value={note} onChange={(event) => setNote(event.target.value)}/>
</div>
<div className="visit-capture-row">
<label className="visit-capture-button">
<Icon name="camera" size={21}/>
<span>Agregar fotos</span>
<small>{photoNames.length ? `${photoNames.length}/6 seleccionadas` : "Máximo 6"}</small>
<input className="visually-hidden" type="file" accept="image/*" multiple onChange={(event) => setPhotoNames(Array.from(event.target.files ?? []).slice(0, 6).map((file) => file.name))}/>
</label>
</div>{photoNames.length > 0 && <div className="attachment-list" aria-label="Fotos seleccionadas">{photoNames.map((name) => <Chip key={name} tone="info">{name}</Chip>)}</div>}<div className="form-footer">
<Button variant="ghost" onClick={() => onNavigate("ACT-05")}>Cancelar</Button>
<Button icon="check" onClick={saveVisit}>Guardar visita</Button>
</div>
</Card>;
}
function ReservationForm({ state, entityId, onToast, onNavigate, dispatch }: {
    state: DemoState;
    entityId?: string | null;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    onNavigate: ScreenNavigator;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const opportunity = entityId === undefined || entityId === null ? state.opportunities[0] : selectedRecord(state.opportunities, entityId);
    const sourceDemand = opportunity?.sourceType === "REQUIREMENT" ? state.demands.find((demand) => demand.id === opportunity.sourceId) : undefined;
    const sourceListingId = sourceDemand?.selectedListingIds?.[0];
    const sourcePropertyId = sourceListingId ? state.listings.find((listing) => listing.id === sourceListingId)?.propertyId : undefined;
    const defaultPropertyTitle = state.properties.find((property) => property.id === sourcePropertyId)?.title ?? state.properties[0]?.title ?? opportunity?.title ?? "";
    const latestAcceptedProposal = opportunity ? state.proposals.filter((proposal) => proposal.opportunityId === opportunity.id && proposal.outcome === "accepted").at(-1) : undefined;
    const [propertyTitle, setPropertyTitle] = useState(defaultPropertyTitle);
    const [deposit, setDeposit] = useState("1000000");
    const [currency, setCurrency] = useState<"ARS" | "USD">("ARS");
    const [validFrom, setValidFrom] = useState("");
    const [expiresAt, setExpiresAt] = useState("");
    const [conditions, setConditions] = useState("");
    const createReservation = () => {
        const amount = Number(deposit.replace(/\./g, "").replace(",", "."));
        if (!opportunity) {
            onToast("No hay una oportunidad válida para crear la reserva.", "warning");
            return;
        }
        if (!validFrom.trim()) {
            onToast("Indica desde cuándo es válida la reserva.", "warning");
            return;
        }
        if (!Number.isFinite(amount) || amount < 0) {
            onToast("La seña debe ser un monto válido.", "warning");
            return;
        }
        const property = state.properties.find((item) => item.title === propertyTitle);
        const listing = property ? state.listings.find((item) => item.propertyId === property.id && item.status !== "Cerrada") : undefined;
        const id = `reservation-${Date.now()}`;
        dispatch({ type: "reservation/create", item: { id, opportunityId: opportunity.id, propertyTitle, deposit: amount, currency, status: "Activa", listingId: listing?.id, acceptedProposalId: latestAcceptedProposal?.id, validFrom, expiresAt: expiresAt || undefined, conditions: conditions.trim() || undefined } });
        onToast("Reserva creada. La seña queda como dato informativo.");
        onNavigate("COM-09", id);
    };
    return <Card className="form-card">
<div className="form-card-heading">
<div>
<span className="eyebrow">Paso de negociación</span>
<h2>Crear reserva</h2>
<p>La reserva abre el camino a la operación; no implica cobrar ni emitir recibos.</p>
</div>
<Tag tone="brand">Datos informativos</Tag>
</div>
<div className="form-grid">
<SelectField label="Inmueble" value={propertyTitle} onChange={(event) => setPropertyTitle(event.target.value)}>{state.properties.map((property) => <option key={property.id}>{property.title}</option>)}</SelectField>
<Field label="Monto de seña" placeholder="1.000.000" value={deposit} onChange={(event) => setDeposit(event.target.value)}/>
<SelectField label="Moneda" value={currency} onChange={(event) => setCurrency(event.target.value as typeof currency)}>
<option>ARS</option>
<option>USD</option>
</SelectField>
<Field label="Válida desde" type="date" value={validFrom} onChange={(event) => setValidFrom(event.target.value)}/>
<Field label="Vencimiento" type="date" value={expiresAt} onChange={(event) => setExpiresAt(event.target.value)}/>
<TextAreaField label="Condiciones" placeholder="Condiciones informativas de la reserva..." rows={3} value={conditions} onChange={(event) => setConditions(event.target.value)}/>
</div>
<Alert tone="info" title="Sin pagos">La seña se registra con monto y moneda, sin integración de cobro ni recibo.</Alert>
<div className="form-footer">
<Button variant="ghost" onClick={() => onNavigate("OPP-04", opportunity?.id)}>Cancelar</Button>
<Button icon="check" onClick={createReservation}>Guardar reserva</Button>
</div>
<small className="form-footnote">Reservas registradas: {state.reservations.length}</small>
</Card>;
}
function OperationForm({ state, entityId, onToast, onNavigate, dispatch }: {
    state: DemoState;
    entityId?: string | null;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    onNavigate: ScreenNavigator;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const linkedReservation = entityId ? state.reservations.find((item) => item.id === entityId || item.opportunityId === entityId) : undefined;
    const reservation = linkedReservation ?? state.reservations.find((item) => item.status === "Activa") ?? state.reservations[0];
    const operationTypes = Array.from(new Set(["Compraventa", "Permuta", ...state.catalogEntries.filter((entry) => entry.catalogType === "operation-type" && entry.status === "Activo").map((entry) => entry.label)]));
    const [operationType, setOperationType] = useState(operationTypes[0] ?? "Compraventa");
    const [notary, setNotary] = useState("");
    const [notes, setNotes] = useState("");
    const createOperation = () => {
        if (!reservation || reservation.status !== "Activa") {
            onToast("Necesitás una reserva activa para crear la operación.", "warning");
            return;
        }
        const id = `operation-${Date.now()}`;
        const listing = reservation.listingId ? state.listings.find((item) => item.id === reservation.listingId) : undefined;
        const property = listing ? state.properties.find((item) => item.id === listing.propertyId) : state.properties.find((item) => item.title === reservation.propertyTitle);
        dispatch({ type: "operation/create", item: { id, reservationId: reservation.id, propertyTitle: reservation.propertyTitle, propertyId: property?.id, listingId: listing?.id, operationType, agreedTerms: [notary.trim() ? `Escribanía: ${notary.trim()}` : "", notes.trim()].filter(Boolean).join(" · ") || undefined, stage: "Documentación", outcome: "OPEN" } });
        onToast("Operación creada en etapa Documentación.");
        onNavigate("COM-12", id);
    };
    return <Card className="form-card">
<div className="form-card-heading">
<div>
<span className="eyebrow">De reserva a operación</span>
<h2>Crear operación</h2>
<p>La operación organiza documentación y escrituración, sin convertirse en un ERP contable.</p>
</div>
<Tag tone="info">Reserva activa</Tag>
</div>
<div className="form-grid">
<Field label="Reserva" value={reservation?.propertyTitle ?? "Sin reserva"} readOnly/>
<SelectField label="Tipo de operación" value={operationType} onChange={(event) => setOperationType(event.target.value)}>{operationTypes.map((operation) => <option key={operation}>{operation}</option>)}</SelectField>
<Field label="Escribanía" placeholder="Dato opcional" value={notary} onChange={(event) => setNotary(event.target.value)}/>
<TextAreaField label="Notas" placeholder="Información operativa de la operación..." rows={4} value={notes} onChange={(event) => setNotes(event.target.value)}/>
</div>
<div className="stepper">
<span className="stepper-step is-active">
<b>1</b>Documentación</span>
<span className="stepper-line"/>
<span className="stepper-step">
<b>2</b>Escrituración</span>
<span className="stepper-line"/>
<span className="stepper-step">
<b>3</b>Cierre</span>
</div>
<div className="form-footer">
<Button variant="ghost" onClick={() => onNavigate("COM-12")}>Cancelar</Button>
<Button onClick={createOperation}>Crear operación</Button>
</div>
</Card>;
}
function CloseOperation({ state, entityId, onToast, onNavigate, dispatch }: {
    state: DemoState;
    entityId?: string | null;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    onNavigate: ScreenNavigator;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const [closeDate, setCloseDate] = useState("");
    const [finalValue, setFinalValue] = useState("");
    const operation = entityId === undefined || entityId === null ? state.operations[0] : selectedRecord(state.operations, entityId);
    if (!operation) return <UnavailableRecord />;
    const close = () => {
        const value = finalValue.trim() ? Number(finalValue.replace(/\./g, "").replace(",", ".")) : undefined;
        const validation = validateOperationClose(closeDate, value);
        if (!validation.valid) {
            onToast(validation.error, "warning");
            return;
        }
        dispatch({ type: "operation/close", id: operation.id, closeDate, finalValue: value });
        onToast("Operación cerrada.");
        onNavigate("COM-12", operation.id);
    };
    return <Card className="form-card">
<div className="form-card-heading">
<div>
<span className="eyebrow">Cierre</span>
<h2>Cerrar operación</h2>
<p>Confirmá el hecho de cierre. El historial conserva la etapa anterior.</p>
</div>
<Tag tone="warning">Requiere confirmación</Tag>
</div>
<div className="close-summary">
<span className="commercial-icon">
<Icon name="check" size={27}/>
</span>
<div>
<strong>{operation.propertyTitle}</strong>
<span>Etapa actual: {operation.stage}</span>
</div>
</div>
<Alert tone="warning" title="Última revisión">Verificá documentación y escrituración antes de cerrar. Esta acción no cobra ni liquida.</Alert>
<div className="form-grid"><Field label="Fecha de cierre" type="date" value={closeDate} onChange={(event) => setCloseDate(event.target.value)}/><Field label="Valor final" placeholder="235.000" inputMode="decimal" value={finalValue} onChange={(event) => setFinalValue(event.target.value)}/></div>
<div className="form-footer">
<Button variant="ghost" onClick={() => onNavigate("COM-12", operation.id)}>Volver</Button>
<Button onClick={close}>Confirmar cierre</Button>
</div>
</Card>;
}
function ActivityView({ screen, state, entityId, onNavigate, onToast, dispatch }: {
    screen: ScreenDefinition;
    state: DemoState;
    entityId?: string | null;
    onNavigate: ScreenNavigator;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const [activityFilter, setActivityFilter] = useState<"ALL" | "Party" | "Property" | "Opportunity" | "Visita">("ALL");
    const activitySubject = screen.id === "ACT-02"
        ? entityId === undefined || entityId === null ? state.contacts[0] : selectedRecord(state.contacts, entityId)
        : screen.id === "ACT-03"
            ? entityId === undefined || entityId === null ? state.properties[0] : selectedRecord(state.properties, entityId)
            : screen.id === "ACT-04"
                ? entityId === undefined || entityId === null ? state.opportunities[0] : selectedRecord(state.opportunities, entityId)
                : undefined;
    if (entityId !== undefined && entityId !== null && !activitySubject) return <UnavailableRecord />;
    if (screen.id === "ACT-06" && entityId !== undefined && entityId !== null && !state.activities.some((activity) => activity.id === entityId)) return <UnavailableRecord />;
    if (screen.id === "ACT-01" || screen.id === "ACT-06")
        return <ActivityForm screen={screen} state={state} entityId={entityId} onNavigate={onNavigate} onToast={onToast} dispatch={dispatch}/>;
    const scopedActivities = screen.id === "ACT-05"
        ? state.activities
        : activitySubject
            ? screen.id === "ACT-03"
                ? state.activities.filter((activity) => activity.relatedRecordId === activitySubject.id || activity.propertyId === activitySubject.id)
                : screen.id === "ACT-04"
                    ? state.activities.filter((activity) => activity.relatedRecordId === activitySubject.id || activity.pipelineItemId === activitySubject.id)
                    : state.activities.filter((activity) => activity.relatedRecordId === activitySubject.id)
            : [];
    const filteredActivities = scopedActivities.filter((activity) => activityFilter === "ALL"
        || (activityFilter === "Visita" ? activity.type === "Visita" : activity.relatedRecordType === activityFilter));
    const scopedStageHistory = screen.id === "ACT-04" && activitySubject
        ? state.stageHistory.filter((item) => item.opportunityId === activitySubject.id)
        : [];
    const mixedTimeline = [...filteredActivities.map((activity) => ({
        at: Date.parse(activity.occurredAt ?? activity.createdAt) || 0,
        text: `${activity.subject} · ${activity.actor} · ${activity.occurredAt ?? activity.createdAt}${activity.historical ? " · Histórico" : ""}`,
    })), ...scopedStageHistory.map((item) => ({
        at: Date.parse(item.at) || 0,
        text: `${item.from} → ${item.to} · ${item.actor} · ${item.at}${item.reason ? ` · ${item.reason}` : ""}`,
    }))].sort((left, right) => right.at - left.at).map((item) => item.text);
    return <div className="feature-stack">
<Card>
<div className="activity-toolbar">
<div>
<span className="eyebrow">Hechos ocurridos</span>
<h2>{screen.id === "ACT-05" ? "Actividad reciente" : "Timeline de la actividad"}</h2>
<p>Email y WhatsApp se muestran como registros históricos; este producto no envía mensajes.</p>
</div>
<Button icon="plus" onClick={() => onNavigate("ACT-01")}>Registrar actividad</Button>
</div>
{mixedTimeline.length ? <Timeline items={mixedTimeline}/> : <EmptyState icon="activity" title="Sin hechos registrados" description="Los hechos de este registro van a aparecer acá cuando se registren."/>}
</Card>
<Card>
<SectionHeading eyebrow="Trazabilidad" title="Filtrar por objeto" action={<Button variant="outline" size="small" icon="filter" aria-expanded={activityFilter !== "ALL"} onClick={() => setActivityFilter((current) => current === "ALL" ? "Party" : "ALL")}>Filtrar</Button>}/>
<div className="activity-filters">
{(["ALL", "Party", "Property", "Opportunity", "Visita"] as const).map((filter) => <button type="button" className={`activity-filter ${activityFilter === filter ? "is-active" : ""}`} key={filter} onClick={() => setActivityFilter(filter)}>{filter === "ALL" ? "Todas" : filter === "Party" ? "Contacto" : filter === "Property" ? "Inmueble" : filter === "Opportunity" ? "Oportunidad" : "Visita"}</button>)}
</div>
</Card></div>;
}
function ActivityForm({ screen, state, entityId, onNavigate, onToast, dispatch }: {
    screen: ScreenDefinition;
    state: DemoState;
    entityId?: string | null;
    onNavigate: ScreenNavigator;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const existingActivity = screen.id === "ACT-06"
        ? (entityId ? state.activities.find((activity) => activity.id === entityId) : state.activities[0])
        : undefined;
    const [activityType, setActivityType] = useState<DemoActivity["type"]>(existingActivity?.type ?? "Nota");
    const [occurredAt, setOccurredAt] = useState(existingActivity?.occurredAt ?? "");
    const [recordedByUserId, setRecordedByUserId] = useState(existingActivity?.recordedByUserId ?? state.users[0]?.id ?? "");
    const relationOptions = [
        ...state.contacts.map((contact) => ({ key: `Party:${contact.id}`, id: contact.id, type: "Party" as const, label: `Contacto · ${contact.name}` })),
        ...state.properties.map((property) => ({ key: `Property:${property.id}`, id: property.id, type: "Property" as const, label: `Inmueble · ${property.title}` })),
        ...state.listings.map((listing) => ({ key: `Listing:${listing.id}`, id: listing.id, type: "Listing" as const, label: `Publicación · ${listing.title}` })),
        ...state.demands.map((demand) => ({ key: `Demand:${demand.id}`, id: demand.id, type: "Demand" as const, label: `Búsqueda · ${demand.title}` })),
        ...state.opportunities.map((opportunity) => ({ key: `Opportunity:${opportunity.id}`, id: opportunity.id, type: "Opportunity" as const, label: `Oportunidad · ${opportunity.title}` })),
    ];
    const initialRelation = existingActivity?.relatedRecordType && existingActivity.relatedRecordId
        ? `${existingActivity.relatedRecordType}:${existingActivity.relatedRecordId}`
        : entityId && relationOptions.some((option) => option.id === entityId)
            ? relationOptions.find((option) => option.id === entityId)?.key ?? relationOptions[0]?.key ?? ""
            : relationOptions[0]?.key ?? "";
    const [relatedRecordKey, setRelatedRecordKey] = useState(initialRelation);
    const [detail, setDetail] = useState("");
    const activityTypes = Array.from(new Set([
        existingActivity?.type ?? "Nota",
        ...state.catalogEntries.filter((entry) => entry.catalogType === "activity-type" && entry.status === "Activo").map((entry) => entry.label as DemoActivity["type"]),
    ])) as DemoActivity["type"][];
    const save = () => {
        const recorder = state.users.find((user) => user.id === recordedByUserId);
        const relation = relationOptions.find((option) => option.key === relatedRecordKey);
        const validation = validateActivity({ activityType, occurredAt, actor: recorder?.name, relatedRecordId: relation?.id });
        if (!validation.valid) {
            onToast(validation.error, "warning");
            return;
        }
        const body = detail.trim() || (screen.id === "ACT-06" ? "Actividad corregida." : "Actividad guardada desde la captura rápida.");
        if (screen.id === "ACT-06") {
            if (!existingActivity || !relation) {
                onToast("Elegí una actividad y un registro válidos para corregir.", "warning");
                return;
            }
            dispatch({ type: "activity/update", id: existingActivity.id, changes: { type: activityType, body, subject: `${activityType} corregida`, actor: recorder?.name, occurredAt, recordedByUserId, relatedRecordId: relation.id, relatedRecordType: relation.type } });
            onToast("Actividad corregida.");
        }
        else {
            if (!relation) {
                onToast("Elegí un registro relacionado válido.", "warning");
                return;
            }
            dispatch({ type: "activity/add", item: { id: `activity-${Date.now()}`, type: activityType, subject: `${activityType} registrada`, body, actor: recorder?.name ?? "", occurredAt, recordedByUserId, relatedRecordId: relation.id, relatedRecordType: relation.type, createdAt: "Ahora" } });
            onToast("Actividad guardada.");
        }
        onNavigate("ACT-05", relation?.id);
    };
    return <Card className="form-card mobile-action-card">
<div className="form-card-heading">
<div>
<span className="eyebrow">Captura rápida</span>
<h2>{screen.id === "ACT-06" ? "Corregir actividad" : "Registrar actividad"}</h2>
<p>Un hecho corto ahora vale más que una tarea inventada para después.</p>
</div>
<span className="mobile-action-icon">
<Icon name="activity" size={27}/>
</span>
</div>
<div className="activity-type-grid">{activityTypes.map((type) => <button type="button" key={type} className={activityType === type ? "activity-type is-active" : "activity-type"} onClick={() => setActivityType(type)}>
<Icon name={type === "Email" || type === "Email histórico" || type === "Correo electrónico" ? "mail" : type === "Llamada" || type === "WhatsApp" || type === "WhatsApp histórico" || type === "Mensaje" ? "phone" : type === "Visita" ? "calendar" : "activity"} size={19}/>
<span>{type}</span>
</button>)}</div>
<div className="form-grid">
<Field label="Cuándo ocurrió" type="datetime-local" value={occurredAt} onChange={(event) => setOccurredAt(event.target.value)}/>
<SelectField label="Registrado por" value={recordedByUserId} onChange={(event) => setRecordedByUserId(event.target.value)}>{state.users.map((user) => <option key={user.id} value={user.id}>{user.name}</option>)}</SelectField>
<SelectField label="Registro relacionado" value={relatedRecordKey} onChange={(event) => setRelatedRecordKey(event.target.value)}>{relationOptions.map((option) => <option key={option.key} value={option.key}>{option.label}</option>)}</SelectField>
<TextAreaField label="Detalle" placeholder="Qué pasó, qué se detectó, qué cambió..." rows={5} value={detail} onChange={(event) => setDetail(event.target.value)}/>
</div>{["Email", "Email histórico", "WhatsApp", "WhatsApp histórico", "Correo electrónico", "Mensaje"].includes(activityType) && <Alert tone="info" title="Registro histórico">Este formulario no envía mensajes. Solo deja constancia de una comunicación que ocurrió.</Alert>}<div className="form-footer">
<Button variant="ghost" onClick={() => onNavigate("ACT-05")}>Cancelar</Button>
<Button icon="check" onClick={save}>{screen.id === "ACT-06" ? "Guardar corrección" : "Guardar actividad"}</Button>
</div>
</Card>;
}
function AnalyticsView({ screen, state, onNavigate }: {
    screen: ScreenDefinition;
    state: DemoState;
    onNavigate: ScreenNavigator;
}) {
    const [filters, setFilters] = useState<AnalyticsFilters>({ period: "30d", responsible: "ALL", pipelineKind: "ALL", origin: "ALL" });
    const snapshot = useMemo(() => getAnalyticsSnapshot(state, filters), [filters, state]);
    const origins = useMemo(() => Array.from(new Set([...state.demands.map((demand) => demand.origin ?? "Origen desconocido"), ...state.captations.map((captation) => captation.origin ?? "Origen desconocido")])).sort(), [state.captations, state.demands]);
    const isDirector = screen.id === "ANA-03";
    const isFunnel = screen.id === "ANA-04";
    const isSupply = screen.id === "ANA-05";
    const isExport = screen.id === "ANA-07";
    if (isExport) return <Card><Alert tone="info" title="Exportación no disponible en V2">La documentación no define un contrato de exportación para este panel. El CRM conserva el análisis navegable y no ofrece una acción que prometa un archivo inexistente.</Alert><div className="form-footer"><Button variant="outline" onClick={() => onNavigate("ANA-06")}>Ver definición y lineage</Button></div></Card>;
    return <div className="feature-stack">
<div className="kpi-grid analytics-kpis">
<Kpi title="Oportunidades abiertas" value={String(snapshot.openOpportunities)} detail={`${snapshot.opportunities.length} oportunidades en el alcance`} tone="blue"/>
<Kpi title="Conversión visita → reserva" value={formatAnalyticsMetric(snapshot.conversionRate, "%")} detail="Reservas / visitas registradas" tone="orange"/>
<Kpi title="Tiempo medio de etapa" value={formatAnalyticsMetric(snapshot.averageStageDays, " días")} detail="Promedio del alcance seleccionado" tone="green"/>
<Kpi title="Honorarios estimados" value={formatAnalyticsMoney(snapshot.estimatedFees)} detail="Suma cruda · no ponderada" tone="purple"/>
</div>
<Card>
<div className="analytics-header">
<div>
<span className="eyebrow">{isDirector ? "Dirección" : screen.id === "ANA-02" ? "Responsable comercial" : "Mi rendimiento"}</span>
<h2>{isFunnel ? "Embudo comercial" : isSupply ? "Oferta y demanda" : "Panel de desempeño"}</h2>
<p>La métrica se puede abrir hasta las oportunidades concretas que la componen.</p>
</div>
<div className="analytics-actions">
<SelectField label="Período" value={filters.period} onChange={(event) => setFilters((current) => ({ ...current, period: event.target.value as AnalyticsFilters["period"] }))}><option value="30d">Últimos 30 días</option><option value="all">Todo el historial</option></SelectField>
<SelectField label="Responsable" value={filters.responsible} onChange={(event) => setFilters((current) => ({ ...current, responsible: event.target.value }))}><option value="ALL">Todos los responsables</option>{state.users.map((user) => <option value={user.name} key={user.id}>{user.name}</option>)}</SelectField>
<SelectField label="Tipo de pipeline" value={filters.pipelineKind} onChange={(event) => setFilters((current) => ({ ...current, pipelineKind: event.target.value as AnalyticsFilters["pipelineKind"] }))}><option value="ALL">Todos los pipelines</option><option value="REQUIREMENT">Búsquedas</option><option value="CAPTATION_CASE">Captaciones</option></SelectField>
<SelectField label="Origen" value={filters.origin} onChange={(event) => setFilters((current) => ({ ...current, origin: event.target.value }))}><option value="ALL">Todos los orígenes</option>{origins.map((origin) => <option value={origin} key={origin}>{origin}</option>)}</SelectField>
</div>
</div>{!snapshot.opportunities.length && <Alert tone="info" title="Sin datos en este alcance">No hay oportunidades que cumplan los filtros. Las métricas derivadas se muestran como UNKNOWN cuando no hay observaciones suficientes.</Alert>}{isFunnel ? <FunnelChart opportunities={snapshot.opportunities} onNavigate={onNavigate}/> : isSupply ? <SupplyDemand snapshot={snapshot} state={state}/> : <PerformanceGrid opportunities={snapshot.opportunities} onNavigate={onNavigate}/>}</Card>
<div className="content-grid">
<Card>
<SectionHeading eyebrow="Definición" title="¿Qué significa cada número?" action={<Button variant="ghost" size="small" onClick={() => onNavigate("ANA-06")}>Ver origen de los datos <Icon name="arrow" size={14}/>
</Button>}/>
<div className="metric-definitions">
<Definition label="Oportunidad abierta" value="Oportunidad en seguimiento que no está cerrada"/>
<Definition label="Conversión" value="Reservas / visitas registradas"/>
<Definition label="Honorarios" value="Suma estimada sin ponderar por etapa"/>
<Definition label="Versión" value="V2 · cálculo local de revisión"/>
<Definition label="Lineage" value="Opportunity, Activity, Reservation y origen del registro"/>
</div>
</Card>
<Card>
<SectionHeading eyebrow="Calidad del dato" title="Estado de actualización"/>
<div className="refresh-state">
<Icon name="refresh" size={19}/>
<div>
<strong>Recalculando algunas métricas</strong>
<span>Último corte disponible hace 2 horas. No se oculta el dato anterior.</span>
</div>
<Chip tone="warning">Recalculando</Chip>
</div>
</Card>
</div>
</div>;
}
function formatAnalyticsMetric(value: AnalyticsMetricValue, suffix = ""): string {
    return value === "UNKNOWN" ? "UNKNOWN" : `${value}${suffix}`;
}
function formatAnalyticsMoney(value: AnalyticsMetricValue): string {
    return value === "UNKNOWN" ? "UNKNOWN" : new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS", maximumFractionDigits: 0 }).format(value);
}
function PerformanceGrid({ opportunities, onNavigate }: {
    opportunities: DemoState["opportunities"];
    onNavigate: ScreenNavigator;
}) {
    const people = ["Martín Quiroga", "Lucía Ferrari", "Rodrigo Vergara"];
    return <div className="performance-grid">{people.map((person) => { const personOpportunities = opportunities.filter((item) => item.owner === person); const first = personOpportunities[0]; const ratio = opportunities.length ? Math.round((personOpportunities.length / opportunities.length) * 100) : 0; return <button className="performance-row" key={person} onClick={() => onNavigate("OPP-02", first?.id)} disabled={!first}>
<Avatar name={person} size="small"/>
<span>
<strong>{person}</strong>
<small>{person === "Rodrigo Vergara" ? "Responsable · equipo completo" : "Vendedor"}</small>
</span>
<span className="performance-bar">
<i style={{ width: `${ratio}%` }}/>
</span>
<strong>{ratio}%</strong>
<span>{personOpportunities.length} oportunidades</span>
<Icon name="arrow" size={16}/>
</button>; })}</div>;
}
function FunnelChart({ opportunities, onNavigate }: {
    opportunities: DemoState["opportunities"];
    onNavigate: ScreenNavigator;
}) {
    const max = Math.max(1, ...stageOrder.map((stage) => opportunities.filter((item) => item.stage === stage).length));
    return <div className="funnel-chart">{stageOrder.map((stage) => { const matching = opportunities.filter((item) => item.stage === stage); const count = matching.length; return <button className="funnel-chart-row" key={stage} onClick={() => onNavigate("OPP-02", matching[0]?.id)} disabled={!matching.length}>
<span>{stage}</span>
<span className="funnel-track">
<i style={{ width: `${Math.max(8, Math.round((count / max) * 100))}%` }}/>
</span>
<strong>{count}</strong>
<small>ver oportunidades <Icon name="arrow" size={13}/>
</small>
</button>; })}</div>;
}
function SupplyDemand({ snapshot, state }: { snapshot: ReturnType<typeof getAnalyticsSnapshot>; state: DemoState }) {
    const activeProperties = state.properties.filter((property) => property.status !== "Archivado").length;
    const activeDemandRatio = snapshot.activeDemands + snapshot.activeListings ? Math.round((snapshot.activeDemands / (snapshot.activeDemands + snapshot.activeListings)) * 100) : 0;
    return <div className="supply-demand">
<div className="supply-card">
<span>Oferta activa</span>
<strong>{snapshot.activeListings}</strong>
<div className="supply-bar supply-blue">
<i style={{ width: `${Math.max(8, activeProperties ? Math.round((snapshot.activeListings / activeProperties) * 100) : 0)}%` }}/>
</div>
<small>{activeProperties} inmuebles no archivados</small>
</div>
<div className="supply-card">
<span>Demanda activa</span>
<strong>{snapshot.activeDemands}</strong>
<div className="supply-bar supply-orange">
<i style={{ width: `${Math.max(8, activeDemandRatio)}%` }}/>
</div>
<small>{snapshot.activeMatches} compatibilidades registradas</small>
</div>
<div className="supply-insight">
<Icon name="target" size={20}/>
<div>
<strong>{snapshot.activeDemands ? "La brecha se puede abrir desde las búsquedas" : "No hay demanda activa"}</strong>
<p>{snapshot.activeDemands ? `${snapshot.activeDemands} búsquedas activas y ${snapshot.activeListings} publicaciones activas en el alcance.` : "No se inventan zonas ni criterios cuando el dato no está disponible."}</p>
</div>
</div>
</div>;
}
function AssistantView({ screen, state, onNavigate, onToast, dispatch }: {
    screen: ScreenDefinition;
    state: DemoState;
    onNavigate: ScreenNavigator;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const pendingSuggestions = state.aiSuggestions.filter((suggestion) => suggestion.status === "pending");
    const reviewedSuggestions = state.aiSuggestions.filter((suggestion) => suggestion.status !== "pending");
    return <div className="feature-stack">
<Card className="assistant-hero">
<div className="assistant-hero-icon">
<Icon name="spark" size={27}/>
</div>
<div>
<span className="eyebrow">Asistente contextual</span>
<h2>Sugerencias que se pueden revisar</h2>
<p>Cada propuesta muestra su evidencia y la acción antes de tocar el dato.</p>
</div>
<Chip tone="brand">Sin automatismos</Chip>
</Card>
<div className="content-grid">
{pendingSuggestions.map((suggestion) => <AISuggestion key={suggestion.id} title={suggestion.title} body={suggestion.body} evidence={suggestion.evidence} onReview={() => { dispatch({ type: "ai/review", id: suggestion.id }); onToast("Sugerencia revisada; ningún dato se modificó automáticamente.", "info"); onNavigate(suggestion.id === "ai-match" ? "MAT-02" : "IA-02", suggestion.id === "ai-match" ? state.demands[0]?.id : undefined); }} onDismiss={() => { dispatch({ type: "ai/dismiss", id: suggestion.id }); onToast("Sugerencia descartada; no se modificó ningún dato.", "info"); }}/>) }
{pendingSuggestions.length === 0 && <Card><EmptyState icon="check" title="No hay sugerencias pendientes" description="Las revisiones y descartes quedan en el historial del asistente."/></Card>}
</div>
<Card>
<SectionHeading eyebrow="Evidencia" title={screen.id === "IA-04" ? "Historial del asistente" : "Acciones pendientes de revisión"} action={<Button variant="ghost" size="small" onClick={() => onNavigate("IA-04")}>Ver historial <Icon name="arrow" size={14}/>
</Button>}/>
<div className="assistant-history">{reviewedSuggestions.length ? reviewedSuggestions.map((suggestion) => <div className="assistant-history-row" key={suggestion.id}>
<span className="ai-spark">
<Icon name="spark" size={15}/>
</span>
<div>
<strong>{suggestion.title}</strong>
<small>{suggestion.evidence} · {suggestion.updatedAt ?? "Historial"}</small>
</div>
<Tag tone={suggestion.status === "dismissed" ? "warning" : "success"}>{suggestion.status === "dismissed" ? "Descartada" : "Revisada"}</Tag>
</div>) : <span className="muted">Todavía no hay revisiones registradas.</span>}</div>
</Card>{screen.id === "IA-05" && <Alert tone="warning" title="Información faltante">La sugerencia no bloquea el flujo. Podés guardar el dato como desconocido y seguir trabajando.</Alert>}</div>;
}
function AdminView({ screen, state, roleId, onRoleChange, onNavigate, onToast, dispatch }: {
    screen: ScreenDefinition;
    state: DemoState;
    roleId: RoleId;
    onRoleChange: (role: RoleId) => void;
    onNavigate: ScreenNavigator;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    dispatch: React.Dispatch<DemoAction>;
}) {
    if (screen.id === "ADM-02")
        return <InviteUserForm onToast={onToast} onNavigate={onNavigate} dispatch={dispatch}/>;
    const isRoles = screen.id === "ADM-03" || screen.id === "ADM-04";
    const isCatalog = Number(screen.id.slice(4)) >= 6 && Number(screen.id.slice(4)) <= 12;
    if (screen.id === "ADM-05") return <CatalogIndex onNavigate={onNavigate}/>;
    if (screen.id === "ADM-12") return <CatalogVersionPanel state={state} onNavigate={onNavigate}/>;
    return <div className="feature-stack">
<Card>
<div className="admin-header">
<div>
<span className="eyebrow">Administración</span>
<h2>{isRoles ? "Usuarios, roles y permisos" : isCatalog ? "Catálogos comerciales" : "Usuarios"}</h2>
<p>La configuración se versiona y se aplica a esta instalación.</p>
</div>
<Button icon="plus" onClick={() => onNavigate("ADM-02")}>Invitar usuario</Button>
</div>{isRoles ? <RoleMatrix roleId={roleId} onRoleChange={onRoleChange}/> : isCatalog ? <CatalogList screen={screen} state={state} onToast={onToast} onNavigate={onNavigate} dispatch={dispatch}/> : <UserTable state={state} dispatch={dispatch} onToast={onToast}/>}</Card>{screen.id === "ADM-13" && <Card>
<SectionHeading eyebrow="Datos de la inmobiliaria" title="Inmobiliaria Norte"/>
<div className="definition-grid">
<Definition label="Nombre comercial" value="Inmobiliaria Norte"/>
<Definition label="Email operativo" value="operaciones@inmobiliaria.com.ar"/>
<Definition label="Versión de catálogos" value={`v${state.catalogVersion}`}/>
<Definition label="Estado" value={<StatusDot label="Operativa"/>}/>
</div>
</Card>}</div>;
}
function UserTable({ state, dispatch, onToast }: {
    state: DemoState;
    dispatch: React.Dispatch<DemoAction>;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
}) {
    return <div className="table-wrap">
<table className="data-table">
<thead>
<tr>
<th>Usuario</th>
<th>Email</th>
<th>Rol</th>
<th>Estado</th>
<th />
</tr>
</thead>
<tbody>{state.users.map((user) => <tr key={user.id}>
<td>
<div className="table-person">
<Avatar name={user.name} size="small"/>
<strong>{user.name}</strong>
</div>
</td>
<td>{user.email}</td>
<td>
<Chip tone="info">{user.role}</Chip>
</td>
<td>
<StatusDot label={user.status} tone={user.status === "Habilitado" ? "success" : "warning"}/>
</td>
<td>
<Button variant="ghost" size="xsmall" icon="more" aria-label={`Cambiar estado de ${user.name}`} onClick={() => { const next = user.status === "Habilitado" ? "Pendiente" : "Habilitado"; dispatch({ type: "user/update", id: user.id, changes: { status: next } }); onToast(next === "Habilitado" ? `${user.name} habilitado.` : `${user.name} pasó a pendiente.`, "info"); }}/>
</td>
</tr>)}</tbody>
</table>
</div>;
}
function RoleMatrix({ roleId, onRoleChange }: {
    roleId: RoleId;
    onRoleChange: (role: RoleId) => void;
}) {
    return <div className="role-matrix-wrap">
<div className="role-matrix-head">
<div>
<h3>Permisos efectivos</h3>
<p>Seleccioná un rol para ver el alcance, no solo la casilla de una matriz.</p>
</div>
<div className="role-pill-row">{(["vendedor", "responsable", "direccion", "administradora"] as RoleId[]).map((id) => <button type="button" className={roleId === id ? "role-pill is-active" : "role-pill"} key={id} onClick={() => onRoleChange(id)}>{getRole(id).name}</button>)}</div>
</div>
<div className="table-wrap">
<table className="data-table permission-matrix">
<thead>
<tr>
<th>Capacidad</th>
<th>Estado</th>
<th>Por qué</th>
</tr>
</thead>
<tbody>{(["party.read", "party.write", "property.write", "commercial.write", "analytics.read", "admin.write"] as Permission[]).map((permission) => <tr key={permission}>
<td>
<strong>{permissionLabel(permission)}</strong>
</td>
<td>{hasPermission(roleId, permission) ? <Chip tone="success">Permitido</Chip> : <Chip tone="neutral">No habilitado</Chip>}</td>
<td>{permissionReason(roleId, permission)}</td>
</tr>)}</tbody>
</table>
</div>
</div>;
}
function CatalogIndex({ onNavigate }: { onNavigate: ScreenNavigator }) {
    return <div className="feature-stack"><Card><div className="section-heading"><div><span className="overline">Administración</span><h2>Catálogos comerciales</h2><p>Seis familias independientes, versionadas y editables dentro de la revisión local.</p></div></div><Alert tone="info" title="Revisión local">Estos cambios quedan en la instalación de revisión. La publicación productiva requiere el servicio de configuración.</Alert><div className="content-grid">{crmCatalogFamilies.map((family) => <button type="button" className="quick-action-card" key={family.id} onClick={() => onNavigate(family.screenId)}><Icon name="database" size={20}/><span><strong>{family.label}</strong><small>{family.description}</small></span><Icon name="arrow" size={15}/></button>)}</div></Card></div>;
}

function CatalogVersionPanel({ state, onNavigate }: { state: DemoState; onNavigate: ScreenNavigator }) {
    const active = state.catalogEntries.filter((entry) => entry.status === "Activo").length;
    return <div className="feature-stack"><Card><div className="page-header"><div className="page-header-copy"><h1>Versión de un catálogo</h1><p>Resumen de la versión local y de las familias que la componen.</p></div><div className="page-actions"><Button variant="outline" onClick={() => onNavigate("ADM-05")}>Volver a catálogos</Button></div></div><div className="definition-grid"><Definition label="Versión en revisión" value={`v${state.catalogVersion}`} /><Definition label="Entradas activas" value={active} /><Definition label="Estado" value={<Tag tone="info">Revisión local</Tag>} /><Definition label="Persistencia" value="Solo este dispositivo" /></div><Alert tone="warning" title="No es una publicación productiva">La UI conserva el cambio para revisión, pero no habilita automáticamente valores en el dominio.</Alert></Card></div>;
}

function catalogFamilyForScreen(screenId: string): CatalogFamily {
    return crmCatalogFamilies.find((family) => family.screenId === screenId)?.id ?? "pipeline-stage";
}

function CatalogList({ screen, state, onToast, onNavigate, dispatch }: {
    screen: ScreenDefinition;
    state: DemoState;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    onNavigate: ScreenNavigator;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const [editing, setEditing] = useState<string | null>(null);
    const [draft, setDraft] = useState("");
    const family = crmCatalogFamilies.find((item) => item.id === catalogFamilyForScreen(screen.id)) ?? crmCatalogFamilies[0];
    const entries = state.catalogEntries.filter((entry) => entry.catalogType === family.id);
    const save = (id: string, status: "Activo" | "Cierre comercial") => { if (!draft.trim()) {
        onToast("El nombre del catálogo no puede quedar vacío.", "warning");
        return;
    } dispatch({ type: "catalog/update-entry", id, label: draft.trim(), status }); setEditing(null); onToast("Entrada guardada para revisión local."); };
    return <div className="catalog-list">
<div className="catalog-version">
<Icon name="database" size={18}/>
<div>
<strong>{family.label} · v{state.catalogVersion}</strong>
<span>{family.description} Los cambios no se publican automáticamente.</span>
</div>
<Button variant="outline" size="small" onClick={() => onNavigate("ADM-12")}>Ver versión</Button>
</div>
<Alert tone="info" title="Semántica protegida">Las etapas conservan su estado OPEN/WON/LOST. Editar la etiqueta no cambia el significado histórico.</Alert>
{entries.length ? entries.map((entry, index) => <div className="catalog-row" key={entry.id}>
<span className="catalog-index">{String(index + 1).padStart(2, "0")}</span>{editing === entry.id ? <Field label={`Nombre de ${entry.label}`} value={draft} onChange={(event) => setDraft(event.target.value)}/> : <strong>{entry.label}</strong>}<span>{entry.semanticState ? `${entry.status} · ${entry.semanticState}` : entry.status}</span>{entry.protected && <Tag tone="neutral">Protegida</Tag>}{editing === entry.id ? <Button variant="ghost" size="xsmall" icon="check" aria-label={`Guardar ${entry.label}`} onClick={() => save(entry.id, entry.status)}/> : <Button variant="ghost" size="xsmall" icon="edit" aria-label={`Editar ${entry.label}`} onClick={() => { setEditing(entry.id); setDraft(entry.label); }}/>}</div>) : <EmptyState icon="database" title="No hay entradas en esta familia" description="El catálogo todavía no tiene valores locales disponibles; no se muestran filas inventadas." />}</div>;
}
function InviteUserForm({ onToast, onNavigate, dispatch }: {
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    onNavigate: ScreenNavigator;
    dispatch: React.Dispatch<DemoAction>;
}) {
    const [name, setName] = useState("");
    const [email, setEmail] = useState("");
    const [role, setRole] = useState<"Vendedor" | "Responsable comercial" | "Dirección" | "Administradora">("Vendedor");
    const save = () => { if (!name.trim() || !email.trim()) {
        onToast("Completá nombre y email para enviar la invitación.", "warning");
        return;
    } dispatch({ type: "user/invite", item: { id: `user-${Date.now()}`, name: name.trim(), email: email.trim(), role, status: "Pendiente" } }); onToast("Invitación creada como pendiente."); onNavigate("ADM-01"); };
    return <Card className="form-card">
<div className="form-card-heading">
<div>
<span className="eyebrow">Acceso</span>
<h2>Invitar usuario</h2>
<p>La invitación crea una solicitud; la habilitación efectiva queda registrada.</p>
</div>
<Tag tone="info">Sin auto habilitar</Tag>
</div>
<div className="form-grid">
<Field label="Nombre" placeholder="Nombre y apellido" value={name} onChange={(event) => setName(event.target.value)} autoFocus/>
<Field label="Email" placeholder="persona@inmobiliaria.com" type="email" value={email} onChange={(event) => setEmail(event.target.value)}/>
<SelectField label="Rol" value={role} onChange={(event) => setRole(event.target.value as typeof role)}>
<option>Vendedor</option>
<option>Responsable comercial</option>
<option>Dirección</option>
<option>Administradora</option>
</SelectField>
</div>
<Alert tone="info" title="Permiso explícito">La persona no accede hasta que la administradora la habilite.</Alert>
<div className="form-footer">
<Button variant="ghost" onClick={() => onNavigate("ADM-01")}>Cancelar</Button>
<Button onClick={save}>Enviar invitación</Button>
</div>
</Card>;
}
function GlobalStateSurface({ screen, onToast, onNavigate }: {
    screen: ScreenDefinition;
    onToast: (message: string, tone?: "success" | "info" | "warning") => void;
    onNavigate: ScreenNavigator;
}) {
    const stateCopy: Record<string, {
        tone: "info" | "warning" | "error";
        title: string;
        body: string;
    }> = {
        "GLB-01": { tone: "info", title: "Esta instalación está vacía", body: "Empezá con un contacto o un inmueble. La pantalla no inventa datos iniciales." },
        "GLB-02": { tone: "info", title: "No hay resultados para este filtro", body: "Probá limpiar el filtro o crear un nuevo registro." },
        "GLB-03": { tone: "info", title: "Cargando información", body: "Mostramos skeletons de filas para conservar el contexto de la pantalla." },
        "GLB-04": { tone: "warning", title: "Parte de la información no está disponible", body: "Podés seguir revisando las secciones que sí cargaron." },
        "GLB-05": { tone: "error", title: "No pudimos cargar el CRM", body: "Reintentá cuando la conexión vuelva a responder." },
        "GLB-06": { tone: "warning", title: "Sin conexión", body: "Las acciones de captura pueden quedar en la cola y sincronizarse después." },
        "GLB-07": { tone: "error", title: "No tenés permiso para esta capacidad", body: "El acceso efectivo se explica por rol y alcance." },
        "GLB-08": { tone: "info", title: "Vista de solo lectura", body: "Podés consultar el contexto, pero no guardar cambios." },
        "GLB-09": { tone: "info", title: "Sección no disponible", body: "Esta sección estará disponible más adelante." },
        "GLB-10": { tone: "warning", title: "Dato desconocido", body: "Desconocido no significa No. Se conserva como dato faltante." },
        "GLB-11": { tone: "info", title: "Contenido archivado", body: "El historial permanece disponible y la acción de negocio queda detenida." },
        "GLB-12": { tone: "warning", title: "Recalculando", body: "Mostramos el último dato disponible hasta terminar el cálculo." },
    };
    const copy = stateCopy[screen.id] ?? stateCopy["GLB-02"];
    if (screen.id === "GLB-03")
        return <Card>
<div className="skeleton-state">
<Skeleton width="44%" height={25}/>
<Skeleton width="75%" height={15}/>
<div className="skeleton-table">{[1, 2, 3, 4].map((item) => <div key={item}>
<Skeleton width="34%"/>
<Skeleton width="18%"/>
<Skeleton width="22%"/>
</div>)}</div>
</div>
</Card>;
    if (screen.id === "GLB-02")
        return <Card>
<EmptyState icon="search" title={copy.title} description={copy.body} action={<Button size="small" icon="plus" onClick={() => onNavigate("PTY-02")}>Crear registro</Button>}/>
</Card>;
    if (screen.id === "GLB-01")
        return <Card>
<EmptyState icon="database" title={copy.title} description={copy.body} action={<div className="empty-state-actions"><Button size="small" icon="users" onClick={() => onNavigate("PTY-02")}>Crear contacto</Button><Button variant="outline" size="small" icon="building" onClick={() => onNavigate("PRP-03")}>Cargar inmueble</Button></div>}/>
</Card>;
    if (screen.id === "GLB-10")
        return <div className="feature-stack">
<Alert tone="warning" title={copy.title}>{copy.body}</Alert>
<Card>
<div className="unknown-grid">
<Definition label="Superficie cubierta" value={<UnknownIndicator />}/>
<Definition label="Tiene patio" value="No"/>
<Definition label="Geo" value={<UnknownIndicator label="Desconocida"/>}/>
<Definition label="Mandato" value="No"/>
</div>
</Card>
</div>;
    return <Card className="state-card">
<div className={`state-illustration state-${copy.tone}`}>
<Icon name={screen.id === "GLB-06" ? "wifi" : screen.id === "GLB-07" ? "lock" : screen.id === "GLB-12" ? "refresh" : screen.id === "GLB-11" ? "archive" : "alert"} size={28}/>
</div>
<div>
<span className="eyebrow">Estado persistente</span>
<h2>{copy.title}</h2>
<p>{copy.body}</p>
<div className="state-actions">
<Button variant="outline" icon="refresh" onClick={() => onToast("Se reintentó la lectura.", "info")}>Reintentar</Button>{screen.id === "GLB-07" && <Button onClick={() => onNavigate("AUT-03")}>Ver habilitación requerida</Button>}</div>
</div>
</Card>;
}
function DeferredSurface({ screen }: {
    screen: ScreenDefinition;
}) {
    return <Card className="deferred-state">
<div className="deferred-icon">
<Icon name="lock" size={27}/>
</div>
<div>
<div className="page-title-row">
<span className="eyebrow">Próximamente</span>
</div>
<h2>{screen.title}</h2>
<p>Esta sección todavía no está disponible para trabajar desde el CRM.</p>
<div className="deferred-meta">
<span>
<strong>Disponibilidad</strong>Próximamente</span>
<span>
<strong>Estado</strong>No disponible</span>
</div>
</div>
</Card>;
}
function AuthSurface({ screen, onNavigate }: {
    screen: ScreenDefinition;
    onNavigate: ScreenNavigator;
}) {
    if (screen.id === "AUT-01")
        return <Card className="auth-card">
<div className="auth-mark">b</div>
<span className="eyebrow">Acceso al CRM</span>
<h2>Ingresá a tu instalación</h2>
<p>La autenticación se completa con el proveedor de identidad.</p>
<Button fullWidth onClick={() => {
    if (process.env.NEXT_PUBLIC_CRM_WEB_MODE !== "demo" && process.env.NEXT_PUBLIC_CRM_BFF_URL) {
        window.location.href = `${process.env.NEXT_PUBLIC_CRM_BFF_URL}/api/v1/auth/login`;
    } else {
        onNavigate("INI-01");
    }
}}>Continuar con proveedor de identidad</Button>
<small>Esta vista no solicita credenciales.</small>
</Card>;
    if (screen.id === "AUT-03")
        return <Card className="state-card">
<div className="state-illustration state-warning">
<Icon name="lock" size={28}/>
</div>
<div>
<span className="eyebrow">Acceso</span>
<h2>Usuario sin habilitación</h2>
<p>Tu identidad fue reconocida, pero todavía no tenés un rol habilitado en esta instalación.</p>
<Button onClick={() => onNavigate("ADM-02")}>Iniciar solicitud de habilitación</Button>
</div>
</Card>;
    if (screen.id === "AUT-04")
        return <Onboarding onNavigate={onNavigate}/>;
    return <Card className="state-card">
<div className="state-illustration state-info">
<Icon name="refresh" size={28}/>
</div>
<div>
<span className="eyebrow">Sesión expirada</span>
<h2>Guardamos tu borrador</h2>
<p>Volvé a autenticarte para continuar. Lo que estabas escribiendo queda en el dispositivo hasta confirmar la sesión.</p>
<Button onClick={() => onNavigate("AUT-01")}>Volver a ingresar</Button>
</div>
</Card>;
}
function Onboarding({ onNavigate }: { onNavigate: ScreenNavigator }) {
    const [name, setName] = useState("");
    const [currency, setCurrency] = useState("ARS");
    const [step, setStep] = useState(1);
    const continueOnboarding = () => { if (step < 4) setStep((current) => current + 1); else onNavigate("INI-01"); };
    return <Card className="onboarding-card">
<div className="onboarding-heading">
<span className="eyebrow">Primer ingreso</span>
<h2>Preparemos la instalación</h2>
<p>Cuatro decisiones simples para empezar con datos consistentes.</p>
</div>
<div className="stepper onboarding-stepper">
<span className={`stepper-step ${step >= 1 ? "is-active" : ""}`}>
<b>1</b>Identidad</span>
<span className="stepper-line"/>
<span className={`stepper-step ${step >= 2 ? "is-active" : ""}`}>
<b>2</b>Catálogos</span>
<span className="stepper-line"/>
<span className={`stepper-step ${step >= 3 ? "is-active" : ""}`}>
<b>3</b>Equipo</span>
<span className="stepper-line"/>
<span className={`stepper-step ${step >= 4 ? "is-active" : ""}`}>
<b>4</b>Listo</span>
</div>
<div className="onboarding-grid">
{step === 1 && <><Field label="Nombre de la inmobiliaria" placeholder="Inmobiliaria Norte" value={name} onChange={(event) => setName(event.target.value)}/>
<SelectField label="Moneda habitual" value={currency} onChange={(event) => setCurrency(event.target.value)}>
<option>ARS</option>
<option>USD</option>
 </SelectField></>}
{step === 2 && <Alert tone="info" title="Catálogos base">Se usarán las seis familias comerciales documentadas. Podés editarlas luego desde Administración.</Alert>}
{step === 3 && <Alert tone="info" title="Equipo inicial">La incorporación de personas queda pendiente de invitación explícita; no se habilita a nadie automáticamente.</Alert>}
{step === 4 && <Alert tone="success" title="Instalación lista">{name.trim() ? `${name.trim()} queda preparada con moneda ${currency}.` : "La instalación queda preparada con la configuración disponible."}</Alert>}
</div>
<div className="form-footer">
<Button variant="ghost" onClick={() => onNavigate("AUT-01")}>{step === 1 ? "Salir" : "Cancelar"}</Button>
<Button onClick={continueOnboarding} disabled={step === 1 && !name.trim()}>{step === 4 ? "Ir al inicio" : "Continuar"}</Button>
</div>
</Card>;
}
function Timeline({ items }: {
    items: string[];
}) {
    return <div className="timeline">{items.map((item, index) => <div className="timeline-item" key={`${item}-${index}`}>
<span className="timeline-dot"/>
<div>
<strong>{item.split(" · ")[0]}</strong>
<small>{item.includes(" · ") ? item.slice(item.indexOf(" · ") + 3) : index === 0 ? "Hoy · 09:42" : "Registro histórico"}</small>
</div>
</div>)}</div>;
}
function DesignCatalog() {
    const [query, setQuery] = useState("");
    const [phase, setPhase] = useState<"ALL" | "V2" | "F2">("ALL");
    const filtered = screenRegistry.filter((screen) => (phase === "ALL" || screen.phase === phase) && `${screen.id} ${screen.title} ${screen.module}`.toLowerCase().includes(query.toLowerCase())).slice(0, 80);
    const v2 = screenRegistry.filter((screen) => screen.phase === "V2").length;
    const f2 = screenRegistry.filter((screen) => screen.phase === "F2").length;
    return <div className="design-catalog">
<header className="catalog-header">
<div className="brand-lockup">
<span className="brand-mark">b</span>
<span className="brand-name">brick<span>/</span>eminent</span>
</div>
<Tag tone="brand">__design · dev only</Tag>
</header>
<div className="catalog-intro">
<div>
<span className="eyebrow">V2-UX-001 · Design handoff</span>
<h1>CRM design catalog</h1>
<p>Mapa tipado de pantallas, tokens y decisiones. Esta ruta no aparece en el shell productivo.</p>
</div>
<div className="catalog-counts">
<div>
<strong>{v2}</strong>
<span>V2 funcionales</span>
</div>
<div>
<strong>{f2}</strong>
<span>F2 diferidas</span>
</div>
<div>
<strong>172</strong>
<span>inventario total</span>
</div>
</div>
</div>
<div className="catalog-grid">
<Card>
<SectionHeading eyebrow="Inventario completo" title="Pantalla → módulo → journey"/>
<div className="catalog-controls">
<div className="inline-search">
<Icon name="search" size={16}/>
<input aria-label="Buscar pantalla" value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Buscar por ID o nombre"/>
</div>
<div className="role-pill-row">{(["ALL", "V2", "F2"] as const).map((item) => <button type="button" className={phase === item ? "role-pill is-active" : "role-pill"} key={item} onClick={() => setPhase(item)}>{item === "ALL" ? "Todas" : item === "F2" ? "Diferidas" : "V2"}</button>)}</div>
</div>
<div className="catalog-table">
<div className="catalog-table-head">
<span>ID</span>
<span>Superficie</span>
<span>Módulo</span>
<span>Journey</span>
<span>Fase</span>
</div>{filtered.map((screen) => <div className="catalog-table-row" key={screen.id}>
<code>{screen.id}</code>
<strong>{screen.title}</strong>
<span>{screen.module}</span>
<span>{screen.journey}</span>
<Tag tone={screen.phase === "V2" ? "info" : "warning"}>{screen.phase}</Tag>
</div>)}</div>
<small className="catalog-limit">Mostrando {filtered.length} coincidencias para mantener la revisión cómoda; el registro tipado completo conserva las {screenRegistry.length} entradas.</small>
</Card>
<div className="catalog-side">
<Card>
<SectionHeading eyebrow="Tokens" title="Brick-compatible"/>
<div className="token-swatches">
<span style={{ background: "#003973" }}>Eminent</span>
<span style={{ background: "#fa6400" }}>Brand</span>
<span style={{ background: "#f4f4f4", color: "#2b2b2b" }}>Grey 20</span>
<span style={{ background: "#258825" }}>Success</span>
</div>
<div className="token-lines">
<span>Inter · 400 / 500</span>
<span>Radii · 4px / 8px / pill</span>
<span>Grid · 4px</span>
<span>Targets · 44–52px</span>
<span>Motion · 150–350ms</span>
</div>
</Card>
<Card>
<SectionHeading eyebrow="Decisiones" title="Guardrails"/>
<div className="decision-list">
<span>
<Icon name="check" size={15}/>Sin tenantId ni selector de organización.</span>
<span>
<Icon name="check" size={15}/>CommercialPipelineItem como fachada.</span>
<span>
<Icon name="check" size={15}/>Unknown nunca es negativo.</span>
<span>
<Icon name="check" size={15}/>Email y WhatsApp solo históricos.</span>
<span>
<Icon name="lock" size={15}/>Platform Admin queda fuera de esta app.</span>
</div>
</Card>
<Card>
<SectionHeading eyebrow="Responsive" title="Mobile first en tres flujos"/>
<div className="mobile-catalog-list">
<span>
<Icon name="calendar" size={16}/>Visita ocurrida</span>
<span>
<Icon name="activity" size={16}/>Actividad rápida</span>
<span>
<Icon name="users" size={16}/>Ficha de contacto</span>
<span>
<Icon name="wifi" size={16}/>Cola offline demo</span>
</div>
</Card>
</div>
</div>
</div>;
}
