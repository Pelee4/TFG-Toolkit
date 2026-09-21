using UnityEngine;
using UnityEditor;
using TFGToolkit;
using TFGToolkit.Agent;

public class ToolkitWindow : EditorWindow
{

    [MenuItem("TFG Toolkit/Abrir Toolkit")]

    public static void ShowWindow()
    {
        GetWindow<ToolkitWindow>("TFG Toolkit");
    }

    private void OnGUI()
    {
        GUILayout.Label("Game Design Toolkit", EditorStyles.boldLabel);

        if(GUILayout.Button("Fichas de mecanicas"))
        {
            MechanicEditorWindow.ShowWindow();
        }

        if(GUILayout.Button("Fichas de personajes"))
        {
            CharacterEditorWindow.ShowWindow();
        }

        if (GUILayout.Button("Exportar GDD"))
        {
            GDDExporterWindow.ShowWindow();
        }

        if(GUILayout.Button("Notas de diseño"))
        {
            DesignNotesWindow.ShowWindow();
        }

        if (GUILayout.Button("Kanban"))
        {
            ProjectBoardWindow.ShowWindow();
        }

        if (GUILayout.Button("Agente IA"))
        {
            AgentWindow.ShowWindow();
        }

        if (GUILayout.Button("Grafo de mecanicas"))
        {
            MechanicGraphWindow.ShowWindow();
        }

        if (GUILayout.Button("Grafo de dialogos"))
        {
            DialogueGraphWindow.ShowWindow();
        }

        if(GUILayout.Button("Libreria de Prototipos"))
        {
            PrototypeLibraryWindow.ShowWindow();
        }

        if(GUILayout.Button("Guia de Estilo"))
        {
            ArtStyleGuideWindow.ShowWindow();
        }
    }
}