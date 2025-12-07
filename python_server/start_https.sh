#!/bin/bash
# Script para iniciar servidor Python con HTTPS en Linux

echo "================================================"
echo "Iniciando servidor Python con HTTPS"
echo "================================================"
echo ""

# Configurar variables de entorno
export USE_SSL=true
export FLASK_PORT=5003
export SSL_CERT=cert.pem
export SSL_KEY=key.pem

# Si tus certificados están en otra ubicación, modifica estas rutas:
# export SSL_CERT=/path/to/your/certificate.crt
# export SSL_KEY=/path/to/your/private.key

echo "Configuracion:"
echo "- Puerto HTTPS: $FLASK_PORT"
echo "- Puerto Control TCP: 5002"
echo "- Certificado: $SSL_CERT"
echo "- Llave: $SSL_KEY"
echo ""

# Verificar que existen los archivos
if [ ! -f "$SSL_CERT" ]; then
    echo "[ERROR] No se encontró el certificado: $SSL_CERT"
    echo "Coloca tu certificado wildcard en esta carpeta o modifica la ruta"
    exit 1
fi

if [ ! -f "$SSL_KEY" ]; then
    echo "[ERROR] No se encontró la llave privada: $SSL_KEY"
    echo "Coloca tu llave privada en esta carpeta o modifica la ruta"
    exit 1
fi

echo "[OK] Archivos de certificado encontrados"
echo ""

# Iniciar servidor
echo "Iniciando servidor..."
python3 server.py
