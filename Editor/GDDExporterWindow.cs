using UnityEngine;
using UnityEditor;
using System.IO;

namespace TFGToolkit
{
    /// <summary>
    /// Ventana para configurar y lanzar la exportación del GDD.
    /// Dos botones: exportar por plantilla y generar con IA.
    /// </summary>
    public class GDDExporterWindow : EditorWindow
    {
        private GDDExporter.ExportConfig config = new GDDExporter.ExportConfig();
        private Vector2 scroll;
        private string lastExportMessage = "";
        private bool lastExportSuccess = false;

        // Estado de la generación por IA
        private bool isGeneratingAI = false;
        private string aiStatus = "";

        public static void ShowWindow()
        {
            var window = GetWindow<GDDExporterWindow>("Exportar GDD");
            window.minSize = new Vector2(420, 600);
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            // Cabecera
            GUILayout.Space(8);
            GUILayout.Label("Exportar Game Design Document", EditorStyles.boldLabel);
            GUILayout.Label("Genera un GDD a partir de las fichas del proyecto", EditorStyles.miniLabel);
            GUILayout.Space(12);

            // Datos del juego
            GUILayout.Label("Datos del juego", EditorStyles.boldLabel);
            config.GameTitle = EditorGUILayout.TextField("Título", config.GameTitle);
            config.Author = EditorGUILayout.TextField("Autor", config.Author);
            config.Version = EditorGUILayout.TextField("Versión", config.Version);
            GUILayout.Label("Descripción general");
            config.GameDescription = EditorGUILayout.TextArea(config.GameDescription, GUILayout.Height(60));
            GUILayout.Space(10);

            // Secciones a incluir
            GUILayout.Label("Secciones a incluir", EditorStyles.boldLabel);
            config.IncludeMechanics = EditorGUILayout.Toggle("Mecánicas", config.IncludeMechanics);
            config.IncludeCharacters = EditorGUILayout.Toggle("Personajes", config.IncludeCharacters);
            config.IncludeDialogues = EditorGUILayout.Toggle("Diálogos", config.IncludeDialogues);
            config.IncludeArtStyle = EditorGUILayout.Toggle("Guía de estilo", config.IncludeArtStyle);
            config.IncludeMechanicGraph = EditorGUILayout.Toggle("Relaciones (grafos)", config.IncludeMechanicGraph);
            config.IncludeNotes = EditorGUILayout.Toggle("Notas de diseño", config.IncludeNotes);
            GUILayout.Space(10);

            // Formato
            GUILayout.Label("Formato de exportación", EditorStyles.boldLabel);
            config.ExportMarkdown = EditorGUILayout.Toggle("Markdown (.md)", config.ExportMarkdown);
            config.ExportHTML = EditorGUILayout.Toggle("HTML (.html)", config.ExportHTML);
            GUILayout.Space(10);

            // Carpeta destino
            GUILayout.Label("Carpeta de destino", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            config.OutputFolder = EditorGUILayout.TextField(config.OutputFolder);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string selected = EditorUtility.OpenFolderPanel("Selecciona carpeta de destino", Application.dataPath, "");
                if (!string.IsNullOrEmpty(selected))
                {
                    string projectPath = Application.dataPath.Replace("/Assets", "");
                    if (selected.StartsWith(projectPath))
                        selected = selected.Substring(projectPath.Length + 1);
                    config.OutputFolder = selected;
                }
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(16);

            // Vista previa
            DrawPreviewInfo();
            GUILayout.Space(12);

            bool canExport = config.ExportHTML || config.ExportMarkdown;

            // --- Botón 1: exportar por plantilla ---
            GUI.enabled = canExport && !isGeneratingAI;
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
            if (GUILayout.Button("Exportar GDD (plantilla)", GUILayout.Height(36)))
                DoExport();
            GUI.backgroundColor = Color.white;

            GUILayout.Space(6);

            // --- Botón 2: generar con IA ---
            GUI.backgroundColor = new Color(0.55f, 0.4f, 0.9f);
            if (GUILayout.Button(isGeneratingAI ? "Generando con IA..." : "✦ Generar GDD con IA", GUILayout.Height(36)))
                DoExportAI();
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            if (!canExport)
                EditorGUILayout.HelpBox("Selecciona al menos un formato de exportación.", MessageType.Warning);

            if (isGeneratingAI && !string.IsNullOrEmpty(aiStatus))
                EditorGUILayout.HelpBox(aiStatus, MessageType.Info);

            // Mensaje del último export
            if (!string.IsNullOrEmpty(lastExportMessage))
            {
                GUILayout.Space(8);
                EditorGUILayout.HelpBox(lastExportMessage, lastExportSuccess ? MessageType.Info : MessageType.Error);
                if (lastExportSuccess && GUILayout.Button("Abrir carpeta"))
                {
                    string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", config.OutputFolder));
                    EditorUtility.RevealInFinder(fullPath);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawPreviewInfo()
        {
            GUILayout.Label("Vista previa", EditorStyles.boldLabel);
            int mechanicCount = config.IncludeMechanics ? CountAssets<MechanicData>() : 0;
            int characterCount = config.IncludeCharacters ? CountAssets<CharacterData>() : 0;
            int dialogueCount = config.IncludeDialogues ? CountAssets<DialogueData>() : 0;
            int styleCount = config.IncludeArtStyle ? CountAssets<ArtStyleGuideData>() : 0;
            int graphCount = config.IncludeMechanicGraph ? CountAssets<MechanicGraphData>() : 0;

            EditorGUILayout.HelpBox(
                $"Se exportarán:\n" +
                $"  • {mechanicCount} mecánica(s)\n" +
                $"  • {characterCount} personaje(s)\n" +
                $"  • {dialogueCount} árbol(es) de diálogo\n" +
                $"  • {styleCount} guía(s) de estilo\n" +
                $"  • {graphCount} grafo(s) de mecánicas\n" +
                $"\nDestino: {config.OutputFolder}",
                MessageType.None);
        }

        // Exportación por plantilla
        private void DoExport()
        {
            try
            {
                GDDExporter.Export(config);
                lastExportSuccess = true;
                lastExportMessage = $"GDD exportado correctamente en:\n{config.OutputFolder}";
            }
            catch (System.Exception e)
            {
                lastExportSuccess = false;
                lastExportMessage = $"Error al exportar: {e.Message}";
                Debug.LogError($"[TFG Toolkit] Error exportando GDD: {e}");
            }
            Repaint();
        }

        // Generación con IA
        private void DoExportAI()
        {
            isGeneratingAI = true;
            aiStatus = "Recopilando datos del proyecto y consultando a la IA...";
            lastExportMessage = "";
            Repaint();

            string prompt = GDDExporter.BuildGDDPrompt(config);

            OllamaAPI.SendMessage(
                prompt,
                onResult: result =>
                {
                    try
                    {
                        GDDExporter.SaveAIGeneratedGDD(result, config);
                        lastExportSuccess = true;
                        lastExportMessage = $"GDD generado por IA y guardado en:\n{config.OutputFolder}\n(archivos GDD_IA.md / GDD_IA.html)";
                    }
                    catch (System.Exception e)
                    {
                        lastExportSuccess = false;
                        lastExportMessage = $"Error al guardar el GDD de IA: {e.Message}";
                    }
                    isGeneratingAI = false;
                    aiStatus = "";
                    Repaint();
                },
                onError: error =>
                {
                    lastExportSuccess = false;
                    lastExportMessage = $"Error de la IA: {error}";
                    isGeneratingAI = false;
                    aiStatus = "";
                    Repaint();
                });
        }

        private int CountAssets<T>() where T : ScriptableObject
            => AssetDatabase.FindAssets($"t:{typeof(T).Name}").Length;
    }
}