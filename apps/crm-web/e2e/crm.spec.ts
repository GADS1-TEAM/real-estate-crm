import { expect, test } from "@playwright/test";

test.beforeEach(async ({ page }) => {
  await page.addInitScript(() => {
    if (!window.localStorage.getItem("crm-web:authenticated:v1")) {
      window.localStorage.setItem("crm-web:authenticated:v1", "true");
      window.localStorage.setItem("crm-web:role-id:v1", "vendedor");
      window.localStorage.setItem("crm-web:user-name:v1", "Martín Quiroga");
      window.localStorage.setItem("crm-web:user-email:v1", "martin@inmobiliaria.com.ar");
      window.localStorage.setItem("crm-web:user-login:v1", "martin.quiroga");
    }
  });
});

test("recorre inicio, criterios y cambio de etapa", async ({ page }) => {
  test.skip(test.info().project.name !== "chromium", "El journey principal se valida en desktop; el flujo mobile tiene su escenario propio.");
  await page.goto("/inicio");
  await expect(page.getByRole("heading", { name: "Inicio" })).toBeVisible();
  await page.getByRole("link", { name: "Búsquedas" }).click();
  await expect(page.getByRole("heading", { name: "Búsquedas" })).toBeVisible();
  await page.getByRole("button", { name: /Ana busca casa/ }).first().click();
  await expect(page.getByRole("heading", { name: /Búsqueda 360/ })).toBeVisible();
  await page.getByRole("button", { name: "Editar criterios" }).click();
  await expect(page.getByText("Score recalculado")).toBeVisible();
  await page.getByRole("link", { name: "Oportunidades" }).click();
  await expect(page.getByRole("heading", { name: "Oportunidades", exact: true })).toBeVisible();
  await page.getByRole("button", { name: /Carla Benítez/ }).first().click();
  await expect(page.locator(".detail-hero")).toBeVisible();
  await page.getByRole("button", { name: "Cambiar etapa" }).first().click();
  const stageSelect = page.getByLabel("Nueva etapa");
  await expect(stageSelect).toBeVisible();
  await stageSelect.selectOption("Negociación");
  await page.getByLabel("Motivo del cambio").fill("Se confirmó una visita con la parte interesada.");
  await page.getByRole("button", { name: "Guardar cambio de etapa" }).click();
  await expect(page.getByRole("status").filter({ hasText: "Cambio de etapa guardado" })).toBeVisible();
});

test("registra una contrapropuesta como secuencia inmutable", async ({ page }) => {
  test.skip(test.info().project.name !== "chromium", "La progresión comercial se valida en desktop.");
  await page.goto("/oportunidades?screen=COM-06&entity=opp-2");
  await expect(page.getByRole("heading", { level: 1, name: "Registrar propuesta" })).toBeVisible();
  await page.getByLabel("Tipo de propuesta").selectOption("CONTRAPROPUESTA");
  await page.getByLabel("Monto propuesto").fill("180000");
  await page.getByLabel("Condiciones").fill("Entrega en 45 días.");
  await page.getByRole("button", { name: "Registrar contrapropuesta" }).click();
  await expect(page.getByText("Contrapropuesta #1").first()).toBeVisible();
  await expect(page.getByText("Inmutable").first()).toBeVisible();
});

test("mobile exposes visit and offline-safe activity actions", async ({ page }) => {
  test.skip(test.info().project.name !== "mobile", "Este escenario se valida en el proyecto mobile.");
  await page.goto("/inicio");
  await expect(page.locator(".mobile-bottom-nav")).toBeVisible();
  await page.getByRole("button", { name: "Registrar", exact: true }).click({ force: true });
  await expect(page.getByRole("dialog", { name: "Crear rápido" })).toBeVisible();
  await page.getByRole("button", { name: /Actividad Hecho ocurrido/ }).click();
  await expect(page.locator("h2").filter({ hasText: "Registrar actividad" })).toBeVisible();
});

test("the product shell does not expose implementation labels", async ({ page }) => {
  await page.goto("/inicio");
  await expect(page.getByText("Modo demo", { exact: true })).toHaveCount(0);
  await expect(page.getByText("BFF", { exact: true })).toHaveCount(0);
  await expect(page.getByText("V2", { exact: true })).toHaveCount(0);
  await expect(page.getByText("F2", { exact: true })).toHaveCount(0);
  await expect(page.locator("body")).not.toContainText("work package");
});

test("business routes keep implementation terminology out of the customer-facing UI", async ({ page }) => {
  for (const route of ["/inicio", "/contactos", "/inmuebles", "/publicaciones", "/captaciones", "/busquedas", "/compatibilidades", "/oportunidades", "/actividad", "/metricas", "/asistente", "/administracion", "/login"]) {
    await page.goto(route, { waitUntil: "domcontentloaded" });
    await expect(page.locator("body")).toBeVisible();
    await expect(page.locator("body")).not.toContainText(/\b(v2|f2|demo|bff)\b/i);
  }
});

test("deferred surfaces stay visible but disabled in the design catalog", async ({ page }) => {
  await page.goto("/__design");
  await expect(page.getByText("172", { exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Diferidas" }).click();
  await expect(page.getByText("F2", { exact: true }).first()).toBeVisible();
  await expect(page.getByText("Centro de novedades")).toBeVisible();
});

test("navega a Requiere atención (INI-02) y permite retornar a Inicio", async ({ page }) => {
  test.skip(test.info().project.name !== "chromium", "El flujo principal se valida en desktop.");
  await page.goto("/inicio");
  await page.getByRole("button", { name: "Ver requiere atención" }).click();
  await expect(page).toHaveURL(/\/inicio\?screen=INI-02/);
  await expect(page.getByRole("heading", { name: /Oportunidades para revisar/ })).toBeVisible();
  await page.getByRole("button", { name: "← Volver al inicio" }).click();
  await expect(page).toHaveURL(/\/inicio\?screen=INI-01/);
});

test("búsqueda global y búsqueda en tabla con insensibilidad a tildes", async ({ page }) => {
  test.skip(test.info().project.name !== "chromium", "Validación en desktop.");
  await page.goto("/contactos");
  await page.getByRole("button", { name: /Buscar en toda la instalación/ }).click();
  const dialog = page.getByRole("dialog", { name: "Búsqueda global" });
  await expect(dialog).toBeVisible();
  await dialog.getByRole("textbox").fill("norte");
  await expect(dialog.getByText("Estudio Norte SA")).toBeVisible();
  await dialog.getByRole("textbox").press("Enter");
  await expect(dialog).not.toBeVisible();
  await expect(page).toHaveURL(/\/contactos\?screen=PTY-06/);

  await page.goto("/contactos");
  const searchInput = page.getByPlaceholder("Buscar por nombre, teléfono o email");
  await searchInput.fill("carla");
  await expect(page.getByText("Carla Benítez")).toBeVisible();
  await expect(page.getByText("Estudio Norte SA")).not.toBeVisible();
  await page.getByRole("button", { name: "Limpiar búsqueda" }).click();
  await expect(page.getByText("Estudio Norte SA")).toBeVisible();
});

test("perfil vendedor no puede reasignar responsable en OPP-02 ni entrar a Métricas, pero responsable comercial sí", async ({ page }) => {
  test.skip(test.info().project.name !== "chromium", "Validación de permisos de rol en desktop.");
  await page.goto("/oportunidades?screen=OPP-02");
  const ownerSelect = page.getByLabel("Responsable de Carla Benítez · PH en Guardia Vieja");
  await expect(ownerSelect).toBeVisible();
  await expect(ownerSelect).toBeDisabled();

  // Métricas debe estar deshabilitado en navegación para vendedor
  const metricasItem = page.locator(".nav-item.nav-disabled", { hasText: "Métricas" });
  await expect(metricasItem).toBeVisible();

  // Si navega directamente a /metricas, ve pantalla de acceso restringido
  await page.goto("/metricas");
  await expect(page.getByRole("heading", { name: "Acceso restringido a Métricas" })).toBeVisible();
  await page.getByRole("button", { name: "Cambiar a Responsable comercial" }).click();
  await expect(page.getByRole("heading", { name: "Acceso restringido a Métricas" })).toHaveCount(0);

  // Con rol responsable comercial puede reasignar en OPP-02
  await page.goto("/oportunidades?screen=OPP-02");
  await expect(ownerSelect).toBeEnabled();
  await ownerSelect.selectOption("Lucía Ferrari");
  await expect(page.getByRole("status").filter({ hasText: "Responsable actualizado." })).toBeVisible();
  await expect(ownerSelect).toHaveValue("Lucía Ferrari");
});

test("mobile layout no tiene overflow horizontal y permite abrir el menú lateral desde Más", async ({ page }) => {
  test.skip(test.info().project.name !== "mobile", "Este escenario se valida en el proyecto mobile.");
  for (const url of ["/inicio", "/oportunidades?screen=OPP-01", "/oportunidades?screen=OPP-02", "/oportunidades?screen=OPP-05&entity=opp-1"]) {
    await page.goto(url);
    await expect(page.locator("body")).toBeVisible();
    const hasOverflow = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth);
    expect(hasOverflow).toBe(false);
  }

  await page.getByRole("button", { name: "Más", exact: true }).click();
  await expect(page.locator(".sidebar.is-mobile-open")).toBeVisible();
  await page.getByRole("button", { name: "Cerrar menú", exact: true }).click();
  await expect(page.locator(".sidebar.is-mobile-open")).toHaveCount(0);
});
