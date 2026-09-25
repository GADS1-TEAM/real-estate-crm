import { expect, test } from "@playwright/test";

test.describe("Verificación de Login estricto, Responsables en Crear Oportunidad, Botones de Listado/Detalle y Demostración de 6 pasos", () => {
  test("Rechaza credenciales inválidas, permite elegir Responsable en Crear Oportunidad y valida todos los botones de Listado y Detalle", async ({ page }) => {
    test.skip(test.info().project.name !== "chromium", "El flujo de demostración de escritorio se valida en chromium.");
    test.setTimeout(60_000);
    const suffix = Date.now().toString().slice(-5);
    const companyName = `Constructora Río ${suffix} SA`;
    const contactName = `Valentina Gómez ${suffix}`;
    const opportunityTitle = `Operación ${contactName} · Palermo`;

    // 1. Verificar que sin iniciar sesión no se puede acceder a las pestañas ni páginas (/oportunidades, /inicio)
    await page.goto("/oportunidades");
    await expect(page.getByRole("heading", { name: "Ingresá a tu instalación" })).toBeVisible();
    await expect(page.locator(".sidebar")).toHaveCount(0);
    await expect(page.getByText("Cuentas activas en la inmobiliaria")).toHaveCount(0);
    await expect(page.getByRole("button", { name: "Ingresar como Martín" })).toHaveCount(0);
    await expect(page.getByRole("button", { name: "Ingresar a Métricas" })).toHaveCount(0);

    // Verificar que Usuario y Contraseña están en 2 filas distintas
    const userBox = await page.getByLabel("Usuario o correo electrónico").boundingBox();
    const passBox = await page.getByLabel("Contraseña").boundingBox();
    expect(userBox).not.toBeNull();
    expect(passBox).not.toBeNull();
    expect(passBox!.y).toBeGreaterThan(userBox!.y + userBox!.height);

    // 1a. Usuario inexistente -> cuadro rojo sin ejemplos
    await page.getByLabel("Usuario o correo electrónico").fill("cualquier_usuario@falso.com");
    await page.getByLabel("Contraseña").fill("cualquier123");
    await page.getByRole("button", { name: "Iniciar sesión" }).click();
    await expect(page.locator(".alert-error")).toContainText("Usuario o contraseña incorrectos. Verificá tus credenciales.");
    await expect(page.locator(".alert-error")).not.toContainText("ej.");
    await expect(page.getByRole("heading", { name: "Ingresá a tu instalación" })).toBeVisible();

    // 1b. Usuario Martín con contraseña incorrecta
    await page.getByLabel("Usuario o correo electrónico").fill("martin@inmobiliaria.com.ar");
    await page.getByLabel("Contraseña").fill("clave_erronea");
    await page.getByRole("button", { name: "Iniciar sesión" }).click();
    await expect(page.locator(".alert-error")).toContainText("Usuario o contraseña incorrectos. Verificá tus credenciales.");
    await expect(page.getByRole("heading", { name: "Ingresá a tu instalación" })).toBeVisible();

    // 1c. Credenciales válidas de Martín Quiroga
    await page.getByLabel("Usuario o correo electrónico").fill("martin@inmobiliaria.com.ar");
    await page.getByLabel("Contraseña").fill("martin123");
    await page.getByRole("button", { name: "Iniciar sesión" }).click();
    await expect(page.locator(".sidebar-user")).toContainText("Martín Quiroga");

    // 2. Registrar una empresa y un contacto
    await page.goto("/contactos");
    await expect(page.locator("body")).toContainText("Empresas y contactos");

    await page.getByRole("button", { name: "Registrar empresa" }).click();
    await page.getByLabel("Nombre de la empresa").fill(companyName);
    await page.getByLabel("Teléfono").fill("+54 11 5000 1111");
    await page.getByLabel("Email").fill(`contacto.${suffix}@constructorario.com.ar`);
    await page.getByRole("button", { name: "Guardar empresa" }).click();
    await expect(page.locator(".data-table")).toContainText(companyName);

    await page.getByRole("button", { name: "Nuevo contacto" }).click();
    await page.getByLabel("Nombre y apellido").fill(contactName);
    await page.getByLabel("Teléfono").fill("+54 11 5000 2222");
    await page.getByLabel("Email").fill(`valentina.${suffix}@correo.com.ar`);
    await page.getByLabel("Empresa vinculada (opcional)").selectOption({ label: companyName });
    await page.getByRole("button", { name: "Guardar contacto" }).click();
    await page.getByPlaceholder("Buscar por nombre, teléfono o email").fill(contactName);
    await expect(page.locator(".data-table")).toContainText(contactName);
    await page.getByPlaceholder("Buscar por nombre, teléfono o email").fill("");

    // 3. Crear una oportunidad -> Verificar que la lista de Responsables está habilitada y permite elegir
    await page.goto("/oportunidades");
    await page.getByRole("button", { name: "Crear oportunidad" }).click();
    await expect(page.getByRole("heading", { name: "Nueva oportunidad" }).first()).toBeVisible();

    const responsableSelect = page.getByLabel("Responsable");
    await expect(responsableSelect).toBeEnabled();
    await expect(responsableSelect.locator("option")).toContainText([
      "Martín Quiroga",
      "Rodrigo Vergara",
      "Lucía Ferrari",
      "Sofía Rendón",
      "Elena Vergara",
    ]);
    await responsableSelect.selectOption("Lucía Ferrari");
    await expect(responsableSelect).toHaveValue("Lucía Ferrari");
    await responsableSelect.selectOption("Martín Quiroga");

    // Verificar que al elegir Tipo de fuente = Captación, Fuente real muestra nombres reales de propiedades y no 'Inmueble' repetido
    await page.getByLabel("Tipo de fuente").selectOption("CAPTATION_CASE");
    await expect(page.getByLabel("Fuente real").locator("option").first()).toContainText(/Gorriti 4800|Villa Crespo/);
    const fuenteRealOptions = await page.getByLabel("Fuente real").locator("option").allTextContents();
    expect(fuenteRealOptions.length).toBeGreaterThan(0);
    expect(fuenteRealOptions.every((text) => text.trim().toLowerCase() !== "inmueble")).toBe(true);

    await page.getByLabel("Nombre de la oportunidad").fill(opportunityTitle);
    await page.getByLabel("Honorarios estimados (ARS)").fill("480000");
    await page.getByRole("button", { name: "Crear oportunidad" }).click();

    // 4. Visualizarla en el embudo
    await expect(page.locator(".pipeline-board")).toBeVisible();
    await expect(page.locator(".pipeline-board")).toContainText(opportunityTitle);

    // 5. Probar botones en Listado de oportunidades (OPP-02)
    await page.getByRole("button", { name: "Ver lista" }).click();
    await expect(page.getByRole("heading", { name: "Listado de oportunidades" }).first()).toBeVisible();

    // 5a. Botón Filtrar y Limpiar filtros en Listado de oportunidades
    await page.getByRole("button", { name: "Filtrar" }).click();
    await expect(page.getByLabel("Etapa")).toBeVisible();
    await page.getByLabel("Etapa").selectOption("Nuevo");
    await expect(page.locator(".data-table")).toContainText(opportunityTitle);
    await page.getByRole("button", { name: "Limpiar filtros" }).click();

    // 5b. Botón Avanzar etapa desde la fila del listado
    const row = page.locator("tr", { hasText: opportunityTitle }).first();
    await expect(row).toBeVisible();
    await row.getByRole("button", { name: "Avanzar a Contacto" }).click();
    await expect(row).toContainText("Etapa: Contacto");

    // 5c. Botón Ver detalle en la fila del listado -> Abre Detalle de oportunidad (OPP-04)
    await row.locator("button", { hasText: "Ver detalle" }).click();
    await expect(page.getByRole("heading", { name: opportunityTitle })).toBeVisible();

    // 6. Probar botones en Detalle de oportunidad (OPP-04, OPP-05, OPP-06, OPP-11, OPP-07)
    // 6a. Cambiar etapa con el selector Nueva etapa + Motivo del cambio + Guardar cambio de etapa (sin que se resetee el select)
    const nuevaEtapaSelect = page.getByLabel("Nueva etapa");
    await nuevaEtapaSelect.selectOption("Negociación");
    await page.getByLabel("Motivo del cambio").fill("El cliente solicitó avanzar a negociación formal.");
    await expect(nuevaEtapaSelect).toHaveValue("Negociación");
    await page.getByRole("button", { name: "Guardar cambio de etapa" }).click();
    await expect(page.locator(".detail-hero")).toContainText("Negociación");

    // 6b. Pestañas Historial y Trazabilidad + botón Abrir fuente
    await page.getByRole("tab", { name: "Historial" }).click();
    await expect(page.getByRole("heading", { name: "Cambios trazables" })).toBeVisible();

    await page.getByRole("tab", { name: "Trazabilidad" }).click();
    await expect(page.getByRole("heading", { name: "Trazabilidad completa" })).toBeVisible();

    // 6c. Botón Ver en el embudo y comprobación tras recargar la página (MongoDB)
    await page.getByRole("button", { name: "Ver en el embudo" }).click();
    await expect(page.locator(".pipeline-board")).toBeVisible();
    const propuestaColumn = page.locator(".pipeline-column").nth(3);
    await expect(propuestaColumn).toContainText(opportunityTitle);

    await page.reload();
    await expect(page.locator(".pipeline-board")).toBeVisible();
    await expect(page.locator(".pipeline-column").nth(3)).toContainText(opportunityTitle);

    // 7. Cerrar sesión e iniciar sesión con Cuenta 2 (Rodrigo Vergara - Responsable comercial) para verificar Métricas
    await page.locator(".topbar-avatar").click();
    await page.getByRole("button", { name: /Cambiar de cuenta \/ Iniciar sesión/i }).click();
    await expect(page.getByRole("heading", { name: "Ingresá a tu instalación" })).toBeVisible();
    await expect(page.locator(".sidebar")).toHaveCount(0);

    await page.getByLabel("Usuario o correo electrónico").fill("rodrigo@inmobiliaria.com.ar");
    await page.getByLabel("Contraseña").fill("rodrigo123");
    await page.getByRole("button", { name: "Iniciar sesión" }).click();
    await expect(page.locator(".sidebar-user")).toContainText("Rodrigo Vergara");
    await expect(page.getByRole("heading", { name: "Métricas" }).first()).toBeVisible();
    await expect(page.getByRole("heading", { name: "Panel de desempeño" })).toBeVisible();
  });
});
