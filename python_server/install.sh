#!/bin/bash
# Script de instalación para servidor Python en Linux

echo "================================================"
echo "Instalando servidor Python con SSL"
echo "================================================"
echo ""

# Verificar Python 3
if ! command -v python3 &> /dev/null; then
    echo "[ERROR] Python 3 no está instalado"
    echo "Instala Python 3 con: sudo yum install python3"
    exit 1
fi

echo "[OK] Python 3 encontrado: $(python3 --version)"
echo ""

# Crear entorno virtual
if [ ! -d "venv" ]; then
    echo "Creando entorno virtual..."
    python3 -m venv venv
    echo "[OK] Entorno virtual creado"
else
    echo "[OK] Entorno virtual ya existe"
fi

# Activar entorno virtual
echo "Activando entorno virtual..."
source venv/bin/activate

# Actualizar pip
echo "Actualizando pip..."
pip install --upgrade pip

# Instalar dependencias
echo "Instalando dependencias..."
pip install -r requirements.txt

echo ""
echo "================================================"
echo "Instalación completada"
echo "================================================"
echo ""
echo "Para iniciar el servidor con HTTPS:"
echo "  ./start_https.sh"
echo ""
echo "Para iniciar el servidor sin SSL (desarrollo):"
echo "  ./start_http.sh"
echo ""
echo "No olvides:"
echo "  1. Copiar tus certificados SSL (cert.pem y key.pem)"
echo "  2. Hacer los scripts ejecutables: chmod +x *.sh"
echo "  3. Abrir puertos en el firewall si es necesario"
echo ""
