using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.GraphView.GraphView;


namespace TFGToolkit
{
    // -- Nodo del Grafo --
    public class MechanicNodeView : Node
    {
        //Nodo, mecanica asociada, y puertos de entrada y salida de relaciones
        public GraphNodeData NodeData { get; private set; }
        public MechanicData BoundMechanic { get; private set; }
        public Port InputPort { get; private set; }
        public Port OutputPort{ get; private set; }

        private MechanicGraphWindow _window;

        public MechanicNodeView(
            GraphNodeData data,
            MechanicData mechanic,
            MechanicGraphWindow window)
        {
            NodeData = data;
            BoundMechanic = mechanic;
            _window = window;

            // Titulo y posicion del nodo
            title = mechanic.mechanicName;
            SetPosition(new Rect(data.PosX, data.PosY, 160, 100));

            BuildPorts();
            BuildBody();
            ApplyStatusColor();

            //Guardar posicion si muevo el nodo
            RegisterCallback<GeometryChangedEvent>(OnMoved);
        }

        private void BuildPorts()
        {
            //Puerto de entrada
            InputPort = Port.Create<Edge>(
                Orientation.Horizontal,
                Direction.Input,
                Port.Capacity.Multi,
                typeof(bool));
            InputPort.portName = "";
            inputContainer.Add(InputPort);

            //Puerto de salida
            OutputPort = Port.Create<Edge>(
                Orientation.Horizontal,
                Direction.Output,
                Port.Capacity.Multi,
                typeof(bool));
            OutputPort.portName = "";
            inputContainer.Add(OutputPort);
        }

        private void BuildBody()
        {
            //Descripcion
            if (!string.IsNullOrEmpty(BoundMechanic.description))
            {
                //TODO: Cambiar datos para ajustarlo como quiero
                var desc = new Label(BoundMechanic.description.Length > 60
                    ? BoundMechanic.description.Substring(0, 57) + "..."
                    : BoundMechanic.description);
                desc.style.fontSize = 10;
                desc.style.color = new Color(0.7f, 0.7f, 0.7f);
                desc.style.whiteSpace = WhiteSpace.Normal;
                desc.style.paddingLeft = 4;
                desc.style.paddingRight = 4;
                desc.style.paddingBottom = 4;
                extensionContainer.Add(desc);
            }

            //Tags (input, duracion, etc)
            var tagRow = new VisualElement();
            tagRow.style.flexDirection = FlexDirection.Row;
            tagRow.style.flexWrap = Wrap.Wrap;
            tagRow.style.paddingLeft = 4;
            tagRow.style.paddingBottom = 4;

            if (!string.IsNullOrEmpty(BoundMechanic.input))
                tagRow.Add(MakeTag(BoundMechanic.input));
            if (BoundMechanic.duration > 0)
                tagRow.Add(MakeTag($"{BoundMechanic.duration}s"));

            extensionContainer.Add(tagRow);
            RefreshExpandedState();
        }

        private VisualElement MakeTag(string text)
        {
            //TODO: Cambiar datos para ajustarlo como quiero
            var tag = new Label(text);
            tag.style.fontSize = 9;
            tag.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            tag.style.color = new Color(0.7f, 0.7f, 0.7f);
            tag.style.borderLeftWidth = tag.style.borderRightWidth =
            tag.style.borderTopWidth = tag.style.borderBottomWidth = 0.5f;
            tag.style.borderLeftColor = tag.style.borderRightColor =
            tag.style.borderTopColor = tag.style.borderBottomColor =
                new Color(0.35f, 0.35f, 0.35f);
            tag.style.borderTopLeftRadius =
            tag.style.borderTopRightRadius =
            tag.style.borderBottomLeftRadius =
            tag.style.borderBottomRightRadius = 3;
            tag.style.paddingLeft = tag.style.paddingRight = 4;
            tag.style.marginRight = 3;
            return tag;
        }

        private void ApplyStatusColor()
        {
            Color headerColor = BoundMechanic.status switch
            {
                //TODO: Cambiar datos para ajustarlo como quiero
                MechanicStatus.Implementada => new Color(0.11f, 0.20f, 0.12f),
                MechanicStatus.EnDesarrollo => new Color(0.24f, 0.17f, 0.04f),
                _ => new Color(0.18f, 0.11f, 0.30f)
            };

            //Colorear el titleContainer
            titleContainer.style.backgroundColor = headerColor;
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



    // -- Aristas con tipos de relacion
    public class MechanicEdgeView : Edge
    {
        public GraphEdgeData EdgeData { get; private set;  }

        public MechanicEdgeView(GraphEdgeData data) : base()
        {
            EdgeData = data;
            ApplyRelationStyle(data.Relation);
        }

        private void ApplyRelationStyle(RelationType r)
        {
            Color EdgeColor = r switch
            {
                RelationType.Activa => new Color(0.29f, 0.50f, 0.76f),
                RelationType.Requiere => new Color(0.22f, 0.63f, 0.41f),
                RelationType.Cancela => new Color(0.90f, 0.24f, 0.24f),
                RelationType.Modifica => new Color(0.84f, 0.62f, 0.17f),
                _ => Color.white
            };

            edgeControl.inputColor = EdgeColor;
            edgeControl.outputColor = EdgeColor;
        }
    }



    // -- Graph View Principal --
    public class MechanicGraphView : GraphView
    {
        private MechanicGraphData _graphData;
        private MechanicGraphWindow _window;

        //Mapa nodeId -> vista
        private Dictionary<string, MechanicNodeView> _nodeViews = new();

        public RelationType ActiveRelation = RelationType.Activa;

        public MechanicGraphView(MechanicGraphData data, MechanicGraphWindow window)
        {
            _graphData = data;
            _window = window;

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // Grid de fondo
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            // Estilo base
            style.flexGrow = 1;
            StyleBackground();

            // Callback cuando se crea una arista arrastrando puertos
            graphViewChanged += OnGraphViewChanged;
        }

        private void StyleBackground()
        {
            style.backgroundColor = new Color(0.10f, 0.10f, 0.10f); //Gris flojo
        }

        // Cargamos el grafo desde _graphData
        public void LoadGraph()
        {
            _nodeViews.Clear();
            var elementsToRemove = graphElements.ToList();
            DeleteElements(elementsToRemove);

            //Crear nodos
            foreach (var nodeData in _graphData.Nodes)
            {
                string path = AssetDatabase.GUIDToAssetPath(nodeData.MechanicId);
                var mechanic = AssetDatabase.LoadAssetAtPath<MechanicData>(path);
                if (mechanic == null) continue;
                InternalAddNode(nodeData, mechanic);
            }

            // Crear aristas
            foreach (var edgeData in _graphData.Edges)
                InternalAddEdge(edgeData);
        }

        //Metodos para el graphWindow
        public void AddNodeView(GraphNodeData data, MechanicData mechanic)
        {
            InternalAddNode(data, mechanic);
        }

        public void RemoveNodeView(MechanicNodeView view)
        {
            if (view == null) return;
            _nodeViews.Remove(view.NodeData.Id);
            RemoveElement(view);
        }

        //Creacion de nodos y de aristas
        private MechanicNodeView InternalAddNode(GraphNodeData data, MechanicData mechanic)
        {
            var view = new MechanicNodeView(data, mechanic, _window);
            AddElement(view);
            _nodeViews[data.Id] = view;
            return view;
        }

        private void InternalAddEdge(GraphEdgeData edgeData)
        {
            if (!_nodeViews.TryGetValue(edgeData.SourceNodeId, out var srcView)) return;
            if (!_nodeViews.TryGetValue(edgeData.TargetNodeId, out var tgtView)) return;

            var edge = new MechanicEdgeView(edgeData);
            edge.output = srcView.OutputPort;
            edge.input = tgtView.InputPort;
            srcView.OutputPort.Connect(edge);
            tgtView.InputPort.Connect(edge);
            AddElement(edge);
        }

        //Callback -- Manejo de creacion y destruccion de nodos
        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate)
                {
                    if (edge.output?.node is MechanicNodeView srcNode && edge.input?.node is MechanicNodeView tgtNode)
                    {
                        var edgeData = new GraphEdgeData
                        {
                            Id = Guid.NewGuid().ToString(),
                            SourceNodeId = srcNode.NodeData.Id,
                            TargetNodeId = tgtNode.NodeData.Id,
                            Relation = ActiveRelation
                        };

                        //Reemplazsar la Edge Generica por la creada
                        var mecEdge = new MechanicEdgeView(edgeData);
                        mecEdge.output = edge.output;
                        mecEdge.input = edge.input;
                        edge.output.Connect(mecEdge);
                        edge.input.Connect(mecEdge);
                        AddElement(mecEdge);

                        _window?.OnEdgeCreated(edgeData);

                        // Devolvemos lista vacía para que GraphView no añada la Edge genérica
                        change.edgesToCreate.Clear();
                        change.edgesToCreate.Add(mecEdge);
                        break;
                    }
                }
            }

            // Elementos eliminados
            if (change.elementsToRemove != null)
            {
                foreach (var el in change.elementsToRemove)
                {
                    if (el is MechanicEdgeView edgeView)
                    {
                        _window?.OnEdgeDeleted(edgeView.EdgeData.Id);
                    }
                }
            }

            return change;
        }

        // GraphView requiere este metodo para saber que puertos son compatibles
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports
                .Where(p => p.direction != startPort.direction
                         && p.node != startPort.node)
                .ToList();
        }

    }


}

