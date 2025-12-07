#!/bin/bash
# Script para crear servicio systemd (auto-inicio)

echo "================================================"
echo "Configurando servicio systemd"
echo "================================================"
echo ""

# Obtener ruta absoluta del proyecto
PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
VENV_PYTHON="$PROJECT_DIR/venv/bin/python3"

echo "Ruta del proyecto: $PROJECT_DIR"
echo ""

# Crear archivo de servicio
SERVICE_FILE="/tmp/python-ngrok.service"

cat > $SERVICE_FILE << EOF
[Unit]
Description=Python HTTPS Ngrok-style Server
After=network.target

[Service]
Type=simple
User=$USER
WorkingDirectory=$PROJECT_DIR
Environment="USE_SSL=true"
Environment="FLASK_PORT=5003"
Environment="SSL_CERT=cert.pem"
Environment="SSL_KEY=key.pem"
ExecStart=$VENV_PYTHON server.py
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
EOF

echo "Archivo de servicio creado en: $SERVICE_FILE"
echo ""
echo "Para instalar el servicio, ejecuta como root:"
echo ""
echo "  sudo cp $SERVICE_FILE /etc/systemd/system/"
echo "  sudo systemctl daemon-reload"
echo "  sudo systemctl enable python-ngrok.service"
echo "  sudo systemctl start python-ngrok.service"
echo ""
echo "Para ver el estado:"
echo "  sudo systemctl status python-ngrok.service"
echo ""
echo "Para ver logs:"
echo "  sudo journalctl -u python-ngrok.service -f"
echo ""
