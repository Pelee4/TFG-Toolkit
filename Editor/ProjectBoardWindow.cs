using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TFGToolkit
{
    public class ProjectBoardWindow : EditorWindow
    {
        private const string CONFIG_PATH = "Assets/TFGToolkit/Data/BoardConfig.json";

        // Datos del proyecto
        private BoardConfig config = new BoardConfig();
        private List<MechanicData> mechanics = new List<MechanicData>();
        private List<CharacterData> characters = new List<CharacterData>();

        // UI state
        private int activeTab = 0; // 0=Kanban 1=Dashboard 2=Config 3=Campos
        private Vector2 kanbanScroll;
        private Vector2 dashScroll;
        private Vector2 configScroll;
        private Vector2 fieldsScroll;

        // Kanban — asset seleccionado para ver campos en panel lateral
        private UnityEngine.Object selectedAsset;
        private bool showFieldsPanel = false;
        private Vector2 fieldsPanelScroll;

        // Drag kanban
        private MechanicData draggedMechanic = null;
        private string dragTargetColumnId = "";

        private static readonly string[] TAB_LABELS =
            { "📋 Kanban", "📊 Dashboard", "⚙️ Config", "🔧 Campos" };

        public static void ShowWindow()
        {
            var w = GetWindow<ProjectBoardWindow>("Tablero del Proyecto");
            w.minSize = new Vector2(860, 500);
            w.LoadAll();
        }

        private void OnEnable() => LoadAll();
        private void OnFocus() { RefreshAssets(); CustomFieldService.Reload(); }

        private void LoadAll() { LoadConfig(); RefreshAssets(); CustomFieldService.Reload(); }

        // -------------------------------------------------------
        // OnGUI
        // -------------------------------------------------------
        private void OnGUI()
        {
            // Barra de pestañas
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            for (int i = 0; i < TAB_LABELS.Length; i++)
            {
                bool active = activeTab == i;
                GUIStyle s = active
                    ? new GUIStyle(EditorStyles.toolbarButton) { fontStyle = FontStyle.Bold }
                    : EditorStyles.toolbarButton;
                if (GUILayout.Button(TAB_LABELS[i], s, GUILayout.Height(22)))
                    activeTab = i;
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("↺", EditorStyles.toolbarButton, GUILayout.Width(26)))
                LoadAll();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(2);

            switch (activeTab)
            {
                case 0: DrawKanban(); break;
                case 1: DrawDashboard(); break;
                case 2: DrawConfiguration(); break;
                case 3: DrawFieldsEditor(); break;
            }
        }

        // ===================================================
        // PESTAÑA 0 — KANBAN
        // ===================================================
        private void DrawKanban()
        {
            var visible = config.Columns.Where(c => c.Visible).ToList();
            if (visible.Count == 0)
            {
                EditorGUILayout.HelpBox("No hay columnas visibles. Ve a Config para añadirlas.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();

            // Panel principal del kanban
            EditorGUILayout.BeginVertical();
            kanbanScroll = EditorGUILayout.BeginScrollView(kanbanScroll);
            EditorGUILayout.BeginHorizontal();

            float panelWidth = showFieldsPanel ? position.width - 280 : position.width;
            float colWidth = Mathf.Max(170, (panelWidth - 20) / visible.Count);

            foreach (var col in visible)
                DrawKanbanColumn(col, colWidth);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // Panel lateral de campos del asset seleccionado
            if (showFieldsPanel && selectedAsset != null)
                DrawAssetFieldsPanel();

            EditorGUILayout.EndHorizontal();

            // Aplicar drag
            if (draggedMechanic != null && !string.IsNullOrEmpty(dragTargetColumnId))
            {
                ApplyDrag(draggedMechanic, dragTargetColumnId);
                draggedMechanic = null;
                dragTargetColumnId = "";
            }
        }

        private void DrawKanbanColumn(BoardColumn col, float width)
        {
            Color colColor = HexToColor(col.ColorHex);
            EditorGUILayout.BeginVertical(GUILayout.Width(width));

            // Cabecera
            Rect headerRect = GUILayoutUtility.GetRect(width, 26);
            EditorGUI.DrawRect(headerRect, colColor * 0.55f);
            GUI.Label(headerRect, $"  {col.Label}",
                new GUIStyle(EditorStyles.boldLabel)
                {
                    normal = { textColor = Color.white },
                    fontSize = 11,
                    alignment = TextAnchor.MiddleLeft
                });

            GUILayout.Space(3);

            // Tarjetas de mecánicas
            if (config.ShowMechanics)
                foreach (var m in GetMechanicsForColumn(col))
                    DrawMechanicCard(m, col, colColor, width);

            // Tarjetas de personajes (solo en primera columna visible)
            if (config.ShowCharacters && col == config.Columns.FirstOrDefault(c => c.Visible))
                foreach (var ch in characters)
                    DrawCharacterCard(ch, colColor, width);

            GUILayout.FlexibleSpace();

            // Zona de drop
            Rect dropZone = GUILayoutUtility.GetRect(width, 26);
            EditorGUI.DrawRect(dropZone, new Color(1, 1, 1, 0.03f));
            GUI.Label(dropZone, "— soltar aquí —",
                new GUIStyle(EditorStyles.centeredGreyMiniLabel));
            if (Event.current.type == EventType.MouseUp
                && dropZone.Contains(Event.current.mousePosition))
            {
                dragTargetColumnId = col.Id;
                Repaint();
            }

            EditorGUILayout.EndVertical();

            // Separador
            Rect sep = GUILayoutUtility.GetRect(1, float.MaxValue, GUILayout.Width(1));
            EditorGUI.DrawRect(sep, new Color(1, 1, 1, 0.08f));
        }

        private void DrawMechanicCard(MechanicData m, BoardColumn col, Color colColor, float width)
        {
            string guid = CustomFieldService.GetGuid(m);
            var schema = CustomFieldService.Schema.MechanicFields;
            var cardFields = schema.Where(f => f.ShowInCard).ToList();
            float cardHeight = 58 + cardFields.Count * 18;

            Rect cardRect = GUILayoutUtility.GetRect(width - 6, cardHeight);
            EditorGUI.DrawRect(cardRect, new Color(0.18f, 0.18f, 0.23f));
            EditorGUI.DrawRect(new Rect(cardRect.x, cardRect.y, 4, cardRect.height), colColor);

            // Título
            GUI.Label(new Rect(cardRect.x + 10, cardRect.y + 5, cardRect.width - 80, 18),
                m.mechanicName,
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 });

            // Status icon
            string icon = m.status switch
            {
                MechanicStatus.Idea => "💡",
                MechanicStatus.EnDesarrollo => "🔧",
                MechanicStatus.Implementada => "✅",
                _ => ""
            };
            GUI.Label(new Rect(cardRect.xMax - 70, cardRect.y + 5, 24, 18), icon);

            // Botones
            if (GUI.Button(new Rect(cardRect.xMax - 46, cardRect.y + 4, 18, 18), "✏",
                EditorStyles.miniButton))
            {
                MechanicEditorWindow.ShowWindow();
                EditorGUIUtility.PingObject(m);
            }
            bool fieldsOpen = showFieldsPanel && selectedAsset == m;
            GUI.backgroundColor = fieldsOpen ? new Color(0.4f, 0.7f, 1f) : Color.white;
            if (GUI.Button(new Rect(cardRect.xMax - 26, cardRect.y + 4, 20, 18), "⊞",
                EditorStyles.miniButton))
            {
                selectedAsset = fieldsOpen ? null : m;
                showFieldsPanel = !fieldsOpen || selectedAsset != null;
            }
            GUI.backgroundColor = Color.white;

            // Descripción corta
            if (!string.IsNullOrEmpty(m.description))
                GUI.Label(new Rect(cardRect.x + 10, cardRect.y + 22, cardRect.width - 14, 18),
                    m.description.Length > 55 ? m.description.Substring(0, 52) + "…" : m.description,
                    new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } });

            // Campos personalizados visibles en tarjeta
            float fieldY = cardRect.y + 40;
            foreach (var fd in cardFields)
            {
                string val = CustomFieldService.GetValue(guid, fd.Id);
                if (string.IsNullOrEmpty(val)) val = fd.DefaultValue;
                string display = fd.Type == CustomFieldType.CheckBox
                    ? (val == "true" ? $"☑ {fd.Label}" : $"☐ {fd.Label}")
                    : $"{fd.Label}: {val}";
                GUI.Label(new Rect(cardRect.x + 10, fieldY, cardRect.width - 14, 16),
                    display,
                    new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.7f, 0.9f, 1f) } });
                fieldY += 18;
            }

            // Drag
            if (Event.current.type == EventType.MouseDown
                && cardRect.Contains(Event.current.mousePosition))
            {
                draggedMechanic = m;
                Event.current.Use();
            }

            GUILayout.Space(2);
        }

        private void DrawCharacterCard(CharacterData ch, Color colColor, float width)
        {
            string guid = CustomFieldService.GetGuid(ch);
            var schema = CustomFieldService.Schema.CharacterFields;
            var cardFields = schema.Where(f => f.ShowInCard).ToList();
            float cardHeight = 46 + cardFields.Count * 18;

            string roleIcon = ch.role switch
            {
                CharacterRole.Protagonista => "🦸",
                CharacterRole.Enemigo => "🦹",
                CharacterRole.NPC => "🧑",
                CharacterRole.Compañero => "🤝",
                CharacterRole.Jefe => "👑",
                _ => "👤"
            };

            Rect cardRect = GUILayoutUtility.GetRect(width - 6, cardHeight);
            EditorGUI.DrawRect(cardRect, new Color(0.14f, 0.18f, 0.24f));
            EditorGUI.DrawRect(new Rect(cardRect.x, cardRect.y, 4, cardRect.height), colColor * 0.7f);

            GUI.Label(new Rect(cardRect.x + 10, cardRect.y + 4, cardRect.width - 70, 18),
                $"{roleIcon} {ch.characterName}",
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 });

            if (GUI.Button(new Rect(cardRect.xMax - 46, cardRect.y + 4, 18, 18), "✏",
                EditorStyles.miniButton))
                EditorGUIUtility.PingObject(ch);

            bool fieldsOpen = showFieldsPanel && selectedAsset == ch;
            GUI.backgroundColor = fieldsOpen ? new Color(0.4f, 0.7f, 1f) : Color.white;
            if (GUI.Button(new Rect(cardRect.xMax - 26, cardRect.y + 4, 20, 18), "⊞",
                EditorStyles.miniButton))
            {
                selectedAsset = fieldsOpen ? null : ch;
                showFieldsPanel = !fieldsOpen || selectedAsset != null;
            }
            GUI.backgroundColor = Color.white;

            GUI.Label(new Rect(cardRect.x + 10, cardRect.y + 22, cardRect.width - 14, 16),
                ch.role.ToString(),
                new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } });

            float fieldY = cardRect.y + 40;
            foreach (var fd in cardFields)
            {
                string val = CustomFieldService.GetValue(guid, fd.Id);
                if (string.IsNullOrEmpty(val)) val = fd.DefaultValue;
                string display = fd.Type == CustomFieldType.CheckBox
                    ? (val == "true" ? $"☑ {fd.Label}" : $"☐ {fd.Label}")
                    : $"{fd.Label}: {val}";
                GUI.Label(new Rect(cardRect.x + 10, fieldY, cardRect.width - 14, 16), display,
                    new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.7f, 0.9f, 1f) } });
                fieldY += 18;
            }

            GUILayout.Space(2);
        }

        // -------------------------------------------------------
        // Panel lateral de campos del asset seleccionado
        // -------------------------------------------------------
        private void DrawAssetFieldsPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(260));

            bool isMechanic = selectedAsset is MechanicData;
            string assetName = isMechanic
                ? ((MechanicData)selectedAsset).mechanicName
                : ((CharacterData)selectedAsset).characterName;
            var schema = isMechanic
                ? CustomFieldService.Schema.MechanicFields
                : CustomFieldService.Schema.CharacterFields;
            string guid = CustomFieldService.GetGuid(selectedAsset);

            // Cabecera del panel
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(260, 28),
                new Color(0.15f, 0.15f, 0.2f));
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"  {assetName}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(20)))
            {
                showFieldsPanel = false;
                selectedAsset = null;
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);

            if (schema.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No hay campos personalizados definidos.\nVe a la pestaña Campos para añadirlos.",
                    MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            fieldsPanelScroll = EditorGUILayout.BeginScrollView(fieldsPanelScroll);

            foreach (var fd in schema)
            {
                string current = CustomFieldService.GetValue(guid, fd.Id);
                if (string.IsNullOrEmpty(current)) current = fd.DefaultValue;

                GUILayout.Label(fd.Label, EditorStyles.miniLabel);

                string newVal = current;
                switch (fd.Type)
                {
                    case CustomFieldType.Text:
                        newVal = EditorGUILayout.TextField(current);
                        break;

                    case CustomFieldType.Number:
                        // Mostramos como float
                        float num = 0;
                        float.TryParse(current, out num);
                        float newNum = EditorGUILayout.FloatField(num);
                        newVal = newNum.ToString();
                        break;

                    case CustomFieldType.CheckBox:
                        bool boolVal = current == "true";
                        bool newBool = EditorGUILayout.Toggle(boolVal);
                        newVal = newBool ? "true" : "false";
                        break;

                    case CustomFieldType.Dropdown:
                        if (fd.DropdownOptions.Count == 0) break;
                        int idx = Mathf.Max(0, fd.DropdownOptions.IndexOf(current));
                        int newIdx = EditorGUILayout.Popup(idx, fd.DropdownOptions.ToArray());
                        newVal = fd.DropdownOptions[newIdx];
                        break;
                }

                if (newVal != current)
                    CustomFieldService.SetValue(guid, fd.Id, newVal);

                GUILayout.Space(4);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void ApplyDrag(MechanicData m, string targetColId)
        {
            var col = config.Columns.FirstOrDefault(c => c.Id == targetColId);
            if (col == null || col.MechanicStatusValue < 0) return;
            m.status = (MechanicStatus)col.MechanicStatusValue;
            EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssets();
            RefreshAssets();
        }

        // ===================================================
        // PESTAÑA 1 — DASHBOARD
        // ===================================================
        private void DrawDashboard()
        {
            dashScroll = EditorGUILayout.BeginScrollView(dashScroll);

            GUILayout.Space(6);
            GUILayout.Label("Resumen del proyecto", EditorStyles.boldLabel);
            GUILayout.Space(8);

            // Métricas globales
            EditorGUILayout.BeginHorizontal();
            DrawMetricBox("Mecánicas", mechanics.Count.ToString(), new Color(0.3f, 0.6f, 1f));
            DrawMetricBox("Personajes", characters.Count.ToString(), new Color(0.6f, 0.3f, 1f));
            int done = mechanics.Count(m => m.status == MechanicStatus.Implementada);
            DrawMetricBox("Implementadas", done.ToString(), new Color(0.3f, 0.8f, 0.4f));
            int inProg = mechanics.Count(m => m.status == MechanicStatus.EnDesarrollo);
            DrawMetricBox("En desarrollo", inProg.ToString(), new Color(1f, 0.7f, 0.2f));
            DrawMetricBox("Ideas", mechanics.Count(m => m.status == MechanicStatus.Idea).ToString(),
                new Color(0.7f, 0.7f, 0.7f));
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Barra de progreso
            if (mechanics.Count > 0)
            {
                GUILayout.Label("Progreso de implementación", EditorStyles.boldLabel);
                float progress = (float)done / mechanics.Count;
                Rect bar = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
                    GUILayout.Height(22), GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(bar, new Color(0.2f, 0.2f, 0.2f));
                EditorGUI.DrawRect(new Rect(bar.x, bar.y, bar.width * progress, bar.height),
                    new Color(0.3f, 0.8f, 0.4f));
                GUI.Label(bar, $"  {Mathf.RoundToInt(progress * 100)}%  ({done}/{mechanics.Count})",
                    new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.white } });
                GUILayout.Space(12);
            }

            EditorGUILayout.BeginHorizontal();

            // Mecánicas por columna
            if (config.ShowMechanics)
            {
                EditorGUILayout.BeginVertical();
                GUILayout.Label("Mecánicas por estado", EditorStyles.boldLabel);
                foreach (var col in config.Columns.Where(c => c.Visible && c.MechanicStatusValue >= 0))
                    DrawDashSection(col.Label, col.ColorHex,
                        GetMechanicsForColumn(col).Select(m => m.mechanicName).ToList());
                EditorGUILayout.EndVertical();
            }

            GUILayout.Space(10);

            // Personajes por rol
            if (config.ShowCharacters && characters.Count > 0)
            {
                EditorGUILayout.BeginVertical();
                GUILayout.Label("Personajes por rol", EditorStyles.boldLabel);
                foreach (var group in characters.GroupBy(c => c.role))
                {
                    string icon = group.Key switch
                    {
                        CharacterRole.Protagonista => "🦸",
                        CharacterRole.Enemigo => "🦹",
                        CharacterRole.NPC => "🧑",
                        CharacterRole.Compañero => "🤝",
                        CharacterRole.Jefe => "👑",
                        _ => "👤"
                    };
                    DrawDashSection($"{icon} {group.Key}", "#7ec8e3",
                        group.Select(c => c.characterName).ToList());
                }
                EditorGUILayout.EndVertical();
            }

            // Resumen de campos personalizados más usados
            DrawCustomFieldSummary();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

        private void DrawCustomFieldSummary()
        {
            var mechFields = CustomFieldService.Schema.MechanicFields;
            if (mechFields.Count == 0) return;

            GUILayout.Space(10);
            EditorGUILayout.BeginVertical();
            GUILayout.Label("Campos personalizados (mecánicas)", EditorStyles.boldLabel);

            // Solo mostramos checkboxes y dropdowns (los más interesantes para un resumen)
            foreach (var fd in mechFields.Where(f => f.Type == CustomFieldType.CheckBox
                                                  || f.Type == CustomFieldType.Dropdown))
            {
                if (fd.Type == CustomFieldType.CheckBox)
                {
                    int trueCount = mechanics.Count(m =>
                        CustomFieldService.GetValue(CustomFieldService.GetGuid(m), fd.Id) == "true");
                    DrawDashSection($"☑ {fd.Label}", "#e0e0e0",
                        new List<string> { $"{trueCount} / {mechanics.Count} marcadas" });
                }
                else // Dropdown
                {
                    var grouped = mechanics
                        .GroupBy(m => {
                            string v = CustomFieldService.GetValue(CustomFieldService.GetGuid(m), fd.Id);
                            return string.IsNullOrEmpty(v) ? "(sin valor)" : v;
                        })
                        .Select(g => $"{g.Key}: {g.Count()}")
                        .ToList();
                    DrawDashSection(fd.Label, "#b0c4de", grouped);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawMetricBox(string label, string value, Color color)
        {
            Rect r = GUILayoutUtility.GetRect(100, 68, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, new Color(0.18f, 0.18f, 0.22f));
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 3), color);
            GUI.Label(new Rect(r.x, r.y + 10, r.width, 32), value,
                new GUIStyle(EditorStyles.boldLabel)
                { fontSize = 24, alignment = TextAnchor.MiddleCenter, normal = { textColor = color } });
            GUI.Label(new Rect(r.x, r.y + 44, r.width, 18), label,
                new GUIStyle(EditorStyles.miniLabel)
                { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.gray } });
        }

        private void DrawDashSection(string title, string colorHex, List<string> items)
        {
            Color c = HexToColor(colorHex);
            Rect r = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
                GUILayout.Height(16), GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(new Rect(r.x, r.y, 3, r.height), c);
            GUI.Label(new Rect(r.x + 8, r.y, r.width, r.height),
                $"{title} ({items.Count})", EditorStyles.boldLabel);
            GUILayout.Space(2);
            foreach (var item in items.DefaultIfEmpty("(vacío)"))
                GUILayout.Label($"  • {item}", EditorStyles.miniLabel);
            GUILayout.Space(6);
        }

        // ===================================================
        // PESTAÑA 2 — CONFIGURACIÓN (columnas)
        // ===================================================
        private void DrawConfiguration()
        {
            configScroll = EditorGUILayout.BeginScrollView(configScroll);
            GUILayout.Space(6);
            GUILayout.Label("Configuración del tablero", EditorStyles.boldLabel);
            GUILayout.Space(8);

            // Qué assets mostrar
            GUILayout.Label("Tipos de asset", EditorStyles.boldLabel);
            bool newM = EditorGUILayout.Toggle("Mecánicas", config.ShowMechanics);
            bool newC = EditorGUILayout.Toggle("Personajes", config.ShowCharacters);
            if (newM != config.ShowMechanics || newC != config.ShowCharacters)
            { config.ShowMechanics = newM; config.ShowCharacters = newC; SaveConfig(); }

            GUILayout.Space(10);
            GUILayout.Label("Columnas del Kanban", EditorStyles.boldLabel);
            GUILayout.Label("Color · Nombre · Estado vinculado · Visible · Orden",
                EditorStyles.miniLabel);
            GUILayout.Space(4);

            for (int i = 0; i < config.Columns.Count; i++)
            {
                var col = config.Columns[i];
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.BeginHorizontal();

                Color picked = EditorGUILayout.ColorField(GUIContent.none,
                    HexToColor(col.ColorHex), false, false, false, GUILayout.Width(36));
                if (ColorToHex(picked) != col.ColorHex)
                { col.ColorHex = ColorToHex(picked); SaveConfig(); }

                string newLabel = EditorGUILayout.TextField(col.Label, GUILayout.ExpandWidth(true));
                if (newLabel != col.Label) { col.Label = newLabel; SaveConfig(); }

                GUILayout.Label("→", GUILayout.Width(16));
                string[] opts = new[] { "Ninguno" }
                    .Concat(Enum.GetNames(typeof(MechanicStatus))).ToArray();
                int curIdx = col.MechanicStatusValue < 0 ? 0 : col.MechanicStatusValue + 1;
                int newIdx = EditorGUILayout.Popup(curIdx, opts, GUILayout.Width(120));
                if (newIdx != curIdx)
                { col.MechanicStatusValue = newIdx - 1; SaveConfig(); }

                bool newVis = EditorGUILayout.Toggle(col.Visible, GUILayout.Width(18));
                if (newVis != col.Visible) { col.Visible = newVis; SaveConfig(); }
                GUILayout.Label("vis", GUILayout.Width(26));

                GUI.enabled = i > 0;
                if (GUILayout.Button("▲", GUILayout.Width(22)))
                { config.Columns.RemoveAt(i); config.Columns.Insert(i - 1, col); SaveConfig(); break; }
                GUI.enabled = i < config.Columns.Count - 1;
                if (GUILayout.Button("▼", GUILayout.Width(22)))
                { config.Columns.RemoveAt(i); config.Columns.Insert(i + 1, col); SaveConfig(); break; }
                GUI.enabled = true;

                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("✕", GUILayout.Width(22)))
                { config.Columns.RemoveAt(i); SaveConfig(); break; }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            GUILayout.Space(6);
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
            if (GUILayout.Button("+ Añadir columna"))
            {
                config.Columns.Add(new BoardColumn
                {
                    Id = Guid.NewGuid().ToString(),
                    Label = "Nueva columna",
                    ColorHex = "#" + ColorUtility.ToHtmlStringRGB(
                        Color.HSVToRGB(UnityEngine.Random.value, 0.6f, 0.85f)),
                    Visible = true,
                    MechanicStatusValue = -1
                });
                SaveConfig();
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("↩ Restaurar por defecto"))
                if (EditorUtility.DisplayDialog("Restaurar", "¿Borrar columnas personalizadas?", "Sí", "No"))
                { CreateDefaultColumns(); SaveConfig(); }

            EditorGUILayout.EndScrollView();
        }

        // ===================================================
        // PESTAÑA 3 — EDITOR DE CAMPOS PERSONALIZADOS
        // ===================================================
        private void DrawFieldsEditor()
        {
            fieldsScroll = EditorGUILayout.BeginScrollView(fieldsScroll);
            GUILayout.Space(6);
            GUILayout.Label("Campos personalizados por tipo de asset", EditorStyles.boldLabel);
            GUILayout.Label("Define qué campos extra aparecen en las tarjetas y en el panel lateral.",
                EditorStyles.miniLabel);
            GUILayout.Space(10);

            DrawFieldList("Campos de Mecánicas",
                CustomFieldService.Schema.MechanicFields,
                () => { CustomFieldService.Schema.MechanicFields.Add(NewField()); CustomFieldService.SaveSchema(); });

            GUILayout.Space(16);

            DrawFieldList("Campos de Personajes",
                CustomFieldService.Schema.CharacterFields,
                () => { CustomFieldService.Schema.CharacterFields.Add(NewField()); CustomFieldService.SaveSchema(); });

            EditorGUILayout.EndScrollView();
        }

        private void DrawFieldList(string title, List<CustomFieldDef> fields, Action onAdd)
        {
            GUILayout.Label(title, EditorStyles.boldLabel);

            if (fields.Count == 0)
                EditorGUILayout.HelpBox("No hay campos. Pulsa + para añadir uno.", MessageType.None);

            for (int i = 0; i < fields.Count; i++)
            {
                var fd = fields[i];
                EditorGUILayout.BeginVertical(GUI.skin.box);

                EditorGUILayout.BeginHorizontal();

                // Nombre del campo
                string newLabel = EditorGUILayout.TextField("Nombre", fd.Label);
                if (newLabel != fd.Label) { fd.Label = newLabel; CustomFieldService.SaveSchema(); }

                // Tipo
                var newType = (CustomFieldType)EditorGUILayout.EnumPopup(fd.Type, GUILayout.Width(110));
                if (newType != fd.Type) { fd.Type = newType; CustomFieldService.SaveSchema(); }

                // Mostrar en tarjeta
                bool newShow = EditorGUILayout.Toggle(fd.ShowInCard, GUILayout.Width(18));
                if (newShow != fd.ShowInCard) { fd.ShowInCard = newShow; CustomFieldService.SaveSchema(); }
                GUILayout.Label("en tarjeta", GUILayout.Width(66));

                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("✕", GUILayout.Width(22)))
                { fields.RemoveAt(i); CustomFieldService.SaveSchema(); break; }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();

                // Valor por defecto
                string newDefault = EditorGUILayout.TextField("Valor por defecto", fd.DefaultValue);
                if (newDefault != fd.DefaultValue) { fd.DefaultValue = newDefault; CustomFieldService.SaveSchema(); }

                // Opciones del dropdown
                if (fd.Type == CustomFieldType.Dropdown)
                {
                    GUILayout.Label("Opciones (una por línea)", EditorStyles.miniLabel);
                    string joined = string.Join("\n", fd.DropdownOptions);
                    string newJoined = EditorGUILayout.TextArea(joined, GUILayout.Height(60));
                    if (newJoined != joined)
                    {
                        fd.DropdownOptions = newJoined
                            .Split('\n')
                            .Select(s => s.Trim())
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToList();
                        CustomFieldService.SaveSchema();
                    }
                }

                EditorGUILayout.EndVertical();
            }

            GUILayout.Space(4);
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
            if (GUILayout.Button($"+ Añadir campo a {title}")) onAdd();
            GUI.backgroundColor = Color.white;
        }

        private CustomFieldDef NewField() => new CustomFieldDef
        {
            Id = Guid.NewGuid().ToString(),
            Label = "Nuevo campo",
            Type = CustomFieldType.Text,
            ShowInCard = true,
            DefaultValue = ""
        };

        // -------------------------------------------------------
        // Persistencia de configuración del tablero
        // -------------------------------------------------------
        private void LoadConfig()
        {
            if (!File.Exists(CONFIG_PATH)) { config = new BoardConfig(); CreateDefaultColumns(); return; }
            try
            {
                config = JsonUtility.FromJson<BoardConfig>(File.ReadAllText(CONFIG_PATH)) ?? new BoardConfig();
                if (config.Columns == null || config.Columns.Count == 0) CreateDefaultColumns();
            }
            catch { config = new BoardConfig(); CreateDefaultColumns(); }
        }

        private void SaveConfig()
        {
            string folder = Path.GetDirectoryName(CONFIG_PATH);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            File.WriteAllText(CONFIG_PATH, JsonUtility.ToJson(config, prettyPrint: true));
            AssetDatabase.Refresh();
            Repaint();
        }

        private void CreateDefaultColumns()
        {
            config.Columns = new List<BoardColumn>
            {
                new BoardColumn { Id = Guid.NewGuid().ToString(), Label = "💡 Idea",          ColorHex = "#8892a4", Visible = true, MechanicStatusValue = (int)MechanicStatus.Idea },
                new BoardColumn { Id = Guid.NewGuid().ToString(), Label = "🔧 En desarrollo",  ColorHex = "#f0a500", Visible = true, MechanicStatusValue = (int)MechanicStatus.EnDesarrollo },
                new BoardColumn { Id = Guid.NewGuid().ToString(), Label = "✅ Implementada",   ColorHex = "#4caf50", Visible = true, MechanicStatusValue = (int)MechanicStatus.Implementada },
            };
        }

        private void RefreshAssets()
        {
            mechanics = LoadAssets<MechanicData>();
            characters = LoadAssets<CharacterData>();
            Repaint();
        }

        private List<T> LoadAssets<T>() where T : ScriptableObject
        {
            var result = new List<T>();
            foreach (var guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) result.Add(asset);
            }
            return result;
        }

        // -------------------------------------------------------
        // Helpers de color
        // -------------------------------------------------------
        private List<MechanicData> GetMechanicsForColumn(BoardColumn col)
        {
            if (col.MechanicStatusValue < 0) return new List<MechanicData>();
            return mechanics.Where(m => m.status == (MechanicStatus)col.MechanicStatusValue).ToList();
        }

        private Color HexToColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color c)) return c;
            return Color.white;
        }

        private string ColorToHex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);
    }
}
