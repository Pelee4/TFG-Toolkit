using UnityEngine;

namespace TFGToolkit
{

    public enum CharacterRole
    {
        Protagonista,
        Enemigo,
        NPC,
        Jefe,
        Compañero,
        Vendedor
    }

    //Crea el nuevo menu
    [CreateAssetMenu(fileName = "NuevoPersonaje", menuName = "TFG Toolkit/Ficha de Personaje")]
    public class CharacterData : ScriptableObject
    {
        [Header("Identificación")]
        public string characterName = "Nuevo Personaje";
        public CharacterRole role = CharacterRole.Protagonista;

        [TextArea(2, 4)]
        public string description = "";

        [Header("Trasfondo")]
        [TextArea(3, 6)]
        public string backstory = "";
        public string motivation = "";  // ¿Qué quiere el personaje?

        [Header("Estadísticas base")]
        public int health = 100;
        public int attack = 10;
        public int defense = 10;
        public float speed = 5f;

        [Header("Habilidades")]
        [TextArea(2, 5)]
        public string abilities = ""; // Descripción libre de sus habilidades

        [Header("Mecánicas asociadas")]
        // Referencia directa a fichas de mecánicas que usa este personaje
        public MechanicData[] relatedMechanics;

        [Header("Notas de diseño")]
        [TextArea(2, 5)]
        public string designNotes = "";
    }

}
