using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace TFGToolkit
{
    public class CharacterEditorWindow : EditorWindow
    {
        // --- Estado de la ventana ---
        private List<CharacterData> characters = new List<CharacterData>();
        private CharacterData selectedCharacter;
        private Vector2 listScroll;
        private Vector2 detailScroll;

        // -------------------------------------------------------
        // Llama a este método desde tu ventana principal con:
        // CharacterEditorWindow.ShowWindow();
        // -------------------------------------------------------
        public static void ShowWindow()
        {
            var window = GetWindow<CharacterEditorWindow>("Fichas de Personajes");
            window.minSize = new Vector2(650, 450);
            window.RefreshCharacterList();
        }

        private void OnEnable()
        {
            RefreshCharacterList();
        }

        // Busca todos los CharacterData que existan en el proyecto
        private void RefreshCharacterList()
        {
            characters.Clear();
            string[] guids = AssetDatabase.FindAssets("t:CharacterData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CharacterData data = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
                if (data != null) characters.Add(data);
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            DrawCharacterList();
            DrawCharacterDetail();
            EditorGUILayout.EndHorizontal();
        }

        // ----------------------------------------------------------
        // PANEL IZQUIERDO — Lista de personajes
        // ----------------------------------------------------------
        private void DrawCharacterList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(200));

            GUILayout.Label("Personajes", EditorStyles.boldLabel);

            if (GUILayout.Button("+ Nuevo Personaje"))
                CreateNewCharacter();

            if (GUILayout.Button("↺ Actualizar lista"))
                RefreshCharacterList();

            GUILayout.Space(4);

            listScroll = EditorGUILayout.BeginScrollView(listScroll);

            foreach (var character in characters)
            {
                bool isSelected = character == selectedCharacter;
                GUIStyle style = isSelected
                    ? new GUIStyle(EditorStyles.toolbarButton) { fontStyle = FontStyle.Bold }
                    : EditorStyles.toolbarButton;

                // Icono según el rol del personaje
                string roleIcon = character.role switch
                {
                    CharacterRole.Protagonista => "🦸",
                    CharacterRole.Enemigo => "🦹",
                    CharacterRole.NPC => "🧑",
                    CharacterRole.Compañero => "🤝",
                    CharacterRole.Jefe => "👑",
                    _ => "❓"
                };

                if (GUILayout.Button($"{roleIcon} {character.characterName}", style))
                    SelectCharacter(character);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ----------------------------------------------------------
        // PANEL DERECHO — Detalle y edición del personaje seleccionado
        // ----------------------------------------------------------
        private void DrawCharacterDetail()
        {
            EditorGUILayout.BeginVertical();

            if (selectedCharacter == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Selecciona o crea un personaje.", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
                return;
            }

            detailScroll = EditorGUILayout.BeginScrollView(detailScroll);

            GUILayout.Label("Ficha de Personaje", EditorStyles.boldLabel);
            GUILayout.Space(4);

            SerializedObject so = new SerializedObject(selectedCharacter);
            so.Update();

            // Identificación
            EditorGUILayout.PropertyField(so.FindProperty("characterName"), new GUIContent("Nombre"));
            EditorGUILayout.PropertyField(so.FindProperty("role"), new GUIContent("Rol"));
            EditorGUILayout.PropertyField(so.FindProperty("description"), new GUIContent("Descripción"));

            GUILayout.Space(6);

            // Trasfondo
            GUILayout.Label("Trasfondo", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("backstory"), new GUIContent("Historia"));
            EditorGUILayout.PropertyField(so.FindProperty("motivation"), new GUIContent("Motivación"));

            GUILayout.Space(6);

            // Estadísticas
            GUILayout.Label("Estadísticas base", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(so.FindProperty("health"), new GUIContent("Vida"));
            EditorGUILayout.PropertyField(so.FindProperty("attack"), new GUIContent("Ataque"));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(so.FindProperty("defense"), new GUIContent("Defensa"));
            EditorGUILayout.PropertyField(so.FindProperty("speed"), new GUIContent("Velocidad"));
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6);

            // Habilidades
            GUILayout.Label("Habilidades", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("abilities"), new GUIContent(""));

            GUILayout.Space(6);

            // Mecánicas asociadas — referencia cruzada con MechanicData
            GUILayout.Label("Mecánicas asociadas", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                so.FindProperty("relatedMechanics"),
                new GUIContent("Mecánicas"),
                includeChildren: true);

            // Botón para abrir la ventana de mecánicas directamente
            if (GUILayout.Button("📋 Abrir editor de mecánicas"))
                MechanicEditorWindow.ShowWindow();

            GUILayout.Space(6);

            // Notas
            GUILayout.Label("Notas de diseño", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("designNotes"), new GUIContent(""));

            GUILayout.Space(6);

            // Botón IA (placeholder, igual que en mecánicas)
            GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
            if (GUILayout.Button("✨ Generar descripción con IA"))
            {
                // TODO: llamar a AnthropicAPI con los datos del personaje
                Debug.Log($"[TFG Toolkit] Generando descripción para: {selectedCharacter.characterName}");
            }
            GUI.backgroundColor = Color.white;

            so.ApplyModifiedProperties();

            GUILayout.Space(8);

            // Botones de acción
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("💾 Guardar asset"))
            {
                // Guarda las modificaciones internas
                EditorUtility.SetDirty(selectedCharacter);
                AssetDatabase.SaveAssets();

                // Gestionar renombrado del fichero
                string currentPath = AssetDatabase.GetAssetPath(selectedCharacter);
                string newName = selectedCharacter.characterName;

                // Evitar nombres vacíos o caracteres raros
                if (string.IsNullOrWhiteSpace(newName))
                    newName = "NuevoPersonaje";

                // Formateamos quitando espacios y caracteres inválidos
                newName = newName.Replace(" ", "").Replace("/", "").Replace("\\", "");

                // Renombramos si el nombre original difiere del nombre nuevo
                string currentFileName = System.IO.Path.GetFileNameWithoutExtension(currentPath);
                if (currentFileName != newName)
                {
                    AssetDatabase.RenameAsset(currentPath, newName);
                    AssetDatabase.SaveAssets();
                    RefreshCharacterList(); // Refresca la barra izquierda
                }
            }

            if (GUILayout.Button("📄 Seleccionar en proyecto"))
                EditorGUIUtility.PingObject(selectedCharacter);

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("🗑 Eliminar"))
                DeleteSelectedCharacter();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ----------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------
        private void SelectCharacter(CharacterData character)
        {
            selectedCharacter = character;
            GUI.FocusControl(null);
        }

        private void CreateNewCharacter()
        {
            string folder = "Assets/TFGToolkit/Data/Character";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/TFGToolkit/Data", "Character");

            CharacterData newChar = CreateInstance<CharacterData>();
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/NuevoPersonaje.asset");
            AssetDatabase.CreateAsset(newChar, path);
            AssetDatabase.SaveAssets();

            RefreshCharacterList();
            SelectCharacter(newChar);
        }

        private void DeleteSelectedCharacter()
        {
            if (selectedCharacter == null) return;
            bool confirm = EditorUtility.DisplayDialog(
                "Eliminar personaje",
                $"¿Seguro que quieres eliminar '{selectedCharacter.characterName}'?",
                "Eliminar", "Cancelar");

            if (!confirm) return;

            string path = AssetDatabase.GetAssetPath(selectedCharacter);
            AssetDatabase.DeleteAsset(path);
            selectedCharacter = null;
            RefreshCharacterList();
        }
    }
}