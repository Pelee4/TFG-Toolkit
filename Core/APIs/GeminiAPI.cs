using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFGToolkit
{
    /// <summary>
    /// Clase est�tica reutilizable para llamar a la API de Gemini (Google).
    /// �sala exactamente igual que AnthropicAPI:
    ///
    ///   GeminiAPI.SendMessage("tu prompt", resultado => {
    ///       // resultado es el string que devuelve Gemini
    ///   });
    ///
    /// API Key gratuita en: https://aistudio.google.com
    /// </summary>
    public static class GeminiAPI
    {
        // -------------------------------------------------------
        // CONFIGURACI�N � solo toca esto
        // -------------------------------------------------------
        private const string MODEL = "gemini-2.0-flash"; // Modelo gratuito
        private const string API_KEY = ""; // Tu API Key de Google
        private const int MAX_TOKENS = 1024;

        // La URL incluye el modelo y la key como par�metro (as� funciona Gemini)
        private static string API_URL =>
            $"https://generativelanguage.googleapis.com/v1/models/{MODEL}:generateContent?key={API_KEY}";
        // -------------------------------------------------------

        private static readonly HttpClient client = new HttpClient();

        /// <summary>
        /// Env�a un prompt a Gemini y devuelve la respuesta por callback.
        /// onResult se ejecuta cuando Gemini responde correctamente.
        /// onError se llama si algo falla.
        /// </summary>
        public static async void SendMessage(
            string prompt,
            Action<string> onResult,
            Action<string> onError = null)
        {
            try
            {
                // Formato JSON que espera la API de Gemini
                string jsonBody = $@"{{
                    ""contents"": [
                        {{
                            ""parts"": [
                                {{
                                    ""text"": {JsonEscape(prompt)}
                                }}
                            ]
                        }}
                    ],
                    ""generationConfig"": {{
                        ""maxOutputTokens"": {MAX_TOKENS}
                    }}
                }}";

                var request = new HttpRequestMessage(HttpMethod.Post, API_URL);
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    onError?.Invoke($"Error {response.StatusCode}: {responseBody}");
                    return;
                }

                string text = ExtractTextFromResponse(responseBody);
                onResult?.Invoke(text);
            }
            catch (Exception e)
            {
                onError?.Invoke($"Excepci�n: {e.Message}");
            }
        }

        // -------------------------------------------------------
        // Helpers de parseo manual (sin dependencias externas)
        // -------------------------------------------------------

        /// <summary>
        /// Escapa un string para incluirlo como valor JSON.
        /// </summary>
        private static string JsonEscape(string text)
        {
            text = text.Replace("\\", "\\\\")
                       .Replace("\"", "\\\"")
                       .Replace("\n", "\\n")
                       .Replace("\r", "\\r")
                       .Replace("\t", "\\t");
            return $"\"{text}\"";
        }

        /// <summary>
        /// Extrae el texto de la respuesta de Gemini.
        /// La respuesta tiene esta estructura:
        /// {
        ///   "candidates": [
        ///     {
        ///       "content": {
        ///         "parts": [
        ///           { "text": "AQUI_ESTA_LA_RESPUESTA" }
        ///         ]
        ///       }
        ///     }
        ///   ]
        /// }
        /// </summary>
        private static string ExtractTextFromResponse(string json)
        {
            const string marker = "\"text\":";
            int markerIndex = json.IndexOf(marker);
            if (markerIndex < 0) return json; // fallback: devuelve el json crudo

            int start = json.IndexOf('"', markerIndex + marker.Length) + 1;
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
    }
}