using System.Collections.Generic;
using TFGToolkit.Agent.Actions;
using UnityEngine;

namespace TFGToolkit.Agent 
{

    /// <summary>
    /// Registra todas las acciones disponibles y despacha las que llegan
    /// en la respuesta JSON de la IA.
    /// 
    /// Para añadir una solo se añade la linea correspondiente en el RegisterActions()
    /// </summary>
    public class ActionDispatcher
    {
        private readonly Dictionary<string, IAgentAction> _actions = new();

        public ActionDispatcher()
        {
            RegisterActions();
        }

        //Registro de las acciones definidas en AgentActions.cs
        private void RegisterActions()
        {
            Register(new CreateMechanicAction());
            Register(new UpdateMechanicStatusAction());
            Register(new CreateNoteAction());
            Register(new GenerateScriptAction());
            Register(new SceneAction());
            Register(new GenerateGDDAction());
        }

        private void Register(IAgentAction action)
        {
            _actions[action.ActionName] = action;
        }

        /// <summary>
        /// Parsea la respuesta de la IA y ejecuta la acción si la encuentra.
        /// Devuelve null si no hay acción en la respuesta (respuesta conversacional normal).
        /// </summary>
        public string TryDispatch(string apiResponse)
        {
            var parsed = TryParseActionJson(apiResponse);
            if (parsed == null) return null;

            if (!parsed.TryGetValue("action", out var actionName))
                return null;

            if (!_actions.TryGetValue(actionName, out var action))
            {
                Debug.LogWarning($"[TFG Agente] Acción desconocida: '{actionName}'");
                return $"Acción desconocida: '{actionName}'";
            }

            return action.Execute(parsed);
        }

        /// <summary>
        /// Devuelve la descripción de todas las acciones registradas.
        /// Se inyecta en el system prompt para que la IA sepa qué puede hacer.
        /// </summary>
        public string BuildActionsDescription()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("ACCIONES DISPONIBLES (responde con JSON cuando quieras ejecutar una):");
            sb.AppendLine();
            foreach (var action in _actions.Values)
            {
                sb.AppendLine($"  - {action.ActionName}: {action.Description}");
            }
            sb.AppendLine();
            sb.AppendLine("Formato JSON para ejecutar una acción:");
            sb.AppendLine("  {\"action\": \"nombre_accion\", \"param1\": \"valor1\", ...}");
            sb.AppendLine();
            sb.AppendLine("Si no necesitas ejecutar ninguna acción, responde con texto normal.");
            return sb.ToString();
        }

        // -------------------------------------------------------
        // Parser JSON manual
        // -------------------------------------------------------

        /// <summary>
        /// Detecta si la respuesta de la IA contiene un bloque JSON con "action"
        /// y extrae los pares clave-valor como Dictionary.
        /// Busca el JSON tanto si viene solo como si viene dentro de texto.
        /// </summary>
        private static Dictionary<string, string> TryParseActionJson(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;

            // Busca el primer { y el último } que lo cierre
            int start = text.IndexOf('{');
            int end = text.LastIndexOf('}');
            if (start < 0 || end < 0 || end <= start) return null;

            string json = text.Substring(start, end - start + 1);

            // Debe contener "action" para considerarse una instrucción
            if (!json.Contains("\"action\"")) return null;

            var result = new Dictionary<string, string>();

            // Extraemos pares "clave": "valor" con regex básico
            var matches = System.Text.RegularExpressions.Regex.Matches(
                json,
                "\"([^\"]+)\"\\s*:\\s*\"([^\"]*)\"");

            foreach (System.Text.RegularExpressions.Match m in matches)
                result[m.Groups[1].Value] = m.Groups[2].Value;

            // También capturamos números sin comillas: "param": 1.5
            var numMatches = System.Text.RegularExpressions.Regex.Matches(
                json,
                "\"([^\"]+)\"\\s*:\\s*([0-9.-]+)");

            foreach (System.Text.RegularExpressions.Match m in numMatches)
                if (!result.ContainsKey(m.Groups[1].Value))
                    result[m.Groups[1].Value] = m.Groups[2].Value;

            return result.Count > 0 ? result : null;
        }

    }

        
}
