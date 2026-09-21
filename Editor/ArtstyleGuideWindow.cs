using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TFGToolkit
{
    public class ArtStyleGuideWindow : EditorWindow
    {
        private ArtStyleGuideData _guideData;
        private Vector2 _scrollPos;

        // Tabs
        private int _selectedTab = 0; // 0=Overview, 1=Colores, 2=Tipografías, 3=Detalles

        private const float PADDING = 12f;
        private const float SECTION_PADDING = 8f;

        public static void ShowWindow()
        {
            var w = GetWindow<ArtStyleGuideWindow>("Guía de Estilo Visual");
            w.minSize = new Vector2(600, 500);
        }

        private void OnGUI()
        {
            DrawLoadGuide();

            if (_guideData == null)
            {
                EditorGUILayout.HelpBox(
                    "No se encontró guía de estilo.\n\n" +
                    "Crea una nueva desde:\n" +
                    "Assets > Create > TFG Toolkit > Guía de Estilo Visual",
                    MessageType.Info);
                return;
            }

            DrawTabs();
            DrawContent();
        }

        // =========================================================
        // CARGA DE GUÍA
        // =========================================================
        private void DrawLoadGuide()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("Guía de Estilo Visual", EditorStyles.boldLabel,
                GUILayout.Width(150));

            EditorGUILayout.LabelField(_guideData != null ? _guideData.GameTitle : "Sin guía",
                EditorStyles.miniLabel, GUILayout.Width(200));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Cargar guía", EditorStyles.toolbarButton, GUILayout.Width(80)))
                OpenGuide();

            if (_guideData != null && GUILayout.Button("Editar", EditorStyles.toolbarButton,
                GUILayout.Width(50)))
            {
                Selection.activeObject = _guideData;
                EditorGUIUtility.PingObject(_guideData);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void OpenGuide()
        {
            // Buscar guía existente
            var guids = AssetDatabase.FindAssets("t:ArtStyleGuideData");
            if (guids.Length > 0)
            {
                _guideData = AssetDatabase.LoadAssetAtPath<ArtStyleGuideData>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));
                Repaint();
                return;
            }

            // Si no existe, crear una nueva
            bool create = EditorUtility.DisplayDialog(
                "Crear guía",
                "No se encontró guía de estilo.\n¿Crear una nueva?",
                "Crear", "Cancelar");

            if (create)
            {
                string folder = "Assets/TFGToolkit/Data/ArtStyle";
                if (!AssetDatabase.IsValidFolder(folder))
                    AssetDatabase.CreateFolder("Assets/TFGToolkit/Data", "ArtStyle");

                var newGuide = ScriptableObject.CreateInstance<ArtStyleGuideData>();
                string path = $"{folder}/GuiaDeEstilo.asset";
                AssetDatabase.CreateAsset(newGuide, path);
                AssetDatabase.SaveAssets();

                _guideData = newGuide;
                Selection.activeObject = newGuide;
                Repaint();
            }
        }

        // =========================================================
        // TABS
        // =========================================================
        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            string[] tabs = { "📋 General", "🎨 Colores", "✍️  Tipografías", "📝 Detalles" };
            _selectedTab = GUILayout.Toolbar(_selectedTab, tabs);
            EditorGUILayout.EndHorizontal();
        }

        // =========================================================
        // CONTENIDO
        // =========================================================
        private void DrawContent()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            switch (_selectedTab)
            {
                case 0: DrawGeneralTab(); break;
                case 1: DrawColorsTab(); break;
                case 2: DrawTypographyTab(); break;
                case 3: DrawDetailsTab(); break;
            }

            EditorGUILayout.EndScrollView();
        }

        // =========================================================
        // TAB 0: GENERAL
        // =========================================================
        private void DrawGeneralTab()
        {
            DrawSection("📌 Información General", () =>
            {
                GUILayout.Label($"Juego: {_guideData.GameTitle}", EditorStyles.boldLabel);
                GUILayout.Label($"Estilo: {_guideData.PrimaryStyle}", EditorStyles.miniLabel);
            });

            DrawSection("🎭 Dirección Artística", () =>
            {
                EditorGUILayout.LabelField(_guideData.ArtisticDirectionStatement,
                    new GUIStyle(EditorStyles.label) { wordWrap = true });
            });

            DrawSection("📚 Inspiraciones", () =>
            {
                EditorGUILayout.LabelField(_guideData.VisualReferences,
                    new GUIStyle(EditorStyles.label) { wordWrap = true });
            });

            DrawSection("💭 Tono y Sentimiento", () =>
            {
                EditorGUILayout.LabelField(_guideData.Mood,
                    new GUIStyle(EditorStyles.label) { wordWrap = true });
            });

            // Preview de colores pequeño
            DrawSection("🎨 Paleta (preview)", () =>
            {
                if (_guideData.ColorPalette.Count > 0)
                    DrawColorGridSmall(_guideData.ColorPalette, 5);
                else
                    GUILayout.Label("Sin colores definidos.", EditorStyles.centeredGreyMiniLabel);
            });

            // Preview de tipografías pequeño
            DrawSection("✍️  Tipografías (preview)", () =>
            {
                if (_guideData.Typographies.Count > 0)
                {
                    foreach (var typ in _guideData.Typographies.Take(2))
                        DrawTypographyPreview(typ);
                    if (_guideData.Typographies.Count > 2)
                        GUILayout.Label($"+{_guideData.Typographies.Count - 2} más...",
                            EditorStyles.centeredGreyMiniLabel);
                }
                else
                    GUILayout.Label("Sin tipografías definidas.", EditorStyles.centeredGreyMiniLabel);
            });
        }

        // =========================================================
        // TAB 1: COLORES
        // =========================================================
        private void DrawColorsTab()
        {
            DrawSection("🎨 Paleta de colores", () =>
            {
                if (_guideData.ColorPalette.Count == 0)
                {
                    GUILayout.Label("Sin colores definidos.",
                        EditorStyles.centeredGreyMiniLabel);
                    GUILayout.Label("Abre el asset de guía de estilo en el Inspector " +
                        "para añadir colores.",
                        new GUIStyle(EditorStyles.miniLabel) { wordWrap = true });
                    return;
                }

                // Grid de colores
                DrawColorGrid(_guideData.ColorPalette);
            });
        }

        // =========================================================
        // TAB 2: TIPOGRAFÍAS
        // =========================================================
        private void DrawTypographyTab()
        {
            DrawSection("✍️  Tipografías del juego", () =>
            {
                if (_guideData.Typographies.Count == 0)
                {
                    GUILayout.Label("Sin tipografías definidas.",
                        EditorStyles.centeredGreyMiniLabel);
                    GUILayout.Label("Abre el asset de guía de estilo en el Inspector " +
                        "para añadir fuentes.",
                        new GUIStyle(EditorStyles.miniLabel) { wordWrap = true });
                    return;
                }

                foreach (var typ in _guideData.Typographies)
                    DrawTypographyCard(typ);
            });
        }

        // =========================================================
        // TAB 3: DETALLES
        // =========================================================
        private void DrawDetailsTab()
        {
            DrawSection("📐 Descripción del estilo", () =>
            {
                EditorGUILayout.LabelField(_guideData.StyleDescription,
                    new GUIStyle(EditorStyles.label) { wordWrap = true });
            });

            DrawSection("🎮 Guía de UI", () =>
            {
                EditorGUILayout.LabelField(_guideData.UIGuidelines,
                    new GUIStyle(EditorStyles.label) { wordWrap = true });
            });

            DrawSection("⚙️  Restricciones técnicas", () =>
            {
                EditorGUILayout.LabelField(_guideData.TechnicalConstraints,
                    new GUIStyle(EditorStyles.label) { wordWrap = true });
            });

            DrawSection("📊 Resumen", () =>
            {
                DrawInfoRow("Colores definidos:", $"{_guideData.ColorPalette.Count}");
                DrawInfoRow("Tipografías:", $"{_guideData.Typographies.Count}");
                DrawInfoRow("Estilo principal:", _guideData.PrimaryStyle.ToString());
            });
        }

        // =========================================================
        // COMPONENTES VISUALES
        // =========================================================
        private void DrawSection(string title, System.Action content)
        {
            GUILayout.Space(SECTION_PADDING);

            // Fondo sutil
            Rect sectionRect = EditorGUILayout.GetControlRect(GUILayout.Height(0));
            sectionRect.height = 1; // se ajustará con el contenido

            GUILayout.Label(title, EditorStyles.boldLabel);
            GUILayout.Space(4);

            content();

            GUILayout.Space(PADDING);
        }

        private void DrawColorGrid(List<ColorPaletteEntry> colors)
        {
            int cols = 4;
            for (int i = 0; i < colors.Count; i += cols)
            {
                EditorGUILayout.BeginHorizontal();
                for (int j = 0; j < cols; j++)
                {
                    if (i + j >= colors.Count) break;
                    DrawColorItem(colors[i + j]);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawColorGridSmall(List<ColorPaletteEntry> colors, int cols)
        {
            for (int i = 0; i < colors.Count; i += cols)
            {
                EditorGUILayout.BeginHorizontal();
                for (int j = 0; j < cols; j++)
                {
                    if (i + j >= colors.Count) break;
                    DrawColorItemSmall(colors[i + j]);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawColorItem(ColorPaletteEntry color)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(140));

            // Forzar Alpha a 1 por si en el Inspector está a 0 (Totalmente transparente)
            Color displayColor = color.Value;
            displayColor.a = 1f;

            // Cuadro de color grande
            Rect colorRect = GUILayoutUtility.GetRect(128, 80);
            EditorGUI.DrawRect(colorRect, displayColor);

            // Borde sutil
            Handles.color = new Color(1, 1, 1, 0.2f);
            Handles.DrawSolidRectangleWithOutline(colorRect, Color.clear, Color.white);

            GUILayout.Space(4);

            // Nombre y hex
            GUILayout.Label(color.Name, EditorStyles.boldLabel);
            string hexColor = ColorUtility.ToHtmlStringRGB(displayColor);
            GUILayout.Label($"#{hexColor}", EditorStyles.miniLabel);

            // Uso
            if (!string.IsNullOrEmpty(color.Usage))
                GUILayout.Label(color.Usage,
                    new GUIStyle(EditorStyles.miniLabel)
                    { wordWrap = true, normal = { textColor = Color.gray } });

            EditorGUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private void DrawColorItemSmall(ColorPaletteEntry color)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(70));

            Color displayColor = color.Value;
            displayColor.a = 1f; // Forzar Alpha a 1

            Rect colorRect = GUILayoutUtility.GetRect(60, 40);
            EditorGUI.DrawRect(colorRect, displayColor);
            Handles.color = new Color(1, 1, 1, 0.2f);
            Handles.DrawSolidRectangleWithOutline(colorRect, Color.clear, Color.white);

            GUILayout.Space(2);
            GUILayout.Label(color.Name, EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
            GUILayout.Space(4);
        }

        private void DrawTypographyPreview(TypographyEntry typ)
        {
            EditorGUILayout.BeginVertical(
                new GUIStyle(GUI.skin.box) { padding = new RectOffset(8, 8, 6, 6) });

            // Parse seguro del FontStyle
            FontStyle parsedStyle = FontStyle.Normal;
            if (!string.IsNullOrEmpty(typ.Style))
                System.Enum.TryParse(typ.Style, true, out parsedStyle);

            Color textColor = typ.Color;
            textColor.a = 1f; // Evitar que el texto sea invisible

            // Visualizar con aproximación del tamaño
            GUIStyle previewStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = Mathf.Max(11, typ.Size / 3),
                fontStyle = parsedStyle,
                normal = { textColor = textColor },
                hover = { textColor = textColor } // Prevenir salto de color al poner el ratón
            };

            GUILayout.Label("The quick brown fox", previewStyle);

            GUILayout.Space(4);
            GUILayout.Label($"{typ.Name} — {typ.Size}pt {typ.Style}",
                EditorStyles.miniLabel);
            if (!string.IsNullOrEmpty(typ.Usage))
                GUILayout.Label(typ.Usage,
                    new GUIStyle(EditorStyles.miniLabel)
                    { normal = { textColor = Color.gray } });

            EditorGUILayout.EndVertical();
        }

        private void DrawTypographyCard(TypographyEntry typ)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            // Información
            DrawInfoRow("Nombre:", typ.Name);
            DrawInfoRow("Tamaño:", $"{typ.Size}pt");
            DrawInfoRow("Estilo:", typ.Style);
            if (typ.Font != null)
                DrawInfoRow("Fuente:", typ.Font.name);

            Color displayColor = typ.Color;
            displayColor.a = 1f; // Evitar invisibilidad

            // Preview del color
            Rect colorPreview = GUILayoutUtility.GetRect(200, 20);
            EditorGUI.DrawRect(colorPreview, displayColor);
            Handles.color = new Color(1, 1, 1, 0.2f);
            Handles.DrawSolidRectangleWithOutline(colorPreview, Color.clear, Color.white);
            GUI.Label(colorPreview, $"  {ColorUtility.ToHtmlStringRGB(displayColor)}",
                EditorStyles.whiteMiniLabel);

            if (!string.IsNullOrEmpty(typ.Usage))
            {
                GUILayout.Space(4);
                GUILayout.Label($"Uso: {typ.Usage}",
                    new GUIStyle(EditorStyles.miniLabel) { wordWrap = true });
            }

            // Preview de texto
            GUILayout.Space(6);
            GUILayout.Label("Preview del texto:",
                EditorStyles.boldLabel);

            // Parse seguro del FontStyle
            FontStyle parsedStyle = FontStyle.Normal;
            if (!string.IsNullOrEmpty(typ.Style))
                System.Enum.TryParse(typ.Style, true, out parsedStyle);

            GUIStyle preview = new GUIStyle(EditorStyles.label)
            {
                fontSize = Mathf.Clamp(typ.Size / 2, 11, 24),
                fontStyle = parsedStyle,
                normal = { textColor = displayColor },
                hover = { textColor = displayColor }
            };
            GUILayout.Label("Hola mundo - The Quick Brown Fox Jumps Over The Lazy Dog",
                preview);

            EditorGUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private void DrawInfoRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, EditorStyles.miniLabel, GUILayout.Width(100));
            GUILayout.Label(value, EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }
}