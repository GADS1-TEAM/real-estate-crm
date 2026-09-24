import { dirname } from "node:path";
import { fileURLToPath } from "node:url";
import { FlatCompat } from "@eslint/eslintrc";
import { globalIgnores } from "eslint/config";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const compat = new FlatCompat({ baseDirectory: __dirname });

const config = [globalIgnores([".next/**", ".wrangler/**", "out/**", "node_modules/**", "coverage/**", "playwright-report/**", "test-results/**", "next-env.d.ts"]), ...compat.extends("next/core-web-vitals", "next/typescript")];

export default config;
