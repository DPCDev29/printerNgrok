# Script PowerShell para iniciar servidor Python con HTTPS

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Iniciando servidor Python con HTTPS" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Configurar variables de entorno
$env:USE_SSL = "true"
$env:FLASK_PORT = "5003"
$env:SSL_CERT = "cert.pem"
$env:SSL_KEY = "key.pem"

# Si tus certificados están en otra ubicación, modifica estas rutas:
# $env:SSL_CERT = "C:\path\to\your\certificate.crt"
# $env:SSL_KEY = "C:\path\to\your\private.key"

Write-Host "Configuracion:" -ForegroundColor Yellow
Write-Host "- Puerto HTTPS: $env:FLASK_PORT" -ForegroundColor White
Write-Host "- Puerto Control TCP: 5002" -ForegroundColor White
Write-Host "- Certificado: $env:SSL_CERT" -ForegroundColor White
Write-Host "- Llave: $env:SSL_KEY" -ForegroundColor White
Write-Host ""

# Verificar que existen los archivos
if (-not (Test-Path $env:SSL_CERT)) {
    Write-Host "[ERROR] No se encontró el certificado: $env:SSL_CERT" -ForegroundColor Red
    Write-Host "Coloca tu certificado wildcard en esta carpeta o modifica la ruta" -ForegroundColor Red
    Read-Host "Presiona Enter para salir"
    exit 1
}

if (-not (Test-Path $env:SSL_KEY)) {
    Write-Host "[ERROR] No se encontró la llave privada: $env:SSL_KEY" -ForegroundColor Red
    Write-Host "Coloca tu llave privada en esta carpeta o modifica la ruta" -ForegroundColor Red
    Read-Host "Presiona Enter para salir"
    exit 1
}

Write-Host "[OK] Archivos de certificado encontrados" -ForegroundColor Green
Write-Host ""

# Iniciar servidor
Write-Host "Iniciando servidor..." -ForegroundColor Cyan
py -3 server.py
