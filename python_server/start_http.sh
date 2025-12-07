#!/bin/bash
# Script para iniciar servidor Python sin SSL (solo HTTP)

echo "================================================"
echo "Iniciando servidor Python HTTP (sin SSL)"
echo "================================================"
echo ""

# Configurar variables de entorno
export USE_SSL=false
export FLASK_PORT=5003

echo "Configuracion:"
echo "- Puerto HTTP: $FLASK_PORT"
echo "- Puerto Control TCP: 5002"
echo "- SSL: Deshabilitado"
echo ""
echo "[ADVERTENCIA] Servidor sin SSL - Solo para desarrollo/pruebas"
echo ""

# Iniciar servidor
echo "Iniciando servidor..."
python3 server.py
