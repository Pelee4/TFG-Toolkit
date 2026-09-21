using UnityEngine;

namespace TFGToolkit
{

    public enum MechanicStatus
    {
        Idea,
        EnDesarrollo,
        Implementada
    }

    //Crea el nuevo menu
    [CreateAssetMenu(fileName = "NuevaMecanica", menuName = "TFG Toolkit/Ficha de Mecánica")]
public class MechanicData : ScriptableObject
{
        [Header("Identificación")]
        public string mechanicName = "Nueva Mecánica";

        [TextArea(2, 4)]
        public string description = "";

        [Header("Definición")]
        public string input = ""; //Input o trigger necesario para que ocurra
        public string effect = ""; //Efecto de dicha mecanica
        public float duration = 0f; //Duracion del efecto, mecanica, etc.

        [Header("Estado")]
        public MechanicStatus status = MechanicStatus.Idea;

        [Header("Script generado")]
        [TextArea(4, 10)]
        public string generatedScript = "";
}

}
