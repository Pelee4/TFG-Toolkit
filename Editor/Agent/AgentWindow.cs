using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TFGToolkit.Agent
{
    /// <summary>
    /// Ventana de chat del agente dentro del editor de Unity.
    /// </summary>
    public class AgentWindow : EditorWindow
    {
        // Instancia del agente
        private ToolkitAgent _agent = new ToolkitAgent();

        // Estado de la UI
        private string _inputText = "";
        private bool _isWaiting = false;
        private string _statusMessage = "";
        private Vector2 _chatScroll;

        // Mensajes del chat para mostrar
        private readonly List<ChatMessage> _messages = new();

        private struct ChatMessage
        {
            public string Role;    // "user" | "assistant" | "system"
            public string Content;
        }

        public static void ShowWindow()
        {
            var window = GetWindow<AgentWindow>("Agente de Diseño");
            window.minSize = new Vector2(420, 500);
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawChatHistory();
            DrawInputArea();
        }

        // -------------------------------------------------------
        // Header
        // -------------------------------------------------------
        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Agente de Diseño TFG Toolkit", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            // Botón para refrescar el contexto manualmente
            if (GUILayout.Button("Contexto", EditorStyles.toolbarButton, GUILayout.Width(80)))
                AddSystemMessage("Contexto del proyecto actualizado.");

            // Botón para limpiar el historial
            if (GUILayout.Button("Limpiar", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                _messages.Clear();
                _agent.ClearHistory();
                _statusMessage = "";
            }
            EditorGUILayout.EndHorizontal();

            // Mensaje de estado (esperando / error)
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                GUIStyle statusStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    wordWrap = true,
                    normal = { textColor = _statusMessage.StartsWith("Error")
                        ? new Color(1f, 0.4f, 0.4f) : Color.gray }
                };
                EditorGUILayout.LabelField(_statusMessage, statusStyle);
            }
        }

        // -------------------------------------------------------
        // Historial del chat
        // -------------------------------------------------------
        private void DrawChatHistory()
        {
            // Área de scroll que ocupa todo el espacio disponible menos el input
            float inputHeight = 90f;
            float chatHeight = position.height - inputHeight - 60f;

            _chatScroll = EditorGUILayout.BeginScrollView(
                _chatScroll,
                GUILayout.Height(Mathf.Max(100, chatHeight)));

            foreach (var msg in _messages)
            {
                DrawMessage(msg);
                GUILayout.Space(4);
            }

            // Indicador de "escribiendo..."
            if (_isWaiting)
            {
                GUILayout.Space(4);
                GUILayout.Label("El agente está procesando...",
                    new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } });
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawMessage(ChatMessage msg)
        {
            bool isUser = msg.Role == "user";
            bool isSys = msg.Role == "system";

            // Color de fondo según el rol
            Color bgColor = isUser
                ? new Color(0.25f, 0.35f, 0.5f, 0.4f)
                : isSys
                    ? new Color(0.3f, 0.5f, 0.3f, 0.3f)
                    : new Color(0.2f, 0.2f, 0.25f, 0.4f);

            // Nombre del emisor
            string label = isUser ? "Tú" : isSys ? "Sistema" : "Agente";

            Rect bgRect = GUILayoutUtility.GetRect(
                GUIContent.none,
                GUIStyle.none,
                GUILayout.Height(1));     // altura 0 — la calcularemos abajo

            // Layout del mensaje
            EditorGUILayout.BeginVertical(GUI.skin.box);

            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = isUser ? new Color(0.5f, 0.8f, 1f) :
                                          isSys  ? new Color(0.5f, 1f, 0.5f) :
                                                   new Color(1f, 0.8f, 0.4f) }
            };
            GUILayout.Label(label, labelStyle);

            GUIStyle contentStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                fontSize = 11,
                richText = true
            };
            GUILayout.Label(msg.Content, contentStyle);

            EditorGUILayout.EndVertical();
        }

        // -------------------------------------------------------
        // Área de input
        // -------------------------------------------------------
        private void DrawInputArea()
        {
            GUILayout.Space(4);
            EditorGUILayout.BeginVertical(GUI.skin.box);

            // Campo de texto
            GUI.SetNextControlName("AgentInput");
            _inputText = EditorGUILayout.TextArea(
                _inputText,
                GUILayout.Height(50),
                GUILayout.ExpandWidth(true));

            EditorGUILayout.BeginHorizontal();

            // Sugerencias rápidas
            GUIStyle suggStyle = new GUIStyle(EditorStyles.miniButton);
            if (GUILayout.Button("¿Qué tengo?", suggStyle)) SetInput("¿Qué mecánicas y personajes tengo en el proyecto?");
            if (GUILayout.Button("Sugerir ideas", suggStyle)) SetInput("Basándote en lo que hay en el proyecto, sugiere 3 mecánicas nuevas que complementen las existentes.");
            if (GUILayout.Button("Revisar notas", suggStyle)) SetInput("Resume las notas de diseño pendientes y sugiere cuál abordar primero.");

            GUILayout.FlexibleSpace();

            // Botón enviar
            GUI.enabled = !_isWaiting && !string.IsNullOrWhiteSpace(_inputText);
            GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
            if (GUILayout.Button(_isWaiting ? "..." : "Enviar", GUILayout.Width(80)))
                SendMessage();
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            // Enter para enviar (Shift+Enter para nueva línea)
            if (Event.current.type == EventType.KeyDown
                && Event.current.keyCode == KeyCode.Return
                && !Event.current.shift
                && GUI.GetNameOfFocusedControl() == "AgentInput"
                && !_isWaiting
                && !string.IsNullOrWhiteSpace(_inputText))
            {
                SendMessage();
                Event.current.Use();
            }
        }

        // -------------------------------------------------------
        // Enviar mensaje al agente
        // -------------------------------------------------------
        private void SendMessage()
        {
            string message = _inputText.Trim();
            if (string.IsNullOrEmpty(message)) return;

            // Añadir mensaje del usuario al chat
            _messages.Add(new ChatMessage { Role = "user", Content = message });
            _inputText = "";
            _isWaiting = true;
            _statusMessage = "Procesando...";
            ScrollToBottom();
            Repaint();

            _agent.SendMessage(
                message,
                onResponse: response =>
                {
                    _messages.Add(new ChatMessage { Role = "assistant", Content = response });
                    _isWaiting = false;
                    _statusMessage = "";
                    ScrollToBottom();
                    Repaint();
                },
                onError: error =>
                {
                    _isWaiting = false;
                    _statusMessage = $"Error: {error}";
                    _messages.Add(new ChatMessage { Role = "system", Content = $"{error}" });
                    ScrollToBottom();
                    Repaint();
                }
            );
        }

        private void AddSystemMessage(string text)
        {
            _messages.Add(new ChatMessage { Role = "system", Content = text });
            Repaint();
        }

        private void SetInput(string text)
        {
            _inputText = text;
            GUI.FocusControl("AgentInput");
            Repaint();
        }

        private void ScrollToBottom()
        {
            _chatScroll.y = float.MaxValue;
        }
    }
}
