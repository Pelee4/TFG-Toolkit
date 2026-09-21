using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TFGToolkit
{
    /// <summary>
    /// Genera un GDD en Markdown y HTML a partir de los ScriptableObjects del proyecto.
    /// Dos vías de generación:
    ///   - Export(config)            → plantilla determinista (recorre los assets)
    ///   - SaveAIGeneratedGDD(...)   → guarda el texto que produce la IA
    /// </summary>
    public static class GDDExporter
    {
        // -------------------------------------------------------
        // Configuración de la exportación
        // -------------------------------------------------------
        public class ExportConfig
        {
            public string GameTitle = "Mi juego";
            public string GameDescription = "";
            public string Author = "";
            public string Version = "0.1";
            public string OutputFolder = "Assets/TFGToolkit/GDD";
            public bool ExportMarkdown = true;
            public bool ExportHTML = true;

            // Secciones a incluir
            public bool IncludeMechanics = true;
            public bool IncludeCharacters = true;
            public bool IncludeDialogues = true;
            public bool IncludeArtStyle = true;
            public bool IncludeMechanicGraph = true;
            public bool IncludeNotes = true;
        }

        // -------------------------------------------------------
        // Punto de exportación por plantilla
        // -------------------------------------------------------
        public static void Export(ExportConfig config)
        {
            string fullOutputPath = Path.Combine(Application.dataPath, "..", config.OutputFolder);
            Directory.CreateDirectory(fullOutputPath);

            string markdown = BuildMarkdown(config);

            if (config.ExportMarkdown)
            {
                string mdPath = Path.Combine(fullOutputPath, "GDD.md");
                File.WriteAllText(mdPath, markdown, Encoding.UTF8);
                Debug.Log($"[TFG Toolkit] GDD Markdown exportado en: {mdPath}");
            }

            if (config.ExportHTML)
            {
                string html = ConvertMarkdownToHTML(config, markdown);
                string htmlPath = Path.Combine(fullOutputPath, "GDD.html");
                File.WriteAllText(htmlPath, html, Encoding.UTF8);
                Debug.Log($"[TFG Toolkit] GDD HTML exportado en: {htmlPath}");
            }

            AssetDatabase.Refresh();
        }

        // -------------------------------------------------------
        // Guarda el GDD generado por la IA (texto markdown ya hecho)
        // -------------------------------------------------------
        public static void SaveAIGeneratedGDD(string aiMarkdown, ExportConfig config)
        {
            string fullOutputPath = Path.Combine(Application.dataPath, "..", config.OutputFolder);
            Directory.CreateDirectory(fullOutputPath);

            if (config.ExportMarkdown)
            {
                string mdPath = Path.Combine(fullOutputPath, "GDD_IA.md");
                File.WriteAllText(mdPath, aiMarkdown, Encoding.UTF8);
                Debug.Log($"[TFG Toolkit] GDD (IA) Markdown exportado en: {mdPath}");
            }

            if (config.ExportHTML)
            {
                string html = ConvertMarkdownToHTML(config, aiMarkdown);
                string htmlPath = Path.Combine(fullOutputPath, "GDD_IA.html");
                File.WriteAllText(htmlPath, html, Encoding.UTF8);
                Debug.Log($"[TFG Toolkit] GDD (IA) HTML exportado en: {htmlPath}");
            }

            AssetDatabase.Refresh();
        }

        // -------------------------------------------------------
        // Contexto del proyecto para la IA (usado por la acción del agente
        // y por el botón "Generar con IA" de la ventana)
        // -------------------------------------------------------
        public static string BuildProjectContextForAI(ExportConfig config)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"PROYECTO: {config.GameTitle}");
            sb.AppendLine($"Autor: {config.Author} | Versión: {config.Version}");
            if (!string.IsNullOrEmpty(config.GameDescription))
                sb.AppendLine($"Descripción: {config.GameDescription}");
            sb.AppendLine();

            var mechanics = LoadAllAssets<MechanicData>();
            var characters = LoadAllAssets<CharacterData>();
            var dialogues = LoadAllAssets<DialogueData>();
            var styles = LoadAllAssets<ArtStyleGuideData>();
            var graphs = LoadAllAssets<MechanicGraphData>();

            if (config.IncludeMechanics && mechanics.Count > 0)
            {
                sb.AppendLine($"=== MECÁNICAS ({mechanics.Count}) ===");
                foreach (var m in mechanics)
                {
                    sb.AppendLine($"- {m.mechanicName} [{m.status}]");
                    if (!string.IsNullOrEmpty(m.description)) sb.AppendLine($"  {m.description}");
                    if (!string.IsNullOrEmpty(m.input))
                        sb.AppendLine($"  Input: {m.input} | Efecto: {m.effect} | Duración: {m.duration}s");
                }
                sb.AppendLine();
            }

            if (config.IncludeCharacters && characters.Count > 0)
            {
                sb.AppendLine($"=== PERSONAJES ({characters.Count}) ===");
                foreach (var c in characters)
                {
                    sb.AppendLine($"- {c.characterName} [{c.role}]");
                    if (!string.IsNullOrEmpty(c.description)) sb.AppendLine($"  {c.description}");
                    if (!string.IsNullOrEmpty(c.backstory)) sb.AppendLine($"  Historia: {c.backstory}");
                    if (!string.IsNullOrEmpty(c.motivation)) sb.AppendLine($"  Motivación: {c.motivation}");
                    sb.AppendLine($"  Stats: Vida={c.health} Ataque={c.attack} Defensa={c.defense} Velocidad={c.speed}");
                    if (!string.IsNullOrEmpty(c.abilities)) sb.AppendLine($"  Habilidades: {c.abilities}");
                    if (c.relatedMechanics != null && c.relatedMechanics.Length > 0)
                    {
                        var names = c.relatedMechanics.Where(m => m != null).Select(m => m.mechanicName);
                        sb.AppendLine($"  Mecánicas: {string.Join(", ", names)}");
                    }
                }
                sb.AppendLine();
            }

            if (config.IncludeDialogues && dialogues.Count > 0)
            {
                sb.AppendLine($"=== ÁRBOLES DE DIÁLOGO ({dialogues.Count}) ===");
                foreach (var d in dialogues)
                {
                    sb.AppendLine($"- {d.TreeName}: {d.Description}");
                    sb.AppendLine($"  ({d.Nodes.Count} nodos, {d.Edges.Count} conexiones)");
                    var textNodes = d.Nodes.Where(n =>
                        n.Type == DialogueNodeType.Dialogue && !string.IsNullOrEmpty(n.DialogueText));
                    foreach (var n in textNodes.Take(5))
                        sb.AppendLine($"  \"{n.DialogueText}\"");
                }
                sb.AppendLine();
            }

            if (config.IncludeArtStyle && styles.Count > 0)
            {
                var sg = styles[0];
                sb.AppendLine("=== GUÍA DE ESTILO VISUAL ===");
                sb.AppendLine($"Estilo: {sg.PrimaryStyle}");
                if (!string.IsNullOrEmpty(sg.ArtisticDirectionStatement))
                    sb.AppendLine($"Dirección artística: {sg.ArtisticDirectionStatement}");
                if (!string.IsNullOrEmpty(sg.Mood)) sb.AppendLine($"Tono: {sg.Mood}");
                if (sg.ColorPalette.Count > 0)
                    sb.AppendLine($"Colores: {string.Join(", ", sg.ColorPalette.Select(c => c.Name))}");
                sb.AppendLine();
            }

            if (config.IncludeMechanicGraph && graphs.Count > 0)
            {
                sb.AppendLine($"=== RELACIONES ENTRE MECÁNICAS ({graphs.Count} grafos) ===");
                foreach (var g in graphs)
                {
                    foreach (var edge in g.Edges)
                    {
                        var src = g.GetNode(edge.SourceNodeId);
                        var tgt = g.GetNode(edge.TargetNodeId);
                        if (src == null || tgt == null) continue;
                        var srcM = AssetDatabase.LoadAssetAtPath<MechanicData>(AssetDatabase.GUIDToAssetPath(src.MechanicId));
                        var tgtM = AssetDatabase.LoadAssetAtPath<MechanicData>(AssetDatabase.GUIDToAssetPath(tgt.MechanicId));
                        if (srcM == null || tgtM == null) continue;
                        sb.AppendLine($"  {srcM.mechanicName} --[{edge.Relation}]--> {tgtM.mechanicName}");
                    }
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Construye el prompt completo que se envía a la IA para generar el GDD.
        /// </summary>
        public static string BuildGDDPrompt(ExportConfig config)
        {
            var sb = new StringBuilder();

            // ── Rol y tono ──
            sb.AppendLine("Eres un diseñador de videojuegos senior con experiencia redactando");
            sb.AppendLine("Game Design Documents profesionales para estudios indie y AAA.");
            sb.AppendLine("Tu tarea es redactar un GDD completo, claro y atractivo en español,");
            sb.AppendLine("en formato Markdown, a partir de los datos REALES del proyecto que");
            sb.AppendLine("se incluyen al final de este mensaje.");
            sb.AppendLine();

            // ── Principios de redacción ──
            sb.AppendLine("PRINCIPIOS DE REDACCIÓN:");
            sb.AppendLine("- Empieza amplio y luego concreta: primero la visión, luego los sistemas.");
            sb.AppendLine("- Redacta en prosa profesional, no te limites a listar los datos en crudo.");
            sb.AppendLine("- Conecta las piezas entre sí: explica cómo las mecánicas sirven a la");
            sb.AppendLine("  experiencia, cómo los personajes encarnan la narrativa, cómo el estilo");
            sb.AppendLine("  visual refuerza el tono. Da sentido al conjunto, no enumeres sin más.");
            sb.AppendLine("- Sé concreto y conciso. Frases claras, sin relleno ni marketing vacío.");
            sb.AppendLine("- NO inventes datos que no aparezcan en la información del proyecto.");
            sb.AppendLine("  Si una sección no tiene datos, escribe '*Pendiente de definir*' y sigue.");
            sb.AppendLine("- Usa tablas Markdown para datos estructurados (stats, mecánicas, colores).");
            sb.AppendLine("- Responde ÚNICAMENTE con el documento Markdown, sin preámbulo ni comentarios.");
            sb.AppendLine();

            // ── Estructura obligatoria ──
            sb.AppendLine("ESTRUCTURA DEL DOCUMENTO (respeta este orden y estos encabezados):");
            sb.AppendLine();
            sb.AppendLine("# [Título del juego]");
            sb.AppendLine("Una línea de tagline o concepto de alto nivel (high concept) que resuma");
            sb.AppendLine("el juego en una frase memorable.");
            sb.AppendLine();
            sb.AppendLine("## 1. Visión general");
            sb.AppendLine("- **Pitch:** un párrafo breve que venda el concepto central del juego.");
            sb.AppendLine("- **Género y plataforma.**");
            sb.AppendLine("- **Público objetivo.**");
            sb.AppendLine("- **Pilares de diseño:** 3 principios que guían todas las decisiones.");
            sb.AppendLine();
            sb.AppendLine("## 2. Bucle de juego (gameplay loop)");
            sb.AppendLine("Describe el bucle principal como una secuencia corta");
            sb.AppendLine("(ej: explorar → combatir → recompensa → mejorar → repetir) y explica");
            sb.AppendLine("brevemente por qué es divertido y mantiene al jugador enganchado.");
            sb.AppendLine();
            sb.AppendLine("## 3. Mecánicas");
            sb.AppendLine("Para cada mecánica: qué hace, cómo se activa, qué aporta a la experiencia.");
            sb.AppendLine("Incluye una tabla resumen con input, efecto y duración. Si hay relaciones");
            sb.AppendLine("entre mecánicas, explícalas (qué activa o cancela qué).");
            sb.AppendLine();
            sb.AppendLine("## 4. Personajes");
            sb.AppendLine("Para cada personaje: rol narrativo, trasfondo, motivación y cómo sus");
            sb.AppendLine("estadísticas y mecánicas asociadas reflejan su papel en el juego.");
            sb.AppendLine();
            sb.AppendLine("## 5. Narrativa y diálogos");
            sb.AppendLine("Resume la estructura narrativa, los puntos de decisión del jugador y el");
            sb.AppendLine("tono de los diálogos. Explica cómo se ramifica la historia.");
            sb.AppendLine();
            sb.AppendLine("## 6. Dirección de arte");
            sb.AppendLine("Describe el estilo visual, el tono, la paleta de colores (en tabla) y las");
            sb.AppendLine("tipografías. Explica cómo el apartado artístico refuerza la experiencia.");
            sb.AppendLine();
            sb.AppendLine("## 7. Estado de desarrollo");
            sb.AppendLine("Resume qué mecánicas están implementadas, en desarrollo o son ideas, y");
            sb.AppendLine("recoge las notas de diseño pendientes como próximos pasos.");
            sb.AppendLine();
            sb.AppendLine("Cierra con una línea indicando que es un documento vivo que evoluciona");
            sb.AppendLine("con el proyecto.");
            sb.AppendLine();
            sb.AppendLine("════════════════════════════════════════════");
            sb.AppendLine("DATOS REALES DEL PROYECTO:");
            sb.AppendLine("════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine(BuildProjectContextForAI(config));

            return sb.ToString();
        }

        // -------------------------------------------------------
        // Construcción del Markdown por plantilla
        // -------------------------------------------------------
        private static string BuildMarkdown(ExportConfig config)
        {
            var mechanics = LoadAllAssets<MechanicData>();
            var characters = LoadAllAssets<CharacterData>();
            var dialogues = LoadAllAssets<DialogueData>();
            var styles = LoadAllAssets<ArtStyleGuideData>();
            var graphs = LoadAllAssets<MechanicGraphData>();

            var sb = new StringBuilder();

            // Cabecera
            sb.AppendLine($"# {config.GameTitle}");
            sb.AppendLine();
            sb.AppendLine($"> **Versión:** {config.Version}  ");
            sb.AppendLine($"> **Autor:** {config.Author}  ");
            sb.AppendLine($"> **Fecha:** {DateTime.Now:dd/MM/yyyy}  ");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(config.GameDescription))
            {
                sb.AppendLine("## Descripción del juego");
                sb.AppendLine();
                sb.AppendLine(config.GameDescription);
                sb.AppendLine();
            }

            // Índice
            sb.AppendLine("## Índice");
            sb.AppendLine();
            int section = 1;
            if (config.IncludeMechanics && mechanics.Count > 0) sb.AppendLine($"{section++}. Mecánicas");
            if (config.IncludeCharacters && characters.Count > 0) sb.AppendLine($"{section++}. Personajes");
            if (config.IncludeDialogues && dialogues.Count > 0) sb.AppendLine($"{section++}. Narrativa y diálogos");
            if (config.IncludeArtStyle && styles.Count > 0) sb.AppendLine($"{section++}. Guía de estilo visual");
            if (config.IncludeMechanicGraph && graphs.Count > 0) sb.AppendLine($"{section++}. Relaciones entre sistemas");
            if (config.IncludeNotes) sb.AppendLine($"{section++}. Notas de diseño");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            // Mecánicas
            if (config.IncludeMechanics && mechanics.Count > 0)
            {
                sb.AppendLine("## Mecánicas");
                sb.AppendLine();
                foreach (var m in mechanics)
                {
                    string statusIcon = m.status switch
                    {
                        MechanicStatus.Idea => "💡 Idea",
                        MechanicStatus.EnDesarrollo => "🔧 En desarrollo",
                        MechanicStatus.Implementada => "✅ Implementada",
                        _ => ""
                    };
                    sb.AppendLine($"### {m.mechanicName}");
                    sb.AppendLine();
                    sb.AppendLine($"**Estado:** {statusIcon}  ");
                    sb.AppendLine();
                    if (!string.IsNullOrEmpty(m.description))
                    {
                        sb.AppendLine($"**Descripción:** {m.description}  ");
                        sb.AppendLine();
                    }
                    sb.AppendLine("| Campo | Valor |");
                    sb.AppendLine("|-------|-------|");
                    sb.AppendLine($"| Input | {EscapeMarkdownTable(m.input)} |");
                    sb.AppendLine($"| Efecto | {EscapeMarkdownTable(m.effect)} |");
                    sb.AppendLine($"| Duración | {(m.duration == 0 ? "Instantáneo / Permanente" : $"{m.duration}s")} |");
                    sb.AppendLine();
                    if (!string.IsNullOrEmpty(m.generatedScript))
                    {
                        sb.AppendLine("**Script generado:**");
                        sb.AppendLine();
                        sb.AppendLine("```csharp");
                        sb.AppendLine(m.generatedScript);
                        sb.AppendLine("```");
                        sb.AppendLine();
                    }
                    sb.AppendLine("---");
                    sb.AppendLine();
                }
            }

            // Personajes
            if (config.IncludeCharacters && characters.Count > 0)
            {
                sb.AppendLine("## Personajes");
                sb.AppendLine();
                foreach (var c in characters)
                {
                    string roleIcon = c.role switch
                    {
                        CharacterRole.Protagonista => "🦸 Protagonista",
                        CharacterRole.Enemigo => "🦹 Antagonista",
                        CharacterRole.NPC => "🧑 NPC",
                        CharacterRole.Compañero => "🤝 Compañero",
                        CharacterRole.Jefe => "👑 Jefe",
                        _ => c.role.ToString()
                    };
                    sb.AppendLine($"### {c.characterName}");
                    sb.AppendLine();
                    sb.AppendLine($"**Rol:** {roleIcon}  ");
                    sb.AppendLine();
                    if (!string.IsNullOrEmpty(c.description))
                    {
                        sb.AppendLine($"**Descripción:** {c.description}  ");
                        sb.AppendLine();
                    }
                    if (!string.IsNullOrEmpty(c.backstory))
                    {
                        sb.AppendLine($"**Historia:** {c.backstory}  ");
                        sb.AppendLine();
                    }
                    if (!string.IsNullOrEmpty(c.motivation))
                        sb.AppendLine($"**Motivación:** {c.motivation}  ");
                    sb.AppendLine();
                    sb.AppendLine("**Estadísticas base:**");
                    sb.AppendLine();
                    sb.AppendLine("| Stat | Valor |");
                    sb.AppendLine("|------|-------|");
                    sb.AppendLine($"| Vida | {c.health} |");
                    sb.AppendLine($"| Ataque | {c.attack} |");
                    sb.AppendLine($"| Defensa | {c.defense} |");
                    sb.AppendLine($"| Velocidad | {c.speed} |");
                    sb.AppendLine();
                    if (!string.IsNullOrEmpty(c.abilities))
                    {
                        sb.AppendLine($"**Habilidades:** {c.abilities}  ");
                        sb.AppendLine();
                    }
                    if (c.relatedMechanics != null && c.relatedMechanics.Length > 0)
                    {
                        sb.AppendLine("**Mecánicas asociadas:**");
                        sb.AppendLine();
                        foreach (var m in c.relatedMechanics)
                            if (m != null) sb.AppendLine($"- {m.mechanicName}");
                        sb.AppendLine();
                    }
                    if (!string.IsNullOrEmpty(c.designNotes))
                    {
                        sb.AppendLine($"**Notas de diseño:** {c.designNotes}  ");
                        sb.AppendLine();
                    }
                    sb.AppendLine("---");
                    sb.AppendLine();
                }
            }

            // Narrativa y diálogos
            if (config.IncludeDialogues && dialogues.Count > 0)
            {
                sb.AppendLine("## Narrativa y diálogos");
                sb.AppendLine();
                foreach (var d in dialogues)
                {
                    sb.AppendLine($"### {d.TreeName}");
                    sb.AppendLine();
                    if (!string.IsNullOrEmpty(d.Description))
                    {
                        sb.AppendLine(d.Description);
                        sb.AppendLine();
                    }
                    sb.AppendLine($"**Estructura:** {d.Nodes.Count} nodos, {d.Edges.Count} conexiones  ");
                    sb.AppendLine();

                    var textNodes = d.Nodes
                        .Where(n => n.Type == DialogueNodeType.Dialogue && !string.IsNullOrEmpty(n.DialogueText))
                        .ToList();
                    if (textNodes.Count > 0)
                    {
                        sb.AppendLine("**Líneas de diálogo:**");
                        sb.AppendLine();
                        foreach (var n in textNodes)
                        {
                            string speaker = "Narrador";
                            if (!string.IsNullOrEmpty(n.CharacterGUID))
                            {
                                var ch = AssetDatabase.LoadAssetAtPath<CharacterData>(
                                    AssetDatabase.GUIDToAssetPath(n.CharacterGUID));
                                if (ch != null) speaker = ch.characterName;
                            }
                            sb.AppendLine($"> **{speaker}:** \"{n.DialogueText}\"");
                            sb.AppendLine();
                        }
                    }

                    var choiceNodes = d.Nodes.Where(n => n.Type == DialogueNodeType.Choice).ToList();
                    if (choiceNodes.Count > 0)
                    {
                        sb.AppendLine("**Puntos de elección:**");
                        sb.AppendLine();
                        foreach (var n in choiceNodes)
                            foreach (var choice in n.Choices)
                                sb.AppendLine($"- {choice.Text}");
                        sb.AppendLine();
                    }
                    sb.AppendLine("---");
                    sb.AppendLine();
                }
            }

            // Guía de estilo visual
            if (config.IncludeArtStyle && styles.Count > 0)
            {
                var sg = styles[0];
                sb.AppendLine("## Guía de estilo visual");
                sb.AppendLine();
                sb.AppendLine($"**Estilo artístico:** {sg.PrimaryStyle}  ");
                sb.AppendLine();
                if (!string.IsNullOrEmpty(sg.ArtisticDirectionStatement))
                {
                    sb.AppendLine(sg.ArtisticDirectionStatement);
                    sb.AppendLine();
                }
                if (!string.IsNullOrEmpty(sg.Mood))
                {
                    sb.AppendLine($"**Tono y sentimiento:** {sg.Mood}  ");
                    sb.AppendLine();
                }
                if (!string.IsNullOrEmpty(sg.VisualReferences))
                {
                    sb.AppendLine($"**Inspiraciones:** {sg.VisualReferences}  ");
                    sb.AppendLine();
                }
                if (sg.ColorPalette.Count > 0)
                {
                    sb.AppendLine("**Paleta de colores:**");
                    sb.AppendLine();
                    sb.AppendLine("| Color | Hex | Uso |");
                    sb.AppendLine("|-------|-----|-----|");
                    foreach (var col in sg.ColorPalette)
                        sb.AppendLine($"| {EscapeMarkdownTable(col.Name)} | #{ColorUtility.ToHtmlStringRGB(col.Value)} | {EscapeMarkdownTable(col.Usage)} |");
                    sb.AppendLine();
                }
                if (sg.Typographies.Count > 0)
                {
                    sb.AppendLine("**Tipografías:**");
                    sb.AppendLine();
                    sb.AppendLine("| Nombre | Tamaño | Estilo | Uso |");
                    sb.AppendLine("|--------|--------|--------|-----|");
                    foreach (var t in sg.Typographies)
                        sb.AppendLine($"| {EscapeMarkdownTable(t.Name)} | {t.Size}pt | {EscapeMarkdownTable(t.Style)} | {EscapeMarkdownTable(t.Usage)} |");
                    sb.AppendLine();
                }
                if (!string.IsNullOrEmpty(sg.UIGuidelines))
                {
                    sb.AppendLine($"**Guía de UI:** {sg.UIGuidelines}  ");
                    sb.AppendLine();
                }
                if (!string.IsNullOrEmpty(sg.TechnicalConstraints))
                {
                    sb.AppendLine($"**Restricciones técnicas:** {sg.TechnicalConstraints}  ");
                    sb.AppendLine();
                }
                sb.AppendLine("---");
                sb.AppendLine();
            }

            // Relaciones entre sistemas (grafos)
            if (config.IncludeMechanicGraph && graphs.Count > 0)
            {
                sb.AppendLine("## Relaciones entre sistemas");
                sb.AppendLine();
                foreach (var g in graphs)
                {
                    sb.AppendLine($"### {g.GraphName}");
                    sb.AppendLine();
                    if (!string.IsNullOrEmpty(g.Description))
                    {
                        sb.AppendLine(g.Description);
                        sb.AppendLine();
                    }
                    if (g.Edges.Count > 0)
                    {
                        sb.AppendLine("| Mecánica origen | Relación | Mecánica destino |");
                        sb.AppendLine("|-----------------|----------|------------------|");
                        foreach (var edge in g.Edges)
                        {
                            var src = g.GetNode(edge.SourceNodeId);
                            var tgt = g.GetNode(edge.TargetNodeId);
                            if (src == null || tgt == null) continue;
                            var srcM = AssetDatabase.LoadAssetAtPath<MechanicData>(AssetDatabase.GUIDToAssetPath(src.MechanicId));
                            var tgtM = AssetDatabase.LoadAssetAtPath<MechanicData>(AssetDatabase.GUIDToAssetPath(tgt.MechanicId));
                            if (srcM == null || tgtM == null) continue;
                            sb.AppendLine($"| {EscapeMarkdownTable(srcM.mechanicName)} | {edge.Relation} | {EscapeMarkdownTable(tgtM.mechanicName)} |");
                        }
                        sb.AppendLine();
                    }
                    if (!string.IsNullOrEmpty(g.LastAIAnalysis))
                    {
                        sb.AppendLine("**Análisis IA del sistema:**");
                        sb.AppendLine();
                        sb.AppendLine(g.LastAIAnalysis);
                        sb.AppendLine();
                    }
                    sb.AppendLine("---");
                    sb.AppendLine();
                }
            }

            // Notas de diseño
            if (config.IncludeNotes)
            {
                const string NOTES_PATH = "Assets/TFGToolkit/Data/Notes/DesignNotes.json";
                if (File.Exists(NOTES_PATH))
                {
                    try
                    {
                        var notesData = JsonUtility.FromJson<DesignNotesData>(File.ReadAllText(NOTES_PATH));
                        var pending = notesData?.Notes?.Where(n => !n.Resolved).ToList();
                        if (pending != null && pending.Count > 0)
                        {
                            sb.AppendLine("## Notas de diseño pendientes");
                            sb.AppendLine();
                            foreach (var note in pending)
                            {
                                sb.AppendLine($"### [{note.Type}] {note.Title}");
                                sb.AppendLine();
                                if (!string.IsNullOrEmpty(note.Content)) { sb.AppendLine(note.Content); sb.AppendLine(); }
                                if (!string.IsNullOrEmpty(note.Author)) { sb.AppendLine($"*— {note.Author}, {note.Date}*"); sb.AppendLine(); }
                            }
                            sb.AppendLine("---");
                            sb.AppendLine();
                        }
                    }
                    catch { }
                }
            }

            sb.AppendLine();
            sb.AppendLine($"*Documento generado automáticamente por TFG Toolkit el {DateTime.Now:dd/MM/yyyy HH:mm}*");
            return sb.ToString();
        }

        // -------------------------------------------------------
        // Conversión a HTML (sin cambios respecto a tu versión)
        // -------------------------------------------------------
        private static string ConvertMarkdownToHTML(ExportConfig config, string markdown)
        {
            var lines = markdown.Split('\n');
            var body = new StringBuilder();
            bool inCodeBlock = false, inTable = false, tableHeaderDone = false;

            foreach (var rawLine in lines)
            {
                string line = rawLine.TrimEnd();

                if (line.StartsWith("```"))
                {
                    if (!inCodeBlock) { body.AppendLine("<pre><code>"); inCodeBlock = true; }
                    else { body.AppendLine("</code></pre>"); inCodeBlock = false; }
                    continue;
                }
                if (inCodeBlock) { body.AppendLine(HtmlEncode(line)); continue; }

                if (line.StartsWith("|"))
                {
                    if (!inTable) { body.AppendLine("<table>"); inTable = true; tableHeaderDone = false; }
                    if (line.Replace("|", "").Replace("-", "").Replace(" ", "") == "")
                    {
                        body.AppendLine("</thead><tbody>");
                        tableHeaderDone = true;
                        continue;
                    }
                    var cells = line.Split('|');
                    string tag = tableHeaderDone ? "td" : "th";
                    body.Append("<tr>");
                    foreach (var cell in cells)
                    {
                        if (string.IsNullOrWhiteSpace(cell)) continue;
                        body.Append($"<{tag}>{ParseInlineMarkdown(cell.Trim())}</{tag}>");
                    }
                    body.AppendLine("</tr>");
                    if (!tableHeaderDone) body.AppendLine("<thead>");
                    continue;
                }
                else if (inTable) { body.AppendLine("</tbody></table>"); inTable = false; }

                if (line.StartsWith("# ")) { body.AppendLine($"<h1>{ParseInlineMarkdown(line.Substring(2))}</h1>"); continue; }
                else if (line.StartsWith("## ")) { body.AppendLine($"<h2>{ParseInlineMarkdown(line.Substring(3))}</h2>"); continue; }
                else if (line.StartsWith("### ")) { body.AppendLine($"<h3>{ParseInlineMarkdown(line.Substring(4))}</h3>"); continue; }
                if (line == "---") { body.AppendLine("<hr>"); continue; }
                if (line.StartsWith("> ")) { body.AppendLine($"<blockquote>{ParseInlineMarkdown(line.Substring(2))}</blockquote>"); continue; }
                if (line.StartsWith("- ")) { body.AppendLine($"<li>{ParseInlineMarkdown(line.Substring(2))}</li>"); continue; }
                if (!string.IsNullOrWhiteSpace(line)) body.AppendLine($"<p>{ParseInlineMarkdown(line)}</p>");
                else body.AppendLine("<br>");
            }
            if (inTable) body.AppendLine("</tbody></table>");

            return $@"<!DOCTYPE html>
<html lang=""es"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{HtmlEncode(config.GameTitle)} — GDD</title>
    <style>
        :root {{ --bg:#1a1a2e; --surface:#16213e; --card:#0f3460; --accent:#e94560; --text:#eaeaea; --muted:#8892a4; --code-bg:#0d1117; --border:#30363d; }}
        * {{ box-sizing: border-box; margin: 0; padding: 0; }}
        body {{ font-family:'Segoe UI',system-ui,sans-serif; background:var(--bg); color:var(--text); line-height:1.7; padding:2rem; }}
        h1 {{ font-size:2.4rem; color:var(--accent); margin-bottom:0.5rem; }}
        h2 {{ font-size:1.6rem; color:var(--accent); margin:2.5rem 0 1rem; padding-bottom:0.4rem; border-bottom:2px solid var(--accent); }}
        h3 {{ font-size:1.2rem; color:#7ec8e3; margin:1.8rem 0 0.6rem; }}
        p  {{ margin:0.5rem 0; color:var(--text); }}
        li {{ margin:0.3rem 0 0.3rem 1.5rem; }}
        hr {{ border:none; border-top:1px solid var(--border); margin:1.5rem 0; }}
        blockquote {{ border-left:3px solid var(--accent); padding:0.5rem 1rem; color:var(--muted); background:var(--surface); border-radius:0 6px 6px 0; margin:1rem 0; }}
        pre {{ background:var(--code-bg); border:1px solid var(--border); border-radius:8px; padding:1rem; overflow-x:auto; margin:1rem 0; }}
        code {{ font-family:'Consolas','Fira Code',monospace; font-size:0.88rem; color:#c9d1d9; }}
        table {{ width:100%; border-collapse:collapse; margin:1rem 0; background:var(--surface); border-radius:8px; overflow:hidden; }}
        th {{ background:var(--card); color:var(--text); padding:0.6rem 1rem; text-align:left; font-weight:600; }}
        td {{ padding:0.6rem 1rem; border-top:1px solid var(--border); }}
        tr:hover td {{ background:rgba(255,255,255,0.03); }}
        strong {{ color:#7ec8e3; }}
        em {{ color:var(--muted); font-style:italic; }}
        @media print {{ body{{background:white;color:black;}} h1,h2{{color:#c0392b;}} h3{{color:#2980b9;}} }}
    </style>
</head>
<body>
{body}
</body>
</html>";
        }

        // -------------------------------------------------------
        // Helpers
        // -------------------------------------------------------
        public static List<T> LoadAllAssets<T>() where T : ScriptableObject
        {
            var result = new List<T>();
            foreach (var guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) result.Add(asset);
            }
            return result;
        }

        private static string HtmlEncode(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        private static string ParseInlineMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            text = HtmlEncode(text);
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\*(.+?)\*", "<em>$1</em>");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"`(.+?)`", "<code>$1</code>");
            return text;
        }

        private static string EscapeMarkdownTable(string text)
        {
            if (string.IsNullOrEmpty(text)) return "—";
            return text.Replace("|", "\\|").Replace("\n", " ");
        }
    }
}