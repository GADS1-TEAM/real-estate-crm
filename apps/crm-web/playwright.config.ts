import { defineConfig, devices } from "@playwright/test";

const port = Number(process.env.CRM_WEB_PORT ?? 3000);
const baseURL = `http://localhost:${port}`;

export default defineConfig({
  testDir: "./e2e",
  use: {
    baseURL,
    trace: "off",
  },
  webServer: {
    command: `${process.platform === "win32" ? "npm.cmd" : "npm"} run dev -- --port ${port}`,
    url: `${baseURL}/inicio`,
    reuseExistingServer: true,
    timeout: 120_000,
    env: {
      CRM_WEB_DIST_DIR: ".next-playwright",
    },
  },
  projects: [
    { name: "chromium", use: { ...devices["Desktop Chrome"] } },
    { name: "mobile", use: { ...devices["Pixel 5"] } },
  ],
});
