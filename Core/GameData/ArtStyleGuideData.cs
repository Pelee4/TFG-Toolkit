using UnityEngine;
using System;
using System.Collections.Generic;

namespace TFGToolkit
{
    //Elementos de color con nombre
    [Serializable]
    public class ColorPaletteEntry
    {
        public string Name = "Color principal";
        public Color Value = Color.white;
        public string Usage = "";
    }

    //Entrada de tipografia
    [Serializable]
    public class TypographyEntry
    {
        public string Name = "Titulo";
        public Font Font;
        public int Size = 36;
        public Color Color = Color.white;
        public string Style = "Bold";
        public string Usage = "";
    }

    public enum ArtisticStyle
    {
        PixelArt,
        Celshading,
        Realista,
        Cartoon,
        Minimalista,
        Abstract,
        Otro
    }

    /// <summary>
    /// Guía de estilo visual del juego.
    /// Define la identidad visual completa: colores, tipografías y dirección artística.
    /// Se exporta automáticamente al GDD.
    /// </summary>
    [CreateAssetMenu(fileName = "GuiaDeEstilo", menuName = "TFG Toolkit/Guía de Estilo Visual")]
    public class ArtStyleGuideData : ScriptableObject
    {
        [Header("Identificación")]
        public string GameTitle = "Mi Juego";

        [TextArea(3, 5)]
        public string ArtisticDirectionStatement =
            "Describe la dirección artística general del juego.\n" +
            "Ej: 'Juego 2D pixel art con una paleta cálida inspirada en 8-bit clásico.'";

        [Header("Estilo artístico principal")]
        public ArtisticStyle PrimaryStyle = ArtisticStyle.PixelArt;

        [TextArea(2, 4)]
        public string StyleDescription = "Detalla más sobre el estilo elegido.";

        [Header("Inspiraciones visuales")]
        [TextArea(2, 4)]
        public string VisualReferences =
            "Menciona referencias artísticas.\nEj: 'Zelda, Super Metroid, Hollow Knight'";

        [Header("Paleta de colores")]
        public List<ColorPaletteEntry> ColorPalette = new List<ColorPaletteEntry>();

        [Header("Tipografías")]
        public List<TypographyEntry> Typographies = new List<TypographyEntry>();

        [Header("Tono y sentimiento")]
        [TextArea(2, 4)]
        public string Mood =
            "Describe el sentimiento general que debe transmitir.\n" +
            "Ej: 'Aventura épica, misterio, nostalgia'";

        [Header("Guía de componentes visuales")]
        [TextArea(2, 4)]
        public string UIGuidelines =
            "Notas sobre botones, iconos, elementos de UI.\n" +
            "Ej: 'Bordes redondeados de 4px, sombras suaves'";

        [Header("Restricciones técnicas")]
        [TextArea(2, 4)]
        public string TechnicalConstraints =
            "Limitaciones de resolución, paleta limitada, etc.\n" +
            "Ej: 'Paleta de 16 colores máximo, resolución 320x180'";

        // -------------------------------------------------------
        // Helpers
        // -------------------------------------------------------
        public Color GetColorByName(string colorName)
        {
            var entry = ColorPalette.Find(c => c.Name.ToLower() == colorName.ToLower());
            return entry != null ? entry.Value : Color.white;
        }

        public TypographyEntry GetTypographyByName(string typeName)
        {
            return Typographies.Find(t => t.Name.ToLower() == typeName.ToLower());
        }
    }
}