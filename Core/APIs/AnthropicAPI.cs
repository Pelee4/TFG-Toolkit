using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TFGToolkit
{

    /// <summary>
    /// Clase est�tica que sirve para llamar a la API de Anthropic (ClaudeAI).
    /// 
    /// Se usa de la siguiente manera en cualquier otra clase:
    ///     
    ///     AnthropicAPI.SendMessage("prompt", resultado => {
    ///         // el resultado es el string que devuelve Claude
    ///     }
    ///     
    /// </summary>

    public static class AnthropicAPI
    {
        // -----------------------------------
        // CONFIGURACION
        // -----------------------------------
        private const string API_URL = "https://api.anthropic.com/v1/messages";
        private const string MODEL = "claude-sonnet-4-20250514";
        private const int MAX_TOKENS = 1024;

        //MI API KEY PARA QUE FUNCIONE, CAMBIAR MAS ADELANTE
        private const string API_KEY = ""; 

        private static readonly HttpClient client = new HttpClient();

        /// <summary>
        /// 
        /// Env�a un promp a Claude y devuelve la respuesta con un callback.
        /// onResult se ejecuta en el hilo principal de Unity.
        /// onError se llama si falla algo.
        ///     
        /// </summary>


        public static async void SendMessage(string prompt, Action<string> onResult, Action<string> onError = null)
        {
            try
            {
                //Se construye el json manualmente
                string jsonBody = $@"{{
                    ""model"": ""{MODEL}"",
                    ""max_tokens"": {MAX_TOKENS},
                    ""messages"": [
                        {{
                            ""role"": ""user"",
                            ""content"": {JsonEscape(prompt)}
                        }}
                    ]
                }}";


                var request = new HttpRequestMessage(HttpMethod.Post, API_URL);
                request.Headers.Add("x-api-key", API_KEY);
                request.Headers.Add("anthropic-version", "2023-06-01"); //Revisar si hay mejores
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    onError?.Invoke($"Error {response.StatusCode}: {responseBody}");
                    return;
                }

                //Extraemos el texto de la respuesta
                string text = ExtractTextFromResponse(responseBody);
                onResult?.Invoke(text);

            }
            catch (Exception e)
            {
                onError?.Invoke($"Excepcion: {e.Message}");
            }
        }

        // ----------------------------
        // Helpers de parseo
        // ----------------------------

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
        /// Extrae el campo text del primer content block de la respuesta de Anthropic.
        /// La respuesta tiene esta forma:
        /// { "content": [ { "type": "text", "text": "AQUI_ESTA_LA_RESPUESTA" } ] }
        /// </summary>
        private static string ExtractTextFromResponse(string json)
        {
            // Buscamos "text": "..." dentro del primer bloque content
            const string marker = "\"text\":";
            int markerIndex = json.IndexOf(marker);
            if (markerIndex < 0) return json; // fallback: devuelve el json crudo

            int start = json.IndexOf('"', markerIndex + marker.Length) + 1;
            int end = start;

            // Recorremos char a char para encontrar el cierre correcto de la cadena
            while (end < json.Length)
            {
                if (json[end] == '\\') { end += 2; continue; } // saltar escaped chars
                if (json[end] == '"') { break; }
                end++;
            }

            string raw = json.Substring(start, end - start);

            // Desescapamos los caracteres m�s comunes
            raw = raw.Replace("\\n", "\n")
                     .Replace("\\t", "\t")
                     .Replace("\\\"", "\"")
                     .Replace("\\\\", "\\");

            return raw;
        }

    }

}