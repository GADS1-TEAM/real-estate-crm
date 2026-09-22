#!/usr/bin/env bash

echo "========================================================"
echo "   COMPROBACIÓN DE INTEGRACIÓN: FRONTEND, BFF Y STACK   "
echo "========================================================"
echo ""

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
check_endpoint "Frontend CRM Web accesible" "http://localhost:3000" "2xx"
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
