# Sistema de Puerto Inverso (Estilo ngrok)

Sistema de conexión inversa que permite acceder a servicios HTTP locales (sin HTTPS) desde un servidor público con HTTPS, evitando el bloqueo de Chrome a HTTP inseguro.

## Arquitectura

```
Chrome (HTTPS) → Servidor Python (Puerto 5000/5001) → Cliente C# → Servidor Local (HTTP en ip:puerto)
                                                          ↓
                                                   Respuesta de vuelta
```

## Componentes

### 1. Servidor Python (`python_server/`)

- **Puerto 5003**: Servidor HTTPS Flask con endpoint `/ngrok` (configurable)
- **Puerto 5002**: Servidor TCP de control para conexiones de clientes C# (sin SSL)

### 2. Cliente C# (`PuertoInverso/`)

- Aplicación .NET Framework 4.5.2
- Se conecta al servidor Python (puerto 5001)
- Se registra con un `dominio`
- Espera comandos para conectarse a servicios locales
- Lee las respuestas y las envía de vuelta al servidor Python

## Instalación y Ejecución

### Paso 1: Servidor Python

#### Opción A: Si tienes Python 3.6+ (recomendado)

```powershell
cd c:\Users\dvid1\CascadeProjects\windsurf-project-2\python_server

# Crear entorno virtual
py -3 -m venv venv

# Activar entorno
.\venv\Scripts\activate

# Instalar dependencias
pip install -r requirements.txt

# Ejecutar servidor
python server.py
```

#### Opción B: Si solo tienes Python 3.5 o 2.7

Necesitas instalar Python 3.10+ desde https://www.python.org/downloads/windows/
Durante la instalación, marca "Add Python to PATH".

### Paso 2: Cliente C# (PuertoInverso)

#### Compilar el proyecto

```powershell
cd c:\Users\dvid1\CascadeProjects\windsurf-project-2\PuertoInverso

# Restaurar paquetes NuGet
nuget restore

# Compilar con MSBuild (desde Developer Command Prompt)
msbuild PuertoInverso.sln /p:Configuration=Release
```

O simplemente abre `PuertoInverso.sln` en Visual Studio y presiona F5.

#### Ejecutar el cliente

```powershell
cd PuertoInverso\bin\Debug
.\PuertoInverso.exe lalena
```

Donde `lalena` es el **dominio** con el que se registra el cliente.

## Uso

### Ejemplo 1: Probar con un servidor HTTP local simple

1. Crea un servidor HTTP de prueba en el puerto 8000:

```powershell
# En otra terminal
cd c:\
python -m http.server 8000
```

2. Con el servidor Python y el cliente C# corriendo, haz una petición:

```powershell
curl "http://localhost:5000/ngrok?ip=127.0.0.1&puerto=8000&dominio=lalena"
```

3. Deberías ver la respuesta HTML del servidor local en el JSON de retorno.

### Ejemplo 2: Conectar a un servicio de impresora

Si tienes una impresora en `192.168.1.23:6000`:

```powershell
curl "http://localhost:5000/ngrok?ip=192.168.1.23&puerto=6000&dominio=lalena"
```

### Desde navegador

```
http://localhost:5000/ngrok?ip=192.168.1.23&puerto=6000&dominio=lalena
```

## Respuestas

### Éxito

```json
{
  "ok": true,
  "message": "Respuesta del servidor local",
  "data": "<contenido del servidor local>"
}
```

### Error - Cliente no conectado

```json
{
  "ok": false,
  "error": "No hay cliente conectado para dominio lalena"
}
```

### Error - Timeout

```json
{
  "ok": false,
  "error": "Timeout esperando respuesta del cliente"
}
```

## Configuración

### Cambiar IP/Puerto del servidor Python

En `PuertoInverso\Program.cs`:

```csharp
private const string ServerHost = "TU_IP_PUBLICA"; // IP del servidor Python
private const int ServerPort = 5001;               // Puerto TCP de control
```

### Desplegar en producción

1. **Servidor Python**: Usar Nginx/Apache/Caddy con SSL delante de Flask:

```nginx
server {
    listen 443 ssl;
    server_name printer.restaurant.pe;
    
    ssl_certificate /path/to/cert.pem;
    ssl_certificate_key /path/to/key.pem;
    
    location /ngrok {
        proxy_pass http://localhost:5000;
        proxy_read_timeout 15s;
    }
}
```

2. **Cliente C#**: Ejecutar en la máquina que tiene acceso al servicio local (impresora, etc.)

## Limitaciones actuales

- Lee hasta 100KB de respuesta del servidor local
- Timeout de 3 segundos para leer del servidor local
- Timeout de 10 segundos para respuesta completa
- No maneja múltiples peticiones simultáneas al mismo dominio (la segunda sobrescribirá la primera)

## Próximas mejoras

- [ ] Soporte para POST/PUT con datos hacia el servidor local
- [ ] Túnel bidireccional persistente (WebSocket-style)
- [ ] Múltiples peticiones concurrentes con cola
- [ ] Autenticación y API keys
- [ ] Logs y métricas
- [ ] Reconexión automática del cliente con backoff

## Logs de depuración

### Cliente C#

```
=== C# Reverse Client (.NET Framework 4.5.2) ===
[CS] Conectando a 127.0.0.1:5001...
[CS] Conectado al servidor de control
[CS] Registrado con dominio 'lalena'
[CS] Comando recibido: {"Cmd":"connect","Ip":"127.0.0.1","Port":8000,"RequestId":"..."}
[CS] Conectando a 127.0.0.1:8000...
[CS] Conexión local establecida
[CS] Recibido 1234 bytes del servidor local
[CS] Respuesta: <!DOCTYPE html>...
[CS] Respuesta enviada al servidor Python (RequestId: ...)
```

### Servidor Python

```
[PY] Control server listening on 0.0.0.0:5001
 * Running on http://0.0.0.0:5000
[PY] New client from ('127.0.0.1', 54321)
[PY] Registered client for domain 'lalena'
[PY] Comando enviado, esperando respuesta...
[PY] Respuesta recibida para RequestId: abc-123-...
[PY] Respuesta exitosa recibida
```

## Troubleshooting

### Error: `f-string` syntax error en Python

**Causa**: Estás usando Python 2.7 o 3.5  
**Solución**: Instala Python 3.6+ o superior

### Error: Cliente C# no se conecta

**Causa**: El servidor Python no está corriendo o hay firewall  
**Solución**: 
1. Verifica que `server.py` está corriendo
2. Verifica con `netstat -an | findstr 5001` que el puerto está abierto
3. Desactiva temporalmente el firewall para probar

### Error: Timeout esperando respuesta

**Causa**: El servidor local no responde o no está escuchando  
**Solución**: Verifica que el servicio en `ip:puerto` realmente está activo

### Error: No hay cliente conectado

**Causa**: El cliente C# no está corriendo o se desconectó  
**Solución**: Reinicia `PuertoInverso.exe` con el dominio correcto
