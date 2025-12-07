# Inicio Rápido - SSL/HTTPS

## 🚀 Pasos rápidos para usar tu certificado wildcard

### 1. Copia tus archivos de certificado

Coloca en la carpeta `python_server/`:

- Tu **certificado wildcard** → renombra a `cert.pem`
- Tu **llave privada** → renombra a `key.pem`

```
python_server/
├── cert.pem          ← TU CERTIFICADO AQUÍ
├── key.pem           ← TU LLAVE PRIVADA AQUÍ
└── server.py
```

### 2. Ejecuta el servidor

**Opción A - Usando .bat (más fácil):**
```
Doble clic en: start_https.bat
```

**Opción B - Usando PowerShell:**
```powershell
.\start_https.ps1
```

**Opción C - Manualmente:**
```powershell
$env:USE_SSL="true"
$env:FLASK_PORT="5003"
$env:SSL_CERT="cert.pem"
$env:SSL_KEY="key.pem"
py -3 server.py
```

### 3. Verifica que funciona

Deberías ver:
```
[PY] Control server listening on 0.0.0.0:5002
[PY] Iniciando servidor HTTPS en puerto 5003
[PY] Certificado: cert.pem
[PY] Llave: key.pem
 * Running on https://127.0.0.1:5003
```

### 4. Prueba desde navegador

```
https://printer.restaurant.pe:5003/ngrok?ip=192.168.10.11&puerto=8082&dominio=lalena&local_id=LOCAL001&device_id=DEVICE123
```

---

## ⚙️ Configuración de Puertos

- **5002** = Control TCP (para conexiones C#)
- **5003** = HTTPS Flask (para peticiones web)

El cliente C# ya está configurado para usar puerto 5002.

---

## 📋 Si tus certificados tienen otros nombres

Edita `start_https.bat` o `start_https.ps1`:

```bat
set SSL_CERT=C:\mis-certificados\wildcard_restaurant.crt
set SSL_KEY=C:\mis-certificados\wildcard_restaurant.key
```

---

## 🔧 Troubleshooting Rápido

### "Certificado SSL no encontrado"
→ Verifica que `cert.pem` está en la carpeta `python_server/`

### "Llave privada SSL no encontrada"
→ Verifica que `key.pem` está en la carpeta `python_server/`

### Certificado en formato .pfx
→ Convierte a .pem con OpenSSL (ver SSL_SETUP.md)

---

## 📄 Más información

Lee `SSL_SETUP.md` para configuración avanzada y producción.
