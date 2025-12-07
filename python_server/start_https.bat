@echo off
REM Script para iniciar servidor Python con HTTPS

echo ================================================
echo Iniciando servidor Python con HTTPS
echo ================================================

REM Configurar variables de entorno
set USE_SSL=true
set FLASK_PORT=5003
set SSL_CERT=cert.pem
set SSL_KEY=key.pem

REM Si tus certificados están en otra ubicación, modifica estas rutas:
REM set SSL_CERT=C:\path\to\your\certificate.crt
REM set SSL_KEY=C:\path\to\your\private.key

echo.
echo Configuracion:
echo - Puerto HTTPS: %FLASK_PORT%
echo - Puerto Control TCP: 5002
echo - Certificado: %SSL_CERT%
echo - Llave: %SSL_KEY%
echo.

REM Iniciar servidor
py -3 server.py

pause
