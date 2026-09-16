# apps/

`crm-web` y `platform-admin-web` son las dos aplicaciones Next.js/React/TypeScript
del CRM (V2-UX-001, ya integradas al repo). Esta carpeta no agrega código de
producto: solo scripts de build para que V2-FND-001 pueda documentar cómo se
instalan y construyen ambas webs sin modificarlas.

## Comandos

Desde la raíz del repo:

```bash
# Linux/macOS/Git Bash
bash apps/scripts/build-webs.sh
```

```powershell
# Windows PowerShell
pwsh apps/scripts/build-webs.ps1
```

Cada script corre `npm install` y `npm run build` para `apps/crm-web` y
`apps/platform-admin-web` en ese orden. También se pueden ejecutar manualmente
por app:

```bash
npm install --prefix apps/crm-web && npm run build --prefix apps/crm-web
npm install --prefix apps/platform-admin-web && npm run build --prefix apps/platform-admin-web
```

Ver `apps/crm-web/README.md` y `apps/platform-admin-web/README.md` para
comandos específicos de cada app (dev, lint, test, e2e).
