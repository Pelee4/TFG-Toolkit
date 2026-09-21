using UnityEngine;
using System;
using System.Collections.Generic;

namespace TFGToolkit
{
    public enum PrototypePrefabCategory
    {
        Personajes,
        Entorno,
        Camara,
        UI,
        Otros    
    }

    // Componente base que se añade al prefab cuando se instancia
    [Serializable]
    public class BaseComponentEntry
    {
        public string ComponentTypeName = "";
        public string DisplayName = "";
    }

    [CreateAssetMenu(fileName = "NuevoPrefab", menuName = "TFG Toolkit/Prefab de Prototipo")]
    public class PrototypePrefabData : ScriptableObject
    {
        [Header("Identificación")]
        public string PrefabName = "Nuevo prefab";
        public PrototypePrefabCategory Category = PrototypePrefabCategory.Otros;

        [TextArea(2, 3)]
        public string Description = "";

        [Header("Prefab base (opcional)")]
        [Tooltip("Si asignas un prefab de Unity aquí se instancia directamente. " +
                 "Si está vacío se crea un GameObject vacío con los componentes definidos abajo.")]
        public GameObject BasePrefab;

        [Header("Forma primitiva (si no hay prefab)")]
        public PrimitiveType PrimitiveShape = PrimitiveType.Capsule;
        public Color GizmoColor = new Color(0.4f, 0.7f, 1f, 1f);

        [Header("Componentes base")]
        public List<BaseComponentEntry> BaseComponents = new List<BaseComponentEntry>();

        [Header("Mecánicas vinculadas")]
        [Tooltip("Fichas de mecánicas cuyos scripts generados se ofrecen para asignar a este prefab.")]
        public List<MechanicData> LinkedMechanics = new List<MechanicData>();

        [Header("Scripts de mecánicas asignados")]
        [Tooltip("GUIDs de las mecánicas cuyos scripts ya están asignados por defecto.")]
        public List<string> AssignedMechanicGuids = new List<string>();
    }

}

