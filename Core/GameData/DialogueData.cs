using UnityEngine;
using System;
using System.Collections.Generic;


namespace TFGToolkit
{
    // -- Tipo de nodo del arbol
    public enum DialogueNodeType
    {
        Start,
        Dialogue,
        Choice,
        Condition,
        Event,
        End
    }


    // -- Opcion dentro del nodo de eleccion o dialogo
    [Serializable]
    public class DialogueChoice
    {
        public string Id = "";
        public string Text = "";
        public string TargetNodeId = "";
    }

    // -- Datos de un nodo del arbol --
    [Serializable]
    public class DialogueNodeData
    {
        public string Id = "";
        public DialogueNodeType Type = DialogueNodeType.Dialogue;
        public float PosX = 0f;
        public float PosY = 0f;

        //Dialogo
        public string CharacterGUID = "";
        public string DialogueText = "";

        //Lista de opciones
        public List<DialogueChoice> Choices = new List<DialogueChoice>();

        //Condicion
        public string ConditionVar = "";
        public string ConditionOp = ">";
        public float ConditionValue = 0f;

        //Evento
        public string EventKey = "";

        //Fin 
        public string EndDescription = "";

        //Notas internas del nodo
        public string DesignerNotes = "";
    }


    // -- Conexion etre dos nodos -- //
    [Serializable]
    public class DialogueEdgeData
    {
        public string Id = "";
        public string SourceNodeId = "";
        public string TargetNodeId = "";
        public string ChoiceId = "";
        public string Label = "";
    }

    // -- ScriptableObject de Arbol completo -- //
    [CreateAssetMenu(fileName = "NuevoArbol", menuName = "TFG Toolkit / arbol de Dialogos")]
    public class DialogueData : ScriptableObject
    {
        [Header("Identificación")]
        public string TreeName = "Nuevo árbol";

        [TextArea(2, 3)]
        public string Description = "";

        public string Scene = ""; // escena de Unity donde ocurre

        [Header("Nodos y conexiones")]
        public List<DialogueNodeData> Nodes = new List<DialogueNodeData>();
        public List<DialogueEdgeData> Edges = new List<DialogueEdgeData>();

        [Header("Análisis IA")]
        [TextArea(3, 8)]
        public string LastAIAnalysis = "";


        // -- Helpers -- //
        public DialogueNodeData GetNode(string id)
            => Nodes.Find(n => n.Id == id);

        public DialogueNodeData StartNode
            => Nodes.Find(n => n.Type == DialogueNodeType.Start);

        public List<DialogueEdgeData> GetEdgesFrom(string nodeId)
            => Edges.FindAll(e => e.SourceNodeId == nodeId);

        public List<DialogueEdgeData> GetEdgesTo(string nodeId)
            => Edges.FindAll(e => e.TargetNodeId == nodeId);

        public bool HasEdge(string sourceId, string targetId)
            => Edges.Exists(e => e.SourceNodeId == sourceId && e.TargetNodeId == targetId);
    }

}

