# Configuración SSL para Servidor Python

## Puertos configurados

- **Puerto 5002**: Servidor de control TCP (sin SSL, solo para conexiones C# locales)
- **Puerto 5003**: Servidor HTTPS Flask (con SSL, para peticiones web)

---

## Opción 1: Usar tu Certificado Wildcard (Producción)

### Paso 1: Copia tus archivos de certificado

Tienes un certificado wildcard (por ejemplo, `*.restaurant.pe`). Necesitas dos archivos:

1. **Certificado** (archivo `.crt`, `.pem` o `.cer`)
2. **Llave privada** (archivo `.key`)

Cópialos a la carpeta `python_server/`:

```
python_server/
├── server.py
├── cert.pem          ← Tu certificado wildcard
├── key.pem           ← Tu llave privada
└── requirements.txt
```

**Nombres sugeridos:**
- `cert.pem` para el certificado
- `key.pem` para la llave privada

**IMPORTANTE**: Si tu certificado es un bundle (incluye certificados intermedios), usa el archivo completo.

### Paso 2: Formato de archivos

#### Si tienes archivos .crt y .key:
```bash
# Simplemente renombra o ajusta las variables
cert.crt  → cert.pem
key.key   → key.pem
```

#### Si tienes .pfx (Windows):
Necesitas extraer el certificado y la llave:

```powershell
# Extraer certificado
openssl pkcs12 -in certificado.pfx -clcerts -nokeys -out cert.pem

# Extraer llave privada
openssl pkcs12 -in certificado.pfx -nocerts -out key.pem

# Remover passphrase de la llave (opcional, para autostart)
openssl rsa -in key.pem -out key.pem
```

### Paso 3: Configurar variables de entorno

#### Opción A: Usar archivo .env

Crea un archivo `.env` en `python_server/`:

```env
USE_SSL=true
FLASK_PORT=5003
SSL_CERT=cert.pem
SSL_KEY=key.pem
```

#### Opción B: Configurar en PowerShell

```powershell
$env:USE_SSL="true"
$env:FLASK_PORT="5003"
$env:SSL_CERT="cert.pem"
$env:SSL_KEY="key.pem"

py -3 server.py
```

#### Opción C: Usar el script .bat (más fácil)

Simplemente ejecuta:
```
start_https.bat
```

Si tus certificados están en otra ubicación, edita `start_https.bat`:
```bat
set SSL_CERT=C:\certificados\wildcard_restaurant_pe.crt
set SSL_KEY=C:\certificados\wildcard_restaurant_pe.key
```

### Paso 4: Ejecutar servidor

```powershell
cd c:\Users\dvid1\CascadeProjects\windsurf-project-2\python_server
start_https.bat
```

Deberías ver:
```
[PY] Control server listening on 0.0.0.0:5002
[PY] Iniciando servidor HTTPS en puerto 5003
[PY] Certificado: cert.pem
[PY] Llave: key.pem
 * Serving Flask app 'server'
 * Running on all addresses (0.0.0.0)
 * Running on https://127.0.0.1:5003
 * Running on https://192.168.10.11:5003
```

### Paso 5: Probar

Desde el navegador:
```
https://printer.restaurant.pe:5003/ngrok?ip=192.168.10.11&puerto=8082&dominio=lalena&local_id=LOCAL001&device_id=DEVICE123
```

O con curl:
```bash
curl --insecure https://localhost:5003/ngrok?ip=192.168.10.11&puerto=8082&dominio=lalena&local_id=LOCAL001&device_id=DEVICE123
```

---

## Opción 2: Sin SSL (Desarrollo/Pruebas)

Si solo quieres probar sin SSL:

```powershell
$env:USE_SSL="false"
$env:FLASK_PORT="5003"
py -3 server.py
```

Accede con HTTP:
```
http://localhost:5003/ngrok?...
```

---

## Estructura de archivos final

```
python_server/
├── server.py
├── requirements.txt
├── cert.pem                 ← Tu certificado wildcard
├── key.pem                  ← Tu llave privada
├── .env.example             ← Plantilla de configuración
├── start_https.bat          ← Script para Windows
└── SSL_SETUP.md            ← Esta guía
```

---

## Configuración de Firewall

Abre los puertos en el firewall de Windows:

```powershell
# Puerto 5002 - Control TCP (solo local, opcional abrirlo)
New-NetFirewallRule -DisplayName "Python Control TCP 5002" -Direction Inbound -LocalPort 5002 -Protocol TCP -Action Allow

# Puerto 5003 - HTTPS Flask (necesario si accedes desde otros equipos)
New-NetFirewallRule -DisplayName "Python HTTPS 5003" -Direction Inbound -LocalPort 5003 -Protocol TCP -Action Allow
```

---

## Nginx como Proxy (Recomendado para Producción)

En lugar de exponer Flask directamente, usa Nginx como reverse proxy:

### Configuración Nginx

```nginx
server {
    listen 443 ssl http2;
    server_name printer.restaurant.pe;

    ssl_certificate /path/to/cert.pem;
    ssl_certificate_key /path/to/key.pem;

    location /ngrok {
        proxy_pass http://127.0.0.1:5003;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 15s;
    }
}
```

Ventajas:
- Mejor rendimiento
- Más seguro
- Gestión de SSL más robusta
- Logs centralizados

---

## Troubleshooting

### Error: "Certificado SSL no encontrado: cert.pem"

**Causa**: El archivo no está en la ubicación correcta  
**Solución**: 
1. Verifica que `cert.pem` esté en la carpeta `python_server/`
2. O configura la ruta completa: `set SSL_CERT=C:\ruta\completa\certificado.crt`

### Error: "Permission denied" en puerto 443

**Causa**: En Linux, puertos < 1024 requieren root  
**Solución**: Usa puerto 5003 (como está configurado) o ejecuta con sudo

### Error: "SSL handshake failed"

**Causa**: Certificado o llave incorrectos  
**Solución**:
1. Verifica que el certificado y la llave correspondan
2. Verifica que el certificado incluya la cadena completa (bundle)

### Warning: "This is a development server"

**Causa**: Flask desarrollo no es para producción  
**Solución**: Usa Nginx + Gunicorn para producción:

```bash
pip install gunicorn
gunicorn --certfile=cert.pem --keyfile=key.pem --bind 0.0.0.0:5003 server:app
```

---

## Verificar Certificado

Para ver información de tu certificado:

```bash
openssl x509 -in cert.pem -text -noout
```

Para verificar que certificado y llave coinciden:

```bash
openssl x509 -noout -modulus -in cert.pem | openssl md5
openssl rsa -noout -modulus -in key.pem | openssl md5
```

Los hashes MD5 deben ser idénticos.

---

## Seguridad

### Proteger archivos de certificado

```powershell
# Solo el usuario actual puede leer
icacls key.pem /inheritance:r /grant:r "%USERNAME%:R"
```

### No commitear certificados a Git

Agrega al `.gitignore`:
```
*.pem
*.key
*.crt
*.pfx
.env
```

---

## Actualizar Documentación

Recuerda actualizar:
- `README.md` con las nuevas URLs HTTPS
- `USAGE.md` con ejemplos usando HTTPS
- Scripts de inicio con las nuevas configuraciones
