using System.Collections.Generic;
using UnityEditor;


namespace TFGToolkit.Agent
{
    /// <summary>
    /// Interfaz que sirce de plantilla para implementar cualquier accion del agente
    ///
    /// CÓMO AÑADIR UNA ACCIÓN NUEVA:
    /// 1. Crear un archivo NewAction.cs en Assets/TFGToolkit/Core/Agent/Actions/
    /// 2. Implementae esta interfaz
    /// 3. Regístrae en ActionDispatcher.cs
    ///
    /// EJEMPLO:
    ///   public class MiNuevaAction : IAgentAction {
    ///     public string ActionName => "mi_accion";
    ///     public string Description => "Hace algo útil";
    ///     public string Execute(Dictionary<string,string> args) {
    ///       // tu lógica aquí
    ///       return "Acción ejecutada";
    ///     }
    ///   }
    /// </summary>

public interface IAgentAction
{
    /// <summary>
    /// Nombre exacto de la acción tal como la devuelve la IA en el JSON.
    /// Ejemplo: "create_mechanic", "create_note", "generate_script"
    /// </summary>
    string ActionName { get; }

    /// <summary>
    /// Descripción corta que se inyecta en el system prompt para que la IA
    /// sepa cuándo usar esta acción y qué parámetros puede enviar.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Ejecuta la acción con los argumentos que devolvió la IA.
    /// Devuelve un string con el resultado (se muestra en el chat).
    /// </summary>
    string Execute(Dictionary<string, string> args);
}

}
