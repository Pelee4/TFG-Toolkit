using UnityEngine;

namespace TFGToolkit
{
    /// <summary>
    /// Configuración global del toolkit.
    /// Crea este asset desde: Assets > Create > TFG Toolkit > Configuración
    /// Guárdalo en Assets/TFGToolkit/Settings/ y añade ese archivo a .gitignore
    /// para que la API key nunca se suba a GitHub.
    /// </summary>
    [CreateAssetMenu(fileName = "ToolkitConfig", menuName = "TFG Toolkit/Configuración")]
    public class ToolkitConfig : ScriptableObject
    {
        [Header("API de IA")]
        [Tooltip("Pega aquí tu API Key de Gemini (aistudio.google.com)")]
        public string geminiApiKey = "AIzaSyCamXF331d7smdNbi3qal1XyawVJeEq74Y";

        [Tooltip("Pega aquí tu API Key de Anthropic (console.anthropic.com)")]
        public string anthropicApiKey = "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";


        [Header("Proveedor de IA")]
        [Tooltip("Modelo de Ollama (ej: qwen2.5-coder:3b)")]
        public string ollamaModel = "qwen2.5-coder:3b";
        public enum AIProvider { Ollama, Gemini, Anthropic }
        public AIProvider activeProvider = AIProvider.Ollama;

        [Header("Modelo")]
        [Tooltip("Modelo de Gemini a usar")]
        public string geminiModel = "gemini-2.0-flash";

        [Tooltip("Modelo de Anthropic a usar")]
        public string anthropicModel = "claude-sonnet-4-20250514";



        // -------------------------------------------------------
        // Acceso global — carga el config automáticamente desde
        // Resources sin que tengas que arrastrarlo a ningún sitio
        // -------------------------------------------------------
        private static ToolkitConfig _instance;
        public static ToolkitConfig Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<ToolkitConfig>("ToolkitConfig");

                if (_instance == null)
                    Debug.LogError("[TFG Toolkit] No se encontró ToolkitConfig en Resources/. " +
                                  "Crea el asset desde Assets > Create > TFG Toolkit > Configuración " +
                                  "y muévelo a Assets/TFGToolkit/Resources/");
                return _instance;
            }
        }
    }
}