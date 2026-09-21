using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFGToolkit
{
    /// <summary>
    /// Cliente para modelos de IA locales servidos por Ollama.
    ///
    /// REQUISITOS:
    ///   1. Tener Ollama instalado: https://ollama.com/download
    ///   2. Haber descargado el modelo: ollama pull qwen2.5-coder:7b
    ///   3. Ollama corriendo en segundo plano (se inicia automáticamente al instalar)
    ///
    /// USO:
    ///   OllamaAPI.SendMessage("tu prompt", result => { }, error => { });
    ///
    /// CAMBIAR MODELO:
    ///   Modifica MODEL o cámbialo en ToolkitConfig si prefieres configurarlo desde el Inspector.
    /// </summary>
    public static class OllamaAPI
    {
        // -------------------------------------------------------
        // Configuración
        // -------------------------------------------------------

        // URL del servidor Ollama — siempre localhost mientras sea local
        private const string BASE_URL = "http://localhost:11434";

        // Modelo por defecto. Opciones probadas con Unity:
        //   qwen2.5-coder:7b  — mejor para generar código C# (recomendado, ~4.5 GB)
        //   qwen2.5:3b  — más ligero si tienes poca RAM (~2 GB)
        //   codellama:7b      — alternativa para código (~4 GB)
        public static string Model = "qwen2.5:3b";

        private static readonly HttpClient _client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(120) // los modelos locales pueden ser lentos
        };

        // -------------------------------------------------------
        // Método principal — mismo contrato que GeminiAPI y AnthropicAPI
        // -------------------------------------------------------

        /// <summary>
        /// Envía un prompt a Ollama y devuelve la respuesta por callback.
        /// onResult se llama cuando Ollama responde.
        /// onError se llama si Ollama no está corriendo o hay otro problema.
        /// </summary>
        public static async void SendMessage(
            string prompt,
            Action<string> onResult,
            Action<string> onError = null)
        {
            // Verificamos que Ollama está corriendo antes de enviar
            bool isRunning = await CheckOllamaRunning();
            if (!isRunning)
            {
                onError?.Invoke(
                    "Ollama no está corriendo. Ábrelo desde el menú de aplicaciones " +
                    "o ejecuta 'ollama serve' en una terminal.");
                return;
            }

            try
            {
                string jsonBody = BuildRequestJson(prompt);

                var request = new HttpRequestMessage(HttpMethod.Post, $"{BASE_URL}/api/chat");
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _client.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    onError?.Invoke($"Error {(int)response.StatusCode}: {responseBody}");
                    return;
                }

                string text = ExtractContent(responseBody);
                onResult?.Invoke(text);
            }
            catch (HttpRequestException)
            {
                onError?.Invoke(
                    "No se pudo conectar con Ollama. " +
                    "Asegúrate de que está instalado y corriendo (ollama serve).");
            }
            catch (TaskCanceledException)
            {
                onError?.Invoke(
                    "Timeout: el modelo tardó más de 120 segundos. " +
                    "Prueba con un modelo más pequeño (qwen2.5-coder:3b).");
            }
            catch (Exception e)
            {
                onError?.Invoke($"Error inesperado: {e.Message}");
            }
        }

        /// <summary>
        /// Versión con system prompt separado.
        /// Útil para el agente, que necesita pasar el contexto del proyecto como system.
        /// </summary>
        public static async void SendMessageWithSystem(
            string systemPrompt,
            string userMessage,
            Action<string> onResult,
            Action<string> onError = null)
        {
            bool isRunning = await CheckOllamaRunning();
            if (!isRunning)
            {
                onError?.Invoke("Ollama no está corriendo. Ejecuta 'ollama serve' en una terminal.");
                return;
            }

            try
            {
                string jsonBody = BuildRequestJsonWithSystem(systemPrompt, userMessage);

                var request = new HttpRequestMessage(HttpMethod.Post, $"{BASE_URL}/api/chat");
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _client.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    onError?.Invoke($"Error {(int)response.StatusCode}: {responseBody}");
                    return;
                }

                string text = ExtractContent(responseBody);
                onResult?.Invoke(text);
            }
            catch (Exception e)
            {
                onError?.Invoke($"Error: {e.Message}");
            }
        }

        // -------------------------------------------------------
        // Helpers de construcción de JSON
        // -------------------------------------------------------

        private static string BuildRequestJson(string prompt)
        {
            return $@"{{
                ""model"": ""{Model}"",
                ""messages"": [
                    {{
                        ""role"": ""user"",
                        ""content"": {JsonEscape(prompt)}
                    }}
                ],
                ""stream"": false
            }}";
        }

        private static string BuildRequestJsonWithSystem(string system, string user)
        {
            return $@"{{
                ""model"": ""{Model}"",
                ""messages"": [
                    {{
                        ""role"": ""system"",
                        ""content"": {JsonEscape(system)}
                    }},
                    {{
                        ""role"": ""user"",
                        ""content"": {JsonEscape(user)}
                    }}
                ],
                ""stream"": false
            }}";
        }

        // -------------------------------------------------------
        // Extracción de la respuesta
        // -------------------------------------------------------

        /// <summary>
        /// Extrae el campo message.content de la respuesta de Ollama.
        /// Respuesta: { "message": { "role": "assistant", "content": "AQUI" } }
        /// </summary>
        private static string ExtractContent(string json)
        {
            // Buscamos "content": "..."
            const string marker = "\"content\":";
            int markerIdx = json.IndexOf(marker);
            if (markerIdx < 0) return json;

            int start = json.IndexOf('"', markerIdx + marker.Length) + 1;
            int end = start;

            while (end < json.Length)
            {
                if (json[end] == '\\') { end += 2; continue; }
                if (json[end] == '"') { break; }
                end++;
            }

            string raw = json.Substring(start, end - start);
            raw = raw.Replace("\\n", "\n")
                     .Replace("\\t", "\t")
                     .Replace("\\\"", "\"")
                     .Replace("\\\\", "\\");
            return raw;
        }

        // -------------------------------------------------------
        // Verificación de disponibilidad
        // -------------------------------------------------------

        /// <summary>
        /// Comprueba si Ollama está corriendo haciendo una petición al endpoint de estado.
        /// </summary>
        private static async Task<bool> CheckOllamaRunning()
        {
            try
            {
                var check = await _client.GetAsync($"{BASE_URL}/api/tags");
                return check.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Devuelve los modelos instalados en Ollama.
        /// Útil para mostrarlos en un selector en la ventana de configuración.
        /// </summary>
        public static async void GetInstalledModels(
            Action<string[]> onResult,
            Action<string> onError = null)
        {
            try
            {
                string response = await _client.GetStringAsync($"{BASE_URL}/api/tags");

                // Extraemos los nombres de los modelos del JSON
                var models = new System.Collections.Generic.List<string>();
                var matches = System.Text.RegularExpressions.Regex.Matches(
                    response, "\"name\":\\s*\"([^\"]+)\"");
                foreach (System.Text.RegularExpressions.Match m in matches)
                    models.Add(m.Groups[1].Value);

                onResult?.Invoke(models.ToArray());
            }
            catch (Exception e)
            {
                onError?.Invoke($"Error obteniendo modelos: {e.Message}");
            }
        }

        // -------------------------------------------------------
        // Helpers
        // -------------------------------------------------------
        private static string JsonEscape(string text)
        {
            if (string.IsNullOrEmpty(text)) return "\"\"";
            text = text.Replace("\\", "\\\\")
                       .Replace("\"", "\\\"")
                       .Replace("\n", "\\n")
                       .Replace("\r", "\\r")
                       .Replace("\t", "\\t");
            return $"\"{text}\"";
        }
    }
}