using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;


namespace TFGToolkit
{
    // -- Nodo visual del Arbol -- //
    public class DialogueNodeView : Node
    {
        public DialogueNodeData NodeData { get; private set; }
        public Port InPort { get; private set; }
        public Port OutPort { get; private set; }

        private DialogueGraphWindow _window;

        // Colores por tipo de nodo
        public static Color GetHeadColor(DialogueNodeType t) => t switch
        {
            DialogueNodeType.Start => new Color(0.20f, 0.20f, 0.22f),
            DialogueNodeType.Dialogue => new Color(0.09f, 0.22f, 0.38f),
            DialogueNodeType.Choice => new Color(0.30f, 0.20f, 0.04f),
            DialogueNodeType.Condition => new Color(0.12f, 0.24f, 0.06f),
            DialogueNodeType.Event => new Color(0.22f, 0.18f, 0.32f),
            DialogueNodeType.End => new Color(0.32f, 0.12f, 0.08f),
            _ => new Color(0.15f, 0.15f, 0.15f)
        };

        public static Color GetAccentColor(DialogueNodeType t) => t switch
        {
            DialogueNodeType.Start => new Color(0.53f, 0.53f, 0.53f),
            DialogueNodeType.Dialogue => new Color(0.22f, 0.58f, 0.87f),
            DialogueNodeType.Choice => new Color(0.84f, 0.62f, 0.17f),
            DialogueNodeType.Condition => new Color(0.34f, 0.58f, 0.10f),
            DialogueNodeType.Event => new Color(0.58f, 0.42f, 0.82f),
            DialogueNodeType.End => new Color(0.85f, 0.28f, 0.18f),
            _ => Color.white
        };

        public DialogueNodeView(DialogueNodeData data, DialogueGraphWindow window, string characterName = "")
        {
            NodeData = data;
            _window = window;

            title = GetTitle(data, characterName);
            SetPosition(new Rect(data.PosX, data.PosY, 170, 110));

            BuildPorts(data.Type);
            BuildBody(data, characterName);
            ApplyStyle(data.Type);

            RegisterCallback<GeometryChangedEvent>(OnMoved);
        }

        private string GetTitle(DialogueNodeData d, string charName) => d.Type switch
        {
            DialogueNodeType.Start => "Inicio",
            DialogueNodeType.Dialogue => string.IsNullOrEmpty(charName) ? "Diálogo" : charName,
            DialogueNodeType.Choice => "Elección",
            DialogueNodeType.Condition => "Condición",
            DialogueNodeType.Event => "Evento",
            DialogueNodeType.End => "Fin de rama",
            _ => "Nodo"
        };

        private void BuildPorts(DialogueNodeType type)
        {
            //Todos tienen nodo de entrada excepto el nodo start
            if (type != DialogueNodeType.Start)
            {
                InPort = Port.Create<Edge>(Orientation.Vertical, Direction.Input,
                                           Port.Capacity.Multi, typeof(bool));
                InPort.portName = "";
                inputContainer.Add(InPort);
            }

            //Todos tienen nodo de salida excepto en nodo end
            if (type != DialogueNodeType.End)
            {
                OutPort = Port.Create<Edge>(
                    Orientation.Vertical, Direction.Output,
                    Port.Capacity.Multi, typeof(bool));
                OutPort.portName = "";
                outputContainer.Add(OutPort);
            }
        }

        private void BuildBody(DialogueNodeData d, string charName)
        {
            var ext = extensionContainer;

            switch (d.Type)
            {
                case DialogueNodeType.Dialogue:
                    if (!string.IsNullOrEmpty(d.DialogueText))
                        ext.Add(MakeLabel(
                            d.DialogueText.Length > 80
                                ? d.DialogueText.Substring(0, 77) + "..."
                                : d.DialogueText));
                    break;

                case DialogueNodeType.Choice:
                    for (int i = 0; i < Mathf.Min(d.Choices.Count, 3); i++)
                        ext.Add(MakeLabel($"  {i + 1}. {d.Choices[i].Text}"));
                    if (d.Choices.Count > 3)
                        ext.Add(MakeLabel($"  +{d.Choices.Count - 3} más..."));
                    break;

                case DialogueNodeType.Condition:
                    if (!string.IsNullOrEmpty(d.ConditionVar))
                        ext.Add(MakeTag($"{d.ConditionVar} {d.ConditionOp} {d.ConditionValue}"));
                    break;

                case DialogueNodeType.Event:
                    if (!string.IsNullOrEmpty(d.EventKey))
                        ext.Add(MakeTag(d.EventKey));
                    break;

                case DialogueNodeType.End:
                    if (!string.IsNullOrEmpty(d.EndDescription))
                        ext.Add(MakeLabel(d.EndDescription));
                    break;
            }

            RefreshExpandedState();
        }


        public void RefreshBody(string characterName = "")
        {
            extensionContainer.Clear();
            BuildBody(NodeData, characterName);
            title = GetTitle(NodeData, characterName);
        }

        private Label MakeLabel(string text)
        {
            var l = new Label(text);
            l.style.fontSize = 10;
            l.style.color = new Color(0.75f, 0.75f, 0.75f);
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.paddingLeft = l.style.paddingRight = 6;
            l.style.paddingBottom = 4;
            return l;
        }

        private VisualElement MakeTag(string text)
        {
            var tag = new Label(text);
            tag.style.fontSize = 9;
            tag.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            tag.style.color = new Color(0.65f, 0.65f, 0.65f);
            tag.style.borderLeftWidth = tag.style.borderRightWidth =
            tag.style.borderTopWidth = tag.style.borderBottomWidth = 0.5f;
            tag.style.borderLeftColor = tag.style.borderRightColor =
            tag.style.borderTopColor = tag.style.borderBottomColor =
                new Color(0.35f, 0.35f, 0.35f);
            tag.style.borderTopLeftRadius =
            tag.style.borderTopRightRadius =
            tag.style.borderBottomLeftRadius =
            tag.style.borderBottomRightRadius = 3;
            tag.style.paddingLeft = tag.style.paddingRight = 5;
            tag.style.marginLeft = 6;
            tag.style.marginBottom = 4;
            return tag;
        }

        private void ApplyStyle(DialogueNodeType type)
        {
            titleContainer.style.backgroundColor = GetHeadColor(type);
        }

        private void OnMoved(GeometryChangedEvent e)
        {
            var pos = GetPosition();
            _window?.OnNodeMoved(NodeData.Id, pos.x, pos.y);
        }

        public override void OnSelected()
        {
            base.OnSelected();
            _window?.OnNodeSelected(this);
        }
    }



    // -- Graph View del Arbol de Dialogos -- //
    public class DialogueGraphView : GraphView
    {
        private DialogueData _data;
        private DialogueGraphWindow _window;
        private Dictionary<string, DialogueNodeView> _nodeViews = new();

        public DialogueGraphView(DialogueData data, DialogueGraphWindow window)
        {
            _data = data;
            _window = window;

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            style.flexGrow = 1;
            style.backgroundColor = new Color(0.10f, 0.10f, 0.10f);

            graphViewChanged += OnGraphChanged;
        }

        // -- Carga el grafo desde DialogueData -- //
        public void LoadGraph()
        {
            // Apagamos los eventos temporalmente para no borrar datos del ScriptableObject al limpiar
            graphViewChanged -= OnGraphChanged;

            _nodeViews.Clear();
            var elementsToRemove = graphElements.ToList().Where(e => e is Node || e is Edge).ToList();

            // Usamos RemoveElement nativo en lugar de DeleteElements 
            foreach (var el in elementsToRemove)
            {
                RemoveElement(el);
            }

            foreach (var nodeData in _data.Nodes)
                InternalAddNode(nodeData);

            foreach (var edgeData in _data.Edges)
                InteralAddEdge(edgeData);

            // Volvemos a encender los eventos
            graphViewChanged += OnGraphChanged;
        }

        // -- API publica para la ventana -- //
        public DialogueNodeView AddNodeView(DialogueNodeData data) => InternalAddNode(data);

        public void RemoveNodeView(DialogueNodeView view)
        {
            if (view == null) return;
            _nodeViews.Remove(view.NodeData.Id);
            RemoveElement(view);
        }

        public void RefreshNodeView(string nodeId)
        {
            if (!_nodeViews.TryGetValue(nodeId, out var view)) return;
            string charName = _window.GetCharacterName(view.NodeData.CharacterGUID);
            view.RefreshBody(charName);
        }

        // -- Creacion de nodos y edges -- //
        private DialogueNodeView InternalAddNode(DialogueNodeData data)
        {
            string charName = _window.GetCharacterName(data.CharacterGUID);
            var view = new DialogueNodeView(data, _window, charName);
            AddElement(view);
            _nodeViews[data.Id] = view;
            return view;
        }

        private void InteralAddEdge(DialogueEdgeData edgeData)
        {
            if (!_nodeViews.TryGetValue(edgeData.SourceNodeId, out var src)) return;
            if (!_nodeViews.TryGetValue(edgeData.TargetNodeId, out var tgt)) return;
            if (src.OutPort == null || tgt.InPort == null) return;

            var edge = new Edge
            {
                output = src.OutPort,
                input = tgt.InPort
            };
            edge.output.Connect(edge);
            edge.input.Connect(edge);
            edge.userData = edgeData.Id;
            AddElement(edge);
        }

        // -- Callback de Cambios en el grafo -- //
        private GraphViewChange OnGraphChanged(GraphViewChange change)
        {
            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate)
                {
                    if (edge.output?.node is DialogueNodeView srcView &&
                        edge.input?.node is DialogueNodeView tgtView)
                    {
                        var edgeData = new DialogueEdgeData
                        {
                            Id = Guid.NewGuid().ToString(),
                            SourceNodeId = srcView.NodeData.Id,
                            TargetNodeId = tgtView.NodeData.Id
                        };
                        edge.userData = edgeData.Id;
                        _window?.OnEdgeCreated(edgeData);
                    }
                }
            }

            if (change.elementsToRemove != null)
            {
                foreach (var el in change.elementsToRemove)
                {
                    if (el is Edge edge && edge.userData is string edgeId)
                        _window?.OnEdgeDeleted(edgeId);

                    if (el is DialogueNodeView nodeView)
                        _window?.OnNodeDeleted(nodeView.NodeData.Id);
                }
            }

            return change;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
            => ports
                .Where(p => p.direction != startPort.direction && p.node != startPort.node)
                .ToList();
    }

}

