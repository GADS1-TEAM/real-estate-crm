import { defineConfig, devices } from "@playwright/test";

const port = Number(process.env.CRM_WEB_PORT ?? 3000);
const baseURL = `http://127.0.0.1:${port}`;

export default defineConfig({
  testDir: "./e2e",
  use: {
    baseURL,
    trace: "off",
  },
  webServer: {
    command: `${process.platform === "win32" ? "npm.cmd" : "npm"} run dev:preview -- --port ${port}`,
    url: baseURL,
    reuseExistingServer: true,
    timeout: 120_000,
  },
  projects: [
    { name: "chromium", use: { ...devices["Desktop Chrome"] } },
    { name: "mobile", use: { ...devices["Pixel 5"] } },
  ],
});
