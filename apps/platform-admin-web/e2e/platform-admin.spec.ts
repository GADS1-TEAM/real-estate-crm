import { expect, test } from "@playwright/test";

test.beforeEach(async ({ page }) => {
  await page.addInitScript(() => window.localStorage.clear());
});

test("identidad de producto y navegación global", async ({ page }) => {
  await page.goto("/?screen=PA-001");
  await expect(page).toHaveTitle("Platform Admin");
  await expect(page.getByRole("heading", { name: "Inicio" })).toBeVisible();
  await expect(page.getByRole("button", { name: "Packs 2" })).toBeVisible();
  await expect(page.locator("body")).not.toContainText(/\b(V2|F2|demo)\b/i);
  await page.getByRole("button", { name: /Buscar en Platform Admin/ }).click();
  await page.getByPlaceholder("Buscar pantalla, artefacto o sección").fill("pilotos");
  await page.getByRole("button", { name: /Lista de pilotos/ }).click();
  await expect(page.getByRole("heading", { name: "Pilotos y rollout" })).toBeVisible();
});

test("J1 bloquea y luego permite actualizar la referencia del pack", async ({ page }) => {
  await page.goto("/?screen=PA-013");
  await expect(page.getByText("Borrador referenciado")).toBeVisible();
  await page.getByRole("button", { name: "Actualizar referencia" }).click();
  await expect(page.getByText("Referencia vigente").first()).toBeVisible();
  await expect(page.getByText("Se resolvió el bloqueo de J1.")).toBeVisible();
  await page.reload();
  await expect(page.getByText("Referencia vigente").first()).toBeVisible();
});

test("J2, J3 y J5 completan sus guardrails", async ({ page }) => {
  await page.goto("/?screen=PA-073");
  await page.getByRole("button", { name: "Completar al 100 %" }).click();
  await expect(page.getByRole("button", { name: "Distribución válida" })).toBeVisible();

  await page.goto("/?screen=PA-042");
  await page.getByRole("button", { name: "Corregir dependencia" }).click();
  await expect(page.getByRole("button", { name: "Dependencia corregida" })).toBeVisible();

  await page.goto("/?screen=PA-081");
  await page.getByRole("button", { name: "Agregar límite" }).click();
  await expect(page.getByRole("heading", { name: "Simulador de automatización" })).toBeVisible();
});

test("J4, J7 y J9 muestran resolución de dependencia", async ({ page }) => {
  await page.goto("/?screen=PA-051");
  await page.locator(".matrix-warning").click();
  await expect(page.getByText("Se resolvió el bloqueo de J4.")).toBeVisible();

  await page.goto("/?screen=PA-122");
  await page.getByRole("button", { name: "Resolver dependencia" }).click();
  await expect(page.getByText("Se resolvió el bloqueo de J7.")).toBeVisible();

  await page.goto("/?screen=PA-127");
  await page.getByRole("button", { name: "Resolver dependencia" }).click();
  await expect(page.getByRole("button", { name: "Dependencia resuelta" })).toBeVisible();
});

test("J8 es de solo lectura y J10 respeta permisos", async ({ page }) => {
  await page.goto("/?screen=PA-132");
  await expect(page.getByRole("heading", { name: "Configuración efectiva" })).toBeVisible();
  await expect(page.getByText("Solo lectura")).toBeVisible();
  await page.getByRole("tab", { name: "Overrides" }).click();
  await expect(page.getByText("override dentro de límites")).toBeVisible();

  await page.goto("/correcciones/reprocess-compliance?persona=configurator");
  await expect(page.getByText("Requiere ADMIN_CORRECTION")).toBeVisible();
  await expect(page.getByRole("button", { name: "Autorizar corrección" })).toBeDisabled();
});

test("avisa cuando el ancho no alcanza la experiencia de escritorio", async ({ page }) => {
  await page.setViewportSize({ width: 900, height: 800 });
  await page.goto("/?screen=PA-042");
  await expect(page.getByText("Experiencia pensada para escritorio")).toBeVisible();
});
