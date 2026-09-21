using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TFGToolkit
{
    public class PrototypeLibraryWindow : EditorWindow
    {
        // Config de la biblioteca
        private PrototypeLibraryConfig _config;

        // Lista filtrada
        private List<PrototypePrefabData> _filtered = new();
        private string _search = "";
        private PrototypePrefabCategory? _catFilter = null;

        // Selección
        private PrototypePrefabData _selected;

        // Drag
        private PrototypePrefabData _dragCandidate; // prefab sobre el que se hizo MouseDown
        private bool _isDragging = false;

        // Scroll
        private Vector2 _libScroll;
        private Vector2 _inspScroll;

        // Layout
        private const float SIDEBAR_W = 230f;
        private const float INSPECTOR_W = 230f;
        private const float TOOLBAR_H = 28f;
        private const float CAT_H = 28f;

        // Texturas de preview generadas
        private Dictionary<string, Texture2D> _previewTextures = new();

        public static void ShowWindow()
        {
            var w = GetWindow<PrototypeLibraryWindow>("Biblioteca de Prototipos");
            w.minSize = new Vector2(700, 480);
        }

        private void OnEnable()
        {
            PrototypeDragHandler.Initialize();
            LoadOrCreateConfig();
            ApplyFilter();
        }

        private void OnDisable()
        {
            PrototypeDragHandler.Cleanup();
        }

        // =========================================================
        // OnGUI
        // =========================================================
        private void OnGUI()
        {
            DrawToolbar();

            float cy = TOOLBAR_H;
            float ch = position.height - TOOLBAR_H;

            // Sidebar: biblioteca
            GUILayout.BeginArea(new Rect(0, cy, SIDEBAR_W, ch));
            DrawLibrary(ch);
            GUILayout.EndArea();

            // Centro: info de uso / escena activa
            float centerX = SIDEBAR_W;
            float centerW = position.width - SIDEBAR_W - INSPECTOR_W;
            GUILayout.BeginArea(new Rect(centerX, cy, centerW, ch));
            DrawCenter(centerW, ch);
            GUILayout.EndArea();

            // Inspector
            GUILayout.BeginArea(new Rect(position.width - INSPECTOR_W, cy, INSPECTOR_W, ch));
            DrawInspector();
            GUILayout.EndArea();

            // Separadores verticales
            EditorGUI.DrawRect(new Rect(SIDEBAR_W - 0.5f, cy, 0.5f, ch),
                new Color(1, 1, 1, 0.08f));
            EditorGUI.DrawRect(new Rect(position.width - INSPECTOR_W - 0.5f, cy, 0.5f, ch),
                new Color(1, 1, 1, 0.08f));
        }

        // =========================================================
        // TOOLBAR
        // =========================================================
        private void DrawToolbar()
        {
            GUILayout.BeginArea(new Rect(0, 0, position.width, TOOLBAR_H));
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("Biblioteca de prototipos", EditorStyles.boldLabel,
                GUILayout.Width(180));

            // Buscador en el toolbar
            GUILayout.Label("🔍", GUILayout.Width(18));
            string newSearch = EditorGUILayout.TextField(_search,
                GUILayout.Width(140));
            if (newSearch != _search) { _search = newSearch; ApplyFilter(); }
            if (!string.IsNullOrEmpty(_search))
            {
                if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(18)))
                {
                    _search = "";
                    ApplyFilter();
                }
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Nuevo prefab", EditorStyles.toolbarButton,
                GUILayout.Width(90)))
                CreateNewPrefab();

            if (GUILayout.Button("↺ Sincronizar mecánicas", EditorStyles.toolbarButton,
                GUILayout.Width(150)))
                SyncMechanics();

            if (GUILayout.Button("Crear prefabs por defecto", EditorStyles.toolbarButton,
                GUILayout.Width(160)))
                CreateDefaultPrefabs();

            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        // =========================================================
        // BIBLIOTECA (SIDEBAR)
        // =========================================================
        private void DrawLibrary(float totalH)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(SIDEBAR_W));

            // Filtros de categoría
            DrawCategoryFilters();

            // Contador
            GUILayout.Label($"{_filtered.Count} prefab(s)",
                EditorStyles.centeredGreyMiniLabel);

            // Lista con scroll
            float listH = totalH - CAT_H - 20f;
            _libScroll = EditorGUILayout.BeginScrollView(_libScroll,
                GUILayout.Height(listH));

            if (_filtered.Count == 0)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(
                    _config == null
                        ? "No se encontró configuración.\nPulsa 'Crear prefabs por defecto'."
                        : "No hay prefabs. Pulsa 'Nuevo prefab'.",
                    EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
            }
            else
            {
                // Agrupar por categoría
                var categories = _filtered
                    .Select(p => p.Category)
                    .Distinct()
                    .OrderBy(c => c);

                foreach (var cat in categories)
                {
                    var inCat = _filtered.Where(p => p.Category == cat).ToList();
                    DrawCategorySection(cat.ToString(), inCat);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawCategoryFilters()
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Height(CAT_H));

            string[] labels = { "Todos", "Pers.", "Entorno", "Cámara", "UI", "Otros" };
            PrototypePrefabCategory?[] cats = { null,
                PrototypePrefabCategory.Personajes,
                PrototypePrefabCategory.Entorno,
                PrototypePrefabCategory.Camara,
                PrototypePrefabCategory.UI,
                PrototypePrefabCategory.Otros };

            for (int i = 0; i < labels.Length; i++)
            {
                bool active = _catFilter == cats[i];
                GUIStyle s = active
                    ? new GUIStyle(EditorStyles.toolbarButton)
                    {
                        normal = { textColor = new Color(0.4f, 0.7f, 1f) },
                        fontStyle = FontStyle.Bold
                    }
                    : EditorStyles.toolbarButton;

                if (GUILayout.Button(labels[i], s))
                {
                    _catFilter = cats[i];
                    ApplyFilter();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCategorySection(string catName, List<PrototypePrefabData> prefabs)
        {
            GUILayout.Space(4);
            GUILayout.Label(catName.ToUpper(),
                new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.5f, 0.5f, 0.5f) },
                    fontSize = 9
                });

            foreach (var prefab in prefabs)
                DrawPrefabCard(prefab);
        }

        private void DrawPrefabCard(PrototypePrefabData prefab)
        {
            bool isSelected = prefab == _selected;

            // Color de fondo según categoría
            Color bgColor = GetCategoryColor(prefab.Category, isSelected);

            // Calcular cuántas mecánicas tiene asignadas
            int assignedCount = prefab.AssignedMechanicGuids?.Count ?? 0;
            int totalMechs = prefab.LinkedMechanics?.Count(m => m != null) ?? 0;

            // Rect de la tarjeta
            Rect cardRect = GUILayoutUtility.GetRect(
                SIDEBAR_W - 16f, 58f,
                GUILayout.Width(SIDEBAR_W - 16f),
                GUILayout.Height(58f));

            // Fondo
            EditorGUI.DrawRect(cardRect, bgColor);

            // Borde izquierdo de color según categoría
            EditorGUI.DrawRect(
                new Rect(cardRect.x, cardRect.y, 3f, cardRect.height),
                GetCategoryAccent(prefab.Category));

            // Borde exterior
            Color borderColor = isSelected
                ? new Color(0.4f, 0.7f, 1f, 0.8f)
                : new Color(1f, 1f, 1f, 0.07f);
            Handles.color = borderColor;
            Handles.DrawSolidRectangleWithOutline(cardRect, Color.clear, borderColor);

            // Nombre
            GUI.Label(
                new Rect(cardRect.x + 10, cardRect.y + 6, cardRect.width - 14, 18),
                prefab.PrefabName,
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 });

            // Descripción
            if (!string.IsNullOrEmpty(prefab.Description))
                GUI.Label(
                    new Rect(cardRect.x + 10, cardRect.y + 22, cardRect.width - 14, 16),
                    prefab.Description.Length > 38
                        ? prefab.Description.Substring(0, 35) + "..."
                        : prefab.Description,
                    new GUIStyle(EditorStyles.miniLabel)
                    { normal = { textColor = new Color(0.6f, 0.6f, 0.6f) } });

            // Badge de mecánicas
            if (totalMechs > 0)
            {
                string mechLabel = $"⚡ {assignedCount}/{totalMechs} mecánicas";
                GUI.Label(
                    new Rect(cardRect.x + 10, cardRect.y + 38, cardRect.width - 14, 14),
                    mechLabel,
                    new GUIStyle(EditorStyles.miniLabel)
                    { normal = { textColor = new Color(0.4f, 0.7f, 1f) } });
            }

            // ── Manejo de eventos para selección y drag ──
            Event e = Event.current;

            if (e.type == EventType.MouseDown && cardRect.Contains(e.mousePosition))
            {
                _dragCandidate = prefab;
                _selected = prefab;
                _isDragging = false;
                GUI.FocusControl(null);
                Repaint();
                e.Use();
            }

            if (e.type == EventType.MouseDrag
                && cardRect.Contains(e.mousePosition)
                && _dragCandidate == prefab
                && !_isDragging)
            {
                _isDragging = true;
                PrototypeDragHandler.StartDrag(prefab);
                e.Use();
            }

            if (e.type == EventType.MouseUp)
            {
                if (_dragCandidate == prefab) _dragCandidate = null;
                _isDragging = false;
            }

            GUILayout.Space(4);
        }

        // =========================================================
        // PANEL CENTRAL
        // =========================================================
        private void DrawCenter(float w, float h)
        {
            // Fondo con grid
            EditorGUI.DrawRect(new Rect(0, 0, w, h), new Color(0.10f, 0.10f, 0.10f));

            // Grid de puntos
            float dotSpacing = 24f;
            for (float x = 0; x < w; x += dotSpacing)
                for (float y = 0; y < h; y += dotSpacing)
                    EditorGUI.DrawRect(new Rect(x, y, 1.5f, 1.5f), new Color(1, 1, 1, 0.06f));

            // Info de la escena activa
            var scene = EditorSceneManager.GetActiveScene();
            string sceneName = string.IsNullOrEmpty(scene.name) ? "Sin escena" : scene.name;

            GUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);
            GUILayout.Label($"Escena activa: {sceneName}",
                new GUIStyle(EditorStyles.miniLabel)
                { normal = { textColor = new Color(0.5f, 0.5f, 0.5f) } });
            EditorGUILayout.EndHorizontal();

            // Instrucciones centradas
            float cx = w * 0.5f;
            float cy = h * 0.5f;

            if (_selected == null)
            {
                DrawCenteredLabel(cx, cy - 30, w, "Selecciona un prefab de la biblioteca");
                DrawCenteredLabel(cx, cy, w, "y arrástralo a la Scene View de Unity");
                DrawCenteredLabel(cx, cy + 30, w, "para colocarlo en la escena");
            }
            else
            {
                // Icono grande del prefab seleccionado
                float iconSize = 56f;
                Color accent = GetCategoryAccent(_selected.Category);
                Rect iconRect = new Rect(cx - iconSize * 0.5f, cy - iconSize - 20, iconSize, iconSize);
                EditorGUI.DrawRect(iconRect, new Color(accent.r, accent.g, accent.b, 0.15f));
                Handles.color = new Color(accent.r, accent.g, accent.b, 0.4f);
                Handles.DrawSolidRectangleWithOutline(iconRect, Color.clear,
                    new Color(accent.r, accent.g, accent.b, 0.4f));

                DrawCenteredLabelColor(cx, cy - 20, w,
                    _selected.PrefabName, accent, 14, true);

                DrawCenteredLabel(cx, cy + 10, w, "Arrastra desde la lista a la Scene View");

                // Flecha animada indicando dirección de drag
                GUIStyle arrowStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                { fontSize = 20 };
                GUI.Label(new Rect(cx - 20, cy + 36, 40, 28), "→", arrowStyle);

                DrawCenteredLabel(cx, cy + 68, w, "o pulsa el botón del inspector");
            }
        }

        private void DrawCenteredLabel(float cx, float cy, float w, string text)
        {
            GUI.Label(new Rect(cx - w * 0.5f, cy, w, 20), text,
                new GUIStyle(EditorStyles.centeredGreyMiniLabel));
        }

        private void DrawCenteredLabelColor(float cx, float cy, float w,
            string text, Color color, int size, bool bold)
        {
            GUI.Label(new Rect(cx - w * 0.5f, cy, w, size + 8), text,
                new GUIStyle(bold ? EditorStyles.boldLabel : EditorStyles.label)
                {
                    fontSize = size,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = color }
                });
        }

        // =========================================================
        // INSPECTOR
        // =========================================================
        private void DrawInspector()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(INSPECTOR_W));

            if (_selected == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Selecciona un prefab.", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
                return;
            }

            // Cabecera con color de categoría
            Color accent = GetCategoryAccent(_selected.Category);
            Rect hdrRect = GUILayoutUtility.GetRect(INSPECTOR_W, 36);
            EditorGUI.DrawRect(hdrRect,
                new Color(accent.r, accent.g, accent.b, 0.12f));
            EditorGUI.DrawRect(
                new Rect(hdrRect.x, hdrRect.y, 3, hdrRect.height), accent);
            GUI.Label(
                new Rect(hdrRect.x + 10, hdrRect.y + 4, hdrRect.width - 14, 18),
                _selected.PrefabName,
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 });
            GUI.Label(
                new Rect(hdrRect.x + 10, hdrRect.y + 20, hdrRect.width - 14, 14),
                _selected.Category.ToString(),
                new GUIStyle(EditorStyles.miniLabel)
                { normal = { textColor = accent } });

            _inspScroll = EditorGUILayout.BeginScrollView(_inspScroll);

            // Descripción
            GUILayout.Space(6);
            if (!string.IsNullOrEmpty(_selected.Description))
            {
                GUILayout.Label(_selected.Description,
                    new GUIStyle(EditorStyles.label)
                    {
                        wordWrap = true,
                        fontSize = 11,
                        normal = { textColor = new Color(0.65f, 0.65f, 0.65f) }
                    });
                GUILayout.Space(8);
            }

            // Componentes base
            DrawInspectorSection("Componentes base",
                () =>
                {
                    if (_selected.BaseComponents.Count == 0)
                    {
                        GUILayout.Label("Sin componentes base.",
                            EditorStyles.centeredGreyMiniLabel);
                    }
                    else
                    {
                        foreach (var comp in _selected.BaseComponents)
                            DrawComponentRow(comp.DisplayName, "base",
                                new Color(0.5f, 0.5f, 0.5f));
                    }
                });

            GUILayout.Space(6);

            // Mecánicas disponibles y asignadas
            DrawInspectorSection("Scripts de mecánicas",
                () => DrawMechanicsSection());

            EditorGUILayout.EndScrollView();

            // Botones de acción al fondo
            GUILayout.Space(6);
            DrawActionButtons();
            GUILayout.Space(8);

            EditorGUILayout.EndVertical();
        }

        private void DrawInspectorSection(string title, Action content)
        {
            GUILayout.Label(title, EditorStyles.boldLabel);
            GUILayout.Space(2);
            content();
        }

        private void DrawComponentRow(string name, string badge, Color badgeColor)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.DrawRect(
                GUILayoutUtility.GetRect(3, 14, GUILayout.Width(3), GUILayout.Height(14)),
                new Color(badgeColor.r, badgeColor.g, badgeColor.b, 0.5f));
            GUILayout.Space(4);
            GUILayout.Label(name,
                new GUIStyle(EditorStyles.label) { fontSize = 11 });
            GUILayout.FlexibleSpace();
            GUIStyle badgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = badgeColor },
                fontSize = 9
            };
            GUILayout.Label(badge, badgeStyle, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2);
        }

        private void DrawMechanicsSection()
        {
            if (_selected.LinkedMechanics == null || _selected.LinkedMechanics.Count == 0)
            {
                GUILayout.Label(
                    "Sin mecánicas vinculadas.\nAbre el prefab en el Inspector\n" +
                    "y asigna mecánicas en 'LinkedMechanics'.",
                    new GUIStyle(EditorStyles.miniLabel)
                    { wordWrap = true, normal = { textColor = new Color(0.5f, 0.5f, 0.5f) } });
                return;
            }

            bool changed = false;
            foreach (var mechanic in _selected.LinkedMechanics)
            {
                if (mechanic == null) continue;

                string guid = AssetDatabase.AssetPathToGUID(
                    AssetDatabase.GetAssetPath(mechanic));
                bool assigned = _selected.AssignedMechanicGuids.Contains(guid);

                // Comprobar si el script existe
                string scriptPath = $"Assets/TFGToolkit/Scripts/Generated/" +
                                    $"{ToPascalCase(mechanic.mechanicName)}.cs";
                bool scriptExists = System.IO.File.Exists(scriptPath);

                EditorGUILayout.BeginHorizontal();

                // Dot de estado
                Color dotColor = assigned
                    ? new Color(0.3f, 0.75f, 0.4f)
                    : scriptExists
                        ? new Color(0.4f, 0.7f, 1f)
                        : new Color(0.5f, 0.5f, 0.5f);

                Rect dotRect = GUILayoutUtility.GetRect(8, 8,
                    GUILayout.Width(8), GUILayout.Height(8));
                dotRect.y += 4;
                EditorGUI.DrawRect(dotRect, dotColor);
                GUILayout.Space(5);

                GUILayout.Label(mechanic.mechanicName,
                    new GUIStyle(EditorStyles.label) { fontSize = 11 });

                GUILayout.FlexibleSpace();

                if (!scriptExists)
                {
                    GUILayout.Label("sin script",
                        new GUIStyle(EditorStyles.miniLabel)
                        { normal = { textColor = new Color(0.5f, 0.5f, 0.5f) } });
                }
                else if (assigned)
                {
                    GUI.backgroundColor = new Color(0.3f, 0.75f, 0.4f, 0.3f);
                    if (GUILayout.Button("✓ Asignado", EditorStyles.miniButton,
                        GUILayout.Width(72)))
                    {
                        _selected.AssignedMechanicGuids.Remove(guid);
                        EditorUtility.SetDirty(_selected);
                        changed = true;
                    }
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    GUI.backgroundColor = new Color(0.4f, 0.7f, 1f, 0.3f);
                    if (GUILayout.Button("+ Asignar", EditorStyles.miniButton,
                        GUILayout.Width(72)))
                    {
                        _selected.AssignedMechanicGuids.Add(guid);
                        EditorUtility.SetDirty(_selected);
                        changed = true;
                    }
                    GUI.backgroundColor = Color.white;
                }

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(3);
            }

            if (changed) AssetDatabase.SaveAssets();
        }

        private void DrawActionButtons()
        {
            // Botón principal: colocar en escena (alternativa al drag)
            GUI.backgroundColor = new Color(0.3f, 0.6f, 1f, 0.8f);
            if (GUILayout.Button("⊕  Colocar en escena", GUILayout.Height(32)))
                PlaceSelectedInScene();
            GUI.backgroundColor = Color.white;

            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Editar prefab", EditorStyles.miniButton))
            {
                Selection.activeObject = _selected;
                EditorGUIUtility.PingObject(_selected);
            }

            if (GUILayout.Button("Eliminar", EditorStyles.miniButton))
                RemovePrefabFromLibrary(_selected);

            EditorGUILayout.EndHorizontal();
        }

        // =========================================================
        // ACCIONES
        // =========================================================
        private void PlaceSelectedInScene()
        {
            if (_selected == null) return;
            PrototypeDragHandler.HandleHierarchyDrop(_selected);
        }

        private void SyncMechanics()
        {
            if (_config == null) return;

            // Para cada prefab, busca si hay nuevas mecánicas en el proyecto
            // y las añade a LinkedMechanics si no estaban
            var allMechanics = new List<MechanicData>();
            foreach (var guid in AssetDatabase.FindAssets("t:MechanicData"))
            {
                var m = AssetDatabase.LoadAssetAtPath<MechanicData>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (m != null) allMechanics.Add(m);
            }

            int added = 0;
            foreach (var prefab in _config.Prefabs)
            {
                if (prefab.LinkedMechanics == null)
                    prefab.LinkedMechanics = new List<MechanicData>();

                // Añadir mecánicas que tengan script generado y no estén vinculadas
                foreach (var m in allMechanics)
                {
                    if (prefab.LinkedMechanics.Contains(m)) continue;

                    string scriptPath = $"Assets/TFGToolkit/Scripts/Generated/" +
                                        $"{ToPascalCase(m.mechanicName)}.cs";
                    if (System.IO.File.Exists(scriptPath))
                    {
                        prefab.LinkedMechanics.Add(m);
                        EditorUtility.SetDirty(prefab);
                        added++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[TFG Toolkit] Sincronización completada. {added} mecánicas añadidas.");
            EditorUtility.DisplayDialog("Sincronización",
                $"Completada. {added} mecánica(s) añadida(s) a los prefabs.", "OK");
        }

        private void CreateNewPrefab()
        {
            string folder = "Assets/TFGToolkit/Data/Prefabs";
            if (!AssetDatabase.IsValidFolder("Assets/TFGToolkit/Data/Prefabs"))
                AssetDatabase.CreateFolder("Assets/TFGToolkit/Data", "Prefabs");

            var newPrefab = ScriptableObject.CreateInstance<PrototypePrefabData>();
            newPrefab.PrefabName = "Nuevo prefab";
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/NuevoPrefab.asset");
            AssetDatabase.CreateAsset(newPrefab, path);

            if (_config != null)
            {
                _config.Prefabs.Add(newPrefab);
                EditorUtility.SetDirty(_config);
                AssetDatabase.SaveAssets();
            }

            _selected = newPrefab;
            ApplyFilter();
            Selection.activeObject = newPrefab;
        }

        private void RemovePrefabFromLibrary(PrototypePrefabData prefab)
        {
            bool confirm = EditorUtility.DisplayDialog(
                "Eliminar de la biblioteca",
                $"¿Quitar '{prefab.PrefabName}' de la biblioteca? " +
                "(El asset no se borra del disco)",
                "Eliminar", "Cancelar");
            if (!confirm) return;

            _config?.Prefabs.Remove(prefab);
            if (_config != null) EditorUtility.SetDirty(_config);
            AssetDatabase.SaveAssets();
            if (_selected == prefab) _selected = null;
            ApplyFilter();
        }

        // =========================================================
        // PREFABS POR DEFECTO
        // =========================================================
        private void CreateDefaultPrefabs()
        {
            if (_config == null) LoadOrCreateConfig();

            string folder = "Assets/TFGToolkit/Data/Prefabs";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/TFGToolkit/Data", "Prefabs");

            var defaults = new[]
            {
                new {
                    Name = "Jugador base", Cat = PrototypePrefabCategory.Personajes,
                    Desc = "Cápsula con CharacterController y movimiento genérico.",
                    Shape = PrimitiveType.Capsule, Color = new Color(0.3f, 0.6f, 1f),
                    Comps = new[]{ ("CharacterController","CharacterController"),
                                   ("Rigidbody","Rigidbody") }
                },
                new {
                    Name = "Enemigo base", Cat = PrototypePrefabCategory.Personajes,
                    Desc = "Cápsula con NavMeshAgent para IA de movimiento.",
                    Shape = PrimitiveType.Capsule, Color = new Color(0.9f, 0.3f, 0.3f),
                    Comps = new[]{ ("NavMeshAgent","NavMeshAgent"),
                                   ("CapsuleCollider","CapsuleCollider") }
                },
                new {
                    Name = "NPC base", Cat = PrototypePrefabCategory.Personajes,
                    Desc = "Cápsula con trigger de diálogo.",
                    Shape = PrimitiveType.Capsule, Color = new Color(0.3f, 0.8f, 0.4f),
                    Comps = new[]{ ("CapsuleCollider","CapsuleCollider") }
                },
                new {
                    Name = "Plataforma", Cat = PrototypePrefabCategory.Entorno,
                    Desc = "Cubo estático con BoxCollider.",
                    Shape = PrimitiveType.Cube, Color = new Color(0.6f, 0.6f, 0.6f),
                    Comps = new[]{ ("BoxCollider","BoxCollider") }
                },
                new {
                    Name = "Trigger de zona", Cat = PrototypePrefabCategory.Entorno,
                    Desc = "Cubo invisible que detecta colisiones.",
                    Shape = PrimitiveType.Cube, Color = new Color(0.9f, 0.7f, 0.2f),
                    Comps = new[]{ ("BoxCollider","BoxCollider (isTrigger)") }
                },
                new {
                    Name = "Item recogible", Cat = PrototypePrefabCategory.Entorno,
                    Desc = "Esfera con SphereCollider trigger.",
                    Shape = PrimitiveType.Sphere, Color = new Color(0.7f, 0.4f, 1f),
                    Comps = new[]{ ("SphereCollider","SphereCollider (isTrigger)") }
                },
                new {
                    Name = "Proyectil", Cat = PrototypePrefabCategory.Entorno,
                    Desc = "Esfera pequeña con Rigidbody para proyectiles.",
                    Shape = PrimitiveType.Sphere, Color = new Color(1f, 0.5f, 0.2f),
                    Comps = new[]{ ("Rigidbody","Rigidbody"),
                                   ("SphereCollider","SphereCollider") }
                },
            };

            int created = 0;
            foreach (var d in defaults)
            {
                if (_config.Prefabs.Any(p => p.PrefabName == d.Name)) continue;

                var p = ScriptableObject.CreateInstance<PrototypePrefabData>();
                p.PrefabName = d.Name;
                p.Category = d.Cat;
                p.Description = d.Desc;
                p.PrimitiveShape = d.Shape;
                p.GizmoColor = d.Color;

                foreach (var (typeName, displayName) in d.Comps)
                    p.BaseComponents.Add(new BaseComponentEntry
                    { ComponentTypeName = typeName, DisplayName = displayName });

                string path = AssetDatabase.GenerateUniqueAssetPath(
                    $"{folder}/{d.Name.Replace(" ", "")}.asset");
                AssetDatabase.CreateAsset(p, path);
                _config.Prefabs.Add(p);
                created++;
            }

            EditorUtility.SetDirty(_config);
            AssetDatabase.SaveAssets();
            ApplyFilter();

            Debug.Log($"[TFG Toolkit] {created} prefab(s) por defecto creados.");
        }

        // =========================================================
        // FILTRADO
        // =========================================================
        private void ApplyFilter()
        {
            if (_config == null) { _filtered.Clear(); return; }

            var source = string.IsNullOrEmpty(_search)
                ? new List<PrototypePrefabData>(_config.Prefabs)
                : _config.Search(_search);

            _filtered = _catFilter.HasValue
                ? source.Where(p => p.Category == _catFilter.Value).ToList()
                : source;

            Repaint();
        }

        // =========================================================
        // CONFIG
        // =========================================================
        private void LoadOrCreateConfig()
        {
            // Buscar config existente
            var guids = AssetDatabase.FindAssets("t:PrototypeLibraryConfig");
            if (guids.Length > 0)
            {
                _config = AssetDatabase.LoadAssetAtPath<PrototypeLibraryConfig>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));
                return;
            }

            // Crear una nueva
            string folder = "Assets/TFGToolkit/Data";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/TFGToolkit", "Data");

            _config = ScriptableObject.CreateInstance<PrototypeLibraryConfig>();
            AssetDatabase.CreateAsset(_config, $"{folder}/PrototypeLibrary.asset");
            AssetDatabase.SaveAssets();
        }

        // =========================================================
        // HELPERS DE COLOR Y ESTILO
        // =========================================================
        private Color GetCategoryColor(PrototypePrefabCategory cat, bool selected)
        {
            Color accent = GetCategoryAccent(cat);
            float alpha = selected ? 0.18f : 0.08f;
            return new Color(accent.r, accent.g, accent.b, alpha);
        }

        private Color GetCategoryAccent(PrototypePrefabCategory cat) => cat switch
        {
            PrototypePrefabCategory.Personajes => new Color(0.30f, 0.60f, 1.00f),
            PrototypePrefabCategory.Entorno => new Color(0.55f, 0.55f, 0.55f),
            PrototypePrefabCategory.Camara => new Color(0.40f, 0.85f, 0.65f),
            PrototypePrefabCategory.UI => new Color(0.85f, 0.65f, 0.20f),
            _ => new Color(0.70f, 0.45f, 0.90f)
        };

        private string ToPascalCase(string s)
        {
            if (string.IsNullOrEmpty(s)) return "Script";
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
}