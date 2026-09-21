using System.Collections.Generic;
using System.IO;
using TFGToolkit.Agent;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static TreeEditor.TreeEditorHelper;


namespace TFGToolkit.Agent.Actions
{

    //Accion : --- Crear nueva mecánica ---
    public class CreateMechanicAction : IAgentAction
    {
        public string ActionName => "create_mechanic";

        public string Description => 
            "Crea una nueva ficha de mecánica en el proyecto. " +
            "Parámetros: name (string), description (string, opcional), " +
            "input (string, opcional), effect (string, opcional), duration (float, opcional).";

        public string Execute(Dictionary<string, string> args)
        {
            string folder = "Assets/TFGToolkit/Data/Mechanic";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/TFGToolkit/Data", "Mechanic");

            var mechanic = ScriptableObject.CreateInstance<MechanicData>();

            if (args.TryGetValue("name",        out var name))        mechanic.mechanicName = name;
            if (args.TryGetValue("description", out var desc))        mechanic.description  = desc;
            if (args.TryGetValue("input",       out var input))       mechanic.input        = input;
            if (args.TryGetValue("effect",      out var effect))      mechanic.effect       = effect;
            if (args.TryGetValue("duration",    out var durStr)
                && float.TryParse(durStr, out float dur))             mechanic.duration     = dur;

            string safeName = string.IsNullOrEmpty(mechanic.mechanicName) ? "NuevaMecanica" : mechanic.mechanicName;
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{safeName}.asset");
            AssetDatabase.CreateAsset(mechanic, path);
            AssetDatabase.SaveAssets();

            return $"Mecánica '{mechanic.mechanicName}' creada en {path}";

        }

    }


    //Accion: --- Actualizar estado de una mecanica
    public class UpdateMechanicStatusAction : IAgentAction
    {
        public string ActionName => "update_mechanic_status";
        public string Description =>
            "Actualiza el estado de una mecánica existente. " +
            "Parámetros: name (string, nombre exacto), " +
            "status (string: 'Idea' | 'EnDesarrollo' | 'Implementada').";

        public string Execute(Dictionary<string, string> args)
        {
            if (!args.TryGetValue("name", out var name))
                return "Falta el parámetro 'name'.";
            if (!args.TryGetValue("status", out var statusStr))
                return "Falta el parámetro 'status'.";

            foreach (var guid in AssetDatabase.FindAssets("t:MechanicData"))
            {
                var m = AssetDatabase.LoadAssetAtPath<MechanicData>(AssetDatabase.GUIDToAssetPath(guid));
                if (m == null || m.mechanicName != name) continue;

                m.status = statusStr switch
                {
                    "Idea" => MechanicStatus.Idea,
                    "EnDesarrollo" => MechanicStatus.EnDesarrollo,
                    "Implementada" => MechanicStatus.Implementada,
                    _ => m.status
                };
                EditorUtility.SetDirty(m);
                AssetDatabase.SaveAssets();
                return $"Estado de '{name}' actualizado a '{statusStr}'";
            }
            return $"No se encontró ninguna mecánica con nombre '{name}'";
        }
    }


    //Accion: --- Crear nota de diseño
    public class CreateNoteAction : IAgentAction
    {
        public string ActionName => "create_note";
        public string Description =>
            "Crea una nota de diseño en el sistema de notas del toolkit. " +
            "Parámetros: title (string), content (string, opcional), " +
            "type (string: 'Nota' | 'Decision' | 'Problema' | 'Idea'), " +
            "author (string, opcional).";

        public string Execute(Dictionary<string, string> args)
        {
            args.TryGetValue("title", out var title);
            args.TryGetValue("content", out var content);
            args.TryGetValue("type", out var typeStr);
            args.TryGetValue("author", out var author);

            if (string.IsNullOrEmpty(title))
                return "Falta el parámetro 'title'.";

            NoteType type = typeStr switch
            {
                "Decision" => NoteType.Decision,
                "Problema" => NoteType.Problema,
                "Idea" => NoteType.Idea,
                _ => NoteType.Nota
            };

            var note = new DesignNote
            {
                Id = System.Guid.NewGuid().ToString(),
                Title = title,
                Content = content ?? "",
                Type = type,
                Author = author ?? "Agente IA",
                Date = System.DateTime.Now.ToString("dd/MM/yyyy HH:mm")
            };

            DesignNotesWindow.AddNoteFromAgent(note);
            return $"Nota '{title}' [{type}] creada correctamente";
        }
    }
    
        
        
    //Accion: -- Generar script desde una mecanica
    public class GenerateScriptAction : IAgentAction
    {
        public string ActionName => "generate_script";
        public string Description =>
            "Genera un script de Unity en C# para una mecánica existente usando IA. " +
            "Parámetros: mechanic_name (string, nombre exacto de la mecánica).";

        public string Execute(Dictionary<string, string> args)
        {
            if (!args.TryGetValue("mechanic_name", out var mechName))
                return "Falta el parámetro 'mechanic_name'.";

            foreach (var guid in AssetDatabase.FindAssets("t:MechanicData"))
            {
                var m = AssetDatabase.LoadAssetAtPath<MechanicData>(AssetDatabase.GUIDToAssetPath(guid));
                if (m == null || m.mechanicName != mechName) continue;

                // Reutilizamos la misma lógica que MechanicEditorWindow
                // pero disparada desde el agente
                string prompt = BuildScriptPrompt(m);
                string result = "Generando script... (abre la ficha de mecánicas para ver el resultado)";

                GeminiAPI.SendMessage(
                    prompt,
                    onResult: script =>
                    {
                        m.generatedScript = script;
                        EditorUtility.SetDirty(m);
                        AssetDatabase.SaveAssets();

                        // Guarda también en disco como .cs
                        SaveScriptToDisk(m);
                    },
                    onError: err => Debug.LogError($"[Agente] Error generando script: {err}")
                );

                return result;
            }
            return $"No se encontró mecánica '{mechName}'";
        }

        private static string BuildScriptPrompt(MechanicData m)
        {
            return $@"Eres un experto en Unity C#. Genera un MonoBehaviour para esta mecánica.
                    Nombre: {m.mechanicName}
                    Descripción: {m.description}
                    Input: {m.input}
                    Efecto: {m.effect}
                    Duración: {m.duration}s
                    Devuelve solo código C#, sin markdown.";
        }

        private static void SaveScriptToDisk(MechanicData m)
        {
            string folder = "Assets/TFGToolkit/Scripts/Generated";
            Directory.CreateDirectory(folder);
            string className = ToPascalCase(m.mechanicName);
            string path = $"{folder}/{className}.cs";
            File.WriteAllText(path, m.generatedScript, System.Text.Encoding.UTF8);
            AssetDatabase.Refresh();
        }

        private static string ToPascalCase(string s)
        {
            if (string.IsNullOrEmpty(s)) return "NuevaMecanica";
            var words = s.Trim().Split(' ');
            var sb = new System.Text.StringBuilder();
            foreach (var w in words)
            {
                if (w.Length == 0) continue;
                sb.Append(char.ToUpper(w[0]));
                sb.Append(w.Substring(1).ToLower());
            }
            return sb.ToString();
        }
    }


    //Accion: -- Manipular la escena -- (crear, mover, borrar objetos, añadir componentes...)
    public class SceneAction : IAgentAction
    {
        public string ActionName => "scene_action";
        public string Description =>
            "Manipula GameObjects en la escena activa de Unity. " +
            "Parámetros: operation ('create_gameobject' | 'find_and_move' | 'delete_gameobject' | 'add_component'), " +
            "name (string), " +
            "position_x/y/z (float, opcional), " +
            "component (string, nombre del componente para add_component).";

        public string Execute(Dictionary<string, string> args)
        {
            if (!args.TryGetValue("operation", out var op))
                return "Falta el parámetro 'operation'.";
            if (!args.TryGetValue("name", out var objName))
                return "Falta el parámetro 'name'.";

            Vector3 pos = ParsePosition(args);

            switch (op)
            {
                case "create_gameobject":
                    return CreateGameObject(objName, pos);

                case "find_and_move":
                    return FindAndMove(objName, pos);

                case "delete_gameobject":
                    return DeleteGameObject(objName);

                case "add_component":
                    args.TryGetValue("component", out var compName);
                    return AddComponent(objName, compName);

                default:
                    return $"Operación desconocida: '{op}'";
            }
        }

        //Helpers para las acciones
        private string CreateGameObject(string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = go;
            return $"GameObject '{name}' creado en {pos}";
        }

        private string FindAndMove(string name, Vector3 pos)
        {
            var go = GameObject.Find(name);
            if (go == null) return $"No se encontró '{name}' en la escena";
            go.transform.position = pos;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            return $"'{name}' movido a {pos}";
        }

        private string DeleteGameObject(string name)
        {
            var go = GameObject.Find(name);
            if (go == null) return $"No se encontró '{name}'";
            bool confirm = EditorUtility.DisplayDialog(
                "Confirmar eliminación",
                $"¿Eliminar '{name}' de la escena?",
                "Eliminar", "Cancelar");
            if (!confirm) return "Eliminación cancelada por el usuario.";
            Object.DestroyImmediate(go);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            return $"'{name}' eliminado de la escena";
        }

        private string AddComponent(string goName, string compName)
        {
            if (string.IsNullOrEmpty(compName))
                return "Falta el parámetro 'component'.";
            var go = GameObject.Find(goName);
            if (go == null) return $"No se encontró '{goName}'";

            var type = System.Type.GetType(compName) ??
                        System.Type.GetType($"UnityEngine.{compName}, UnityEngine");
            if (type == null)
                return $"Componente '{compName}' no encontrado. Usa el nombre exacto de la clase.";

            go.AddComponent(type);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            return $"Componente '{compName}' añadido a '{goName}'";
        }

        private static Vector3 ParsePosition(Dictionary<string, string> args)
        {
            float x = 0, y = 0, z = 0;
            if (args.TryGetValue("position_x", out var sx)) float.TryParse(sx, out x);
            if (args.TryGetValue("position_y", out var sy)) float.TryParse(sy, out y);
            if (args.TryGetValue("position_z", out var sz)) float.TryParse(sz, out z);
            return new Vector3(x, y, z);
        }
    }


    public class GenerateGDDAction : IAgentAction
    {
        public string ActionName => "generate_gdd";
        public string Description =>
            "Genera el Game Design Document del proyecto a partir de todas las fichas. " +
            "Parámetros: title (string, opcional), author (string, opcional), " +
            "use_ai ('true' para que la IA redacte el documento, 'false' para usar la plantilla). " +
            "Exporta a Markdown y HTML en Assets/TFGToolkit/GDD.";

        public string Execute(Dictionary<string, string> args)
        {
            var config = new GDDExporter.ExportConfig();

            if (args.TryGetValue("title", out var title) && !string.IsNullOrEmpty(title))
                config.GameTitle = title;
            if (args.TryGetValue("author", out var author) && !string.IsNullOrEmpty(author))
                config.Author = author;
            if (args.TryGetValue("version", out var version) && !string.IsNullOrEmpty(version))
                config.Version = version;

            bool useAI = args.TryGetValue("use_ai", out var aiFlag)
                         && aiFlag.ToLower() == "true";

            if (!useAI)
            {
                // Vía plantilla: síncrona, devolvemos el resultado al momento
                try
                {
                    GDDExporter.Export(config);
                    return $"GDD generado por plantilla en {config.OutputFolder} (GDD.md / GDD.html)";
                }
                catch (System.Exception e)
                {
                    return $"Error generando el GDD: {e.Message}";
                }
            }
            else
            {
                // Vía IA: asíncrona. Lanzamos la petición y devolvemos un mensaje
                // intermedio. El archivo se guarda cuando la IA responde.
                string prompt = GDDExporter.BuildGDDPrompt(config);

                OllamaAPI.SendMessage(
                    prompt,
                    onResult: result =>
                    {
                        GDDExporter.SaveAIGeneratedGDD(result, config);
                        Debug.Log($"[TFG Toolkit] GDD (IA) guardado en {config.OutputFolder}");
                    },
                    onError: error =>
                        Debug.LogError($"[TFG Toolkit] Error generando GDD con IA: {error}"));

                return "Generando el GDD con IA... se guardará en " +
                       $"{config.OutputFolder} (GDD_IA.md / GDD_IA.html) cuando termine.";
            }
        }
    }

}
