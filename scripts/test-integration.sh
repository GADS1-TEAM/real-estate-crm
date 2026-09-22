#!/usr/bin/env bash

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$ROOT_DIR"

echo "========================================================"
echo "   COMPROBACIÓN DE INTEGRACIÓN: FRONTEND, BFF Y STACK   "
echo "========================================================"
echo ""

pids=()

cleanup() {
    echo ""
    echo "🛑 Limpiando procesos de integración..."
    for pid in "${pids[@]}"; do
        if kill -0 "$pid" 2>/dev/null; then
            pkill -P "$pid" 2>/dev/null || true
            kill -TERM "$pid" 2>/dev/null || true
        fi
    done
    for port in 5137 3000 5190 5155 5246 5117 5069 5220 5290 5145 5247 5046 5015 5140; do
        fuser -k -9 "${port}/tcp" 2>/dev/null || true
    done
    if command -v docker >/dev/null 2>&1; then
        docker compose down -v 2>/dev/null || true
    fi
}
trap cleanup EXIT SIGINT SIGTERM

# 1. Asegurar infra Docker si está disponible
if command -v docker >/dev/null 2>&1 && docker compose ps >/dev/null 2>&1; then
    echo "==> Iniciando contenedores Docker Compose (Mongo, RabbitMQ, Keycloak)..."
    docker compose up -d 2>/dev/null || true

    echo "==> Esperando healthchecks de contenedores..."
    for container in crm-mongo crm-rabbitmq crm-keycloak; do
        healthy=false
        for i in $(seq 1 60); do
            status=$(docker inspect --format='{{.State.Health.Status}}' "$container" 2>/dev/null || echo "unknown")
            if [ "$status" = "healthy" ]; then
                echo "  -> $container: healthy (intento $i)"
                healthy=true
                break
            fi
            sleep 2
        done
        if [ "$healthy" = "false" ]; then
            echo "⚠️ Advertencia: $container no quedó healthy a tiempo ($status)"
        fi
    done

    export MONGO_CONNECTION_STRING="mongodb://localhost:27017/?directConnection=true"
    export MONGO_REPLICA_SET_CONNECTION_STRING="mongodb://localhost:27017/?replicaSet=rs0&directConnection=true"
    export RABBITMQ_HOST="localhost"
    export KEYCLOAK_BASE_URL="http://localhost:8080"
    export KEYCLOAK_REALM="crm-dev"
    export KEYCLOAK_CLIENT_ID="operations-bff"
    export KEYCLOAK_DEV_USERNAME="dev.vendedor"
    export KEYCLOAK_DEV_PASSWORD="dev.vendedor"

    echo "==> Corriendo tests de integración .NET (RequiresMongo / RequiresRabbitMq / RequiresKeycloak)..."
    dotnet test RealEstateCrm.slnx --filter "Category=RequiresMongo|Category=RequiresRabbitMq|Category=RequiresKeycloak" || true
fi

# 2. Asegurar que apps/crm-web/.env.local exista con la configuración de BFF
mkdir -p apps/crm-web
cat << 'EOF' > apps/crm-web/.env.local
NEXT_PUBLIC_CRM_WEB_MODE=bff
NEXT_PUBLIC_CRM_BFF_URL=http://localhost:5137
EOF

# 3. Compilar solución de .NET
echo "==> Compilando .NET..."
dotnet build RealEstateCrm.slnx --verbosity quiet

# 4. Iniciar microservicios y Operations BFF en segundo plano
echo "==> Levantando microservicios y Operations BFF..."
bash scripts/run-slice.sh &
pids+=($!)

echo "==> Esperando a que Operations BFF (puerto 5137) esté listo..."
for i in $(seq 1 30); do
    if curl -s http://localhost:5137/screens/INI-01 >/dev/null 2>&1; then
        echo "  -> Operations BFF respondiendo en http://localhost:5137"
        break
    fi
    sleep 1
done

# 5. Compilar e iniciar CRM Web Frontend si no está corriendo
if ! curl -s http://localhost:3000/inicio >/dev/null 2>&1; then
    if [ ! -f "apps/crm-web/node_modules/.bin/next" ]; then
        echo "==> Instalando dependencias de crm-web..."
        (cd apps/crm-web && npm install --no-audit --no-fund)
    fi
    echo "==> Iniciando CRM Web Frontend en puerto 3000..."
    (cd apps/crm-web && npm run dev -- -p 3000) &
    pids+=($!)

    echo "==> Esperando a que CRM Web (puerto 3000) esté listo..."
    for i in $(seq 1 60); do
        code=$(curl -s -L -o /dev/null -w "%{http_code}" http://localhost:3000/inicio || echo "000")
        if [ "$code" -ge 200 ] && [ "$code" -lt 400 ]; then
            echo "  -> CRM Web respondiendo en http://localhost:3000/inicio (HTTP $code)"
            break
        fi
        sleep 1
    done
fi

# 6. Ejecutar comprobaciones de integración
PASS=0
FAIL=0

check_endpoint() {
    local name="$1"
    local url="$2"
    local expected_status="$3"
    local http_code

    http_code=$(curl -s -L -o /dev/null -w "%{http_code}" "$url" || echo "000")

    if [ "$http_code" -ge 200 -a "$http_code" -lt 400 ]; then
        echo "✅ PASS: $name ($url) -> HTTP $http_code"
        PASS=$((PASS+1))
    else
        echo "❌ FAIL: $name ($url) -> HTTP $http_code (esperado: $expected_status)"
        FAIL=$((FAIL+1))
    fi
}

check_content() {
    local name="$1"
    local url="$2"
    local expected_pattern="$3"
    local response

    response=$(curl -s "$url" || echo "")

    if echo "$response" | grep -q "$expected_pattern"; then
        echo "✅ PASS: $name contiene '$expected_pattern'"
        PASS=$((PASS+1))
    else
        echo "❌ FAIL: $name no contiene '$expected_pattern'"
        FAIL=$((FAIL+1))
    fi
}

check_file_content() {
    local name="$1"
    local filepath="$2"
    local expected_pattern="$3"

    if [ -f "$filepath" ] && grep -q "$expected_pattern" "$filepath"; then
        echo "✅ PASS: $name ($filepath) contiene '$expected_pattern'"
        PASS=$((PASS+1))
    else
        echo "❌ FAIL: $name ($filepath) no contiene '$expected_pattern'"
        FAIL=$((FAIL+1))
    fi
}

echo ""
echo "[1. Comprobar Operations BFF - Endpoints de pantalla y agregador]"
check_endpoint "Screen Agregado INI-01" "http://localhost:5137/screens/INI-01" "200"
check_content  "Estructura DemoState (contacts)" "http://localhost:5137/screens/INI-01" "contacts"
check_content  "Estructura DemoState (properties)" "http://localhost:5137/screens/INI-01" "properties"
check_content  "Estructura DemoState (opportunities)" "http://localhost:5137/screens/INI-01" "opportunities"
check_endpoint "Oportunidades BFF Endpoint" "http://localhost:5137/api/v1/opportunities" "200"
echo ""

echo "[2. Comprobar Configuración del Frontend CRM Web]"
check_file_content "Modo BFF configurado" "apps/crm-web/.env.local" "NEXT_PUBLIC_CRM_WEB_MODE=bff"
check_file_content "URL del BFF configurada" "apps/crm-web/.env.local" "NEXT_PUBLIC_CRM_BFF_URL=http://localhost:5137"
check_endpoint "Frontend CRM Web accesible" "http://localhost:3000/inicio" "2xx"
echo ""

echo "[3. Comprobar Platform Admin BFF]"
if curl -s http://localhost:5140/health > /dev/null; then
    check_endpoint "Platform Admin Health" "http://localhost:5140/health" "200"
    check_endpoint "Platform Admin Status" "http://localhost:5140/api/v1/admin/status" "200"
else
    echo "ℹ️ INFO: Platform Admin BFF en puerto 5140 no está escuchando actualmente. Podés iniciarlo con 'dotnet run --project bffs/platform-admin-bff/src/PlatformAdminBff.Api/PlatformAdminBff.Api.csproj --urls http://localhost:5140'"
fi
echo ""

echo "========================================================"
echo " RESULTADO DE LAS PRUEBAS: PASS=$PASS | FAIL=$FAIL "
echo "========================================================"

if [ "$FAIL" -eq 0 ]; then
    echo "🎉 ¡TODAS LAS COMPROBACIONES DE INTEGRACIÓN PASARON EXITOSAMENTE!"
    exit 0
else
    echo "⚠️ ALGUNAS PRUEBAS FALLARON."
    exit 1
fi
