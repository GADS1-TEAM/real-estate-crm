#!/usr/bin/env bash
# Instala y construye crm-web y platform-admin-web sin modificar su código.
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

for app in crm-web platform-admin-web; do
  echo "==> $app: npm install"
  npm install --prefix "$ROOT_DIR/apps/$app"

  echo "==> $app: npm run build"
  npm run build --prefix "$ROOT_DIR/apps/$app"
done
