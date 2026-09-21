using Codice.Client.BaseCommands.WkStatus.Printers;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TFGToolkit
{
    /// <summary>
    /// Gestiona el arrastre de prefabs desde la biblioteca a la Scene View o la Hierarchy.
    ///
    /// CÓMO FUNCIONA:
    /// 1. El usuario hace clic y arrastra una tarjeta en PrototypeLibraryWindow
    /// 2. PrototypeDragHandler.StartDrag() inicia el drag con la API de Unity
    /// 3. SceneView.duringSceneGui detecta cuando el usuario suelta sobre la escena
    /// 4. Se instancia el GameObject con todos sus componentes y scripts de mecánicas
    /// </summary>
    public static class PrototypeDragHandler
    {
        // Key usada en DragAndDrop.SetGenericData para identificar el drag
        private const string DRAG_KEY = "TFGToolkit_PrototypePrefab";

        // Prefab que esta siendo arrastrada actualmente
        private static PrototypePrefabData _dragging;

        //Preview visual en la escena del prototipo draggeado
        private static GameObject _previewGhost;


        // -- Inicializacion - suscribirse a los eventos de la SceneView -- //
        public static void Initialize()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        public static void Cleanup()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            DestroyPreview();
        }


        // -- Iniciar el drag desde la ventana del toolkit -- //
        public static void StartDrag(PrototypePrefabData prefabData)
        {
            if (prefabData == null) return;

            _dragging = prefabData;

            DragAndDrop.PrepareStartDrag();
            DragAndDrop.SetGenericData(DRAG_KEY, prefabData);
            DragAndDrop.objectReferences = new UnityEngine.Object[0];
            DragAndDrop.StartDrag($"Colocar: {prefabData.PrefabName}");
        }


        // -- Callback de la SceneView - se llama cada frame mientra algo se arrastra sobre la escena -- //
        private static void OnSceneGUI(SceneView sceneView)
        {
            Event e = Event.current;

            var draggingData = DragAndDrop.GetGenericData(DRAG_KEY) as PrototypePrefabData;
            if (draggingData == null)
            {
                if (_previewGhost != null) DestroyPreview();
                return;
            }

            switch (e.type)
            {
                case EventType.DragUpdated:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    UpdatePreview(draggingData, e.mousePosition, sceneView);
                    e.Use();
                    break;

                case EventType.DragPerform:
                    DragAndDrop.AcceptDrag();
                    Vector3 worldPos = GetWorldPosition(e.mousePosition, sceneView);
                    InstantiatePrefab(draggingData, worldPos);
                    DestroyPreview();
                    _dragging = null;
                    e.Use();
                    break;

                case EventType.DragExited:
                    DestroyPreview();
                    _dragging = null;
                    break;
            }
        }


        // -- Preview fantasma mientras se arrastra
        private static void UpdatePreview(PrototypePrefabData data, Vector2 mousePos, SceneView sceneView)
        {
            Vector3 worldPos = GetWorldPosition(mousePos, sceneView);

            if (_previewGhost == null)
                _previewGhost = CreatePreviewGhost(data);

            if (_previewGhost != null)
            {
                _previewGhost.transform.position = worldPos;

                //Dibujar nombre del prototipo sobre el ghost
                Handles.BeginGUI();
                Vector2 guiPos = HandleUtility.WorldToGUIPoint(worldPos);
                GUI.Label(
                    new Rect(guiPos.x - 60, guiPos.y - 40, 120, 20),
                    data.PrefabName,
                    new GUIStyle(EditorStyles.boldLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = data.GizmoColor }
                    });
                Handles.EndGUI();
            }

            sceneView.Repaint();
        }

        private static GameObject CreatePreviewGhost(PrototypePrefabData data)
        {
            GameObject ghost;

            if (data.BasePrefab != null)
            {
                ghost = (GameObject)PrefabUtility.InstantiatePrefab(data.BasePrefab);
            }
            else
            {
                ghost = GameObject.CreatePrimitive(data.PrimitiveShape);
            }

            if (ghost == null) return null;

            ghost.name = $"[Preview] {data.PrefabName}";

            // Hacer el ghost semitransparente
            var renderers = ghost.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                var mats = r.sharedMaterials;
                var newMats = new Material[mats.Length];
                for (int i = 0; i < mats.Length; i++)
                {
                    var mat = new Material(Shader.Find("Standard") ?? mats[i].shader);
                    mat.color = new Color(data.GizmoColor.r, data.GizmoColor.g, data.GizmoColor.b, 0.4f);
                    SetMaterialTransparent(mat);
                    newMats[i] = mat;
                }
                r.sharedMaterials = newMats;
            }

            var rigidbodies = ghost.GetComponentsInChildren<Rigidbody>();
            foreach (var rb in rigidbodies) rb.isKinematic = true;

            ghost.hideFlags = HideFlags.HideAndDontSave;
            return ghost;
        }

        private static void DestroyPreview()
        {
            if (_previewGhost != null)
            {
                UnityEngine.Object.DestroyImmediate(_previewGhost);
                _previewGhost = null;
            }
        }

        private static void SetMaterialTransparent(Material mat)
        {
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }


        // -- Instanciacion final cuando se suelta en la escena -- //
        private static void InstantiatePrefab(PrototypePrefabData data, Vector3 position)
        {
            GameObject go;

            if (data.BasePrefab != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(data.BasePrefab);
                if (go == null) go = UnityEngine.Object.Instantiate(data.BasePrefab);
            }
            else
            {
                go = GameObject.CreatePrimitive(data.PrimitiveShape);
            }

            go.name = data.PrefabName;
            go.transform.position = position;

            //Añadir componentes base definidos
            foreach (var comp in data.BaseComponents)
            {
                if (string.IsNullOrEmpty(comp.ComponentTypeName)) continue;

                Type type = GetComponentType(comp.ComponentTypeName);
                if (type == null)
                {
                    Debug.LogWarning(
                        $"[TFG Toolkit] Componente '{comp.ComponentTypeName}' no encontrado. " +
                        $"Asegúrate de usar el nombre exacto de la clase.");
                    continue;
                }

                if (go.GetComponent(type) == null)
                    go.AddComponent(type);
            }

            //Añadir scripts de mecanicas asignados
            AddAssignedMechanicScripts(go, data);

            // Registrar en Undo para que el usuario pueda deshacer con Ctrl+Z
            Undo.RegisterCreatedObjectUndo(go, $"Colocar {data.PrefabName}");

            // Marcar la escena como modificada
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            // Seleccionar el objeto recién creado
            Selection.activeGameObject = go;

            Debug.Log($"[TFG Toolkit] '{data.PrefabName}' colocado en {position}");
        }


        // -- Scripts de mecanicas -- //
        private static void AddAssignedMechanicScripts(GameObject go, PrototypePrefabData data)
        {
            foreach (var mechanic in data.LinkedMechanics)
            {
                if (mechanic == null) continue;

                string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(mechanic));
                if (!data.AssignedMechanicGuids.Contains(guid)) continue;

                // El script generado se guarda en Assets/TFGToolkit/Scripts/Generated/
                string scriptName = ToPascalCase(mechanic.mechanicName);
                string scriptPath = $"Assets/TFGToolkit/Scripts/Generated/{scriptName}.cs";

                var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                if (monoScript == null)
                {
                    Debug.LogWarning(
                        $"[TFG Toolkit] Script '{scriptName}.cs' no encontrado en " +
                        $"Assets/TFGToolkit/Scripts/Generated/. " +
                        $"Genera el script desde la ficha de mecánica primero.");
                    continue;
                }

                Type scriptType = monoScript.GetClass();
                if (scriptType == null) continue;

                if (go.GetComponent(scriptType) == null)
                {
                    go.AddComponent(scriptType);
                    Debug.Log($"[TFG Toolkit] Script '{scriptName}' añadido a '{go.name}'");
                }
            }
        }

        // -------------------------------------------------------
        // También permite arrastrar a la Hierarchy
        // Llama a este método desde OnGUI de la ventana cuando
        // detectes drag sobre el área de la Hierarchy
        // -------------------------------------------------------
        public static void HandleHierarchyDrop(PrototypePrefabData data)
        {
            if (data == null) return;

            // Posición por defecto cuando se suelta en la Hierarchy
            Vector3 defaultPos = Vector3.zero;

            // Intentar centrar en la vista de la SceneView activa
            if (SceneView.lastActiveSceneView != null)
            {
                defaultPos = SceneView.lastActiveSceneView.camera.transform.position
                           + SceneView.lastActiveSceneView.camera.transform.forward * 5f;
                defaultPos.y = 0f;
            }

            InstantiatePrefab(data, defaultPos);
        }

        // -------------------------------------------------------
        // Helpers
        // -------------------------------------------------------

        /// Convierte la posición del mouse en coordenadas del mundo 3D
        private static Vector3 GetWorldPosition(Vector2 mousePos, SceneView sceneView)
        {
            // Raycast contra el plano Y=0 (suelo por defecto)
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);

            float t = 0f;
            if (Mathf.Abs(ray.direction.y) > 0.001f)
                t = -ray.origin.y / ray.direction.y;

            if (t > 0f)
                return ray.origin + ray.direction * t;

            // Fallback: proyectar a cierta distancia de la cámara
            return ray.origin + ray.direction * 10f;
        }

        private static Type GetComponentType(string typeName)
        {
            // Buscar en UnityEngine primero
            Type t = Type.GetType(typeName);
            if (t != null) return t;

            t = Type.GetType($"UnityEngine.{typeName}, UnityEngine");
            if (t != null) return t;

            // Buscar en todos los assemblies cargados (para scripts del proyecto)
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                t = assembly.GetType(typeName);
                if (t != null) return t;

                t = assembly.GetType($"TFGToolkit.{typeName}");
                if (t != null) return t;
            }

            return null;
        }

        private static string ToPascalCase(string s)
        {
            if (string.IsNullOrEmpty(s)) return "Script";
            var words = s.Trim().Split(' ');
            var sb = new System.Text.StringBuilder();
            foreach (var w in words)
            {
                if (w.Length == 0) continue;
                sb.Append(char.ToUpper(w[0]));
                sb.Append(w.Substring(1).ToLower());
            }
            return sb.ToString();
        }
    }

}
