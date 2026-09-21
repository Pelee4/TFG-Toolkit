using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Globalization;

namespace TFGToolkit
{
public class MechanicEditorWindow : EditorWindow
{
        //Estado actual de la ventana
        private List<MechanicData> mechanics = new List<MechanicData>();
        private MechanicData selectedMechanic;
        private Vector2 listScroll;
        private Vector2 detailScroll;

        //Campos necesarios para la llamada de la IA
        private bool isGenerating = false;
        private string statusMessage = ""; //Mensaje que le aparece al usuario

        //Llamamos a este metodo desde la ventana principal del toolkit para abrir esta ventana
        public static void ShowWindow()
        {
            var window = GetWindow<MechanicEditorWindow>("Fichas de mecánicas");
            window.minSize = new Vector2(600, 400); //Tamaño de la ventana (ajustable)
            window.RefreshMechanicList();
        }
        
        //Metodo para refrescar las mecanicas
        private void OnEnable()
        {
            RefreshMechanicList();
        }

        private void RefreshMechanicList()
        {
            mechanics.Clear();
            //t: es el filtro para encontrar los assets de ese tipo en la database
            string[] guids = AssetDatabase.FindAssets("t:MechanicData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MechanicData data = AssetDatabase.LoadAssetAtPath<MechanicData>(path);
                if (data != null) mechanics.Add(data);
            }
        }


        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            DrawMechanicList();
            DrawMechanicDetail();

            EditorGUILayout.EndHorizontal();
        }

        //--------------------------------------
        // PRIMER PANEL - Lista de mecanicas
        //--------------------------------------
        private void DrawMechanicList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(200)); //Ajustable

            GUILayout.Label("Mecánicas", EditorStyles.boldLabel);

            if (GUILayout.Button("+ Nueva Mecánica")) // Crea el boton dentro del propio if, no hace falta crearlo fuera
            {
                CreateNewMechanic();
            }

            if (GUILayout.Button(" Actualizar Lista"))
                RefreshMechanicList();

            GUILayout.Space(4);

            listScroll = EditorGUILayout.BeginScrollView(listScroll);

            foreach (var mechanic in mechanics)
            {
                bool isSelected = (mechanic == selectedMechanic);
                GUIStyle style = isSelected ? new GUIStyle(EditorStyles.toolbarButton) { fontStyle = FontStyle.Bold } : EditorStyles.toolbarButton;

                //Muestra un icono junto al nombre dependiendo del status de la mecanica
                string statusIcon = mechanic.status switch
                {
                    MechanicStatus.Idea => "💡",
                    MechanicStatus.EnDesarrollo => "🔧",
                    MechanicStatus.Implementada => "✅",
                                              _ => ""
                };

                if (GUILayout.Button($"{statusIcon} {mechanic.mechanicName}", style))
                    SelectMechanic(mechanic);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }


        //-----------------------------------------------------------
        // PANEL DERECHO - Detalle de la mecanica seleccionada
        //-----------------------------------------------------------
        private void DrawMechanicDetail()
        {
            EditorGUILayout.BeginVertical();

            if (selectedMechanic == null)
            {
                GUILayout.FlexibleSpace(); //Ver que hace
                GUILayout.Label("Seleccion o crea una mecánica.", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
                return;
            }

            detailScroll = EditorGUILayout.BeginScrollView(detailScroll);

            GUILayout.Label("Ficha de Mecánica", EditorStyles.boldLabel);
            GUILayout.Space(4);

            //Usamos SerializedObject para no se hay que investigarlo jej
            SerializedObject so = new SerializedObject(selectedMechanic);
            so.Update();

            EditorGUILayout.PropertyField(so.FindProperty("mechanicName"), new GUIContent("Nombre"));
            EditorGUILayout.PropertyField(so.FindProperty("description"), new GUIContent("Descripcion"));

            GUILayout.Space(6);
            GUILayout.Label("Definición", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("input"), new GUIContent("Input"));
            EditorGUILayout.PropertyField(so.FindProperty("effect"), new GUIContent("Efecto"));
            EditorGUILayout.PropertyField(so.FindProperty("duration"), new GUIContent("Duracion"));

            GUILayout.Space(6);
            EditorGUILayout.PropertyField(so.FindProperty("status"), new GUIContent("Estado"));

            GUILayout.Space(8);
            GUILayout.Label("Script generado", EditorStyles.boldLabel);

            //--------------------------
            // LLAMADA A LA API
            //--------------------------

            //Desactivamos el boton si esta generando
            GUI.enabled = !isGenerating;
            GUI.backgroundColor = new Color(0.4f, 0.7f, 1.0f);

            if (GUILayout.Button(isGenerating ? "Generando..." : "Generar Script"))
            {
                GenerateScriptWithAI();
            }


            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            //Mensaje de estado de la llamada
            if (!string.IsNullOrEmpty(statusMessage))
            {
                GUIStyle statusStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    wordWrap = true,
                    normal = {textColor = statusMessage.StartsWith("Error") ? new Color (1f, 0.4f, 0.4f) : Color.gray}
                };
                GUILayout.Label(statusMessage, statusStyle);
            }

            EditorGUILayout.PropertyField(so.FindProperty("generatedScript"), new GUIContent(""));

            so.ApplyModifiedProperties();

            GUILayout.Space(8);

            //Botones de Accion
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Guardar Asset"))
            {
                // Guarda las modificaciones internas
                EditorUtility.SetDirty(selectedMechanic);
                AssetDatabase.SaveAssets();

                // Gestionar renombrado del fichero
                string currentPath = AssetDatabase.GetAssetPath(selectedMechanic);
                string newName = selectedMechanic.mechanicName;

                // Evitar nombres vacíos o caracteres raros
                if (string.IsNullOrWhiteSpace(newName))
                    newName = "NuevaMecanica";

                // Formateamos quitando espacios y caracteres inválidos
                newName = newName.Replace(" ", "").Replace("/", "").Replace("\\", "");

                // Renombramos si el nombre original difiere del nombre nuevo (obviando extension)
                string currentFileName = System.IO.Path.GetFileNameWithoutExtension(currentPath);
                if (currentFileName != newName)
                {
                    AssetDatabase.RenameAsset(currentPath, newName);
                    AssetDatabase.SaveAssets();
                    RefreshMechanicList(); // Refresca la barra izquierda para ver el nombre nuevo
                }
            }

            if (GUILayout.Button("Seleccionar en proyecto")){
                EditorGUIUtility.PingObject(selectedMechanic);
            }

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("🗑 Eliminar"))
                DeleteSelectedMechanic();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }


        //-------------------------------
        // Helpers
        //-------------------------------
        private void SelectMechanic(MechanicData mechanic)
        {
            selectedMechanic = mechanic;
            GUI.FocusControl(null); //Para quitar el focus del teclado de la mecanica de la izquierda
        }

        private void CreateNewMechanic()
        {
            //Crea el asset en la ruta pertinente
            string folder = "Assets/TFGToolkit/Data/Mechanic";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/TFGToolkit/Data", "Mechanic");
            }

            MechanicData newMechanic = CreateInstance<MechanicData>();
            //Lo que se genera es el asset, luego a partir de este, la IA genera el script .cs
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/NuevaMecanica.asset");

            AssetDatabase.CreateAsset(newMechanic, path);
            AssetDatabase.SaveAssets();

            RefreshMechanicList();
            SelectMechanic(newMechanic);
        }

        private void DeleteSelectedMechanic()
        {
            if (selectedMechanic == null) return;
            bool confirm = EditorUtility.DisplayDialog(
                "Eliminar mecanica",
                $"¿Seguro que quieres eliminar '{selectedMechanic.mechanicName}'?",
                "Eliminar", "Cancelar"
                );

            if (!confirm) return;

            string path = AssetDatabase.GetAssetPath(selectedMechanic);
            AssetDatabase.DeleteAsset(path);
            selectedMechanic = null;
            RefreshMechanicList();
        }
        

        private void GenerateScriptWithAI()
        {
            //Activamos el estado de carga
            isGenerating = true;
            statusMessage = "Conectando con la API...";
            Repaint();

            if (string.IsNullOrEmpty(selectedMechanic.mechanicName))
            {
                statusMessage = "Error: La mecánica debe tener un nombre.";
                isGenerating = false;
                return;
            }

            //Construimos el prompt con los datos de la mecanica (modificable)
            string prompt = $@"Eres un experto en desarrollo de videojuegos con Unity y C#.
Genera un script de Unity en C# para implementar la siguiente mecánica de juego.
 
Mecánica: {selectedMechanic.mechanicName}
Descripción: {selectedMechanic.description}
Input del jugador: {selectedMechanic.input}
Efecto: {selectedMechanic.effect}
Duración: {selectedMechanic.duration} segundos (0 = instantáneo o permanente)
 
Requisitos:
- Nombre de clase: {(selectedMechanic.mechanicName)}, pero con mayusculas en el inicio de cada palabra y sin espacios.
- Hereda de MonoBehaviour
- Incluye comentarios explicando cada parte
- Usa Input.GetKeyDown o el nuevo Input System según corresponda
- Código limpio siguiendo buenas prácticas de Unity
 
Devuelve únicamente el código C#, sin explicaciones ni bloques markdown.";


            // Paso 3 — Llamar a la API con dos callbacks: éxito y error
            OllamaAPI.SendMessage(
                prompt,

                onResult: result =>
                {
                    // Guardamos el resultado en el asset
                    selectedMechanic.generatedScript = result;
                    EditorUtility.SetDirty(selectedMechanic);
                    AssetDatabase.SaveAssets();

                    isGenerating = false;
                    statusMessage = "✅ Script generado correctamente.";
                    Repaint();
                },

                onError: error =>
                {
                    isGenerating = false;
                    statusMessage = $"Error: {error}";
                    Debug.LogError($"[TFG Toolkit] {error}");
                    Repaint();
                }
            );
        }
    }

}

