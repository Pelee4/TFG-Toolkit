using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TFGToolkit
{
    public class DialogueGraphWindow : EditorWindow
    {
        private DialogueData _data;
        private DialogueGraphView _graphView;

        // Sidebar
        private List<CharacterData> _characters = new();
        private Vector2 _sidebarScroll;

        // Inspector
        private DialogueNodeView _selectedNode;
        private Vector2 _inspectorScroll;
        private Vector2 _choiceScroll;

        // IA
        private bool _isGenerating = false;
        private string _aiError = "";
        private Vector2 _aiScroll;

        // Layout
        private const float SIDEBAR_W = 200f;
        private const float INSPECTOR_W = 200f;
        private const float TOOLBAR_H = 28f;
        private const float AI_PANEL_H = 120f;

        public static void ShowWindow()
        {
            var w = GetWindow<DialogueGraphWindow>("Editor de Diálogos");
            w.minSize = new Vector2(820, 540);
        }

        private void OnEnable()
        {
            RefreshCharacters();
            if (_data != null) RebuildGraph();
        }

        private void OnDisable() => DestroyGraphView();



        // =========================================================
        // OnGUI
        // =========================================================
        private void OnGUI()
        {
            DrawToolbar();

            float cy = TOOLBAR_H;
            float ch = position.height - TOOLBAR_H;

            GUILayout.BeginArea(new Rect(0, cy, SIDEBAR_W, ch));
            DrawSidebar();
            GUILayout.EndArea();

            // El GraphView se gestiona como VisualElement — solo actualizamos su rect
            if (_graphView != null)
            {
                float gw = position.width - SIDEBAR_W - INSPECTOR_W;
                _graphView.style.left = SIDEBAR_W;
                _graphView.style.top = TOOLBAR_H;
                _graphView.style.width = gw;
                _graphView.style.height = ch;
            }

            GUILayout.BeginArea(new Rect(position.width - INSPECTOR_W, cy, INSPECTOR_W, ch));
            DrawInspector();
            GUILayout.EndArea();
        }

        // =========================================================
        // TOOLBAR
        // =========================================================
        private void DrawToolbar()
        {
            GUILayout.BeginArea(new Rect(0, 0, position.width, TOOLBAR_H));
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("Editor de Diálogos", EditorStyles.boldLabel, GUILayout.Width(150));

            if (GUILayout.Button("Nuevo árbol", EditorStyles.toolbarButton, GUILayout.Width(80)))
                CreateNewTree();
            if (GUILayout.Button("Abrir", EditorStyles.toolbarButton, GUILayout.Width(50)))
                OpenTree();

            GUI.enabled = _data != null;
            if (GUILayout.Button("Guardar", EditorStyles.toolbarButton, GUILayout.Width(60)))
                SaveTree();
            GUI.enabled = true;

            GUILayout.FlexibleSpace();

            // Botones IA
            GUIStyle aiStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                normal = { textColor = new Color(0.65f, 0.45f, 1f) }
            };
            GUI.enabled = _data != null && _selectedNode != null && !_isGenerating;
            if (GUILayout.Button(_isGenerating ? "Generando..." : "✦ Generar diálogo", aiStyle, GUILayout.Width(120)))
                GenerateDialogue();
            if (GUILayout.Button("✦ Sugerir continuación", aiStyle, GUILayout.Width(140)))
                SuggestContinuation();
            GUI.enabled = true;

            GUILayout.Space(4);
            if (GUILayout.Button("Ajustar vista", EditorStyles.toolbarButton, GUILayout.Width(80)))
                _graphView?.FrameAll();

            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        // =========================================================
        // SIDEBAR
        // =========================================================
        private void DrawSidebar()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(SIDEBAR_W));

            // Personajes
            GUILayout.Label("Personajes", EditorStyles.boldLabel);
            _sidebarScroll = EditorGUILayout.BeginScrollView(
                _sidebarScroll, GUILayout.Height(160));

            foreach (var ch in _characters)
                DrawCharacterItem(ch);

            if (_characters.Count == 0)
                GUILayout.Label("No hay personajes.", EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.EndScrollView();

            GUILayout.Space(8);

            // Tipos de nodo
            GUILayout.Label("Añadir nodo", EditorStyles.boldLabel);
            DrawNodeTypeButton(DialogueNodeType.Dialogue, "Diálogo", new Color(0.22f, 0.58f, 0.87f));
            DrawNodeTypeButton(DialogueNodeType.Choice, "Elección", new Color(0.84f, 0.62f, 0.17f));
            DrawNodeTypeButton(DialogueNodeType.Condition, "Condición", new Color(0.34f, 0.58f, 0.10f));
            DrawNodeTypeButton(DialogueNodeType.Event, "Evento", new Color(0.58f, 0.42f, 0.82f));
            DrawNodeTypeButton(DialogueNodeType.End, "Fin de rama", new Color(0.85f, 0.28f, 0.18f));

            GUILayout.Space(8);

            // Leyenda
            GUILayout.Label("Leyenda", EditorStyles.boldLabel);
            DrawLegendItem("Secuencia", new Color(0.5f, 0.5f, 0.5f), false);
            DrawLegendItem("Opción de elección", new Color(0.84f, 0.62f, 0.17f), true);
            DrawLegendItem("Condición", new Color(0.34f, 0.58f, 0.10f), false);

            GUILayout.Space(4);
            if (GUILayout.Button("↺ Actualizar personajes", EditorStyles.miniButton))
                RefreshCharacters();

            EditorGUILayout.EndVertical();
        }

        private void DrawCharacterItem(CharacterData ch)
        {
            Color avatarColor = ch.role switch
            {
                CharacterRole.Protagonista => new Color(0.09f, 0.22f, 0.38f),
                CharacterRole.Enemigo => new Color(0.32f, 0.12f, 0.08f),
                CharacterRole.NPC => new Color(0.12f, 0.24f, 0.06f),
                CharacterRole.Compañero => new Color(0.24f, 0.18f, 0.32f),
                _ => new Color(0.20f, 0.16f, 0.08f)
            };

            EditorGUILayout.BeginHorizontal();
            Rect dotRect = GUILayoutUtility.GetRect(20, 20,
                GUILayout.Width(20), GUILayout.Height(20));
            dotRect.y += 2;
            EditorGUI.DrawRect(dotRect, avatarColor);
            GUILayout.Label(ch.characterName, EditorStyles.label);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawNodeTypeButton(DialogueNodeType type, string label, Color color)
        {
            GUI.enabled = _data != null;
            Rect r = GUILayoutUtility.GetRect(
                GUIContent.none, EditorStyles.miniButton,
                GUILayout.Height(22), GUILayout.ExpandWidth(true));

            EditorGUI.DrawRect(new Rect(r.x, r.y + 4, 6, 14), color);

            if (GUI.Button(new Rect(r.x + 10, r.y, r.width - 10, r.height),
                label, EditorStyles.miniButton))
                AddNode(type);
            GUI.enabled = true;
        }

        private void DrawLegendItem(string label, Color color, bool dashed)
        {
            EditorGUILayout.BeginHorizontal();
            Rect lr = GUILayoutUtility.GetRect(24, 12,
                GUILayout.Width(24), GUILayout.Height(12));
            lr.y += 4;
            if (!dashed)
                EditorGUI.DrawRect(new Rect(lr.x, lr.y + 4, 24, 2), color);
            else
            {
                EditorGUI.DrawRect(new Rect(lr.x, lr.y + 4, 6, 2), color);
                EditorGUI.DrawRect(new Rect(lr.x + 10, lr.y + 4, 6, 2), color);
                EditorGUI.DrawRect(new Rect(lr.x + 19, lr.y + 4, 5, 2), color);
            }
            GUILayout.Label(label, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        // =========================================================
        // INSPECTOR
        // =========================================================
        private void DrawInspector()
        {
            float fullH = position.height - TOOLBAR_H;
            float bodyH = fullH - AI_PANEL_H;

            // Cuerpo del inspector
            EditorGUILayout.BeginVertical(GUILayout.Width(INSPECTOR_W), GUILayout.Height(bodyH));

            if (_selectedNode == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Selecciona un nodo.", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
            }
            else
            {
                DrawNodeInspector(_selectedNode.NodeData);
            }

            EditorGUILayout.EndVertical();

            // Panel IA
            GUILayout.BeginArea(new Rect(0, bodyH, INSPECTOR_W, AI_PANEL_H));
            DrawAIPanel();
            GUILayout.EndArea();
        }

        private void DrawNodeInspector(DialogueNodeData d)
        {
            GUILayout.Label("Inspector", EditorStyles.boldLabel);

            // Color de cabecera según tipo
            Color c = DialogueNodeView.GetAccentColor(d.Type);
            Rect typeRect = GUILayoutUtility.GetRect(INSPECTOR_W - 20, 18);
            EditorGUI.DrawRect(typeRect, DialogueNodeView.GetHeadColor(d.Type));
            GUI.Label(typeRect, $"  {d.Type}", new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = c }
            });

            _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);

            // Estilo dinámico
            GUIStyle dynamicTextArea = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true
            };

            // Anchura disponible para los cuadros de texto (200 - padding de la scrollbar)
            float textWidth = INSPECTOR_W - 25f;

            // Personaje (solo para Dialogue)
            if (d.Type == DialogueNodeType.Dialogue)
            {
                GUILayout.Label("Personaje", EditorStyles.miniLabel);
                int currentIdx = _characters.FindIndex(ch =>
                    AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(ch)) == d.CharacterGUID);
                string[] charNames = _characters.Select(ch => ch.characterName).ToArray();
                int newIdx = EditorGUILayout.Popup(currentIdx, charNames.Length > 0 ? charNames : new[] { "(ninguno)" });
                if (newIdx != currentIdx && newIdx >= 0 && newIdx < _characters.Count)
                {
                    d.CharacterGUID = AssetDatabase.AssetPathToGUID(
                        AssetDatabase.GetAssetPath(_characters[newIdx]));
                    _graphView?.RefreshNodeView(d.Id);
                    SaveTree();
                }

                GUILayout.Space(4);
                GUILayout.Label("Texto del diálogo", EditorStyles.miniLabel);

                // Calculamos altura basándonos en el texto actual garantizando un mínimo de 60
                float h = Mathf.Max(dynamicTextArea.CalcHeight(new GUIContent(d.DialogueText), textWidth), 60f);
                string newText = EditorGUILayout.TextArea(d.DialogueText, dynamicTextArea, GUILayout.Height(h));

                if (newText != d.DialogueText)
                {
                    d.DialogueText = newText;
                    _graphView?.RefreshNodeView(d.Id);
                    SaveTree();
                }
            }

            // Opciones (Choice)
            if (d.Type == DialogueNodeType.Choice)
            {
                GUILayout.Label("Opciones del jugador", EditorStyles.boldLabel);
                _choiceScroll = EditorGUILayout.BeginScrollView(_choiceScroll, GUILayout.Height(100));
                for (int i = 0; i < d.Choices.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    string newChoiceText = EditorGUILayout.TextField(d.Choices[i].Text);
                    if (newChoiceText != d.Choices[i].Text)
                    {
                        d.Choices[i].Text = newChoiceText;
                        _graphView?.RefreshNodeView(d.Id);
                        SaveTree();
                    }
                    GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                    if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(20)))
                    {
                        d.Choices.RemoveAt(i);
                        _graphView?.RefreshNodeView(d.Id);
                        SaveTree();
                        break;
                    }
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();

                if (GUILayout.Button("+ Añadir opción", EditorStyles.miniButton))
                {
                    d.Choices.Add(new DialogueChoice
                    { Id = Guid.NewGuid().ToString(), Text = "Nueva opción" });
                    _graphView?.RefreshNodeView(d.Id);
                    SaveTree();
                }
            }

            // Condición
            if (d.Type == DialogueNodeType.Condition)
            {
                GUILayout.Label("Variable", EditorStyles.miniLabel);
                string nv = EditorGUILayout.TextField(d.ConditionVar);
                if (nv != d.ConditionVar) { d.ConditionVar = nv; SaveTree(); }

                GUILayout.Label("Operador", EditorStyles.miniLabel);
                string[] ops = { ">", "<", "==", "!=", ">=", "<=" };
                int opIdx = Array.IndexOf(ops, d.ConditionOp);
                int newOp = EditorGUILayout.Popup(opIdx < 0 ? 0 : opIdx, ops);
                if (newOp != opIdx) { d.ConditionOp = ops[newOp]; SaveTree(); }

                GUILayout.Label("Valor", EditorStyles.miniLabel);
                float nVal = EditorGUILayout.FloatField(d.ConditionValue);
                if (nVal != d.ConditionValue) { d.ConditionValue = nVal; SaveTree(); }
            }

            // Evento
            if (d.Type == DialogueNodeType.Event)
            {
                GUILayout.Label("Clave del evento", EditorStyles.miniLabel);
                string nk = EditorGUILayout.TextField(d.EventKey);
                if (nk != d.EventKey) { d.EventKey = nk; SaveTree(); }
            }

            // Fin
            if (d.Type == DialogueNodeType.End)
            {
                GUILayout.Label("Descripción", EditorStyles.miniLabel);

                float h = Mathf.Max(dynamicTextArea.CalcHeight(new GUIContent(d.EndDescription), textWidth), 40f);
                string nd = EditorGUILayout.TextArea(d.EndDescription, dynamicTextArea, GUILayout.Height(h));
                if (nd != d.EndDescription) { d.EndDescription = nd; SaveTree(); }
            }

            // Notas del diseñador (todos los tipos)
            GUILayout.Space(6);
            GUILayout.Label("Notas del diseñador", EditorStyles.miniLabel);

            float notesH = Mathf.Max(dynamicTextArea.CalcHeight(new GUIContent(d.DesignerNotes), textWidth), 36f);
            string nn = EditorGUILayout.TextArea(d.DesignerNotes, dynamicTextArea, GUILayout.Height(notesH));
            if (nn != d.DesignerNotes) { d.DesignerNotes = nn; SaveTree(); }

            GUILayout.Space(6);
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("Eliminar nodo", EditorStyles.miniButton))
                RemoveSelectedNode();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndScrollView();
        }

        // =========================================================
        // PANEL IA
        // =========================================================
        private void DrawAIPanel()
        {
            EditorGUI.DrawRect(new Rect(0, 0, INSPECTOR_W, AI_PANEL_H),
                new Color(0.10f, 0.08f, 0.18f));

            GUILayout.Space(6);
            GUILayout.Label("✦  Asistente IA",
                new GUIStyle(EditorStyles.boldLabel)
                { normal = { textColor = new Color(0.65f, 0.45f, 1f) } });

            if (_isGenerating)
            {
                GUILayout.Label("Generando...",
                    new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } });
                return;
            }

            if (!string.IsNullOrEmpty(_aiError))
            {
                GUILayout.Label(_aiError,
                    new GUIStyle(EditorStyles.miniLabel)
                    { wordWrap = true, normal = { textColor = new Color(1f, 0.4f, 0.4f) } });
            }

            GUI.enabled = _data != null && _selectedNode != null;

            GUIStyle aiBtnStyle = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleLeft
            };

            if (GUILayout.Button("  ✦ Generar diálogo para el personaje", aiBtnStyle))
                GenerateDialogue();

            if (GUILayout.Button("  ✦ Sugerir continuación del nodo", aiBtnStyle))
                SuggestContinuation();

            if (GUILayout.Button("  ✦ Adaptar tono al personaje", aiBtnStyle))
                AdaptTone();

            GUI.enabled = true;
        }

        // =========================================================
        // LÓGICA IA
        // =========================================================
        private void GenerateDialogue()
        {
            var d = _selectedNode?.NodeData;
            if (d == null || d.Type != DialogueNodeType.Dialogue) return;

            var character = GetCharacterByGuid(d.CharacterGUID);
            if (character == null)
            {
                _aiError = "Asigna un personaje al nodo primero.";
                Repaint();
                return;
            }

            _isGenerating = true;
            _aiError = "";
            Repaint();

            string prompt = $@"Eres un escritor experto en narrativa de videojuegos.
Genera una línea de diálogo para el personaje '{character.characterName}' con estas características:
- Rol: {character.role}
- Trasfondo: {character.backstory}
- Motivación: {character.motivation}
- Habilidades: {character.abilities}
Contexto del árbol: {_data.TreeName} — {_data.Description}
El diálogo debe ser conciso (1-2 frases), natural y coherente con el personaje.
Devuelve SOLO el texto del diálogo, sin comillas ni explicaciones adicionales.";

            OllamaAPI.SendMessage(prompt,
                onResult: result =>
                {
                    d.DialogueText = result.Trim();
                    _graphView?.RefreshNodeView(d.Id);
                    SaveTree();
                    _isGenerating = false;
                    Repaint();
                },
                onError: error =>
                {
                    _aiError = error;
                    _isGenerating = false;
                    Repaint();
                });
        }

        private void SuggestContinuation()
        {
            var d = _selectedNode?.NodeData;
            if (d == null) return;

            _isGenerating = true;
            _aiError = "";
            Repaint();

            string nodeDesc = d.Type == DialogueNodeType.Dialogue
                ? $"Diálogo de {GetCharacterName(d.CharacterGUID)}: \"{d.DialogueText}\""
                : $"Nodo de tipo {d.Type}";

            string prompt = $@"Eres un diseñador narrativo de videojuegos.
El árbol de diálogos se llama '{_data.TreeName}': {_data.Description}
El nodo actual es: {nodeDesc}
Sugiere 2-3 posibles continuaciones narrativas para este nodo, cada una en una línea.
Sé conciso y práctico. Solo las sugerencias, sin explicaciones.";

            OllamaAPI.SendMessage(prompt,
                onResult: result =>
                {
                    d.DesignerNotes = $"[Sugerencias IA]\n{result.Trim()}";
                    SaveTree();
                    _isGenerating = false;
                    Repaint();
                },
                onError: error =>
                {
                    _aiError = error;
                    _isGenerating = false;
                    Repaint();
                });
        }

        private void AdaptTone()
        {
            var d = _selectedNode?.NodeData;
            if (d == null || string.IsNullOrEmpty(d.DialogueText)) return;

            var character = GetCharacterByGuid(d.CharacterGUID);
            if (character == null) { _aiError = "Asigna un personaje primero."; Repaint(); return; }

            _isGenerating = true;
            _aiError = "";
            Repaint();

            string prompt = $@"Reescribe este diálogo adaptando el tono al personaje indicado.
Personaje: {character.characterName} ({character.role})
Trasfondo: {character.backstory}
Motivación: {character.motivation}
Diálogo original: ""{d.DialogueText}""
Devuelve SOLO el diálogo reescrito, sin comillas ni explicaciones.";

            OllamaAPI.SendMessage(prompt,
                onResult: result =>
                {
                    d.DialogueText = result.Trim();
                    _graphView?.RefreshNodeView(d.Id);
                    SaveTree();
                    _isGenerating = false;
                    Repaint();
                },
                onError: error =>
                {
                    _aiError = error;
                    _isGenerating = false;
                    Repaint();
                });
        }

        // =========================================================
        // GESTIÓN DEL GRAFO
        // =========================================================
        private void RebuildGraph()
        {
            DestroyGraphView();
            if (_data == null) return;

            _graphView = new DialogueGraphView(_data, this);
            _graphView.style.flexGrow = 1;
            rootVisualElement.Add(_graphView);
            _graphView.LoadGraph();
        }

        private void DestroyGraphView()
        {
            var existingViews = rootVisualElement.Query<DialogueGraphView>().ToList();
            foreach (var view in existingViews)
            {
                rootVisualElement.Remove(view);
            }

            _graphView = null;
        }
        private void AddNode(DialogueNodeType type)
        {
            if (_data == null) return;

            var nodeData = new DialogueNodeData
            {
                Id = Guid.NewGuid().ToString(),
                Type = type,
                PosX = UnityEngine.Random.Range(100f, 350f),
                PosY = UnityEngine.Random.Range(80f, 280f)
            };

            // Solo puede haber un nodo Start
            if (type == DialogueNodeType.Start &&
                _data.Nodes.Any(n => n.Type == DialogueNodeType.Start))
            {
                EditorUtility.DisplayDialog("Aviso",
                    "Ya existe un nodo de inicio en este árbol.", "OK");
                return;
            }

            _data.Nodes.Add(nodeData);
            _graphView?.AddNodeView(nodeData);
            SaveTree();
        }

        private void RemoveSelectedNode()
        {
            if (_selectedNode == null || _data == null) return;
            string id = _selectedNode.NodeData.Id;
            _data.Nodes.RemoveAll(n => n.Id == id);
            _data.Edges.RemoveAll(e => e.SourceNodeId == id || e.TargetNodeId == id);
            _graphView?.RemoveNodeView(_selectedNode);
            _selectedNode = null;
            SaveTree();
            Repaint();
        }

        // =========================================================
        // ACCIONES FICHERO
        // =========================================================
        private void CreateNewTree()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Nuevo árbol", "NuevoArbol", "asset",
                "Guardar árbol de diálogos", "Assets/TFGToolkit/Data/Dialogue");
            if (string.IsNullOrEmpty(path)) return;

            var d = ScriptableObject.CreateInstance<DialogueData>();

            // Nodo de inicio por defecto
            d.Nodes.Add(new DialogueNodeData
            {
                Id = Guid.NewGuid().ToString(),
                Type = DialogueNodeType.Start,
                PosX = 200f,
                PosY = 50f
            });

            AssetDatabase.CreateAsset(d, path);
            AssetDatabase.SaveAssets();
            _data = d;
            RebuildGraph();
        }

        private void OpenTree()
        {
            string path = EditorUtility.OpenFilePanel(
                "Abrir árbol", "Assets/TFGToolkit/Data/Dialogue", "asset");
            if (string.IsNullOrEmpty(path)) return;
            if (path.StartsWith(Application.dataPath))
                path = "Assets" + path.Substring(Application.dataPath.Length);
            var d = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (d == null) { EditorUtility.DisplayDialog("Error", "Archivo no válido.", "OK"); return; }
            _data = d;
            RebuildGraph();
        }

        private void SaveTree()
        {
            if (_data == null) return;
            EditorUtility.SetDirty(_data);
            AssetDatabase.SaveAssets();
        }

        // =========================================================
        // CALLBACKS DESDE EL GRAPHVIEW
        // =========================================================
        public void OnNodeSelected(DialogueNodeView node)
        {
            _selectedNode = node;
            _aiError = "";
            Repaint();
        }

        public void OnEdgeCreated(DialogueEdgeData edge)
        {
            _data?.Edges.Add(edge);
            SaveTree();
        }

        public void OnEdgeDeleted(string edgeId)
        {
            _data?.Edges.RemoveAll(e => e.Id == edgeId);
            SaveTree();
        }

        public void OnNodeDeleted(string nodeId)
        {
            if (_data == null) return;

            // Borra tanto el nodo como cualquier conexión vinculada a él del ScriptableObject
            _data.Nodes.RemoveAll(n => n.Id == nodeId);
            _data.Edges.RemoveAll(e => e.SourceNodeId == nodeId || e.TargetNodeId == nodeId);

            if (_selectedNode != null && _selectedNode.NodeData.Id == nodeId)
            {
                _selectedNode = null;
            }
            SaveTree();
            Repaint();
        }

        public void OnNodeMoved(string nodeId, float x, float y)
        {
            var node = _data?.GetNode(nodeId);
            if (node == null) return;
            node.PosX = x;
            node.PosY = y;
            EditorUtility.SetDirty(_data);
        }

        // =========================================================
        // HELPERS PÚBLICOS (usados por DialogueGraphView)
        // =========================================================
        public string GetCharacterName(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return "";
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var ch = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            return ch != null ? ch.characterName : "";
        }

        private CharacterData GetCharacterByGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            string path = AssetDatabase.GUIDToAssetPath(guid);
            return AssetDatabase.LoadAssetAtPath<CharacterData>(path);
        }

        private void RefreshCharacters()
        {
            _characters.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:CharacterData"))
            {
                var ch = AssetDatabase.LoadAssetAtPath<CharacterData>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (ch != null) _characters.Add(ch);
            }
            Repaint();
        }

        // =========================================================
        // INTEGRACIÓN CON EL AGENTE
        // =========================================================
        public static List<DialogueData> LoadAllTreesForAgent()
        {
            var result = new List<DialogueData>();
            foreach (var guid in AssetDatabase.FindAssets("t:DialogueData"))
            {
                var d = AssetDatabase.LoadAssetAtPath<DialogueData>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (d != null) result.Add(d);
            }
            return result;
        }
    }
}