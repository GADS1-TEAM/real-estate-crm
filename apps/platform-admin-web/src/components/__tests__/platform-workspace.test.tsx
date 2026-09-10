import React from "react";
import { act, fireEvent, render } from "@testing-library/react";
import { PlatformWorkspace, type WorkspaceActions } from "@/components/platform-workspace";
import { getScreenById } from "@/lib/screen-registry";
import { reviewInitialState } from "@/lib/review-store";
import { vi } from "vitest";

describe("workspace de Platform Admin", () => {
  it("renderiza el overview con señales accionables", () => {
    const result = render(<PlatformWorkspace screen={getScreenById("PA-001")!} state={reviewInitialState} roleId="release-manager" dispatch={() => undefined} onNavigate={() => undefined} onOpenValidation={() => undefined} />);
    expect(result.container.textContent).toContain("Inicio");
    expect(result.container.textContent).toContain("Cosas que necesitan una decisión");
    expect(result.container.textContent).not.toContain("modo demo");
  });

  it("muestra un bloqueo de workflow y una acción de corrección", () => {
    const result = render(<PlatformWorkspace screen={getScreenById("PA-042")!} state={reviewInitialState} roleId="release-manager" dispatch={() => undefined} onNavigate={() => undefined} onOpenValidation={() => undefined} />);
    expect(result.container.textContent).toContain("Bloquea el publish");
    expect(result.container.textContent).toContain("Corregir dependencia");
  });

  it("no permite abrir confirmación mientras existen blockers", () => {
    const result = render(<PlatformWorkspace screen={getScreenById("PA-015")!} state={reviewInitialState} roleId="release-manager" dispatch={() => undefined} onNavigate={() => undefined} onOpenValidation={() => undefined} />);
    const confirmation = result.queryByRole("button", { name: /4 Confirmación/ });
    expect(confirmation).toBeNull();
    if (confirmation) fireEvent.click(confirmation);
    expect(result.queryByRole("heading", { name: "Confirmar publicación" })).toBeNull();
  });

  it("guarda un campo de metadatos del pack al perder el foco", async () => {
    const saveDraft = vi.fn().mockResolvedValue(undefined);
    const actions: WorkspaceActions = { saveDraft, validateDraft: vi.fn().mockResolvedValue(undefined), publish: vi.fn().mockResolvedValue(undefined), deprecate: vi.fn().mockResolvedValue(undefined), executeCorrection: vi.fn().mockResolvedValue(undefined) };
    const result = render(<PlatformWorkspace screen={getScreenById("PA-013")!} state={reviewInitialState} roleId="release-manager" dispatch={() => undefined} actions={actions} onNavigate={() => undefined} onOpenValidation={() => undefined} />);
    const input = result.getByLabelText("Nombre");
    await act(async () => { fireEvent.change(input, { target: { value: "Argentina Base Pack revisado" } }); fireEvent.blur(input); await Promise.resolve(); });
    expect(saveDraft).toHaveBeenCalledWith({ artifactId: "pack-argentina", field: "name", value: "Argentina Base Pack revisado" });
  });

  it("conecta el switch de feature flags al estado de review", () => {
    const dispatch = vi.fn();
    const result = render(<PlatformWorkspace screen={getScreenById("PA-102")!} state={reviewInitialState} roleId="release-manager" dispatch={dispatch} onNavigate={() => undefined} onOpenValidation={() => undefined} />);
    fireEvent.click(result.getByRole("button", { name: "Activar new_workflow" }));
    expect(dispatch).toHaveBeenCalledWith({ type: "flag/toggle", key: "new_workflow" });
  });

  it("envía una corrección con motivo y no permite una acción vacía", async () => {
    const executeCorrection = vi.fn().mockResolvedValue(undefined);
    const actions: WorkspaceActions = { saveDraft: vi.fn().mockResolvedValue(undefined), validateDraft: vi.fn().mockResolvedValue(undefined), publish: vi.fn().mockResolvedValue(undefined), deprecate: vi.fn().mockResolvedValue(undefined), executeCorrection };
    const result = render(<PlatformWorkspace screen={getScreenById("PA-143")!} state={reviewInitialState} roleId="support" actions={actions} dispatch={() => undefined} onNavigate={() => undefined} onOpenValidation={() => undefined} />);
    const submit = result.getByRole("button", { name: "Autorizar corrección" });
    expect((submit as HTMLButtonElement).disabled).toBe(true);
    act(() => { fireEvent.change(result.getByLabelText("Motivo obligatorio"), { target: { value: "Reprocesar el caso con la versión vigente." } }); });
    expect((submit as HTMLButtonElement).disabled).toBe(false);
    await act(async () => { fireEvent.click(submit); await Promise.resolve(); });
    expect(executeCorrection).toHaveBeenCalledWith({ commandId: "REPROCESS_COMPLIANCE_CASE", targetId: "compliance-case-784", reason: "Reprocesar el caso con la versión vigente." });
  });
});
