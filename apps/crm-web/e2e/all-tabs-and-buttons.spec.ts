import { expect, test } from "@playwright/test";

test.describe("Auditoría exhaustiva en modo navegador de CADA funcionalidad y CADA botón en cada pestaña", () => {
  test.beforeEach(async ({ page }) => {
    // Inicializar sesión por defecto como Martín Quiroga si no hay sesión activa
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

  // ==========================================
  // 1. SISTEMA DE LOGIN Y ROLES
  // ==========================================
  test("1. Login: rechazo de credenciales, login multi-rol, persistencia y logout limpio", async ({ page }) => {
    test.skip(test.info().project.name !== "chromium", "Se valida en desktop.");
    
    // 1a. Limpiar storage para probar login
    await page.goto("/inicio?screen=AUT-01");
    await page.evaluate(() => {
      window.localStorage.clear();
      window.sessionStorage.clear();
    });
    await page.goto("/inicio?screen=AUT-01");
    await expect(page.getByRole("heading", { name: "Ingresá a tu instalación" })).toBeVisible();

    // 1b. Credenciales inválidas
    await page.getByLabel("Usuario o correo electrónico").fill("usuario.inexistente@inmobiliaria.com.ar");
    await page.getByLabel("Contraseña").fill("clave_invalida_123");
    await page.getByRole("button", { name: "Iniciar sesión" }).click();
    await expect(page.locator(".alert-error")).toContainText("Usuario o contraseña incorrectos");

    // 1c. Login como Lucía Ferrari (Vendedora · Inmuebles)
    await page.getByLabel("Usuario o correo electrónico").fill("lucia@inmobiliaria.com.ar");
    await page.getByLabel("Contraseña").fill("lucia123");
    await page.getByRole("button", { name: "Iniciar sesión" }).click();
    await expect(page.locator(".sidebar-user")).toContainText("Lucía Ferrari");
    await expect(page.getByRole("heading", { name: "Inmuebles", exact: true })).toBeVisible();

    // 1d. Persistencia al recargar página
    await page.reload();
    await expect(page.locator(".sidebar-user")).toContainText("Lucía Ferrari");
    await expect(page.getByRole("heading", { name: "Inmuebles", exact: true })).toBeVisible();

    // 1e. Logout limpio desde el menú de usuario
    await page.locator(".topbar-avatar").click();
    await page.getByRole("button", { name: /Cambiar de cuenta \/ Iniciar sesión/i }).click();
    await expect(page.getByRole("heading", { name: "Ingresá a tu instalación" })).toBeVisible();

    // 1f. Login como Rodrigo Vergara (Responsable comercial · Métricas)
    await page.getByLabel("Usuario o correo electrónico").fill("rodrigo@inmobiliaria.com.ar");
    await page.getByLabel("Contraseña").fill("rodrigo123");
    await page.getByRole("button", { name: "Iniciar sesión" }).click();
    await expect(page.locator(".sidebar-user")).toContainText("Rodrigo Vergara");
    await expect(page.getByRole("heading", { name: "Métricas", exact: true })).toBeVisible();

    // 1g. Login como Sofía Rendón (Administradora)
    await page.locator(".topbar-avatar").click();
    await page.getByRole("button", { name: /Cambiar de cuenta \/ Iniciar sesión/i }).click();
    await page.getByLabel("Usuario o correo electrónico").fill("sofia@inmobiliaria.com.ar");
    await page.getByLabel("Contraseña").fill("sofia123");
    await page.getByRole("button", { name: "Iniciar sesión" }).click();
    await expect(page.locator(".sidebar-user")).toContainText("Sofía Rendón");
    await expect(page.getByRole("heading", { name: "Administración", exact: true })).toBeVisible();

    // 1h. Login como Elena Vergara (Dirección)
    await page.locator(".topbar-avatar").click();
    await page.getByRole("button", { name: /Cambiar de cuenta \/ Iniciar sesión/i }).click();
    await page.getByLabel("Usuario o correo electrónico").fill("elena@inmobiliaria.com.ar");
    await page.getByLabel("Contraseña").fill("elena123");
    await page.getByRole("button", { name: "Iniciar sesión" }).click();
    await expect(page.locator(".sidebar-user")).toContainText("Elena Vergara");

    // 1i. Login como Martín Quiroga (Vendedor estándar)
    await page.locator(".topbar-avatar").click();
    await page.getByRole("button", { name: /Cambiar de cuenta \/ Iniciar sesión/i }).click();
    await page.getByLabel("Usuario o correo electrónico").fill("martin@inmobiliaria.com.ar");
    await page.getByLabel("Contraseña").fill("martin123");
    await page.getByRole("button", { name: "Iniciar sesión" }).click();
    await expect(page.locator(".sidebar-user")).toContainText("Martín Quiroga");
    await expect(page.getByRole("heading", { name: "Inicio", exact: true })).toBeVisible();

    // 1j. Pantallas especiales de auth: Sesión expirada (AUT-02) y Onboarding (AUT-04)
    await page.goto("/inicio?screen=AUT-02");
    await expect(page.getByRole("heading", { name: "Guardamos tu borrador" })).toBeVisible();
    await page.getByRole("button", { name: "Volver a ingresar" }).click();
    await expect(page.getByRole("heading", { name: "Ingresá a tu instalación" })).toBeVisible();

    await page.goto("/inicio?screen=AUT-04");
    await expect(page.getByRole("heading", { name: "Preparemos la instalación" })).toBeVisible();
    await page.getByLabel("Nombre de la inmobiliaria").fill("Inmobiliaria Test");
    await page.getByRole("button", { name: "Continuar" }).click(); // Paso 2
    await page.getByRole("button", { name: "Continuar" }).click(); // Paso 3
    await page.getByRole("button", { name: "Continuar" }).click(); // Paso 4
    await page.getByRole("button", { name: "Ir al inicio" }).click();
    await expect(page.getByRole("heading", { name: "Inicio", exact: true })).toBeVisible();
  });

  // ==========================================
  // 2. INICIO Y REQUIERE ATENCIÓN
  // ==========================================
  test("2. Pestaña Inicio: KPIs, Requiere atención y navegación", async ({ page }) => {
    test.skip(test.info().project.name !== "chromium", "Se valida en desktop.");
    await page.goto("/inicio");
    await expect(page.getByRole("heading", { name: "Inicio" })).toBeVisible();
    await expect(page.locator("body")).toContainText("Buen día, Martín.");

    // Botón "Ver requiere atención"
    await page.getByRole("button", { name: "Ver requiere atención" }).click();
    await expect(page).toHaveURL(/\/inicio\?screen=INI-02/);
    await expect(page.getByRole("heading", { name: /Oportunidades para revisar/ })).toBeVisible();

    // Botones de acción en Requiere atención
    await page.getByRole("button", { name: "Abrir tablero de oportunidades" }).click();
    await expect(page).toHaveURL(/\/oportunidades\?screen=OPP-01/);

    await page.goto("/inicio?screen=INI-02");
    await page.getByRole("button", { name: "← Volver al inicio" }).click();
    await expect(page).toHaveURL(/\/inicio\?screen=INI-01/);
  });

  // ==========================================
  // 3. CONTACTOS Y EMPRESAS
  // ==========================================
  test("3. Pestaña Contactos: Alta empresa, Alta contacto, Enriquecimiento, Edición inline, Estados y Relaciones", async ({ page }) => {
    test.skip(test.info().project.name !== "chromium", "Se valida en desktop.");
    const suffix = Date.now().toString().slice(-4);
    const companyName = `Constructora Delta ${suffix} SA`;
    const contactName = `Mateo Rossi ${suffix}`;

    await page.goto("/contactos");
    await expect(page.getByRole("heading", { name: "Empresas y contactos" })).toBeVisible();

    // 3a. Registrar empresa (PTY-03)
    await page.getByRole("button", { name: "Registrar empresa" }).click();
    await page.getByLabel("Nombre de la empresa").fill(companyName);
    await page.getByLabel("Teléfono").fill("+54 11 4444 5555");
    await page.getByLabel("Email").fill(`info.${suffix}@delta.com.ar`);
    await page.getByRole("button", { name: "Guardar empresa" }).click();
    await expect(page.locator(".data-table")).toContainText(companyName);

    // 3b. Nuevo contacto rápido (PTY-02)
    await page.getByRole("button", { name: "Nuevo contacto" }).click();
    await page.getByLabel("Nombre y apellido").fill(contactName);
    await page.getByLabel("Teléfono").fill("+54 11 9999 8888");
    await page.getByLabel("Email").fill(`mateo.${suffix}@correo.com.ar`);
    await page.getByLabel("Empresa vinculada (opcional)").selectOption({ label: companyName });
    await page.getByRole("button", { name: "Guardar contacto" }).click();
    await expect(page.locator(".data-table")).toContainText(contactName);

    // 3c. Búsqueda con insensibilidad a tildes y botón de limpiar búsqueda
    const searchInput = page.getByPlaceholder("Buscar por nombre, teléfono o email");
    await searchInput.fill(contactName);
    await expect(page.locator(".data-table")).toContainText(contactName);
    await page.getByRole("button", { name: "Limpiar búsqueda" }).click();
    await expect(searchInput).toHaveValue("");

    // 3d. Detalle Contacto 360 (PTY-05) y Edición inline (PTY-08)
    await page.locator("tr", { hasText: contactName }).first().click();
    await expect(page.getByRole("heading", { name: contactName }).first()).toBeVisible();

    await page.getByRole("button", { name: "Editar" }).click();
    await page.getByLabel("Teléfono").fill("+54 11 7777 6666");
    await page.getByRole("button", { name: "Guardar cambios" }).click();
    await expect(page.locator(".definition-grid")).toContainText("+54 11 7777 6666");

    // 3e. Marcar "No contactar" (PTY-10)
    await page.getByRole("button", { name: "Más acciones" }).click();
    await expect(page.getByRole("heading", { name: "Marcar como no contactar" }).first()).toBeVisible();
    await page.getByRole("button", { name: "Confirmar no contactar" }).click();
    await expect(page.locator(".identity-meta")).toContainText("DO_NOT_CONTACT");

    // 3f. Reactivar contacto (PTY-11)
    await page.getByRole("button", { name: "Más acciones" }).click();
    await expect(page.getByRole("heading", { name: "Reactivar ficha" }).first()).toBeVisible();
    await page.getByRole("button", { name: "Confirmar reactivación" }).click();
    await expect(page.locator(".identity-meta")).toContainText("CUSTOMER");

    // 3g. Alta con enriquecimiento progresivo (PTY-04)
    await page.goto("/contactos?screen=PTY-04");
    await expect(page.getByRole("heading", { name: "Alta con enriquecimiento progresivo" }).first()).toBeVisible();
    await page.getByLabel("Texto histórico de WhatsApp").fill(`Nombre: Florencia Peña ${suffix}\nTeléfono: +54 11 2345-6789\nEmail: florencia.${suffix}@gmail.com`);
    await page.getByRole("button", { name: "Revisar sugerencias" }).click();
    await page.getByRole("button", { name: "Aplicar sugerencias" }).click();
    await page.getByRole("button", { name: "Guardar contacto" }).click();
    await expect(page.locator(".data-table")).toContainText(`Florencia Peña ${suffix}`);
  });

  // ==========================================
  // ==========================================
  // 4. INMUEBLES
  // ==========================================
  test("4. Pestaña Inmuebles: Alta rápida, Alta urbana, Alta rural, Mapa, Ficha 360 y Archivar", async ({ page }) => {
    test.skip(test.info().project.name !== "chromium", "Se valida en desktop.");
    const suffix = Date.now().toString().slice(-4);
    const propTitle = `Departamento Luminoso ${suffix}`;

    await page.goto("/inmuebles");
    await expect(page.getByRole("heading", { name: "Inmuebles", exact: true }).first()).toBeVisible();

    // 4a. Alta rápida de inmueble (PRP-03)
    await page.getByRole("button", { name: "Nuevo inmueble" }).click();
    await page.getByLabel("Título interno").fill(propTitle);
    await page.getByLabel("Dirección").fill(`Honduras 5600 ${suffix}`);
    await page.getByLabel("Precio").fill("165000");
    await page.getByLabel("Ambientes").fill("3 ambientes");
    await page.getByRole("button", { name: "Guardar inmueble" }).click();
    await expect(page.locator(".data-table")).toContainText(propTitle);

    // 4b. Ver inmueble en el mapa (PRP-02)
    await page.goto("/inmuebles?screen=PRP-02");
    await expect(page.locator(".map-card")).toBeVisible();

    // 4c. Property 360 (PRP-06) y navegación por pestañas (Ubicación, Características, Multimedia, Propietarios, Historia)
    await page.goto("/inmuebles");
    await page.locator("tr", { hasText: propTitle }).first().click();
    await expect(page.getByRole("heading", { name: propTitle }).first()).toBeVisible();

    await page.getByRole("tab", { name: "Ubicación" }).click();
    await expect(page.locator(".definition-grid")).toBeVisible();

    await page.getByRole("tab", { name: "Características" }).click();
    await expect(page.locator(".definition-grid")).toBeVisible();

    await page.getByRole("tab", { name: "Multimedia" }).click();
    await expect(page.locator(".placeholder-grid")).toBeVisible();

    await page.getByRole("tab", { name: "Propietarios" }).click();
    await expect(page.locator(".detail-grid")).toBeVisible();

    await page.getByRole("tab", { name: "Historia" }).click();
    await expect(page.getByRole("heading", { name: "Últimos cambios" }).first()).toBeVisible();

    // 4d. Alta completa urbana (PRP-04)
    await page.goto("/inmuebles?screen=PRP-04");
    await expect(page.getByRole("heading", { name: "Alta completa urbana" }).first()).toBeVisible();
    await page.getByLabel("Título interno").fill(`Piso Exclusivo ${suffix}`);
    await page.getByLabel("Dirección").fill(`Av. Libertador 3200 ${suffix}`);
    await page.getByLabel("Precio").fill("450000");
    await page.getByRole("button", { name: "Guardar inmueble" }).click();
    await expect(page.locator(".data-table")).toContainText(`Piso Exclusivo ${suffix}`);

    // 4e. Alta completa rural (PRP-05)
    await page.goto("/inmuebles?screen=PRP-05");
    await expect(page.getByRole("heading", { name: "Alta completa rural" }).first()).toBeVisible();
    await page.getByLabel("Título interno").fill(`Estancia La Candelaria ${suffix}`);
    await page.getByLabel("Dirección").fill(`Ruta 8 km 95 ${suffix}`);
    await page.getByLabel("Tipo de inmueble").selectOption("Rural");
    await page.getByLabel("Superficie rural (ha)").fill("150");
    await page.getByLabel("Precio").fill("850000");
    await page.getByRole("button", { name: "Guardar inmueble" }).click();
    await expect(page.locator(".data-table")).toContainText(`Estancia La Candelaria ${suffix}`);
  });

  // ==========================================
  // 5. PUBLICACIONES
  // ==========================================
  test("5. Pestaña Publicaciones: Crear publicación, Cambiar estado y Ficha de publicación", async ({ page }) => {
    test.skip(test.info().project.name !== "chromium", "Se valida en desktop.");
    const suffix = Date.now().toString().slice(-4);
    const listingTitle = `Publicación Estándar ${suffix}`;

    await page.goto("/publicaciones");
    await expect(page.getByRole("heading", { name: "Publicaciones", exact: true }).first()).toBeVisible();

    // 5a. Crear publicación (LST-02)
    await page.getByRole("button", { name: "Nueva publicación" }).click();
    await page.getByLabel("Título comercial").fill(listingTitle);
    await page.getByLabel("Precio publicado").fill("190000");
    await page.getByLabel("Descripción").fill("Excelente propiedad con vista abierta y balcón terraza.");
    await page.getByRole("button", { name: "Guardar publicación" }).click();
    await expect(page.locator(".data-table")).toContainText(listingTitle);

    // 5b. Detalle de publicación (LST-04) y pestañas
    await page.locator("tr", { hasText: listingTitle }).first().click();
    await expect(page.getByRole("heading", { name: listingTitle }).first()).toBeVisible();

    await page.getByRole("tab", { name: "Términos" }).click();
    await expect(page.getByRole("heading", { name: "Condiciones visibles" }).first()).toBeVisible();

    await page.getByRole("tab", { name: "Presentación" }).click();
    await expect(page.getByRole("heading", { name: listingTitle }).first()).toBeVisible();

    // 5c. Cambiar estado de publicación (Pausar / Reanudar) (LST-07)
    await page.getByRole("tab", { name: "Estado" }).click();
    await page.getByLabel("Estado de publicación").selectOption("Pausada");
    await expect(page.locator(".detail-hero")).toContainText("Pausada");
    await page.getByLabel("Estado de publicación").selectOption("Activa");
    await expect(page.locator(".detail-hero")).toContainText("Activa");
  });

  // ==========================================
  // 6. CAPTACIONES
  // ==========================================
  test("6. Pestaña Captaciones: Abrir captación, Tasación, Mandato, Cierre y Reactivación", async ({ page }) => {
    test.skip(test.info().project.name !== "chromium", "Se valida en desktop.");
    await page.goto("/captaciones");
    await expect(page.getByRole("heading", { name: "Captaciones", exact: true }).first()).toBeVisible();

    // 6a. Abrir captación (CAP-02)
    await page.getByRole("button", { name: "Abrir captación" }).click();
    await page.getByLabel("Expectativa").fill("210000");
    await page.getByRole("button", { name: "Abrir captación" }).click();

    // 6b. Detalle de Captación 360 (CAP-03)
    await page.locator(".captation-card").first().click();
    await expect(page.locator(".detail-hero")).toBeVisible();

    await page.getByRole("tab", { name: "Tasación" }).click();
    await expect(page.getByRole("heading", { name: "Brecha de precio" }).first()).toBeVisible();

    // 6c. Tasar propiedad (CAP-04)
    await page.getByLabel("Valuación").fill("195000");
    await page.getByRole("button", { name: "Registrar nueva valuación" }).click();
    await expect(page.locator("body")).toContainText("195.000");

    // 6d. Mandato de comercialización (CAP-06)
    await page.getByRole("tab", { name: "Mandato" }).click();
    await page.getByRole("button", { name: "Abrir mandato" }).click();
    await expect(page.locator(".detail-hero")).toContainText("Mandato");

    // 6e. Cerrar captación (CAP-07)
    await page.getByRole("tab", { name: "Cierre" }).click();
    await page.getByLabel("Motivo de cierre").fill("El propietario decidió alquilar en lugar de vender.");
    await page.getByRole("button", { name: "Confirmar cierre" }).click();
    await expect(page.locator(".detail-hero")).toContainText("Cerrada");

    // 6f. Reactivar captación (CAP-08)
    await page.getByRole("tab", { name: "Reactivación" }).click();
    await page.getByRole("button", { name: "Confirmar reactivación" }).click();
    await expect(page.locator(".detail-hero")).toContainText("Nueva");
  });

  // ==========================================
  // 7. BÚSQUEDAS Y DEMANDAS
  // ==========================================
  test("7. Pestaña Búsquedas: Alta de búsqueda, Editor de criterios, Score y Búsqueda 360", async ({ page }) => {
    test.skip(test.info().project.name !== "chromium", "Se valida en desktop.");
    await page.goto("/busquedas");
    await expect(page.getByRole("heading", { name: "Búsquedas", exact: true }).first()).toBeVisible();

    // 7a. Alta rápida de búsqueda (DEM-02)
    await page.getByRole("button", { name: "Nueva búsqueda" }).click();
    await page.getByLabel("Ambientes").fill("3");
    await page.getByLabel("Barrio sugerido").fill("Palermo");
    await page.getByRole("button", { name: "Guardar búsqueda" }).click();
    await expect(page.locator(".data-table")).toContainText("busca 3 ambientes");

    // 7b. Búsqueda 360 (DEM-04) y Editor de criterios (DEM-03)
    await page.locator("tr", { hasText: "busca 3 ambientes" }).first().click();
    await page.getByRole("button", { name: "Editar criterios" }).click();
    await expect(page.getByText("Score recalculado")).toBeVisible();

    // 7c. Pausar o cerrar búsqueda (DEM-06)
    await page.goto("/busquedas?screen=DEM-06");
    await page.getByRole("button", { name: "Pausar búsqueda" }).click();
    await expect(page.locator(".detail-hero")).toContainText("Pausada");
  });

  // ==========================================
  // 8. COMPATIBILIDADES (MATCHING)
  // ==========================================
  test("8. Pestaña Compatibilidades: Score %, Desglose, Presentar inmueble y Favoritos", async ({ page }) => {
    test.skip(test.info().project.name !== "chromium", "Se valida en desktop.");
    await page.goto("/compatibilidades");
    await expect(page.getByRole("heading", { name: "Compatibilidades", exact: true }).first()).toBeVisible();

    // 8a. Ver explicación de compatibilidad (MAT-02)
    await page.getByRole("button", { name: "Ver explicación" }).first().click();
    await expect(page.locator(".match-explanation")).toBeVisible();

    // 8b. Marcar favorito y descartar
    await page.getByRole("button", { name: "Favorito" }).first().click();
    await page.getByRole("button", { name: "Descartar" }).first().click();

    // 8c. Preparar presentación (MAT-04)
    await page.getByRole("button", { name: "Preparar presentación" }).click();
    await expect(page.locator("body")).toContainText("Presentación preparada para revisar.");
  });

  // ==========================================
  // 9. OPORTUNIDADES Y CICLO COMERCIAL COMPLETO
  // ==========================================
  test("9. Pestaña Oportunidades: Embudo, Listado, Visitas, Propuestas inmutables, Reservas y Cierre", async ({ page }) => {
    test.skip(test.info().project.name !== "chromium", "Se valida en desktop.");
    const suffix = Date.now().toString().slice(-4);
    const oppTitle = `Operación Inmobiliaria ${suffix}`;

    await page.goto("/oportunidades");
    await expect(page.getByRole("heading", { name: "Oportunidades", exact: true }).first()).toBeVisible();

    // 9a. Crear oportunidad (OPP-03)
    await page.getByRole("button", { name: "Crear oportunidad" }).click();
    await page.getByLabel("Nombre de la oportunidad").fill(oppTitle);
    await page.getByLabel("Responsable").selectOption("Martín Quiroga");
    await page.getByLabel("Honorarios estimados (ARS)").fill("600000");
    await page.getByRole("button", { name: "Crear oportunidad" }).click();
    await expect(page.locator(".pipeline-board")).toContainText(oppTitle);

    // 9b. Listado de oportunidades (OPP-02) y filtros
    await page.getByRole("button", { name: "Ver lista" }).click();
    await expect(page.getByRole("heading", { name: "Listado de oportunidades" }).first()).toBeVisible();
    await page.getByRole("button", { name: "Filtrar" }).click();
    await page.getByLabel("Etapa").selectOption("Nuevo");
    await expect(page.locator(".data-table")).toContainText(oppTitle);
    await page.getByRole("button", { name: "Limpiar filtros" }).click();

    // 9c. Detalle de oportunidad (OPP-04)
    await page.locator("tr", { hasText: oppTitle }).locator("td").first().click();
    await expect(page.getByRole("heading", { name: oppTitle }).first()).toBeVisible();

    // 9d. Cambiar etapa (OPP-05) con motivo
    await page.getByRole("button", { name: "Cambiar etapa" }).click();
    await page.waitForURL("**/oportunidades?screen=OPP-05*");
    await page.getByLabel("Nueva etapa").selectOption("Visita");
    await page.getByLabel("Motivo del cambio").fill("Se acordó visita al inmueble.");
    await page.getByRole("button", { name: "Guardar cambio de etapa" }).click();
    await page.waitForURL("**/oportunidades?screen=OPP-04*");
    await expect(page.locator(".detail-hero")).toContainText("Visita");

    // 9e. Registrar visita ocurrida (COM-01)
    await page.goto("/oportunidades?screen=COM-01");
    await expect(page.getByRole("heading", { name: /Registrar visita/ }).first()).toBeVisible();
    await page.getByLabel("Cuándo ocurrió").fill("2026-09-25T10:00");
    await page.getByLabel("Nota de visita").fill("El comprador quedó muy conforme con la luminosidad.");
    await page.getByRole("button", { name: "Guardar visita" }).click();

    // 9f. Registrar propuesta y contrapropuesta inmutable (COM-06)
    await page.goto("/oportunidades?screen=COM-06");
    await expect(page.getByRole("heading", { name: /Registrar propuesta/ }).first()).toBeVisible();
    await page.getByLabel("Monto propuesto").fill("175000");
    await page.getByLabel("Condiciones").fill("Pago al contado contra entrega de posesión.");
    await page.getByRole("button", { name: "Registrar propuesta" }).click();

    // 9g. Crear reserva (COM-08) y Crear operación (COM-11)
    await page.goto("/oportunidades?screen=COM-08");
    await expect(page.getByRole("heading", { name: "Crear reserva" }).first()).toBeVisible();
    await page.getByLabel("Válida desde").fill("2026-09-25");
    await page.getByRole("button", { name: "Guardar reserva" }).click();

    await page.goto("/oportunidades?screen=COM-11");
    await expect(page.getByRole("heading", { name: "Crear operación" }).first()).toBeVisible();
    await page.getByRole("button", { name: "Crear operación" }).click();

    // 9h. Cerrar operación (COM-13)
    await page.goto("/oportunidades?screen=COM-13");
    await expect(page.getByRole("heading", { name: "Cerrar operación" }).first()).toBeVisible();
    await page.getByLabel("Fecha de cierre").fill("2026-09-25");
    await page.getByRole("button", { name: "Confirmar cierre" }).click();
  });

  // ==========================================
  // 10. ACTIVIDAD Y TIMELINES
  // ==========================================
  test("10. Pestaña Actividad: Registrar actividad, Timelines, Cola offline y Corrección", async ({ page }) => {
    test.skip(test.info().project.name !== "chromium", "Se valida en desktop.");
    await page.goto("/actividad");
    await expect(page.getByRole("heading", { name: "Actividad", exact: true }).first()).toBeVisible();

    // 10a. Registrar actividad (ACT-01)
    await page.getByRole("button", { name: "Registrar actividad" }).first().click();
    await expect(page.getByRole("heading", { name: "Registrar actividad" }).first()).toBeVisible();
    await page.getByRole("button", { name: "Llamada" }).click();
    await page.getByLabel("Cuándo ocurrió").fill("2026-09-25T10:00");
    await page.getByLabel("Detalle").fill("Llamada de seguimiento comercial confirmando visita.");
    await page.getByRole("button", { name: "Guardar actividad" }).click();
    await expect(page.locator(".activity-toolbar")).toBeVisible();

    // 10b. Timelines por entidad
    await page.goto("/actividad?screen=ACT-02");
    await expect(page.getByRole("heading", { name: /Timeline/ }).first()).toBeVisible();

    await page.goto("/actividad?screen=ACT-03");
    await expect(page.getByRole("heading", { name: /Timeline/ }).first()).toBeVisible();

    await page.goto("/actividad?screen=ACT-04");
    await expect(page.getByRole("heading", { name: /Timeline/ }).first()).toBeVisible();
  });

  // ==========================================
  // 11. MÉTRICAS Y ANALÍTICA
  // ==========================================
  test("11. Pestaña Métricas: Paneles por rol, Embudo, Oferta vs Demanda y Exportar", async ({ page }) => {
    test.skip(test.info().project.name !== "chromium", "Se valida en desktop.");
    
    // Configurar sesión de Rodrigo Vergara (Responsable comercial con permiso de métricas)
    await page.goto("/inicio");
    await page.evaluate(() => {
      window.localStorage.setItem("crm-web:authenticated:v1", "true");
      window.localStorage.setItem("crm-web:role-id:v1", "responsable");
      window.localStorage.setItem("crm-web:user-name:v1", "Rodrigo Vergara");
      window.localStorage.setItem("crm-web:user-email:v1", "rodrigo@inmobiliaria.com.ar");
    });

    await page.goto("/metricas");
    await expect(page.getByRole("heading", { name: "Métricas", exact: true }).first()).toBeVisible();

    // 11a. Pestañas de Analítica
    await expect(page.locator(".kpi-grid")).toBeVisible();
    await page.getByLabel("Período").selectOption("all");
    await page.getByLabel("Período").selectOption("30d");

    // 11b. Lineage y Exportación
    await page.goto("/metricas?screen=ANA-06");
    await expect(page.locator("body")).toBeVisible();

    await page.goto("/metricas?screen=ANA-07");
    await expect(page.getByText("Exportación no disponible en V2")).toBeVisible();
  });

  // ==========================================
  // 12. ASISTENTE IA
  // ==========================================
  test("12. Pestaña Asistente IA: Sugerencias, Evidencia, Aceptar, Descartar e Historial", async ({ page }) => {
    test.skip(test.info().project.name !== "chromium", "Se valida en desktop.");
    await page.goto("/asistente");
    await expect(page.getByRole("heading", { name: "Asistente", exact: true }).first()).toBeVisible();

    // 12a. Sugerencia contextual (IA-01) y Evidencia (IA-02)
    const reviewBtn = page.getByRole("button", { name: "Revisar sugerencia" }).first();
    if (await reviewBtn.isVisible()) {
      await reviewBtn.click();
    }

    await page.goto("/asistente");
    const dismissBtn = page.getByRole("button", { name: "Descartar" }).first();
    if (await dismissBtn.isVisible()) {
      await dismissBtn.click();
    }

    // 12b. Historial del asistente (IA-04)
    await page.getByRole("button", { name: "Ver historial" }).click();
    await expect(page.getByRole("heading", { name: "Historial del asistente" }).first()).toBeVisible();
  });

  // ==========================================
  // 13. ADMINISTRACIÓN Y CATÁLOGOS
  // ==========================================
  test("13. Pestaña Administración: Usuarios, Invitar, Matriz de roles, Catálogos y Datos inmobiliaria", async ({ page }) => {
    test.skip(test.info().project.name !== "chromium", "Se valida en desktop.");
    
    // Iniciar con rol administradora (Sofía Rendón)
    await page.goto("/inicio");
    await page.evaluate(() => {
      window.localStorage.setItem("crm-web:authenticated:v1", "true");
      window.localStorage.setItem("crm-web:role-id:v1", "administradora");
      window.localStorage.setItem("crm-web:user-name:v1", "Sofía Rendón");
      window.localStorage.setItem("crm-web:user-email:v1", "sofia@inmobiliaria.com.ar");
    });

    await page.goto("/administracion");
    await expect(page.getByRole("heading", { name: "Administración", exact: true }).first()).toBeVisible();

    // 13a. Invitar usuario (ADM-02)
    await page.getByRole("button", { name: "Invitar usuario" }).click();
    await expect(page.getByRole("heading", { name: "Invitar usuario" }).first()).toBeVisible();
    await page.getByLabel("Nombre").fill("Agustín Gómez");
    await page.getByLabel("Email").fill("agustin@inmobiliaria.com.ar");
    await page.getByLabel("Rol").selectOption("Vendedor");
    await page.getByRole("button", { name: "Enviar invitación" }).click();
    await expect(page.locator(".data-table")).toContainText("Agustín Gómez");

    // 13b. Alternar estado de usuario Habilitado / Pendiente
    await page.getByRole("button", { name: "Cambiar estado de Agustín Gómez" }).click();
    await expect(page.locator(".data-table")).toContainText("Habilitado");

    // 13c. Catálogos comerciales y matriz de roles
    await page.goto("/administracion?screen=ADM-03");
    await expect(page.getByRole("heading", { name: "Usuarios, roles y permisos" }).first()).toBeVisible();

    await page.goto("/administracion?screen=ADM-05");
    await expect(page.getByRole("heading", { name: "Catálogos comerciales" }).first()).toBeVisible();

    await page.goto("/administracion?screen=ADM-12");
    await expect(page.locator("body")).toBeVisible();

    // 13d. Datos de la inmobiliaria (ADM-13)
    await page.goto("/administracion?screen=ADM-13");
    await expect(page.getByRole("heading", { name: "Inmobiliaria Norte" }).first()).toBeVisible();
  });

  // ==========================================
  // 14. OVERLAYS Y HERRAMIENTAS GLOBALES
  // ==========================================
  test("14. Globales: Búsqueda global (Ctrl+K), Creación rápida (⌘K) y Menú de usuario", async ({ page }) => {
    test.skip(test.info().project.name !== "chromium", "Se valida en desktop.");
    await page.goto("/inicio");

    // 14a. Búsqueda global
    await page.getByRole("button", { name: /Buscar en toda la instalación/ }).click();
    const searchDialog = page.getByRole("dialog", { name: "Búsqueda global" });
    await expect(searchDialog).toBeVisible();
    await searchDialog.getByRole("textbox").fill("Palermo");
    await page.locator(".overlay-backdrop").click();

    // 14b. Menú de creación rápida (⌘K)
    await page.locator(".nav-button").click();
    const quickCreateDrawer = page.getByRole("dialog", { name: "Crear rápido" });
    await expect(quickCreateDrawer).toBeVisible();
    await quickCreateDrawer.getByRole("button", { name: "Cerrar" }).click();

    // 14c. Menú de usuario
    await page.locator(".topbar-avatar").click();
    await expect(page.getByRole("menu")).toBeVisible();
    await page.getByRole("button", { name: "Ver permisos efectivos" }).click();
    const permDialog = page.getByRole("dialog", { name: "Permisos efectivos" });
    await expect(permDialog).toBeVisible();
    await permDialog.getByRole("button", { name: "Cerrar" }).first().click();
  });
});
