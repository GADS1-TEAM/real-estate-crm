"use client";

import { useCallback, useEffect, useMemo, useReducer, useState } from "react";
import { usePathname, useRouter } from "next/navigation";
import { createPlatformAdminDataSource, reviewArtifacts, reviewImpact, PlatformDataSourceError } from "@/lib/data-source";
import { getRole } from "@/lib/permissions";
import { getScreenById, getScreenByRoute } from "@/lib/screen-registry";
import { persistReviewState, readReviewState, reviewInitialState, reviewReducer } from "@/lib/review-store";
import type { CorrectionInput, DraftUpdateInput, EnvironmentName, PlatformSnapshot, PublishInput, ScreenDefinition } from "@/lib/types";
import { PlatformShell, CommandPalette } from "@/components/platform-shell";
import { Button, Alert, Chip, Icon, Skeleton, Toast } from "@/components/ui/primitives";
import { PlatformWorkspace, ValidationDrawer, type WorkspaceActions } from "@/components/platform-workspace";

export function PlatformApp({ initialScreenId }: { initialScreenId?: string }) {
  const router = useRouter();
  const pathname = usePathname();
  const mode = process.env.NEXT_PUBLIC_PLATFORM_ADMIN_MODE === "review" ? "review" : "bff";
  const bffUrl = process.env.NEXT_PUBLIC_PLATFORM_ADMIN_BFF_URL;
  const [state, dispatch] = useReducer(reviewReducer, reviewInitialState);
  const source = useMemo(() => createPlatformAdminDataSource(mode, bffUrl, state.environment), [bffUrl, mode, state.environment]);
  const [snapshot, setSnapshot] = useState<PlatformSnapshot>({ artifacts: reviewArtifacts, impact: reviewImpact, state: reviewInitialState });
  const [hydrated, setHydrated] = useState(mode !== "review");
  const [screenId, setScreenId] = useState(initialScreenId ?? getScreenByRoute(pathname)?.id ?? "PA-001");
  const [searchOpen, setSearchOpen] = useState(false);
  const [bffReady, setBffReady] = useState(mode === "review");
  const [bffError, setBffError] = useState<string | null>(null);
  const [bffLoading, setBffLoading] = useState(mode === "bff");
  const [bffCorrelationId, setBffCorrelationId] = useState<string | null>(null);

  useEffect(() => {
    if (mode !== "review") return;
    const stored = readReviewState(window.localStorage);
    if (stored) dispatch({ type: "state/hydrate", state: stored });
    setHydrated(true);
  }, [mode]);

  useEffect(() => {
    if (mode === "review" && hydrated) persistReviewState(window.localStorage, state);
  }, [hydrated, mode, state]);

  const refreshSnapshot = useCallback(async () => {
    const next = await source.getSnapshot();
    setSnapshot(next);
    dispatch({ type: "state/hydrate", state: next.state });
    return next;
  }, [source]);

  useEffect(() => {
    if (mode !== "bff") return;
    if (!source.getAvailability().available) { setBffLoading(false); setBffReady(false); return; }
    setBffLoading(true);
    refreshSnapshot().then(() => { setBffError(null); setBffCorrelationId(null); setBffReady(true); }).catch((error: unknown) => { setBffError(error instanceof Error ? error.message : "PLATFORM_BFF_ERROR"); setBffCorrelationId(error instanceof PlatformDataSourceError ? error.correlationId ?? null : null); setBffReady(false); }).finally(() => setBffLoading(false));
  }, [mode, refreshSnapshot, source]);

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => { if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "k") { event.preventDefault(); setSearchOpen(true); } };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, []);

  useEffect(() => {
    if (mode !== "review") return;

    const persona = new URLSearchParams(window.location.search).get("persona");
    if (persona && getRole(persona).id === persona) dispatch({ type: "role/select", roleId: persona });
  }, [mode]);

  const navigate = (screen: ScreenDefinition) => {
    setScreenId(screen.id);
    if (pathname !== screen.route) router.push(screen.route);
    window.scrollTo({ top: 0, behavior: "smooth" });
  };
  const current = getScreenById(screenId) ?? getScreenById("PA-001")!;
  const visibleArtifacts = useMemo(() => snapshot.artifacts.map((artifact) => {
    const published = state.published[artifact.id];
    const draft = state.drafts[artifact.id];
    return { ...artifact, status: published?.status === "DEPRECATED" ? "DEPRECATED" as const : draft && !published ? draft.status : artifact.status, publishedVersion: published?.version ?? artifact.publishedVersion, draftVersion: draft?.version ?? undefined, updatedAt: draft?.updatedAt ?? artifact.updatedAt };
  }), [snapshot.artifacts, state.drafts, state.published]);
  const unavailable = mode === "bff" && (!bffReady || Boolean(bffError));
  const runCommand = useCallback(async (request: () => Promise<void>, localAction: Parameters<typeof reviewReducer>[1], successMessage: string) => {
    if (source.kind === "review") {
      dispatch(localAction);
      return;
    }
    try {
      await request();
      await refreshSnapshot();
      dispatch({ type: "toast/show", message: successMessage });
    } catch (error) {
      const message = error instanceof PlatformDataSourceError ? error.code : error instanceof Error ? error.message : "PLATFORM_BFF_ERROR";
      setBffError(message);
      dispatch({ type: "toast/show", message: `No se pudo completar la acción. Código: ${message}.` });
      throw error;
    }
  }, [refreshSnapshot, source]);
  const actions = useMemo<WorkspaceActions>(() => ({
    saveDraft: (input: DraftUpdateInput) => runCommand(() => source.saveDraft(input), { type: "draft/update", artifactId: input.artifactId, field: input.field, value: input.value }, "Borrador guardado."),
    validateDraft: (artifactId: string) => source.kind === "review" ? Promise.resolve(dispatch({ type: "toast/show", message: "Validación actualizada." })) : source.validateDraft(artifactId).then((next) => { setSnapshot(next); dispatch({ type: "state/hydrate", state: next.state }); dispatch({ type: "toast/show", message: "Validación actualizada." }); }),
    publish: (input: PublishInput) => runCommand(() => source.publish(input), { type: "artifact/publish", artifactId: input.artifactId }, "Versión publicada. Las instancias históricas no cambiaron."),
    deprecate: (artifactId: string, reason: string) => runCommand(() => source.deprecate(artifactId, reason), { type: "artifact/deprecate", artifactId }, "Artefacto deprecado. Sus instancias históricas se conservan."),
    executeCorrection: (input: CorrectionInput) => runCommand(() => source.executeCorrection(input), { type: "correction/audit", command: input.commandId, targetId: input.targetId, reason: input.reason }, "Corrección registrada y enviada al servicio owner."),
  }), [runCommand, source]);

  return <>
    <PlatformShell screenId={current.id} roleId={state.selectedRole} session={state.session} environment={state.environment} reviewMode={mode === "review"} onRoleChange={(roleId) => dispatch({ type: "role/select", roleId })} onNavigate={navigate} onOpenSearch={() => setSearchOpen(true)} onEnvironmentChange={(environment: EnvironmentName) => dispatch({ type: "environment/select", environment })}>
      <div className="breadcrumbs"><button type="button" onClick={() => navigate(getScreenById("PA-001")!)}>Platform Admin</button><span className="separator">›</span><span>{current.group}</span><span className="separator">›</span><span className="current">{current.title}</span></div>
      {!unavailable && <PlatformWorkspace screen={current} state={state} roleId={state.selectedRole} dispatch={dispatch} actions={actions} artifacts={visibleArtifacts} impact={snapshot.impact} onNavigate={navigate} onOpenValidation={() => dispatch({ type: "validation/toggle" })} />}
      {unavailable && <UnavailableState bffUrl={bffUrl} error={bffError} loading={bffLoading} correlationId={bffCorrelationId} />}
    </PlatformShell>
    <CommandPalette open={searchOpen} onClose={() => setSearchOpen(false)} onNavigate={navigate} />
    <ValidationDrawer state={state} open={!unavailable && state.validationOpen} onClose={() => dispatch({ type: "validation/toggle" })} go={(id) => { const target = getScreenById(id); if (target) navigate(target); }} fix={(journey) => dispatch({ type: "journey/fix", journey })} onOpenIssue={(issue) => dispatch({ type: "validation/focus", field: issue.field ?? null })} />
    <Toast message={state.toast} action={state.toastAuditId ? <button type="button" className="toast-action" onClick={() => { const audit = getScreenById("PA-141"); if (audit) navigate(audit); }}>Ver auditoría</button> : undefined} onClose={() => dispatch({ type: "toast/clear" })} />
  </>;
}

function UnavailableState({ bffUrl, error, loading, correlationId }: { bffUrl?: string; error: string | null; loading: boolean; correlationId: string | null }) {
  return <div className="no-permission"><div className="lock-mark"><Icon name="plug" size={24} /></div><h1>{loading ? "Conectando con Platform Admin" : "Conexión pendiente"}</h1><p className="lead-copy">Platform Admin necesita su BFF para cargar configuración real. La interfaz no usa datos locales cuando la conexión productiva no está disponible.</p>{loading ? <div className="bff-skeleton" aria-label="Cargando configuración"><Skeleton width="76%" height={14} /><Skeleton width="100%" height={48} /><Skeleton width="34%" height={34} /></div> : <Alert tone="info" title={error ? "No pudimos consultar el BFF" : "BFF no configurado"}>{error ? `Código de diagnóstico: ${error}. Revisá la URL y el estado del servicio.` : "Configurá NEXT_PUBLIC_PLATFORM_ADMIN_BFF_URL para conectar la configuración real."}</Alert>}<div className="header-actions">{!loading && <Button variant="outline" icon="refresh" onClick={() => window.location.reload()}>Reintentar</Button>}{correlationId && <Chip tone="neutral">Correlation ID: {correlationId}</Chip>}{bffUrl && <ChipLine value={bffUrl} />}</div></div>;
}

function ChipLine({ value }: { value: string }) {
  return <div className="url-note"><Icon name="plug" size={14} />{value}</div>;
}
