using UnityEngine;
using System;
using System.Collections.Generic;

namespace TFGToolkit
{
    // -- Tipos de relacion entre nodos --
    public enum RelationType
    {
        Activa,
        Requiere,
        Cancela,
        Modifica
    }

    // -- Datos de un nodo en el grafo
    [Serializable]
    public class GraphNodeData
    {
        public string Id         = ""; //id unico del nodo del grafo
        public string MechanicId = ""; //id de la mecanica vinculada
        public float PosX        = 0f;
        public float PosY        = 0f;
    }

    // -- Datos de una conexion entre dos nodos
    [Serializable]
    public class GraphEdgeData
    {
        public string Id = "";
        public string SourceNodeId = "";
        public string TargetNodeId = "";
        public RelationType Relation = RelationType.Activa;
        public string Label = ""; // etiqueta opcional
    }

    // -- ScriptableObject Principal --
    [CreateAssetMenu(fileName = "Grafo", menuName = "TFG Toolkit/Grafo de Mecánicas")]
    public class MechanicGraphData : ScriptableObject
    {
        [Header("Identificacion")]
        public string GraphName = "Nuevo Grafo";

        [TextArea(2, 3)]
        public string Description = "";

        [Header("Datos del grafo")]
        public List<GraphNodeData> Nodes = new List<GraphNodeData>();
        public List<GraphEdgeData> Edges = new List<GraphEdgeData>();

        [Header("Resultado del ultimo analisis IA")]
        [TextArea(4, 10)]
        public string LastAIAnalysis = "";

        // -- Helpers --
        public GraphNodeData GetNode(string id) => Nodes.Find(n => n.Id == id);

        public GraphNodeData GetNodeByMechanicId(string mechanicId) => Nodes.Find(n => n.MechanicId == mechanicId);

        public List<GraphEdgeData> GetEdgesFrom(string nodeId) => Edges.FindAll(e => e.SourceNodeId == nodeId);

        public List<GraphEdgeData> GetEdgesTo(string nodeId) => Edges.FindAll(e => e.TargetNodeId == nodeId);

        public bool HasConnections(string nodeId) => Edges.Exists(e => e.SourceNodeId == nodeId || e.TargetNodeId == nodeId);
    }
}
