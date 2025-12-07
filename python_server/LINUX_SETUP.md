# Configuración en Linux - Servidor Python HTTPS

Sistema detectado: **RHEL 9 / Rocky Linux 9** (kernel 5.14.0)

---

## 1. Instalación Inicial

### Paso 1: Instalar dependencias del sistema

```bash
# Actualizar sistema
sudo yum update -y

# Instalar Python 3 y herramientas
sudo yum install python3 python3-pip python3-venv -y

# Verificar instalación
python3 --version
```

### Paso 2: Copiar certificados SSL

Copia tus certificados wildcard a la carpeta del proyecto:

```bash
cd /ruta/a/python_server

# Copia tus certificados (ajusta las rutas)
cp /path/to/your/wildcard.crt cert.pem
cp /path/to/your/wildcard.key key.pem

# Asegurar permisos correctos
chmod 600 key.pem
chmod 644 cert.pem
```

### Paso 3: Ejecutar instalación automática

```bash
# Hacer script ejecutable
chmod +x install.sh

# Ejecutar instalación
./install.sh
```

Esto creará un entorno virtual e instalará Flask.

---

## 2. Iniciar Servidor

### Opción A: Con HTTPS (Producción)

```bash
# Hacer script ejecutable
chmod +x start_https.sh

# Iniciar servidor
./start_https.sh
```

### Opción B: Sin SSL (Desarrollo)

```bash
chmod +x start_http.sh
./start_http.sh
```

### Opción C: Manual

```bash
# Activar entorno virtual
source venv/bin/activate

# Configurar variables
export USE_SSL=true
export FLASK_PORT=5003
export SSL_CERT=cert.pem
export SSL_KEY=key.pem

# Ejecutar
python3 server.py
```

---

## 3. Configurar Firewall (firewalld)

```bash
# Abrir puerto 5003 (HTTPS Flask)
sudo firewall-cmd --permanent --add-port=5003/tcp

# Abrir puerto 5002 (Control TCP) - solo si es necesario desde fuera
sudo firewall-cmd --permanent --add-port=5002/tcp

# Recargar firewall
sudo firewall-cmd --reload

# Verificar
sudo firewall-cmd --list-ports
```

---

## 4. Configurar SELinux (si está activo)

```bash
# Verificar estado de SELinux
getenforce

# Si está en Enforcing, permitir conexiones en puertos personalizados
sudo semanage port -a -t http_port_t -p tcp 5003
sudo semanage port -a -t http_port_t -p tcp 5002

# Si semanage no está instalado
sudo yum install policycoreutils-python-utils -y
```

---

## 5. Auto-inicio con systemd

### Crear servicio systemd

```bash
# Generar archivo de servicio
chmod +x systemd_service.sh
./systemd_service.sh

# Copiar e instalar servicio
sudo cp /tmp/python-ngrok.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable python-ngrok.service
sudo systemctl start python-ngrok.service
```

### Comandos útiles del servicio

```bash
# Ver estado
sudo systemctl status python-ngrok.service

# Ver logs en tiempo real
sudo journalctl -u python-ngrok.service -f

# Reiniciar servicio
sudo systemctl restart python-ngrok.service

# Detener servicio
sudo systemctl stop python-ngrok.service

# Deshabilitar auto-inicio
sudo systemctl disable python-ngrok.service
```

---

## 6. Usar con Nginx como Reverse Proxy (Recomendado)

### Instalar Nginx

```bash
sudo yum install nginx -y
sudo systemctl enable nginx
sudo systemctl start nginx
```

### Configurar Nginx

Crear `/etc/nginx/conf.d/ngrok.conf`:

```nginx
server {
    listen 80;
    server_name printer.restaurant.pe;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name printer.restaurant.pe;

    ssl_certificate /path/to/cert.pem;
    ssl_certificate_key /path/to/key.pem;
    
    # SSL moderno
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;

    # Headers de seguridad
    add_header Strict-Transport-Security "max-age=31536000" always;
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;

    location /ngrok {
        proxy_pass http://127.0.0.1:5003;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 15s;
        proxy_connect_timeout 5s;
    }
}
```

Activar y verificar:

```bash
# Verificar sintaxis
sudo nginx -t

# Recargar configuración
sudo systemctl reload nginx

# Ver logs
sudo tail -f /var/log/nginx/error.log
```

Con Nginx, accederías a:
```
https://printer.restaurant.pe/ngrok?ip=...
```
(Sin necesidad de especificar puerto)

---

## 7. Usar con Gunicorn (Producción)

Para mejor rendimiento en producción:

```bash
# Activar entorno virtual
source venv/bin/activate

# Instalar Gunicorn
pip install gunicorn

# Ejecutar con SSL
gunicorn --certfile=cert.pem --keyfile=key.pem \
         --bind 0.0.0.0:5003 \
         --workers 4 \
         --threads 2 \
         --timeout 30 \
         server:app
```

Para auto-inicio, modifica el servicio systemd:

```ini
ExecStart=/ruta/a/venv/bin/gunicorn --certfile=cert.pem --keyfile=key.pem --bind 0.0.0.0:5003 --workers 4 server:app
```

---

## 8. Monitoreo y Logs

### Ver logs del servidor

```bash
# Si usas systemd
sudo journalctl -u python-ngrok.service -f

# Si ejecutas manualmente, redirige logs
./start_https.sh 2>&1 | tee server.log
```

### Monitorear conexiones

```bash
# Ver conexiones activas en puertos
sudo ss -tulpn | grep -E '5002|5003'

# Ver procesos Python
ps aux | grep python
```

---

## 9. Seguridad

### Permisos de archivos

```bash
# Certificados solo legibles por el usuario
chmod 600 key.pem
chmod 644 cert.pem

# Scripts ejecutables
chmod +x *.sh

# Resto de archivos
chmod 644 *.py *.md
```

### Actualizar regularmente

```bash
# Actualizar sistema
sudo yum update -y

# Actualizar paquetes Python
source venv/bin/activate
pip install --upgrade pip
pip install --upgrade -r requirements.txt
```

---

## 10. Verificación

### Test local

```bash
# Con curl
curl -k https://localhost:5003/ngrok?ip=192.168.10.11&puerto=8082&dominio=lalena&local_id=LOCAL001&device_id=DEVICE123

# Ver certificado
openssl s_client -connect localhost:5003 -showcerts
```

### Test remoto

Desde otra máquina:

```bash
curl https://printer.restaurant.pe:5003/ngrok?ip=192.168.10.11&puerto=8082&dominio=lalena&local_id=LOCAL001&device_id=DEVICE123
```

---

## 11. Troubleshooting Linux

### Error: "Permission denied" en puerto < 1024

Solución: Usa puerto 5003 (ya configurado) o:
```bash
# Permitir a Python usar puertos privilegiados
sudo setcap 'cap_net_bind_service=+ep' $(which python3)
```

### Error: "Address already in use"

```bash
# Ver qué proceso usa el puerto
sudo lsof -i :5003
sudo lsof -i :5002

# Matar proceso
sudo kill -9 <PID>
```

### Error: SELinux bloquea conexiones

```bash
# Ver alertas SELinux
sudo ausearch -m avc -ts recent

# Modo permisivo temporal (para debug)
sudo setenforce 0

# Volver a enforcing
sudo setenforce 1
```

### Servidor no responde desde fuera

1. Verificar firewall: `sudo firewall-cmd --list-all`
2. Verificar que escucha en 0.0.0.0: `sudo ss -tulpn | grep 5003`
3. Verificar rutas de red: `ip route`
4. Verificar DNS apunta a tu servidor

---

## 12. Backup y Restauración

### Backup

```bash
# Backup de certificados y configuración
tar -czf backup-ngrok-$(date +%Y%m%d).tar.gz \
    cert.pem key.pem server.py requirements.txt *.sh
```

### Restauración

```bash
# Extraer backup
tar -xzf backup-ngrok-YYYYMMDD.tar.gz

# Reinstalar
./install.sh
```

---

## Scripts disponibles

- `install.sh` - Instalación inicial
- `start_https.sh` - Iniciar con HTTPS
- `start_http.sh` - Iniciar sin SSL (desarrollo)
- `systemd_service.sh` - Generar servicio systemd

Todos los scripts deben tener permisos de ejecución:
```bash
chmod +x *.sh
```
