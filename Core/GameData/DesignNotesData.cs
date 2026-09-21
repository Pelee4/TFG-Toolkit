using System;
using System.Collections.Generic;
using UnityEngine;


namespace TFGToolkit
{

    // -------------------------------------------------------
    // Modelos de datos
    // -------------------------------------------------------

    public enum NoteType
    {
        Nota,           // Anotación general
        Decision,       // Decisión de diseño tomada
        Problema,       // Problema detectado
        Idea            // Idea para explorar
    }


    [Serializable]
    public class DesignNote
    {
        public string Id = "";
        public NoteType Type = NoteType.Nota;
        public string Title = "";
        public string Content = "";
        public string Author = "";
        public string Date = "";
        public string LinkedAsset = ""; //ruta del asset vinculado (opcional)
        public bool Resolved = false;
    }

    [Serializable]
    public class DesignNotesData
    {
        public List<DesignNote> Notes = new List<DesignNote>();
    }

}
