import React from "react";
import { render } from "@testing-library/react";
import { Alert, Button, Drawer, Field, Modal, Tabs } from "@/components/ui/primitives";

describe("componentes transversales", () => {
  it("expone estados persistentes y controles accesibles", () => {
    const result = render(<><Alert tone="error" title="Bloqueo">Hay un error</Alert><Button aria-label="Guardar">Guardar</Button><Field label="Nombre" /><Tabs tabs={[{ id: "summary", label: "Resumen" }]} active="summary" onChange={() => undefined} /></>);
    expect(result.container.querySelector('[role="alert"]')).not.toBeNull();
    expect(result.container.querySelector('button[aria-label="Guardar"]')).not.toBeNull();
    expect(result.container.querySelector('input')).not.toBeNull();
    expect(result.container.querySelector('[role="tab"]')?.getAttribute("aria-selected")).toBe("true");
  });

  it("monta y desmonta drawer y modal sin perder el nombre accesible", () => {
    const result = render(<><Drawer open title="Validación" onClose={() => undefined}>Contenido</Drawer><Modal open title="Confirmar" onClose={() => undefined}>Confirmación</Modal></>);
    expect(result.container.querySelector('[role="dialog"][aria-label="Validación"]')).not.toBeNull();
    expect(result.container.querySelector('[role="dialog"][aria-label="Confirmar"]')).not.toBeNull();
  });
});
