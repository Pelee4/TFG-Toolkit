using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Web;

namespace TFGToolkit
{

    // -------------------------------------------------------
    // Ventana principal
    // -------------------------------------------------------
public class DesignNotesWindow : EditorWindow
{
        //Ruta del archivo JSON donde se guardan las notas
        //Si se guarda en Assets/, se versiona directamente con Git
        private const string NOTES_PATH = "Assets/TFGToolkit/Data/Notes/DesignNotes.json";

        private DesignNotesData data = new DesignNotesData();
        private DesignNote selectedNote;
        private Vector2 listScroll;
        private Vector2 detailScroll;

        //Filtros para buscar en la lista más rápido
        private string searchText = "";
        private NoteType filterType = (NoteType)(-1);
        private bool hideResolved = false;

        //Estado del formulario para crear una nueva nota
        private bool isCreating = false;
        private DesignNote draftNote = new DesignNote();

        public static void ShowWindow()
        {
            var window = GetWindow<DesignNotesWindow>("Notas de Diseño");
            window.minSize = new Vector2(700, 450);
            window.LoadNotes();
        }

        private void OnEnable() => LoadNotes();
        private void OnFocus() => LoadNotes();


        // -------------------------------------------------------
        // Layout principal
        // -------------------------------------------------------
        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();
        }


        // -------------------------------------------------------
        // PANEL IZQUIERDO — Lista y filtros
        // -------------------------------------------------------

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(240));

            GUILayout.Label("Notas de Diseño", EditorStyles.boldLabel);

            // --Botón para crear nueva nota--
            //--------------------------------
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
            if(GUILayout.Button("+ Nueva nota"))
            {
                StartCreatingNote();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(6);

            // --Buscador--
            //-------------
            GUILayout.Label("Buscar", EditorStyles.miniLabel);
            searchText = EditorGUILayout.TextField(searchText);

            // --Filtro por tipo--
            //--------------------
            GUILayout.Label("Filtrar por tipo", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(filterType == (NoteType)(-1), "Todas", EditorStyles.toolbarButton))
            {
                filterType = (NoteType)(-1);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            foreach (NoteType t in Enum.GetValues(typeof(NoteType)))
            {
                bool active = filterType == t;
                if (GUILayout.Toggle(active, TypeLabel(t), EditorStyles.toolbarButton))
                {
                    filterType = t;
                }


            }
            EditorGUILayout.EndHorizontal();

            hideResolved = EditorGUILayout.Toggle("Ocultar resueltas", hideResolved);

            GUILayout.Space(6);


            // --Lista de notas filtrada--
            //----------------------------
            listScroll = EditorGUILayout.BeginScrollView(listScroll);

            int shown = 0;
            foreach (var note in data.Notes)
            {
                if (!PassesFilter(note)) continue;
                shown++;

                bool isSelected = note == selectedNote;
                GUIStyle style = new GUIStyle(EditorStyles.toolbarButton)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal,
                    wordWrap = false
                };

                //Color del fondo según el tipo de nota
                GUI.backgroundColor = TypeColor(note.Type, note.Resolved);

                string label = $"{TypeIcon(note.Type)} {note.Title}";
                if (note.Resolved) label += " ✓";

                if(GUILayout.Button(label, style))
                {
                    selectedNote = note;
                    isCreating = false;
                    GUI.FocusControl(null);
                }
            }

            GUI.backgroundColor = Color.white;

            if (shown == 0)
            {
                GUILayout.Label("No hay notas", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndScrollView();

            //Contador de notas
            GUILayout.Label($"{shown} nota(s)", EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
        }


        // -------------------------------------------------------
        // PANEL DERECHO — Detalle / Formulario nueva nota
        // -------------------------------------------------------
        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical();

            if (isCreating)
                DrawCreateForm();
            else if (selectedNote != null)
                DrawNoteDetail();
            else
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Selecciona o crea una nota", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.EndVertical();
        }


        // -------------------------------------------------------
        // Formulario de creación de notas
        // -------------------------------------------------------
        private void DrawCreateForm()
        {
            detailScroll = EditorGUILayout.BeginScrollView(detailScroll);

            GUILayout.Label("Nueva nota", EditorStyles.boldLabel);
            GUILayout.Space(6);

            draftNote.Type = (NoteType)EditorGUILayout.EnumPopup("Tipo", draftNote.Type);
            draftNote.Title = EditorGUILayout.TextField("Título", draftNote.Title);
            draftNote.Author = EditorGUILayout.TextField("Autor", draftNote.Author);

            GUILayout.Label("Contenido");
            draftNote.Content = EditorGUILayout.TextArea(draftNote.Content, GUILayout.Height(120));

            GUILayout.Space(6);

            //Vincular un asset del proyecto a la nota (opcional)
            GUILayout.Label("Vincular un asset (opcional)", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(string.IsNullOrEmpty(draftNote.LinkedAsset) ? "Ninguno" : draftNote.LinkedAsset, EditorStyles.miniLabel); ;
            if (GUILayout.Button("Seleccionar", GUILayout.Width(90)))
            {
                string selected = EditorUtility.OpenFilePanel(
                    "Selecciona un asset", Application.dataPath, "asset");
                if (!string.IsNullOrEmpty(selected))
                {
                    string projectPath = Application.dataPath.Replace("/Assets", "");
                    if (selected.StartsWith(projectPath))
                    {
                        selected = selected.Substring(projectPath.Length + 1);
                    }
                    draftNote.LinkedAsset = selected;
                }
            }

            if (!string.IsNullOrEmpty(draftNote.LinkedAsset) && GUILayout.Button("✕", GUILayout.Width(24)))
                draftNote.LinkedAsset = "";
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
            if (GUILayout.Button("Guardar nota"))
            {
                if(!string.IsNullOrEmpty(draftNote.Title))
                {
                    SaveDraftNote();
                }
                else
                {
                    EditorUtility.DisplayDialog("Campo Requerido", "El título no puede estar vacío", "OK");
                }
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("Cancelar"))
            {
                isCreating = false;
                draftNote = new DesignNote();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

        // -------------------------------------------------------
        // Vista de detalle de una nota
        // -------------------------------------------------------
        private void DrawNoteDetail()
        {
            detailScroll = EditorGUILayout.BeginScrollView(detailScroll);

            //Cabecera con tipo e icono
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"{TypeIcon(selectedNote.Type)} {selectedNote.Title}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            string resolvedLabel = selectedNote.Resolved ? "✓ Resuelta" : "Marcar resuelta";
            GUI.backgroundColor = selectedNote.Resolved ? new Color(0.3f, 0.8f, 0.4f) : Color.white;

            if (GUILayout.Button(resolvedLabel, GUILayout.Width(130)))
            {
                selectedNote.Resolved = !selectedNote.Resolved;
                SaveNotes();
                Repaint();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);

            //Metadatos de la nota
            GUIStyle metaStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = Color.gray }
            };
            GUILayout.Label($"Tipo: {TypeLabel(selectedNote.Type)}   |   Autor: {selectedNote.Author}   |   Fecha: {selectedNote.Date}", metaStyle);

            if (!string.IsNullOrEmpty(selectedNote.LinkedAsset))
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"📎 Vinculado a: {selectedNote.LinkedAsset}", metaStyle);
                if (GUILayout.Button("Ir", GUILayout.Width(30)))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(selectedNote.LinkedAsset);
                    if (asset != null) EditorGUIUtility.PingObject(asset);
                }
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(8);

            //Contenido editable de la nota
            GUILayout.Label("Contenido", EditorStyles.boldLabel);
            string newContent = EditorGUILayout.TextArea(selectedNote.Content, GUILayout.Height(140));

            if (newContent != selectedNote.Content)
            {
                selectedNote.Content = newContent;
                SaveNotes();
            }

            GUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);

            if (GUILayout.Button("🗑 Eliminar"))
            {
                bool confirm = EditorUtility.DisplayDialog(
                    "Eliminar nota",
                    $"¿Eliminar '{selectedNote.Title}'?",
                    "Eliminar", "Cancelar");
                if (confirm)
                {
                    data.Notes.Remove(selectedNote);
                    selectedNote = null;
                    SaveNotes();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndScrollView();
                    EditorGUILayout.EndVertical();
                    return;
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

        // -------------------------------------------------------
        // Persistencia JSON
        // -------------------------------------------------------
        private DateTime lastLoadedTime = DateTime.MinValue;

        private void LoadNotes()
        {
            if (!File.Exists(NOTES_PATH))
            {
                if (data == null || data.Notes == null)
                    data = new DesignNotesData();
                return;
            }
            try
            {
                DateTime fileTime = File.GetLastWriteTime(NOTES_PATH);
                if (fileTime <= lastLoadedTime && data != null) return;

                string json = File.ReadAllText(NOTES_PATH);
                var loaded = JsonUtility.FromJson<DesignNotesData>(json);
                if (loaded != null)
                {
                    data = loaded;
                    lastLoadedTime = fileTime;
                    Repaint();
                }
            }
            catch
            {
                data = new DesignNotesData();
                Debug.LogWarning("[TFG Toolkit] No se pudo cargar DesignNotes.json, creando vacío.");
            }
        }

        private void SaveNotes()
        {
            string folder = Path.GetDirectoryName(NOTES_PATH);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            File.WriteAllText(NOTES_PATH, JsonUtility.ToJson(data, prettyPrint: true));
            AssetDatabase.Refresh();
        }



        // -------------------------------------------------------
        // Helpers
        // -------------------------------------------------------
        private void StartCreatingNote()
        {
            isCreating = true;
            selectedNote = null;
            draftNote = new DesignNote { Id = Guid.NewGuid().ToString(), Date = DateTime.Now.ToString("dd/MM/yyyy HH:mm") };
        }

        private void SaveDraftNote()
        {
            draftNote.Date = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            if (string.IsNullOrEmpty(draftNote.Id))
                draftNote.Id = Guid.NewGuid().ToString();

            data.Notes.Insert(0, draftNote);
            SaveNotes();

            selectedNote = draftNote;
            isCreating = false;
            draftNote = new DesignNote();
        }


        private bool PassesFilter(DesignNote note)
        {
            if (hideResolved && note.Resolved) return false;
            if (filterType != (NoteType)(-1) && note.Type != filterType) return false;
            if (!string.IsNullOrEmpty(searchText) &&
                !note.Title.ToLower().Contains(searchText.ToLower()) &&
                !note.Content.ToLower().Contains(searchText.ToLower())) return false;
            return true;
        }


        private string TypeIcon(NoteType t) => t switch
        {
            NoteType.Nota => "📝",
            NoteType.Decision => "✅",
            NoteType.Problema => "⚠️",
            NoteType.Idea => "💡",
            _ => "•"
        };

        private string TypeLabel(NoteType t) => t switch
        {
            NoteType.Nota => "Nota",
            NoteType.Decision => "Decisión",
            NoteType.Problema => "Problema",
            NoteType.Idea => "Idea",
            _ => t.ToString()
        };


        private Color TypeColor(NoteType t, bool resolved)
        {
            if (resolved) return new Color(0.5f, 0.5f, 0.5f);
            return t switch
            {
                NoteType.Decision => new Color(0.3f, 0.8f, 0.4f),
                NoteType.Problema => new Color(1f, 0.5f, 0.3f),
                NoteType.Idea => new Color(1f, 0.9f, 0.3f),
                _ => Color.white
            };
        }

        /// <summary>
        /// Carga las notas desde disco y las devuelve para el AgentContextBuilder.
        /// No abre ni modifica la ventana.
        /// </summary>
        public static DesignNotesData LoadNotesForAgent()
        {
            const string path = NOTES_PATH;
            if (!System.IO.File.Exists(path)) return new DesignNotesData();
            try
            {
                string json = System.IO.File.ReadAllText(path);
                return JsonUtility.FromJson<DesignNotesData>(json) ?? new DesignNotesData();
            }
            catch { return new DesignNotesData(); }
        }

        /// <summary>
        /// Añade una nota creada por el agente sin necesidad de abrir la ventana.
        /// Si la ventana está abierta se actualiza automáticamente al ganar foco.
        /// </summary>
            public static void AddNoteFromAgent(DesignNote note)
            {
                const string path = NOTES_PATH;
                var data = LoadNotesForAgent();
                data.Notes.Insert(0, note);
                string folder = System.IO.Path.GetDirectoryName(path);
                if (!System.IO.Directory.Exists(folder)) System.IO.Directory.CreateDirectory(folder);
                System.IO.File.WriteAllText(path, JsonUtility.ToJson(data, prettyPrint: true));
                AssetDatabase.Refresh();
            }
    }
}
