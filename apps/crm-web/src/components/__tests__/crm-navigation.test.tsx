import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { cleanup, fireEvent, render, screen, within } from "@testing-library/react";
import { CrmApp } from "@/components/crm-workspace";
import { demoInitialState, persistDemoState } from "@/lib/demo-store";

const route = vi.hoisted(() => {
  process.env.NEXT_PUBLIC_CRM_WEB_MODE = "demo";
  return { pathname: "/contactos", params: new URLSearchParams(), push: vi.fn() };
});

vi.mock("next/navigation", () => ({
  usePathname: () => route.pathname,
  useSearchParams: () => route.params,
  useRouter: () => ({ push: route.push }),
}));

function open(section: string, screenId: string, entity?: string) {
  route.pathname = `/${section}`;
  route.params = new URLSearchParams({ screen: screenId });
  if (entity !== undefined) route.params.set("entity", entity);
  return render(<CrmApp initialSection={section} />);
}

beforeEach(() => {
  window.localStorage.clear();
  route.push.mockClear();
});
afterEach(cleanup);

describe("CRM selection and navigation", () => {
  it.each([
    ["contactos", "PTY-05"], ["inmuebles", "PRP-06"],
    ["publicaciones", "LST-04"], ["captaciones", "CAP-03"],
    ["busquedas", "DEM-04"], ["compatibilidades", "MAT-01"],
    ["compatibilidades", "MAT-03"], ["oportunidades", "OPP-04"],
    ["oportunidades", "COM-09"], ["oportunidades", "COM-10"],
    ["oportunidades", "COM-12"], ["oportunidades", "COM-13"],
    ["oportunidades", "COM-14"], ["actividad", "ACT-02"],
  ])("never renders an unrelated record for %s / %s", (section, screenId) => {
    for (const entity of ["unknown-record", ""]) {
      const view = open(section, screenId, entity);
      expect(screen.getByText("Registro no disponible")).toBeTruthy();
      expect(screen.queryByRole("heading", { name: "Ana Suárez" })).toBeNull();
      expect(screen.queryByRole("heading", { name: "Casa en Villa Crespo" })).toBeNull();
      view.unmount();
    }
  });

  it.each([
    ["contactos", "PTY-01", "Abrir Carla Benítez", "/contactos?screen=PTY-05&entity=contact-2"],
    ["inmuebles", "PRP-01", "Abrir PH en Guardia Vieja 3355", "/inmuebles?screen=PRP-06&entity=property-2"],
    ["publicaciones", "LST-01", "Abrir PH reciclado cerca del subte", "/publicaciones?screen=LST-04&entity=listing-2"],
    ["busquedas", "DEM-01", "Abrir Carla busca PH con terraza", "/busquedas?screen=DEM-04&entity=demand-2"],
    ["oportunidades", "OPP-02", "Abrir Carla Benítez · PH en Guardia Vieja", "/oportunidades?screen=OPP-04&entity=opp-2"],
  ])("passes the clicked row id for %s", (section, screenId, label, expected) => {
    open(section, screenId);
    fireEvent.click(screen.getByRole("button", { name: label }));
    expect(route.push).toHaveBeenLastCalledWith(expected);
  });

  it.each([
    ["contactos", "PTY-05", "contact-2", "Carla Benítez", "Relaciones", "/contactos?screen=PTY-07&entity=contact-2"],
    ["inmuebles", "PRP-06", "property-2", "PH en Guardia Vieja 3355", "Propietarios", "/inmuebles?screen=PRP-10&entity=property-2"],
    ["publicaciones", "LST-04", "listing-2", "PH reciclado cerca del subte", "Términos", "/publicaciones?screen=LST-05&entity=listing-2"],
    ["captaciones", "CAP-03", "captation-2", "Casa en Villa Devoto", "Tasación", "/captaciones?screen=CAP-04&entity=captation-2"],
    ["oportunidades", "OPP-04", "opp-2", "Carla Benítez · PH en Guardia Vieja", "Historial", "/oportunidades?screen=OPP-06&entity=opp-2"],
  ])("opens the selected %s record and retains it across tabs", (section, screenId, entity, title, tab, expected) => {
    open(section, screenId, entity);
    expect(screen.getAllByRole("heading", { name: title }).length).toBeGreaterThan(0);
    fireEvent.click(screen.getByRole("tab", { name: tab }));
    expect(route.push).toHaveBeenLastCalledWith(expected);
  });

  it("paginates contacts, resets after filtering and opens a company as a company", () => {
    open("contactos", "PTY-01");
    expect(screen.getByText("Estudio Norte SA")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Abrir Estudio Norte SA" }));
    expect(route.push).toHaveBeenLastCalledWith("/contactos?screen=PTY-06&entity=contact-4");
    fireEvent.change(screen.getByPlaceholderText("Buscar por nombre, teléfono o email"), { target: { value: "Carla" } });
    expect(screen.getByText("Carla Benítez")).toBeTruthy();
    expect((screen.getByRole("button", { name: "Página anterior" }) as HTMLButtonElement).disabled).toBe(true);
    expect((screen.getByRole("button", { name: "Página siguiente" }) as HTMLButtonElement).disabled).toBe(true);
  });

  it("opens an entity from global search with its exact id", () => {
    const state = structuredClone(demoInitialState);
    state.contacts[1].id = "contact/Carla & ñ";
    persistDemoState(window.localStorage, state);
    open("contactos", "PTY-01");
    fireEvent.click(screen.getByRole("button", { name: /Buscar en toda la instalación/ }));
    const dialog = within(screen.getByRole("dialog", { name: "Búsqueda global" }));
    fireEvent.change(dialog.getByRole("textbox"), { target: { value: "Carla" } });
    fireEvent.click(dialog.getByRole("button", { name: /Carla Benítez.*Contacto/ }));
    expect(route.push).toHaveBeenLastCalledWith("/contactos?screen=PTY-05&entity=contact%2FCarla+%26+%C3%B1");
  });

  it("exposes Agenda as disabled with a reason and no link", () => {
    open("contactos", "PTY-01");
    const agenda = screen.getByText("Agenda · pronto").closest("[aria-disabled]");
    expect(agenda?.getAttribute("aria-disabled")).toBe("true");
    expect(agenda?.getAttribute("aria-description")).toContain("fase posterior");
    expect(screen.queryByRole("link", { name: "Agenda · pronto" })).toBeNull();
    fireEvent.click(agenda!);
    expect(route.push).not.toHaveBeenCalled();
  });

  it("renders INI-02 'Requiere atención' screen and allows returning to INI-01", () => {
    open("inicio", "INI-01");
    fireEvent.click(screen.getByRole("button", { name: "Ver requiere atención" }));
    expect(route.push).toHaveBeenLastCalledWith("/inicio?screen=INI-02");

    open("inicio", "INI-02");
    expect(screen.getByRole("heading", { name: /Oportunidades para revisar/ })).toBeTruthy();
    expect(screen.getByRole("button", { name: "← Volver al inicio" })).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "← Volver al inicio" }));
    expect(route.push).toHaveBeenLastCalledWith("/inicio?screen=INI-01");
  });
});
