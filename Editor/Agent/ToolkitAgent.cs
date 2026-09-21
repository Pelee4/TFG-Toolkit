using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;

namespace TFGToolkit.Agent
{

    /// <summary>
    /// Núcleo del agente. Coordina:
    /// 1. Construcción del conº historial de conversación
    /// 3. Despacho de acciones si la IA devuelve JSON
    /// 4. Devolución de la respuesta a la ventana de chat
    /// </summary>

    public class ToolkitAgent
    {
        //Historial de la conversacion
        private readonly List<(string role, string content)> _history = new();

        private readonly ActionDispatcher _dispatcher = new();

        //Llamada desde AgentWindow
        public void SendMessage (string userMessage, Action<string> onResponse, Action<string> onError = null)
        {
            //Añadir mensaje al historial de mensajes
            _history.Add(("user", userMessage));

            //Construir el prompt completo
            string systemPrompt = BuildSystemPrompt();
            string userPrompt = BuildUserPrompt(userMessage);


            // 3. Llamar a la API — toda la lógica va DENTRO del callback
            OllamaAPI.SendMessageWithSystem(
                systemPrompt,
                userPrompt,
                onResult: rawResponse =>
                {
                    // 4. Intentar despachar como acción
                    string actionResult = _dispatcher.TryDispatch(rawResponse);
                    string finalResponse = actionResult != null
                        ? $"{actionResult}\n\n_{rawResponse}_"
                        : rawResponse;

                    // 5. Guardar en historial y devolver
                    _history.Add(("assistant", finalResponse));
                    onResponse?.Invoke(finalResponse);
                },
                onError: error =>
                {
                    // Quitamos el mensaje del usuario del historial si falló
                    if (_history.Count > 0 && _history[_history.Count - 1].role == "user")
                        _history.RemoveAt(_history.Count - 1);
                    onError?.Invoke(error);
                }
            );

            ////Esperar la respuesta
            //float timeout = 0;
            //while (rawResponse == null && !gotError)
            //{
            //    await System.Threading.Tasks.Task.Delay(100);
            //    timeout = 0.1f;
            //    if (timeout > 30f)
            //    {
            //        onError?.Invoke("Timeout: la API no respondió en 30 segundos.");
            //        return;
            //    }
            //}
            //if (gotError) return;

            //// Intentar despachar como accion
            //string actionResult = _dispatcher.TryDispatch(rawResponse);

            //string finalResponse;
            //if(actionResult != null)
            //{
            //    // La IA envió una acción — mostramos el resultado de ejecutarla
            //    finalResponse = $"{actionResult}\n\n_{rawResponse}_";
            //}
            //else
            //{
            //    finalResponse = rawResponse;
            //}

            //// Añadimos respuesta al historial
            //_history.Add(("assistant", finalResponse));

            //onResponse?.Invoke(finalResponse);
        }

        // Creamos el system Prompt
        private string BuildSystemPrompt()
        {
            var sb = new StringBuilder();

            sb.AppendLine("Eres el asistente de diseño del TFG Toolkit, integrado en Unity Editor.");
            sb.AppendLine("Tu función es ayudar al diseñador de videojuegos con su proyecto.");
            sb.AppendLine("Tienes acceso completo al estado del proyecto y puedes ejecutar acciones directamente.");
            sb.AppendLine();
            sb.AppendLine("REGLAS:");
            sb.AppendLine("- Responde siempre en español.");
            sb.AppendLine("- Sé conciso y práctico.");
            sb.AppendLine("- Cuando el diseñador pida crear, modificar o ejecutar algo, usa las acciones disponibles.");
            sb.AppendLine("- Cuando solo pida información o consejo, responde con texto normal.");
            sb.AppendLine("- No inventes información sobre el proyecto — usa solo lo que aparece en el contexto.");
            sb.AppendLine();

            // Descripción de acciones disponibles
            sb.AppendLine(_dispatcher.BuildActionsDescription());

            // Contexto actual del proyecto
            sb.AppendLine(AgentContextBuilder.Build());

            return sb.ToString();
        }

        //Costruimos el prompt completo con el historial
        private string BuildFullPrompt(string currentMessage)
        {
            var sb = new StringBuilder();

            sb.AppendLine(BuildSystemPrompt());
            sb.AppendLine("--- HISTORIAL DE CONVERSACIÓN ---");

            // Solo los últimos 10 turnos para no exceder el límite de tokens
            int start = System.Math.Max(0, _history.Count - 11); // -11 porque el actual todavía no está
            for (int i = start; i < _history.Count - 1; i++)
            {
                var (role, content) = _history[i];
                string roleLabel = role == "user" ? "Diseñador" : "Asistente";
                sb.AppendLine($"{roleLabel}: {content}");
            }

            sb.AppendLine("--- FIN DEL HISTORIAL ---");
            sb.AppendLine();
            sb.AppendLine($"Diseñador: {currentMessage}");
            sb.AppendLine("Asistente:");

            return sb.ToString();
        }

        /// <summary>
        /// Construye solo el mensaje del usuario con el historial reciente.
        /// El system prompt va separado en SendMessageWithSystem.
        /// </summary>
        private string BuildUserPrompt(string currentMessage)
        {
            var sb = new StringBuilder();

            // Historial reciente (últimos 8 turnos)
            int start = Math.Max(0, _history.Count - 9);
            bool hasHistory = _history.Count > 1;

            if (hasHistory)
            {
                sb.AppendLine("--- CONVERSACIÓN ANTERIOR ---");
                for (int i = start; i < _history.Count - 1; i++)
                {
                    var (role, content) = _history[i];
                    string label = role == "user" ? "Diseñador" : "Asistente";
                    // Truncamos mensajes largos del historial para no saturar el contexto
                    string truncated = content.Length > 500
                        ? content.Substring(0, 500) + "..."
                        : content;
                    sb.AppendLine($"{label}: {truncated}");
                }
                sb.AppendLine("--- FIN ---");
                sb.AppendLine();
            }

            sb.AppendLine(currentMessage);
            return sb.ToString();
        }

        public void ClearHistory() => _history.Clear();

        public IReadOnlyList<(string role, string content)> History => _history;
    }
}

