using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[InitializeOnLoad]
public class HierarchyPowerTools
{
    private static List<int> selectionHistory = new List<int>();
    private const int MaxHistory = 7;
    private static int tabIndex = -1;

    // --- Variables de Estado (Preview) ---
    private static int lastHoveredID = -1;
    private static bool lastStateWasActive;
    private static GameObject lastHoveredObj;

    // --- Variables de Animación (Focus) ---
    private static Vector3 targetPivot;
    private static Vector3 startPivot;
    private static bool isAnimatingPivot = false;
    private static float animationStartTime;
    private const float AnimationDuration = 0.12f;

    // --- Variables para el Bounding Box y Crosshair ---
    private static GameObject objectToHighlight;
    private static bool isMiddleDragging = false;
    private static bool isMouseInHierarchy = false;
    private static List<int> draggedObjectsSession = new List<int>();

    static HierarchyPowerTools()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnGUI;
        Selection.selectionChanged += UpdateHistory;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void UpdateHistory()
    {
        int currentID = Selection.activeInstanceID;
        if (currentID == 0) return;
        if (selectionHistory.Contains(currentID)) selectionHistory.Remove(currentID);
        selectionHistory.Insert(0, currentID);
        if (selectionHistory.Count > MaxHistory) selectionHistory.RemoveAt(selectionHistory.Count - 1);
        tabIndex = -1;
    }

    private static void OnGUI(int instanceID, Rect selectionRect)
    {
        GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (obj == null) return;

        Event e = Event.current;

        isMouseInHierarchy = EditorWindow.mouseOverWindow != null &&
                             EditorWindow.mouseOverWindow.GetType().Name == "SceneHierarchyWindow";

        bool isHovering = selectionRect.Contains(e.mousePosition);

        // --- GESTIÓN DE HIGHLIGHT ---
        if (!isMouseInHierarchy) objectToHighlight = null;
        else if (isHovering) objectToHighlight = obj;

        // --- LÓGICA DE BARRIDO CON CONTROL ---
        if (isMouseInHierarchy)
        {
            if (e.control)
            {
                if (isHovering && !obj.activeSelf && !draggedObjectsSession.Contains(instanceID))
                {
                    Undo.RecordObject(obj, "Quick Toggle");
                    obj.SetActive(true);
                    draggedObjectsSession.Add(instanceID);
                }
            }
            else if (draggedObjectsSession.Count > 0)
            {
                foreach (int id in draggedObjectsSession)
                {
                    GameObject dragObj = EditorUtility.InstanceIDToObject(id) as GameObject;
                    if (dragObj != null) dragObj.SetActive(false);
                }
                draggedObjectsSession.Clear();
                EditorApplication.RepaintHierarchyWindow();
            }
        }

        // --- LÓGICA TECLA F ---
        if (isHovering && isMouseInHierarchy && e.type == EventType.KeyDown && e.keyCode == KeyCode.F)
        {
            FocusObject(obj);
            e.Use();
        }

        // --- LÓGICA TAB ---
        if (isMouseInHierarchy && e.type == EventType.KeyDown && e.keyCode == KeyCode.Tab)
        {
            CycleSelection();
            e.Use();
        }

        // --- PREVIEW CON BOTÓN CENTRAL ---
        if (e.type == EventType.Repaint)
        {
            HandleControlPreview(instanceID, obj, isHovering && isMouseInHierarchy, isMiddleDragging);
        }

        if (isHovering && e.type == EventType.MouseDown && e.button == 2)
        {
            isMiddleDragging = true;
            e.Use();
        }
        if (e.type == EventType.MouseUp && e.button == 2)
        {
            isMiddleDragging = false;
            RestorePreview();
            e.Use();
        }

        DrawUI(instanceID, obj, selectionRect);
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!isMouseInHierarchy || objectToHighlight == null) return;

        Bounds b = GetBounds(objectToHighlight);
        Vector3 c = b.center;

        // --- 1. LÍNEAS INFINITAS (CROSSHAIR) ---
        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
        Handles.color = new Color(1f, 1f, 0f, 0.5f); // Amarillo con algo de transparencia para no molestar tanto

        float infinite = 100000f;
        // Línea Horizontal (Eje X)
        Handles.DrawLine(c + Vector3.left * infinite, c + Vector3.right * infinite);
        // Línea Vertical (Eje Y)
        Handles.DrawLine(c + Vector3.up * infinite, c + Vector3.down * infinite);
        // Línea de Profundidad (Eje Z) - Opcional, pero útil para 3D
        Handles.DrawLine(c + Vector3.forward * infinite, c + Vector3.back * infinite);

        // --- 2. RECUADRO RELLENO ---
        Vector3 ext = b.extents;
        Vector3[] v = new Vector3[] {
            c + new Vector3(-ext.x, -ext.y, -ext.z), c + new Vector3(ext.x, -ext.y, -ext.z),
            c + new Vector3(ext.x, ext.y, -ext.z), c + new Vector3(-ext.x, ext.y, -ext.z),
            c + new Vector3(-ext.x, -ext.y, ext.z), c + new Vector3(ext.x, -ext.y, ext.z),
            c + new Vector3(ext.x, ext.y, ext.z), c + new Vector3(-ext.x, ext.y, ext.z)
        };

        Handles.color = new Color(1f, 1f, 0f, 0.2f);
        Handles.DrawAAConvexPolygon(v[0], v[1], v[2], v[3]);
        Handles.DrawAAConvexPolygon(v[4], v[5], v[6], v[7]);
        Handles.DrawAAConvexPolygon(v[0], v[1], v[5], v[4]);
        Handles.DrawAAConvexPolygon(v[2], v[3], v[7], v[6]);
        Handles.DrawAAConvexPolygon(v[0], v[4], v[7], v[3]);
        Handles.DrawAAConvexPolygon(v[1], v[5], v[6], v[2]);

        // --- 3. BORDE WIREFRAME ---
        Handles.color = new Color(1f, 1f, 0f, 1f);
        Handles.DrawWireCube(b.center, b.size);

        sceneView.Repaint();
    }

    private static Bounds GetBounds(GameObject obj)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt != null)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Bounds bounds = new Bounds(corners[0], Vector3.zero);
            for (int i = 1; i < 4; i++) bounds.Encapsulate(corners[i]);
            return bounds;
        }
        Renderer r = obj.GetComponentInChildren<Renderer>();
        if (r != null) return r.bounds;
        return new Bounds(obj.transform.position, Vector3.one * 0.5f);
    }

    private static void FocusObject(GameObject obj)
    {
        if (SceneView.lastActiveSceneView == null) return;
        targetPivot = GetBounds(obj).center;
        startPivot = SceneView.lastActiveSceneView.pivot;
        animationStartTime = (float)EditorApplication.timeSinceStartup;
        if (!isAnimatingPivot) { isAnimatingPivot = true; EditorApplication.update += AnimateCamera; }
    }

    private static void AnimateCamera()
    {
        if (SceneView.lastActiveSceneView == null || !isAnimatingPivot) { StopAnimation(); return; }
        float t = Mathf.Clamp01(((float)EditorApplication.timeSinceStartup - animationStartTime) / AnimationDuration);
        float easedT = t * t * (3f - 2f * t);
        SceneView.lastActiveSceneView.pivot = Vector3.Lerp(startPivot, targetPivot, easedT);
        SceneView.lastActiveSceneView.Repaint();
        if (t >= 1f) StopAnimation();
    }

    private static void StopAnimation() { isAnimatingPivot = false; EditorApplication.update -= AnimateCamera; }

    private static void HandleControlPreview(int instanceID, GameObject obj, bool isHovering, bool isTriggered)
    {
        if (isHovering && isTriggered)
        {
            if (lastHoveredID != instanceID)
            {
                RestorePreview();
                lastHoveredID = instanceID;
                lastHoveredObj = obj;
                lastStateWasActive = obj.activeSelf;
                if (!lastStateWasActive) obj.SetActive(true);
            }
        }
        else if (lastHoveredID == instanceID && (!isHovering || !isTriggered)) { RestorePreview(); }
    }

    private static void RestorePreview()
    {
        if (lastHoveredObj != null)
        {
            if (lastHoveredObj.activeSelf != lastStateWasActive) lastHoveredObj.SetActive(lastStateWasActive);
            lastHoveredObj = null; lastHoveredID = -1;
        }
    }

    private static void CycleSelection()
    {
        if (selectionHistory.Count == 0) return;
        tabIndex = (tabIndex + 1) % selectionHistory.Count;
        Selection.activeInstanceID = selectionHistory[tabIndex];
        EditorGUIUtility.PingObject(Selection.activeInstanceID);
    }

    private static void DrawUI(int instanceID, GameObject obj, Rect rect)
    {
        // --- 1. COLUMNA DE HISTORIAL (DERECHA) ---
        int hIndex = selectionHistory.IndexOf(instanceID);
        if (hIndex != -1)
        {
            GUIStyle historyStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };

            if (hIndex == 0)
            {
                GUI.color = new Color(1f, 1f, 0f, 1f); // Amarillo Patito Chillón
                historyStyle.fontSize = 17;
            }
            else
            {
                float t = (float)(hIndex - 1) / (MaxHistory - 1);
                // Azul eléctrico a azul muy lavado
                GUI.color = Color.Lerp(new Color(0f, 0.4f, 1f, 0.7f), new Color(0f, 0.2f, 0.6f, 0.1f), t);
                historyStyle.fontSize = 12;
            }

            GUI.Label(new Rect(rect.xMax - 18, rect.y, 18, rect.height), "●", historyStyle);
        }

        // --- 2. TOGGLE DE ACTIVIDAD (IZQUIERDA) ---
        Rect toggleRect = new Rect(rect.x - 28, rect.y, 20, rect.height);

        // Cursor de manito para feedback de click
        EditorGUIUtility.AddCursorRect(toggleRect, MouseCursor.Link);

        bool selfActive = obj.activeSelf;
        bool inHierarchy = obj.activeInHierarchy;
        bool isChild = obj.transform.parent != null;

        // --- CONFIGURACIÓN DE COLOR LAVADO ---
        // En lugar de negro (0,0,0), usamos un gris carbón (0.15) para que no sea tan "duro"
        Color charcoal = new Color(0.15f, 0.15f, 0.15f);
        Color greyBase = new Color(0.4f, 0.4f, 0.4f);

        float finalAlpha = 1f;
        Color finalRGB = charcoal;

        if (selfActive)
        {
            finalRGB = charcoal;
            // Reducimos el alpha para que no sea tan fuerte (0.7 para raíz, 0.4 para hijos)
            finalAlpha = isChild ? 0.4f : 0.7f;
        }
        else
        {
            finalRGB = greyBase;
            // Apagado es mucho más sutil
            finalAlpha = isChild ? 0.15f : 0.3f;
        }

        GUI.color = new Color(finalRGB.r, finalRGB.g, finalRGB.b, finalAlpha);

        GUIStyle toggleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };

        // ● = Estado propio / ○ = Estado heredado (padre apagado)
        string icon = (selfActive == inHierarchy) ? "●" : "○";

        if (GUI.Button(toggleRect, icon, toggleStyle))
        {
            Undo.RecordObject(obj, "Toggle Active State");
            obj.SetActive(!selfActive);
            EditorApplication.RepaintHierarchyWindow();
        }

        GUI.color = Color.white; // Limpieza de estado de color
    }
}