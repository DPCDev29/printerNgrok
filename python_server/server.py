import threading
import socket
import json
import uuid
import time
from flask import Flask, request, jsonify

app = Flask(__name__)

# Diccionario: (dominio, local_id, device_id) -> socket del cliente C#
clients = {}
clients_lock = threading.Lock()

# Diccionario para esperar respuestas: request_id -> {"event": Event, "response": dict}
pending_responses = {}
pending_lock = threading.Lock()

# Rate limiting: IP -> timestamp de última conexión rechazada SSL
ssl_rejected_ips = {}
ssl_rejected_lock = threading.Lock()


def start_control_server(host="0.0.0.0", port=5002):
    """Servidor TCP que acepta conexiones de los clientes C# y los registra por dominio."""
    server_sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server_sock.bind((host, port))
    server_sock.listen(5)
    print(f"[PY] Control server listening on {host}:{port}")

    def client_handler(conn, addr):
        print(f"[PY] New client from {addr}")
        client_key = None
        try:
            # Leer primera línea con timeout extendido
            conn.settimeout(10.0)  # Dar más tiempo
            buffer = b""
            
            # Leer hasta encontrar newline completo
            while True:
                try:
                    chunk = conn.recv(1024)
                    if not chunk:
                        print(f"[PY] Connection closed by {addr} before sending data")
                        conn.close()
                        return
                    
                    buffer += chunk
                    
                    # Si encontramos newline, ya tenemos la línea completa
                    if b"\n" in buffer:
                        break
                    
                    # Protección: máximo 8KB para el registro
                    if len(buffer) > 8192:
                        print(f"[PY] ✗ Registration too large from {addr}, closing")
                        conn.close()
                        return
                        
                except socket.timeout:
                    print(f"[PY] ✗ Timeout waiting for registration from {addr}")
                    conn.close()
                    return
            
            # Extraer primera línea
            registro_raw = buffer.split(b"\n", 1)[0].decode("utf-8", errors="ignore").strip()
            
            if not registro_raw:
                print(f"[PY] ✗ Empty registration from {addr}")
                conn.close()
                return
            
            # Detectar si es SSL/TLS (comienza con 0x16 0x03)
            if len(buffer) >= 2 and buffer[0] == 0x16 and buffer[1] == 0x03:
                print(f"[PY] ✗ SSL/TLS rejected from {addr} (port 5002 is TCP only)")
                conn.close()
                return
            
            # Debug: mostrar lo que se recibió
            print(f"[PY] Received registration from {addr}: {registro_raw[:150]}")
            
            try:
                registro = json.loads(registro_raw)
                dominio = registro.get("Dominio")
                local_id = registro.get("LocalId")
                device_id = registro.get("DeviceId")
                
                if not dominio or not local_id or not device_id:
                    print(f"[PY] Missing fields from {addr}: Dominio={dominio}, LocalId={local_id}, DeviceId={device_id}")
                    conn.close()
                    return
                
                client_key = (dominio, local_id, device_id)
                with clients_lock:
                    clients[client_key] = conn
                print(f"[PY] ✓ Registered client: Dominio='{dominio}', LocalId='{local_id}', DeviceId='{device_id}'")
                
                # Volver a modo sin timeout para lectura continua
                conn.settimeout(None)
            except json.JSONDecodeError as e:
                print(f"[PY] Error parsing JSON from {addr}: {e}")
                print(f"[PY] Raw data received: {repr(registro_raw)}")
                conn.close()
                return

            # Leer respuestas del cliente (JSONs por líneas)
            buffer = b""
            while True:
                chunk = conn.recv(4096)
                if not chunk:
                    break
                
                buffer += chunk
                
                # Procesar líneas completas
                while b"\n" in buffer:
                    line, buffer = buffer.split(b"\n", 1)
                    if line:
                        try:
                            response = json.loads(line.decode("utf-8", errors="ignore"))
                            request_id = response.get("RequestId")
                            if request_id:
                                print(f"[PY] Respuesta recibida para RequestId: {request_id}")
                                with pending_lock:
                                    if request_id in pending_responses:
                                        pending_responses[request_id]["response"] = response
                                        pending_responses[request_id]["event"].set()
                        except json.JSONDecodeError as e:
                            print(f"[PY] Error parseando respuesta: {e}")
        except Exception as e:
            print(f"[PY] Client handler error: {e}")
        finally:
            with clients_lock:
                if client_key and client_key in clients:
                    print(f"[PY] Removing client: {client_key}")
                    del clients[client_key]
            conn.close()

    while True:
        conn, addr = server_sock.accept()
        t = threading.Thread(target=client_handler, args=(conn, addr), daemon=True)
        t.start()


@app.route("/ngrok", methods=["GET", "POST"])
def ngrok_endpoint():
    """Recibe ip, puerto, dominio, local_id, device_id; manda orden al cliente C# y espera su respuesta.

    URL de ejemplo:
    GET: http://localhost:5000/ngrok?ip=192.168.10.11&puerto=8082&dominio=lalena&local_id=LOCAL001&device_id=DEVICE123
    POST JSON: http://localhost:5000/ngrok (con JSON body)
    POST Form: http://localhost:5000/ngrok (con form-data o x-www-form-urlencoded)
    """
    # Soportar GET, POST JSON, POST form-data
    if request.method == "GET":
        ip = request.args.get("ip")
        puerto = request.args.get("puerto")
        dominio = request.args.get("dominio")
        local_id = request.args.get("local_id")
        device_id = request.args.get("device_id")
        target_method = "GET"
        target_path = request.args.get("path", "/")
        body_data = None
    else:  # POST
        # Detectar tipo de contenido
        content_type = request.content_type or ""
        
        if "application/json" in content_type:
            # POST con JSON
            data = request.get_json() or {}
            ip = data.get("ip")
            puerto = data.get("puerto")
            dominio = data.get("dominio")
            local_id = data.get("local_id")
            device_id = data.get("device_id")
            target_method = data.get("method", "POST")
            target_path = data.get("path", "/")
            body_data = data.get("body")
        else:
            # POST con form-data o x-www-form-urlencoded
            ip = request.form.get("ip")
            puerto = request.form.get("puerto")
            dominio = request.form.get("dominio")
            local_id = request.form.get("local_id")
            device_id = request.form.get("device_id")
            target_method = request.form.get("method", "POST")
            target_path = request.form.get("path", "/")
            body_data = request.form.get("body")

    if not ip or not puerto or not dominio or not local_id or not device_id:
        return jsonify({"ok": False, "error": "Faltan parametros: ip, puerto, dominio, local_id, device_id"}), 400

    try:
        puerto_int = int(puerto)
    except ValueError:
        return jsonify({"ok": False, "error": "puerto debe ser entero"}), 400

    client_key = (dominio, local_id, device_id)
    with clients_lock:
        client_sock = clients.get(client_key)

    if client_sock is None:
        return jsonify({"ok": False, "error": f"No hay cliente conectado para {client_key}"}), 404

    # Generar request_id único
    request_id = str(uuid.uuid4())
    
    # Crear evento para esperar la respuesta
    event = threading.Event()
    with pending_lock:
        pending_responses[request_id] = {"event": event, "response": None}

    # Recopilar headers del request original (excepto Host)
    headers_dict = {}
    for key, value in request.headers.items():
        if key.lower() not in ['host', 'content-length', 'connection']:
            headers_dict[key] = value
    
    # Enviar comando al cliente (C# espera PascalCase)
    cmd = {
        "Cmd": "connect",
        "Ip": ip,
        "Port": puerto_int,
        "RequestId": request_id,
        "Method": target_method,
        "Path": target_path,
        "Headers": headers_dict,
        "Body": body_data
    }
    msg = json.dumps(cmd).encode("utf-8") + b"\n"

    try:
        client_sock.sendall(msg)
        print("[PY] Comando enviado, esperando respuesta...")
    except Exception as e:
        with pending_lock:
            del pending_responses[request_id]
        return jsonify({"ok": False, "error": "Error enviando comando al cliente: " + str(e)}), 500

    # Esperar respuesta con timeout de 10 segundos
    if event.wait(timeout=10.0):
        with pending_lock:
            response_data = pending_responses[request_id]["response"]
            del pending_responses[request_id]
        
        if response_data and response_data.get("Success"):
            print("[PY] Respuesta exitosa recibida")
            return jsonify({
                "ok": True,
                "data": response_data.get("Data"),
                "message": "Respuesta del servidor local"
            })
        else:
            error_msg = response_data.get("Error") if response_data else "Error desconocido"
            return jsonify({"ok": False, "error": "Error del cliente: " + error_msg}), 500
    else:
        # Timeout
        with pending_lock:
            del pending_responses[request_id]
        print("[PY] Timeout esperando respuesta")
        return jsonify({"ok": False, "error": "Timeout esperando respuesta del cliente"}), 504


if __name__ == "__main__":
    import os
    
    # Levantamos el servidor de control TCP en un hilo
    control_thread = threading.Thread(target=start_control_server, daemon=True)
    control_thread.start()

    # Configuración SSL
    use_ssl = os.environ.get('USE_SSL', 'False').lower() == 'true'
    ssl_cert = os.environ.get('SSL_CERT', 'cert.pem')
    ssl_key = os.environ.get('SSL_KEY', 'key.pem')
    port = int(os.environ.get('FLASK_PORT', '5003'))

    if use_ssl:
        # Verificar que existen los archivos de certificado
        if not os.path.exists(ssl_cert):
            print(f"[ERROR] Certificado SSL no encontrado: {ssl_cert}")
            print("Coloca tu certificado wildcard en la carpeta del proyecto")
            print("O configura la variable SSL_CERT con la ruta correcta")
            exit(1)
        
        if not os.path.exists(ssl_key):
            print(f"[ERROR] Llave privada SSL no encontrada: {ssl_key}")
            print("Coloca tu llave privada en la carpeta del proyecto")
            print("O configura la variable SSL_KEY con la ruta correcta")
            exit(1)
        
        print(f"[PY] Iniciando servidor HTTPS en puerto {port}")
        print(f"[PY] Certificado: {ssl_cert}")
        print(f"[PY] Llave: {ssl_key}")
        
        # Servidor HTTPS Flask
        app.run(
            host="0.0.0.0",
            port=port,
            threaded=True,
            ssl_context=(ssl_cert, ssl_key)
        )
    else:
        # Servidor HTTP Flask (sin SSL)
        print(f"[PY] Iniciando servidor HTTP en puerto {port}")
        print("[PY] ADVERTENCIA: Servidor sin SSL. Para HTTPS, configura USE_SSL=true")
        app.run(host="0.0.0.0", port=port, threaded=True)
