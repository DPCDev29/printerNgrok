using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PuertoInverso
{
    class Program
    {
        // Configura estos valores según dónde esté corriendo tu servidor Python
        private const string ServerHost = "printer.restaurant.pe"; // IP o dominio del servidor Python
        private const int ServerPort = 5002;            // Puerto del servidor de control TCP en Python
        private static NetworkStream controlStream = null;

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== C# Reverse Client (.NET Framework 4.5.2) ===");

            if (args.Length < 3)
            {
                Console.WriteLine("Uso: PuertoInverso.exe <dominio> <local_id> <device_id>");
                Console.WriteLine("Ejemplo: PuertoInverso.exe lalena LOCAL001 DEVICE123");
                return;
            }

            string dominio = args[0];
            string localId = args[1];
            string deviceId = args[2];

            int reconnectDelay = 5000; // 5 segundos inicial
            int maxReconnectDelay = 60000; // Máximo 60 segundos
            
            while (true)
            {
                try
                {
                    using (var client = new TcpClient())
                    {
                        Console.WriteLine($"[CS] Conectando a {ServerHost}:{ServerPort}...");
                        
                        // Configurar timeouts
                        client.ReceiveTimeout = 30000; // 30 segundos
                        client.SendTimeout = 10000; // 10 segundos
                        
                        await client.ConnectAsync(ServerHost, ServerPort);
                        Console.WriteLine("[CS] ✓ Conectado al servidor de control");

                        using (NetworkStream stream = client.GetStream())
                        {
                            controlStream = stream;

                            // Enviar registro con dominio, local_id, device_id
                            var registro = new
                            {
                                Dominio = dominio,
                                LocalId = localId,
                                DeviceId = deviceId
                            };
                            string registroJson = JsonConvert.SerializeObject(registro);
                            byte[] registroBytes = Encoding.UTF8.GetBytes(registroJson + "\n");
                            
                            Console.WriteLine($"[CS] Enviando registro: {registroJson}");
                            await stream.WriteAsync(registroBytes, 0, registroBytes.Length);
                            await stream.FlushAsync();
                            
                            // Pequeño delay para asegurar que llegó
                            await Task.Delay(100);
                            
                            Console.WriteLine($"[CS] ✓ Registrado con Dominio: '{dominio}', LocalId: '{localId}', DeviceId: '{deviceId}'");
                            
                            // Reset reconnect delay tras conexión exitosa
                            reconnectDelay = 5000;

                            // Bucle principal: esperar comandos del servidor
                            var buffer = new byte[4096];
                            var sb = new StringBuilder();

                            while (true)
                            {
                                int read = await stream.ReadAsync(buffer, 0, buffer.Length);
                                if (read == 0)
                                {
                                    Console.WriteLine("[CS] Servidor cerró la conexion");
                                    break;
                                }

                                sb.Append(Encoding.UTF8.GetString(buffer, 0, read));

                                // Procesar por líneas (terminadas en \n)
                                string content = sb.ToString();
                                int newlineIndex;
                                while ((newlineIndex = content.IndexOf('\n')) >= 0)
                                {
                                    string line = content.Substring(0, newlineIndex).Trim();
                                    content = content.Substring(newlineIndex + 1);

                                    if (!string.IsNullOrWhiteSpace(line))
                                    {
                                        await HandleCommandAsync(line);
                                    }
                                }

                                sb.Clear();
                                sb.Append(content);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CS] ✗ Error: {ex.Message}");
                    Console.WriteLine($"[CS] Reintentando en {reconnectDelay / 1000}s...");
                    
                    await Task.Delay(reconnectDelay);
                    
                    // Incrementar delay con backoff exponencial
                    reconnectDelay = Math.Min(reconnectDelay * 2, maxReconnectDelay);
                }
            }
        }

        private static async Task HandleCommandAsync(string line)
        {
            string requestId = null;
            try
            {
                Console.WriteLine($"[CS] Comando recibido: {line}");

                var cmd = JsonConvert.DeserializeObject<CommandMessage>(line);
                if (cmd == null || cmd.Cmd != "connect")
                {
                    Console.WriteLine("[CS] Comando desconocido o nulo");
                    return;
                }

                requestId = cmd.RequestId;

                // Conexión inversa a la IP/puerto local indicada
                using (var localClient = new TcpClient())
                {
                    localClient.ReceiveTimeout = 5000; // Timeout de 5 segundos
                    localClient.SendTimeout = 5000;

                    Console.WriteLine($"[CS] Conectando a {cmd.Ip}:{cmd.Port}...");
                    await localClient.ConnectAsync(cmd.Ip, cmd.Port);
                    Console.WriteLine("[CS] Conexión local establecida");

                    using (NetworkStream localStream = localClient.GetStream())
                    {
                        Console.WriteLine("[CS] Stream local abierto");
                        
                        // Construir petición HTTP con el método, headers y body recibidos
                        string method = cmd.Method ?? "GET";
                        string path = cmd.Path ?? "/";
                        
                        Console.WriteLine($"[CS] Construyendo petición HTTP {method} {path}");
                        
                        var httpRequestBuilder = new StringBuilder();
                        httpRequestBuilder.AppendLine($"{method} {path} HTTP/1.1");
                        httpRequestBuilder.AppendLine($"Host: {cmd.Ip}:{cmd.Port}");
                        
                        // Agregar headers personalizados
                        if (cmd.Headers != null && cmd.Headers.Count > 0)
                        {
                            foreach (var header in cmd.Headers)
                            {
                                if (!string.IsNullOrEmpty(header.Key) && !string.IsNullOrEmpty(header.Value))
                                {
                                    httpRequestBuilder.AppendLine($"{header.Key}: {header.Value}");
                                }
                            }
                        }
                        
                        // Si hay body, agregar Content-Length
                        if (!string.IsNullOrEmpty(cmd.Body))
                        {
                            byte[] bodyBytes = Encoding.UTF8.GetBytes(cmd.Body);
                            bool hasContentLength = cmd.Headers != null && cmd.Headers.ContainsKey("Content-Length");
                            if (!hasContentLength)
                            {
                                httpRequestBuilder.AppendLine($"Content-Length: {bodyBytes.Length}");
                            }
                            httpRequestBuilder.AppendLine("Connection: close");
                            httpRequestBuilder.AppendLine(); // Línea vacía antes del body
                            httpRequestBuilder.Append(cmd.Body);
                        }
                        else
                        {
                            httpRequestBuilder.AppendLine("Connection: close");
                            httpRequestBuilder.AppendLine(); // Línea vacía final
                        }
                        
                        string httpRequest = httpRequestBuilder.ToString();
                        byte[] requestBytes = Encoding.UTF8.GetBytes(httpRequest);
                        
                        Console.WriteLine($"[CS] Enviando petición HTTP {method} al servidor...");
                        Console.WriteLine("--- PETICIÓN HTTP ---");
                        Console.WriteLine(httpRequest);
                        Console.WriteLine("--- FIN PETICIÓN ---");
                        
                        await localStream.WriteAsync(requestBytes, 0, requestBytes.Length);
                        await localStream.FlushAsync();
                        Console.WriteLine("[CS] Petición HTTP enviada, esperando respuesta...");
                        
                        // Leer la respuesta del servidor local
                        var buffer = new byte[8192];
                        var responseData = new List<byte>();
                        int totalRead = 0;
                        
                        // Dar un poco de tiempo para que el servidor responda
                        await Task.Delay(100);
                        
                        // Leer con timeout más largo para servidores lentos
                        localStream.ReadTimeout = 5000;
                        
                        try
                        {
                            bool firstRead = true;
                            while (totalRead < 1024 * 100) // Máximo 100KB
                            {
                                int bytesRead = 0;
                                
                                // Primera lectura o si hay datos disponibles
                                if (firstRead || localStream.DataAvailable)
                                {
                                    bytesRead = await localStream.ReadAsync(buffer, 0, buffer.Length);
                                    firstRead = false;
                                    
                                    if (bytesRead == 0) 
                                    {
                                        Console.WriteLine("[CS] Servidor cerró la conexión (0 bytes)");
                                        break;
                                    }
                                    
                                    Console.WriteLine($"[CS] Leídos {bytesRead} bytes en esta iteración");
                                    responseData.AddRange(buffer.Take(bytesRead));
                                    totalRead += bytesRead;
                                    
                                    // Pequeña pausa para ver si llegan más datos
                                    await Task.Delay(50);
                                }
                                else
                                {
                                    // No hay más datos disponibles
                                    break;
                                }
                            }
                        }
                        catch (IOException ex)
                        {
                            // Timeout o fin de lectura, continuar con lo que tengamos
                            Console.WriteLine($"[CS] IOException al leer: {ex.Message}");
                        }

                        string responseText = Encoding.UTF8.GetString(responseData.ToArray());
                        
                        Console.WriteLine("----------------------------------------");
                        Console.WriteLine($"[CS] TOTAL RECIBIDO: {totalRead} bytes del servidor local");
                        Console.WriteLine("[CS] RESPUESTA COMPLETA (con headers HTTP):");
                        Console.WriteLine(responseText);
                        Console.WriteLine("----------------------------------------");

                        // Extraer solo el cuerpo de la respuesta HTTP (después de los headers)
                        string bodyOnly = responseText;
                        int bodyStart = responseText.IndexOf("\r\n\r\n");
                        if (bodyStart >= 0)
                        {
                            bodyOnly = responseText.Substring(bodyStart + 4);
                            Console.WriteLine("[CS] Cuerpo HTTP extraído (sin headers):");
                            Console.WriteLine(bodyOnly);
                            Console.WriteLine("----------------------------------------");
                        }
                        else
                        {
                            Console.WriteLine("[CS] ADVERTENCIA: No se encontraron headers HTTP, enviando respuesta completa");
                        }

                        // Enviar respuesta de vuelta al servidor Python
                        await SendResponseToPython(requestId, true, bodyOnly, null);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CS] Error manejando comando: {ex.Message}");
                // Enviar error de vuelta al servidor Python
                if (requestId != null)
                {
                    await SendResponseToPython(requestId, false, null, ex.Message);
                }
            }
        }

        private static async Task SendResponseToPython(string requestId, bool success, string data, string error)
        {
            try
            {
                var response = new ResponseMessage
                {
                    RequestId = requestId,
                    Success = success,
                    Data = data,
                    Error = error
                };

                // JSON en UNA SOLA LÍNEA (sin formato) para el protocolo
                string jsonCompact = JsonConvert.SerializeObject(response, Formatting.None);
                
                // JSON con formato para mostrar en consola
                string jsonPretty = JsonConvert.SerializeObject(response, Formatting.Indented);
                
                // Mostrar la respuesta que se enviará
                Console.WriteLine("========================================");
                Console.WriteLine("[CS] RESPUESTA QUE SE ENVIARA A PYTHON:");
                Console.WriteLine("========================================");
                Console.WriteLine($"RequestId: {requestId}");
                Console.WriteLine($"Success: {success}");
                if (success && data != null)
                {
                    Console.WriteLine($"Data Length: {data.Length} caracteres");
                    Console.WriteLine("Data Preview (primeros 500 chars):");
                    Console.WriteLine(data.Substring(0, Math.Min(500, data.Length)));
                    if (data.Length > 500)
                    {
                        Console.WriteLine("... [truncado]");
                    }
                }
                if (!success && error != null)
                {
                    Console.WriteLine($"Error: {error}");
                }
                Console.WriteLine("========================================");
                Console.WriteLine("JSON formateado (solo para visualización):");
                Console.WriteLine(jsonPretty);
                Console.WriteLine("========================================");
                Console.WriteLine("JSON compacto que se enviará (una línea):");
                Console.WriteLine(jsonCompact);
                Console.WriteLine("========================================");

                byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonCompact + "\n");

                if (controlStream != null && controlStream.CanWrite)
                {
                    await controlStream.WriteAsync(jsonBytes, 0, jsonBytes.Length);
                    await controlStream.FlushAsync();
                    Console.WriteLine($"[CS] ✓ Respuesta enviada exitosamente al servidor Python");
                }
                else
                {
                    Console.WriteLine("[CS] ✗ No se pudo enviar: stream no disponible");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CS] ✗ Error enviando respuesta a Python: {ex.Message}");
            }
        }

        private class CommandMessage
        {
            public string Cmd { get; set; }
            public string Ip { get; set; }
            public int Port { get; set; }
            public string RequestId { get; set; }
            public string Method { get; set; }
            public string Path { get; set; }
            public Dictionary<string, string> Headers { get; set; }
            public string Body { get; set; }
        }

        private class ResponseMessage
        {
            public string RequestId { get; set; }
            public bool Success { get; set; }
            public string Data { get; set; }
            public string Error { get; set; }
        }
    }
}
