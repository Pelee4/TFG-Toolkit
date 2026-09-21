using UnityEngine;
using System.Collections.Generic;


namespace TFGToolkit
{
    [CreateAssetMenu(fileName = "PrototypeLibrary", menuName = "TFG Toolkit/Biblioteca de Prototipos")]
    public class PrototypeLibraryConfig : ScriptableObject
    {
        [Tooltip("Lista ordenada de prefabs disponibles en la biblioteca.")]
        public List<PrototypePrefabData> Prefabs = new List<PrototypePrefabData>();

        // -- Helpers -- //
        public List<PrototypePrefabData> GetByCategory(PrototypePrefabCategory cat)
            => Prefabs.FindAll(p => p.Category == cat);

        public List<PrototypePrefabData> Search(string query)
        {
            if (string.IsNullOrEmpty(query)) return new List<PrototypePrefabData>(Prefabs);
            string q = query.ToLower();
            return Prefabs.FindAll(p =>
                p.PrefabName.ToLower().Contains(q) ||
                p.Description.ToLower().Contains(q));
        }
    }
}
