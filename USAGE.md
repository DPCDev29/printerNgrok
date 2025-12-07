# Guía de Uso - Sistema Puerto Inverso

## Cambios en esta versión

✅ **Registro con 3 parámetros**: dominio + local_id + device_id  
✅ **Soporte GET y POST**: Python acepta ambos métodos  
✅ **Reenvío completo**: Headers y body se reenvían al servidor local  
✅ **Extracción de body HTTP**: Solo se retorna el JSON, sin headers HTTP  

---

## 1. Compilar C#

```powershell
cd c:\Users\dvid1\CascadeProjects\windsurf-project-2\PuertoInverso
msbuild PuertoInverso.sln /p:Configuration=Debug
```

---

## 2. Ejecutar Servidor Python

```powershell
cd c:\Users\dvid1\CascadeProjects\windsurf-project-2\python_server

# Si tienes Python 3.6+
py -3 server.py
```

Deberías ver:
```
[PY] Control server listening on 0.0.0.0:5001
 * Running on http://127.0.0.1:5000
```

---

## 3. Ejecutar Cliente C#

**IMPORTANTE**: Ahora requiere 3 argumentos:

```powershell
cd c:\Users\dvid1\CascadeProjects\windsurf-project-2\PuertoInverso\PuertoInverso\bin\Debug

.\PuertoInverso.exe lalena LOCAL001 DEVICE123
```

Parámetros:
- `lalena` = **dominio**
- `LOCAL001` = **local_id**
- `DEVICE123` = **device_id**

Deberías ver:
```
=== C# Reverse Client (.NET Framework 4.5.2) ===
[CS] Conectando a 127.0.0.1:5001...
[CS] Conectado al servidor de control
[CS] Registrado con Dominio: 'lalena', LocalId: 'LOCAL001', DeviceId: 'DEVICE123'
```

---

## 4. Ejemplos de Uso

### Ejemplo 1: GET simple

```bash
curl "http://localhost:5000/ngrok?ip=192.168.10.11&puerto=8082&dominio=lalena&local_id=LOCAL001&device_id=DEVICE123"
```

### Ejemplo 2: GET con path personalizado

```bash
curl "http://localhost:5000/ngrok?ip=192.168.10.11&puerto=8082&dominio=lalena&local_id=LOCAL001&device_id=DEVICE123&path=/api/status"
```

### Ejemplo 3: POST con JSON body

```bash
curl -X POST http://localhost:5000/ngrok \
  -H "Content-Type: application/json" \
  -d '{
    "ip": "192.168.10.11",
    "puerto": 8082,
    "dominio": "lalena",
    "local_id": "LOCAL001",
    "device_id": "DEVICE123",
    "method": "POST",
    "path": "/api/print",
    "body": "{\"printer\":\"HP001\",\"data\":\"test\"}"
  }'
```

### Ejemplo 3b: POST con form-data

```bash
curl -X POST http://localhost:5000/ngrok \
  -F "ip=192.168.10.11" \
  -F "puerto=8082" \
  -F "dominio=lalena" \
  -F "local_id=LOCAL001" \
  -F "device_id=DEVICE123" \
  -F "method=POST" \
  -F "path=/api/print" \
  -F "body={\"printer\":\"HP001\",\"data\":\"test\"}"
```

### Ejemplo 3c: POST con x-www-form-urlencoded

```bash
curl -X POST http://localhost:5000/ngrok \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "ip=192.168.10.11&puerto=8082&dominio=lalena&local_id=LOCAL001&device_id=DEVICE123&method=POST&path=/api/print&body={\"printer\":\"HP001\"}"
```

### Ejemplo 4: Desde navegador

```
http://localhost:5000/ngrok?ip=192.168.10.11&puerto=8082&dominio=lalena&local_id=LOCAL001&device_id=DEVICE123
```

---

## 5. Flujo Completo

### GET Request

```
1. Chrome → https://tuservidor.pe/ngrok?ip=192.168.10.11&puerto=8082&...
2. Python recibe GET
3. Python extrae: ip, puerto, dominio, local_id, device_id
4. Python busca cliente C# con clave: (dominio, local_id, device_id)
5. Python envía comando JSON a C#:
   {
     "Cmd": "connect",
     "Ip": "192.168.10.11",
     "Port": 8082,
     "RequestId": "abc-123...",
     "Method": "GET",
     "Path": "/",
     "Headers": {...},
     "Body": null
   }
6. C# conecta a 192.168.10.11:8082
7. C# envía:
   GET / HTTP/1.1
   Host: 192.168.10.11:8082
   Connection: close

8. C# lee respuesta HTTP completa
9. C# extrae solo el body (sin headers HTTP)
10. C# envía respuesta a Python:
    {
      "RequestId": "abc-123...",
      "Success": true,
      "Data": "{\"tipo\":\"3\",\"message\":\"...\"}",
      "Error": null
    }
11. Python retorna a Chrome:
    {
      "ok": true,
      "data": "{\"tipo\":\"3\",\"message\":\"...\"}",
      "message": "Respuesta del servidor local"
    }
```

### POST Request

```
1. Cliente → POST https://tuservidor.pe/ngrok con JSON body
2. Python recibe POST con:
   {
     "ip": "192.168.10.11",
     "puerto": 8082,
     "dominio": "lalena",
     "local_id": "LOCAL001",
     "device_id": "DEVICE123",
     "method": "POST",
     "path": "/api/print",
     "body": "{\"printer\":\"HP001\"}"
   }
3. Python envía a C# el comando con Method=POST y Body
4. C# construye:
   POST /api/print HTTP/1.1
   Host: 192.168.10.11:8082
   Content-Length: 21
   Connection: close

   {"printer":"HP001"}
5. C# lee respuesta y la envía a Python
6. Python retorna al cliente original
```

---

## 6. Estructura de Respuestas

### Éxito

```json
{
  "ok": true,
  "message": "Respuesta del servidor local",
  "data": "{\"tipo\":\"3\",\"data\":\"\",\"message\":\"Nombre de Impresora no Valido\",\"os_version\":\"Microsoft Windows NT 6.2.9200.0\",\"version\":\"16.0.0\",\"error_message\":\"\"}"
}
```

El campo `data` contiene el **JSON body** de la respuesta del servidor local.

### Error - Cliente no conectado

```json
{
  "ok": false,
  "error": "No hay cliente conectado para ('lalena', 'LOCAL001', 'DEVICE123')"
}
```

### Error - Parámetros faltantes

```json
{
  "ok": false,
  "error": "Faltan parametros: ip, puerto, dominio, local_id, device_id"
}
```

---

## 7. Logs Esperados

### En C# (Debug completo)

```
[CS] Conectando a 192.168.10.11:8082...
[CS] Conexión local establecida
[CS] Stream local abierto
[CS] Construyendo petición HTTP GET /
[CS] Enviando petición HTTP GET al servidor...
--- PETICIÓN HTTP ---
GET / HTTP/1.1
Host: 192.168.10.11:8082
Connection: close

--- FIN PETICIÓN ---
[CS] Petición HTTP enviada, esperando respuesta...
[CS] Leídos 268 bytes en esta iteración
----------------------------------------
[CS] TOTAL RECIBIDO: 268 bytes del servidor local
[CS] RESPUESTA COMPLETA (con headers HTTP):
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
Content-Length: 135

{"tipo":"3","data":"","message":"Nombre de Impresora no Valido",...}
----------------------------------------
[CS] Cuerpo HTTP extraído (sin headers):
{"tipo":"3","data":"","message":"Nombre de Impresora no Valido",...}
----------------------------------------
========================================
[CS] RESPUESTA QUE SE ENVIARA A PYTHON:
========================================
RequestId: d8dce070-f6ed-49f7-8379-442e15871497
Success: True
Data Length: 135 caracteres
...
[CS] ✓ Respuesta enviada exitosamente al servidor Python
```

### En Python

```
[PY] New client from ('127.0.0.1', 49431)
[PY] Registered client: Dominio='lalena', LocalId='LOCAL001', DeviceId='DEVICE123'
[PY] Comando enviado, esperando respuesta...
[PY] Respuesta recibida para RequestId: d8dce070-f6ed-49f7-8379-442e15871497
[PY] Respuesta exitosa recibida
127.0.0.1 - - [06/Dec/2025 22:45:00] "GET /ngrok?ip=192.168.10.11&puerto=8082&... HTTP/1.1" 200 -
```

---

## 8. Troubleshooting

### Error: "Uso: PuertoInverso.exe <dominio> <local_id> <device_id>"

**Causa**: Olvidaste pasar los 3 parámetros  
**Solución**: Ejecuta con los 3 argumentos:
```powershell
.\PuertoInverso.exe lalena LOCAL001 DEVICE123
```

### Error: "Faltan parametros: ip, puerto, dominio, local_id, device_id"

**Causa**: El request HTTP no incluye todos los parámetros requeridos  
**Solución**: Asegúrate de pasar los 5 parámetros:
```
?ip=...&puerto=...&dominio=...&local_id=...&device_id=...
```

### Error: "No hay cliente conectado para ('lalena', 'LOCAL001', 'DEVICE123')"

**Causa**: El cliente C# no está corriendo o se registró con valores diferentes  
**Solución**: 
1. Verifica que `PuertoInverso.exe` esté corriendo
2. Verifica que los valores en la URL coincidan exactamente con los argumentos de C#

### Error: Timeout esperando respuesta

**Causa**: El servidor local no respondió a tiempo o C# no pudo extraer el body  
**Solución**:
1. Verifica que el servidor en `ip:puerto` esté corriendo
2. Revisa los logs de C# para ver si hubo errores al leer la respuesta

---

## 9. Próximos Pasos

- [ ] Despliega Python en un servidor con HTTPS (Nginx + SSL)
- [ ] Ejecuta C# en la máquina local que tiene acceso al servicio
- [ ] Prueba con datos reales de impresoras/dispositivos
- [ ] Implementa autenticación/API keys si es necesario
