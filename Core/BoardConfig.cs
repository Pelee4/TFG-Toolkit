using System;
using System.Collections.Generic;

namespace TFGToolkit
{
    /// <summary>
    /// Define una columna del tablero Kanban.
    /// Se persiste en BoardConfig.json junto con BoardConfig.
    /// </summary>
    [Serializable]
    public class BoardColumn
    {
        public string Id = "";
        public string Label = "Nueva columna";
        public string ColorHex = "#FFFFFF";
        public bool Visible = true;

        /// Qué valor de MechanicStatus mapea a esta columna.
        /// -1 = columna libre (no vinculada a ningún estado).
        public int MechanicStatusValue = -1;
    }

    /// <summary>
    /// Configuración global del tablero del proyecto.
    /// Se persiste en Assets/TFGToolkit/Data/BoardConfig.json.
    /// </summary>
    [Serializable]
    public class BoardConfig
    {
        public bool ShowMechanics = true;
        public bool ShowCharacters = true;
        public List<BoardColumn> Columns = new List<BoardColumn>();
    }
}