using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace TFGToolkit
{
    // =========================================================
    // VENTANA PRINCIPAL
    // =========================================================
    public class MechanicGraphWindow : EditorWindow
    {
        private MechanicGraphData _graphData;
        private MechanicGraphView _graphView;

        // Sidebar
        private List<MechanicData> _allMechanics = new();
        private List<MechanicData> _filteredMechanics = new();
        private string _searchText = "";
        private Vector2 _sidebarScroll;

        // Inspector
        private MechanicNodeView _selectedNode;
        private Vector2 _inspectorScroll;

        // IA
        private bool _isAnalyzing = false;
        private string _aiResult = "";
        private string _aiError = "";
        private Vector2 _aiScroll;

        // Tooltip del botón IA
        private bool _showAITooltip = false;

        // Layout
        private const float SIDEBAR_W = 210f;
        private const float INSPECTOR_W = 200f;
        private const float TOOLBAR_H = 28f;
        private const float AI_PANEL_H = 130f;

        public static void ShowWindow()
        {
            var w = GetWindow<MechanicGraphWindow>("Editor de Mecánicas");
            w.minSize = new Vector2(820, 540);
        }

        // -------------------------------------------------------
        // Ciclo de vida
        // -------------------------------------------------------
        private void OnEnable()
        {
            RefreshMechanics();
            if (_graphData != null) RebuildGraph();
        }

        private void OnDisable()
        {
            DestroyGraphView();
        }

        // -------------------------------------------------------
        // OnGUI — layout manual con tres columnas + toolbar
        // -------------------------------------------------------
        private void OnGUI()
        {
            DrawToolbar();

            float contentY = TOOLBAR_H;
            float contentH = position.height - TOOLBAR_H;

            // Sidebar izquierdo
            GUILayout.BeginArea(new Rect(0, contentY, SIDEBAR_W, contentH));
            DrawSidebar();
            GUILayout.EndArea();

            // Área central del grafo
            float graphX = SIDEBAR_W;
            float graphW = position.width - SIDEBAR_W - INSPECTOR_W;
            DrawGraphArea(new Rect(graphX, contentY, graphW, contentH));

            // Inspector derecho
            GUILayout.BeginArea(new Rect(position.width - INSPECTOR_W, contentY, INSPECTOR_W, contentH));
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

            GUILayout.Label("Editor de Mecánicas", EditorStyles.boldLabel, GUILayout.Width(160));

            // Selector de grafo
            if (GUILayout.Button("Nuevo grafo", EditorStyles.toolbarButton, GUILayout.Width(80)))
                CreateNewGraph();
            if (GUILayout.Button("Abrir grafo", EditorStyles.toolbarButton, GUILayout.Width(80)))
                OpenGraph();

            GUI.enabled = _graphData != null;
            if (GUILayout.Button("Guardar", EditorStyles.toolbarButton, GUILayout.Width(64)))
                SaveGraph();
            GUI.enabled = true;

            GUILayout.FlexibleSpace();

            // Botón IA con tooltip
            DrawAIButton();

            GUILayout.Space(4);
            if (GUILayout.Button("Ajustar vista", EditorStyles.toolbarButton, GUILayout.Width(80)))
                _graphView?.FrameAll();
            if (GUILayout.Button("Limpiar", EditorStyles.toolbarButton, GUILayout.Width(54)))
                ClearGraph();

            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawAIButton()
        {
            GUIStyle aiStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                normal = { textColor = new Color(0.7f, 0.5f, 1f) },
                hover = { textColor = new Color(0.85f, 0.7f, 1f) }
            };

            Rect btnRect = GUILayoutUtility.GetRect(
                new GUIContent("Analizar con IA"),
                aiStyle,
                GUILayout.Width(120));

            // Detectar hover para tooltip
            bool isHover = btnRect.Contains(Event.current.mousePosition);
            if (isHover != _showAITooltip)
            {
                _showAITooltip = isHover;
                Repaint();
            }

            GUI.enabled = _graphData != null && !_isAnalyzing;
            if (GUI.Button(btnRect, _isAnalyzing ? "Analizando..." : "Analizar con IA", aiStyle))
                AnalyzeWithAI();
            GUI.enabled = true;

            // Tooltip desplegable bajo el botón
            if (_showAITooltip && !_isAnalyzing)
            {
                float ttW = 260f;
                float ttH = 86f;
                Rect ttRect = new Rect(
                    btnRect.x - ttW + btnRect.width,
                    TOOLBAR_H,
                    ttW, ttH);

                // Fondo del tooltip
                GUI.color = new Color(0.12f, 0.1f, 0.2f, 0.97f);
                GUI.DrawTexture(ttRect, Texture2D.whiteTexture);
                GUI.color = Color.white;
                EditorGUI.DrawRect(ttRect, new Color(0.18f, 0.14f, 0.3f));

                // Borde
                Handles.color = new Color(0.5f, 0.35f, 0.8f, 0.8f);
                Handles.DrawSolidRectangleWithOutline(ttRect,
                    Color.clear,
                    new Color(0.5f, 0.35f, 0.8f, 0.6f));

                GUIStyle ttStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    wordWrap = true,
                    normal = { textColor = new Color(0.75f, 0.65f, 1f) },
                    fontSize = 10
                };
                GUIStyle ttTitle = new GUIStyle(EditorStyles.boldLabel)
                {
                    normal = { textColor = new Color(0.85f, 0.75f, 1f) },
                    fontSize = 11
                };

                GUI.Label(new Rect(ttRect.x + 8, ttRect.y + 6, ttRect.width - 16, 18),
                    "¿Qué hace el análisis IA?", ttTitle);
                GUI.Label(new Rect(ttRect.x + 8, ttRect.y + 24, ttRect.width - 16, 56),
                    "Envía todas las mecánicas y conexiones del grafo a la IA. " +
                    "Detecta nodos sin conexión, posibles ciclos, relaciones " +
                    "inconsistentes y sugiere nuevas mecánicas según el diseño.",
                    ttStyle);
            }
        }

        // =========================================================
        // SIDEBAR
        // =========================================================
        private void DrawSidebar()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(SIDEBAR_W));

            // Cabecera
            EditorGUI.DrawRect(new Rect(0, 0, SIDEBAR_W, 22),
                new Color(0.18f, 0.18f, 0.18f));
            GUILayout.Label("Mecánicas del proyecto", EditorStyles.boldLabel);

            // ── Buscador ──
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("🔍", GUILayout.Width(16));
            string newSearch = EditorGUILayout.TextField(_searchText, GUILayout.ExpandWidth(true));
            if (newSearch != _searchText)
            {
                _searchText = newSearch;
                ApplyFilter();
            }
            if (!string.IsNullOrEmpty(_searchText))
            {
                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(18)))
                {
                    _searchText = "";
                    ApplyFilter();
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Label($"{_filteredMechanics.Count} / {_allMechanics.Count}",
                EditorStyles.centeredGreyMiniLabel);
            GUILayout.Space(2);

            // ── Lista con scroll ──
            float listH = position.height - TOOLBAR_H - 100f;
            _sidebarScroll = EditorGUILayout.BeginScrollView(
                _sidebarScroll,
                GUILayout.Height(listH));

            if (_filteredMechanics.Count == 0)
            {
                GUILayout.Label(
                    string.IsNullOrEmpty(_searchText)
                        ? "No hay mecánicas en el proyecto."
                        : "Sin resultados para la búsqueda.",
                    EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                foreach (var m in _filteredMechanics)
                    DrawSidebarItem(m);
            }

            EditorGUILayout.EndScrollView();

            // ── Botón refrescar ──
            GUILayout.Space(4);
            if (GUILayout.Button("↺ Actualizar lista", EditorStyles.miniButton))
            {
                RefreshMechanics();
                ApplyFilter();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSidebarItem(MechanicData m)
        {
            Color dotColor = m.status switch
            {
                MechanicStatus.Implementada => new Color(0.22f, 0.63f, 0.41f),
                MechanicStatus.EnDesarrollo => new Color(0.84f, 0.62f, 0.17f),
                _ => new Color(0.50f, 0.36f, 0.83f)
            };

            EditorGUILayout.BeginHorizontal();

            // Dot de estado
            Rect dotRect = GUILayoutUtility.GetRect(12, 12,
                GUILayout.Width(12), GUILayout.Height(12));
            dotRect.y += 4;
            EditorGUI.DrawRect(dotRect, dotColor);

            // Nombre
            GUILayout.Label(m.mechanicName, EditorStyles.label);

            GUILayout.FlexibleSpace();

            // Botón añadir al grafo
            bool alreadyInGraph = _graphData != null &&
                _graphData.Nodes.Any(n => n.MechanicId ==
                    AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m)));

            GUI.enabled = _graphData != null && !alreadyInGraph;
            if (GUILayout.Button(alreadyInGraph ? "✓" : "+",
                EditorStyles.miniButton, GUILayout.Width(22)))
                AddMechanicToGraph(m);
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
        }

        // =========================================================
        // ÁREA DEL GRAFO
        // =========================================================
        private void DrawGraphArea(Rect rect)
        {
            if (_graphView == null) return;

            // El GraphView se posiciona dentro de su IMGUIContainer
            // La integración real se hace en RebuildGraph()
        }

        // =========================================================
        // INSPECTOR
        // =========================================================
        private void DrawInspector()
        {
            float fullH = position.height - TOOLBAR_H;

            // Área superior del inspector
            float bodyH = fullH - AI_PANEL_H;
            EditorGUILayout.BeginVertical(GUILayout.Width(INSPECTOR_W), GUILayout.Height(bodyH));

            if (_selectedNode == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Selecciona un nodo.", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
            }
            else
            {
                DrawNodeInspector();
            }

            EditorGUILayout.EndVertical();

            // Panel IA al fondo del inspector
            GUILayout.BeginArea(new Rect(0, bodyH, INSPECTOR_W, AI_PANEL_H));
            DrawAIPanel();
            GUILayout.EndArea();
        }

        private void DrawNodeInspector()
        {
            var m = _selectedNode?.BoundMechanic;
            if (m == null) return;

            GUILayout.Label("Inspector", EditorStyles.boldLabel);
            GUILayout.Space(2);

            _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);

            // Datos de la mecánica (solo lectura — la ficha se edita en MechanicEditorWindow)
            EditorGUILayout.LabelField("Nombre", m.mechanicName);
            EditorGUILayout.LabelField("Estado", m.status.ToString());
            if (!string.IsNullOrEmpty(m.input))
                EditorGUILayout.LabelField("Input", m.input);
            if (!string.IsNullOrEmpty(m.effect))
                EditorGUILayout.LabelField("Efecto", m.effect);
            if (m.duration > 0)
                EditorGUILayout.LabelField("Duración", $"{m.duration}s");

            if (!string.IsNullOrEmpty(m.description))
            {
                GUILayout.Space(4);
                GUILayout.Label("Descripción", EditorStyles.miniLabel);
                EditorGUILayout.LabelField(m.description,
                    new GUIStyle(EditorStyles.label) { wordWrap = true });
            }

            // Conexiones del nodo seleccionado
            if (_graphData != null)
            {
                string nodeId = _selectedNode.NodeData.Id;
                var edgesOut = _graphData.GetEdgesFrom(nodeId);
                var edgesIn = _graphData.GetEdgesTo(nodeId);

                if (edgesOut.Count > 0 || edgesIn.Count > 0)
                {
                    GUILayout.Space(6);
                    GUILayout.Label("Conexiones", EditorStyles.boldLabel);
                    foreach (var e in edgesOut)
                    {
                        var target = GetMechanicByNodeId(e.TargetNodeId);
                        string tName = target != null ? target.mechanicName : e.TargetNodeId;
                        GUILayout.Label($"→ {RelationLabel(e.Relation)} {tName}",
                            EditorStyles.miniLabel);
                    }
                    foreach (var e in edgesIn)
                    {
                        var source = GetMechanicByNodeId(e.SourceNodeId);
                        string sName = source != null ? source.mechanicName : e.SourceNodeId;
                        GUILayout.Label($"← {RelationLabel(e.Relation)} {sName}",
                            EditorStyles.miniLabel);
                    }
                }
            }

            GUILayout.Space(6);
            if (GUILayout.Button("Abrir ficha completa", EditorStyles.miniButton))
            {
                MechanicEditorWindow.ShowWindow();
                EditorGUIUtility.PingObject(m);
            }
            if (GUILayout.Button("Eliminar del grafo", EditorStyles.miniButton))
                RemoveSelectedNode();

            EditorGUILayout.EndScrollView();
        }

        // =========================================================
        // PANEL IA
        // =========================================================
        private void DrawAIPanel()
        {
            EditorGUI.DrawRect(
                new Rect(0, 0, INSPECTOR_W, AI_PANEL_H),
                new Color(0.10f, 0.08f, 0.18f));

            GUILayout.Space(6);
            GUILayout.Label("✦  Análisis IA",
                new GUIStyle(EditorStyles.boldLabel)
                {
                    normal = { textColor = new Color(0.72f, 0.55f, 1f) }
                });

            _aiScroll = EditorGUILayout.BeginScrollView(
                _aiScroll, GUILayout.Height(AI_PANEL_H - 32));

            if (_isAnalyzing)
            {
                GUILayout.Label("Analizando el grafo...",
                    new GUIStyle(EditorStyles.miniLabel)
                    { normal = { textColor = Color.gray } });
            }
            else if (!string.IsNullOrEmpty(_aiError))
            {
                GUILayout.Label($"Error: {_aiError}",
                    new GUIStyle(EditorStyles.miniLabel)
                    { normal = { textColor = new Color(1f, 0.4f, 0.4f) }, wordWrap = true });
            }
            else if (!string.IsNullOrEmpty(_aiResult))
            {
                GUILayout.Label(_aiResult,
                    new GUIStyle(EditorStyles.miniLabel)
                    { wordWrap = true, normal = { textColor = new Color(0.78f, 0.72f, 0.9f) } });
            }
            else
            {
                GUILayout.Label("Pulsa 'Analizar con IA' para detectar problemas de diseño.",
                    new GUIStyle(EditorStyles.miniLabel)
                    { wordWrap = true, normal = { textColor = Color.gray } });
            }

            EditorGUILayout.EndScrollView();
        }

        // =========================================================
        // ANÁLISIS IA
        // =========================================================
        private void AnalyzeWithAI()
        {
            if (_graphData == null) return;

            _isAnalyzing = true;
            _aiResult = "";
            _aiError = "";
            Repaint();

            string prompt = BuildAnalysisPrompt();

            OllamaAPI.SendMessage(
                prompt,
                onResult: result =>
                {
                    _aiResult = result;
                    _isAnalyzing = false;
                    _graphData.LastAIAnalysis = result;
                    EditorUtility.SetDirty(_graphData);
                    AssetDatabase.SaveAssets();
                    Repaint();
                },
                onError: error =>
                {
                    _aiError = error;
                    _isAnalyzing = false;
                    Repaint();
                }
            );
        }

        private string BuildAnalysisPrompt()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Eres un experto en diseño de videojuegos. Analiza este grafo de mecánicas y detecta:");
            sb.AppendLine("1. Mecánicas sin ninguna conexión (posibles mecánicas huérfanas)");
            sb.AppendLine("2. Posibles ciclos de dependencia que podrían causar problemas");
            sb.AppendLine("3. Inconsistencias en las relaciones (ej: A activa B pero B cancela A)");
            sb.AppendLine("4. Sugerencias de nuevas conexiones o mecánicas que complementen el diseño");
            sb.AppendLine();
            sb.AppendLine("MECÁNICAS EN EL GRAFO:");

            foreach (var node in _graphData.Nodes)
            {
                var m = GetMechanicByNodeId(node.Id);
                if (m == null) continue;
                sb.AppendLine($"- {m.mechanicName} [{m.status}]: {m.description}");
            }

            sb.AppendLine();
            sb.AppendLine("CONEXIONES:");
            foreach (var edge in _graphData.Edges)
            {
                var src = GetMechanicByNodeId(edge.SourceNodeId);
                var tgt = GetMechanicByNodeId(edge.TargetNodeId);
                if (src == null || tgt == null) continue;
                sb.AppendLine($"- {src.mechanicName} --[{edge.Relation}]--> {tgt.mechanicName}");
            }

            sb.AppendLine();
            sb.AppendLine("Responde en español con una lista concisa de observaciones. Máximo 150 palabras.");
            return sb.ToString();
        }

        // =========================================================
        // GESTIÓN DEL GRAFO (GraphView)
        // =========================================================
        private void RebuildGraph()
        {
            DestroyGraphView();
            if (_graphData == null) return;

            _graphView = new MechanicGraphView(_graphData, this);

            // Cambiamos flexGrow y valores fijos por posicionamiento absoluto
            _graphView.style.position = Position.Absolute;

            // Anclamos los 4 lados para que ocupe todo el espacio libre entre el sidebar y el inspector.
            // Al decirle a cuántos píxeles debe anclarse del borde derecho, se redimensionará solo.
            _graphView.style.left = SIDEBAR_W;
            _graphView.style.right = INSPECTOR_W;
            _graphView.style.top = TOOLBAR_H;
            _graphView.style.bottom = 0;

            rootVisualElement.Add(_graphView);
            _graphView.LoadGraph();
        }

        private void DestroyGraphView()
        {
            if (_graphView != null)
            {
                rootVisualElement.Remove(_graphView);
                _graphView = null;
            }
        }

        // =========================================================
        // ACCIONES
        // =========================================================
        private void CreateNewGraph()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Nuevo grafo", "NuevoGrafo", "asset",
                "Guardar grafo de mecánicas",
                "Assets/TFGToolkit/Data/Mechanic");
            if (string.IsNullOrEmpty(path)) return;

            var g = ScriptableObject.CreateInstance<MechanicGraphData>();
            AssetDatabase.CreateAsset(g, path);
            AssetDatabase.SaveAssets();
            _graphData = g;
            RebuildGraph();
        }

        private void OpenGraph()
        {
            string path = EditorUtility.OpenFilePanel(
                "Abrir grafo", "Assets/TFGToolkit/Data/Mechanic", "asset");
            if (string.IsNullOrEmpty(path)) return;

            // Convertir ruta absoluta a relativa
            if (path.StartsWith(Application.dataPath))
                path = "Assets" + path.Substring(Application.dataPath.Length);

            var g = AssetDatabase.LoadAssetAtPath<MechanicGraphData>(path);
            if (g == null)
            {
                EditorUtility.DisplayDialog("Error", "El archivo no es un MechanicGraphData válido.", "OK");
                return;
            }
            _graphData = g;
            RebuildGraph();
        }

        private void SaveGraph()
        {
            if (_graphData == null) return;
            EditorUtility.SetDirty(_graphData);
            AssetDatabase.SaveAssets();
            Debug.Log("[TFG Toolkit] Grafo guardado.");
        }

        private void ClearGraph()
        {
            if (_graphData == null) return;
            bool confirm = EditorUtility.DisplayDialog(
                "Limpiar grafo",
                "¿Eliminar todos los nodos y conexiones del grafo?",
                "Limpiar", "Cancelar");
            if (!confirm) return;

            _graphData.Nodes.Clear();
            _graphData.Edges.Clear();
            SaveGraph();
            RebuildGraph();
        }

        private void AddMechanicToGraph(MechanicData m)
        {
            if (_graphData == null || m == null) return;
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m));

            var node = new GraphNodeData
            {
                Id = Guid.NewGuid().ToString(),
                MechanicId = guid,
                PosX = UnityEngine.Random.Range(100f, 400f),
                PosY = UnityEngine.Random.Range(80f, 300f)
            };
            _graphData.Nodes.Add(node);
            SaveGraph();
            _graphView?.AddNodeView(node, m);
        }

        private void RemoveSelectedNode()
        {
            if (_selectedNode == null || _graphData == null) return;
            string id = _selectedNode.NodeData.Id;

            _graphData.Nodes.RemoveAll(n => n.Id == id);
            _graphData.Edges.RemoveAll(e => e.SourceNodeId == id || e.TargetNodeId == id);

            _graphView?.RemoveNodeView(_selectedNode);
            _selectedNode = null;
            SaveGraph();
            Repaint();
        }

        // =========================================================
        // CALLBACKS DESDE EL GRAPHVIEW
        // =========================================================
        public void OnNodeSelected(MechanicNodeView node)
        {
            _selectedNode = node;
            Repaint();
        }

        public void OnEdgeCreated(GraphEdgeData edge)
        {
            _graphData?.Edges.Add(edge);
            SaveGraph();
        }

        public void OnEdgeDeleted(string edgeId)
        {
            _graphData?.Edges.RemoveAll(e => e.Id == edgeId);
            SaveGraph();
        }

        public void OnNodeMoved(string nodeId, float x, float y)
        {
            var node = _graphData?.GetNode(nodeId);
            if (node == null) return;
            node.PosX = x;
            node.PosY = y;
            EditorUtility.SetDirty(_graphData);
        }

        // =========================================================
        // HELPERS
        // =========================================================
        private void RefreshMechanics()
        {
            _allMechanics.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:MechanicData"))
            {
                var m = AssetDatabase.LoadAssetAtPath<MechanicData>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (m != null) _allMechanics.Add(m);
            }
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrEmpty(_searchText))
                _filteredMechanics = new List<MechanicData>(_allMechanics);
            else
                _filteredMechanics = _allMechanics
                    .Where(m => m.mechanicName.ToLower().Contains(_searchText.ToLower())
                             || m.description.ToLower().Contains(_searchText.ToLower()))
                    .ToList();
            Repaint();
        }

        private MechanicData GetMechanicByNodeId(string nodeId)
        {
            var node = _graphData?.GetNode(nodeId);
            if (node == null) return null;
            string path = AssetDatabase.GUIDToAssetPath(node.MechanicId);
            return AssetDatabase.LoadAssetAtPath<MechanicData>(path);
        }

        private static string RelationLabel(RelationType r) => r switch
        {
            RelationType.Activa => "activa →",
            RelationType.Requiere => "requiere →",
            RelationType.Cancela => "cancela →",
            RelationType.Modifica => "modifica →",
            _ => "→"
        };
    }
}
