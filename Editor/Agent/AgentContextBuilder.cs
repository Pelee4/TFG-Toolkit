using System.Collections.Generic;
using System.Linq;
using System.Text;
using TFGToolkit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TFGToolkit.Agent
{
    /// <summary>
    /// Lee el estado actual del proyecto de Unity y construye un resumen
    /// que se inyecta en el system prompt de cada llamada a la IA.
    ///
    /// CÓMO AMPLIAR EL CONTEXTO:
    /// Cuando se añada un nuevo tipo de asset al toolkit (puzzles, niveles, etc.)
    /// añadir un método BuildXxxContext() privado y llamarlo desde Build().
    /// El agente automáticamente tendrá conocimiento del nuevo tipo.
    /// </summary>

public class AgentContextBuilder
{
    /// <summary>
    /// Construye el contexto completo del proyecto.
    /// Se llama antes de cada petición a la IA.
    /// </summary>
    public static string Build()
    {
        var sb = new StringBuilder();

        sb.AppendLine("--- Contexto del proyecto de Unity ---");
        sb.AppendLine();

        BuildMechanicsContext(sb);
        BuildCharactersContext(sb);
        BuildNotesContext(sb);
        BuildSceneContext(sb);
        BuildProjectStructureContext(sb);

        sb.AppendLine("--- Fin del contexto");
        return sb.ToString();
    }

    // -- Mecanicas --
    private static void BuildMechanicsContext(StringBuilder sb)
    {
        var mechanics = LoadAssets<MechanicData>();
        sb.AppendLine($"Mecanicas ({mechanics.Count} total): ");

        if (mechanics.Count == 0)
        {
            sb.AppendLine(" (ninguna) ");
        }
        else
        {
            foreach (var m in mechanics)
            {
                string status = m.status switch
                {
                    MechanicStatus.Idea => "Idea",
                    MechanicStatus.EnDesarrollo => "En desarrollo",
                    MechanicStatus.Implementada => "Implementada",
                    _ => "?"
                };
                sb.AppendLine($"  - {m.mechanicName} [{status}]");
                if (!string.IsNullOrEmpty(m.description))
                    sb.AppendLine($"    Descripción: {m.description}");
                if (!string.IsNullOrEmpty(m.input))
                    sb.AppendLine($"    Input: {m.input} | Efecto: {m.effect} | Duración: {m.duration}s");
                if (!string.IsNullOrEmpty(m.generatedScript))
                    sb.AppendLine($"    Script: generado ({m.generatedScript.Length} chars)");
            }
        }
        sb.AppendLine();
    }


    // -- Perosnajes --
    private static void BuildCharactersContext(StringBuilder sb)
    {
        var characters = LoadAssets<CharacterData>();
        sb.AppendLine($"PERSONAJES ({characters.Count} total):");

        if (characters.Count == 0)
        {
            sb.AppendLine("  (ninguno)");
        }
        else
        {
            foreach (var c in characters)
            {
                sb.AppendLine($"  - {c.characterName} [{c.role}]");
                if (!string.IsNullOrEmpty(c.description))
                    sb.AppendLine($"    {c.description}");
                sb.AppendLine($"    Stats: vida={c.health} ataque={c.attack} defensa={c.defense} velocidad={c.speed}");
                if (c.relatedMechanics != null && c.relatedMechanics.Length > 0)
                {
                    var names = c.relatedMechanics
                        .Where(m => m != null)
                        .Select(m => m.mechanicName);
                    sb.AppendLine($"    Mecánicas: {string.Join(", ", names)}");
                }
            }
        }
        sb.AppendLine();
    }


    // -- Notas de Diseño --
    private static void BuildNotesContext(StringBuilder sb)
    {
        var notesData = DesignNotesWindow.LoadNotesForAgent();
        if (notesData == null || notesData.Notes == null || notesData.Notes.Count == 0)
        {
            sb.AppendLine("NOTAS DE DISEÑO: (ninguna)");
        }
        else
        {
            var pending = notesData.Notes.Where(n => !n.Resolved).ToList();
            sb.AppendLine($"NOTAS DE DISEÑO ({pending.Count} pendientes de {notesData.Notes.Count} total):");
            foreach (var note in pending.Take(10)) // máx 10 para no saturar
            {
                sb.AppendLine($"  [{note.Type}] {note.Title}");
                if (!string.IsNullOrEmpty(note.Content))
                    sb.AppendLine($"    {note.Content.Substring(0, System.Math.Min(100, note.Content.Length))}...");
            }
        }
        sb.AppendLine();
    }


    // -- Escena Activa --
    private static void BuildSceneContext(StringBuilder sb)
    {
        var scene = EditorSceneManager.GetActiveScene();
        sb.AppendLine($"ESCENA ACTIVA: {scene.name} ({scene.path})");

        var rootObjects = scene.GetRootGameObjects();
        sb.AppendLine($"  GameObjects raíz ({rootObjects.Length}):");
        foreach (var go in rootObjects.Take(15)) // máx 15
        {
            var components = go.GetComponents<Component>()
                .Where(c => c != null)
                .Select(c => c.GetType().Name)
                .Where(n => n != "Transform")
                .ToList();
            string compList = components.Count > 0 ? $" [{string.Join(", ", components)}]" : "";
            sb.AppendLine($"    - {go.name}{compList} (activo: {go.activeSelf})");
        }
        if (rootObjects.Length > 15)
            sb.AppendLine($"    ... y {rootObjects.Length - 15} más");
        sb.AppendLine();
    }


    // -- Estructura de carpetas del proyecto
    private static void BuildProjectStructureContext(StringBuilder sb)
    {
        sb.AppendLine("ESTRUCTURA DEL PROYECTO:");
        sb.AppendLine($"  Scripts generados: {AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/TFGToolkit/Scripts/Generated" }).Length}");
        sb.AppendLine($"  Assets de mecánicas: {AssetDatabase.FindAssets("t:MechanicData").Length}");
        sb.AppendLine($"  Assets de personajes: {AssetDatabase.FindAssets("t:CharacterData").Length}");
        sb.AppendLine();
    }


    // -- Helper para cargar assets --
    private static List<T> LoadAssets<T>() where T : ScriptableObject
    {
        var result = new List<T>();
        foreach (var guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null) result.Add(asset);
        }
        return result;
    }
}

}