using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

#region 1. ATRIBUTOS Y ESTADO GLOBAL
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class DisplayStaticInInspector : Attribute { }

public static class UDV_State
{
    public static HashSet<int> ActiveInstances = new HashSet<int>();
    public static Dictionary<string, object[]> MethodArgsStore = new Dictionary<string, object[]>();
    public static bool ClearConsoleOnRun = true;
    public static bool HistoryGlobalMonitor = false;
    public static bool HistoryDebugConsole = false;
    public static int HistoryMaxLimit = 20;
    public static int HistoryMacroLimit = 5; // <--- AGREGADA
    public static int HistoryMicroLimit = 5; // <--- AGREGADA
    public static bool IsolationMode = false;
    public static List<GameObject> HiddenObjects = new List<GameObject>();
}
#endregion

[CustomEditor(typeof(MonoBehaviour), true)]
[CanEditMultipleObjects]
public class UniversalDataViewerEditor : Editor
{
    #region 2. ESTILOS UI
    private static readonly Color ColorAccentData = new Color(0.7f, 0.4f, 0.9f, 1f);
    private static readonly Color ColorAccentVoids = new Color(0.4f, 0.8f, 0.5f, 1f);
    private static readonly Color ColorAccentHistory = new Color(1f, 0.6f, 0.1f, 1f);
    private static readonly Color ColorAccentRefs = new Color(0.3f, 0.9f, 1f, 1f);
    private static readonly Color ColorSectionBg = new Color(0.14f, 0.14f, 0.14f, 1f);
    #endregion

    #region 3. CAMPOS Y CACHE
    private Dictionary<string, bool> foldouts = new Dictionary<string, bool>();
    private List<CachedFieldInfo> cachedFields = new List<CachedFieldInfo>();
    private List<Type> cachedStaticClasses = new List<Type>();
    private List<MethodInfo> cachedMethods = new List<MethodInfo>();
    private List<ChangeRecord> changeHistory = new List<ChangeRecord>();
    private Dictionary<string, object> lastKnownValues = new Dictionary<string, object>();
    private Dictionary<string, MethodCallRecord> callMap = new Dictionary<string, MethodCallRecord>();
    private List<DetectiveResult> detectiveResults = new List<DetectiveResult>();

    private Vector2 mainScroll;
    private bool isCacheInitialized, historyWasActive;
    private int selectedMethodIndex = -1;
    private string injectCode = "", injectStatus = "Ready";

    private List<ChangeRecord> historyToDraw = new List<ChangeRecord>();



    private class CachedFieldInfo { public string Name; public Type Type; public FieldInfo FieldInfo; public bool IsStatic, IsSpecial; }
    
    private class MethodCallRecord { public string methodName, callerMethod; public int count; public DateTime lastExecuted; public float Age => (float)(DateTime.Now - lastExecuted).TotalSeconds; }


    private class SubChange
    {
        public string oldValue, newValue;
        public DateTime time;
        public string stackTrace;
    }

    private class ChangeRecord
    {
        public string summary, className, varName;
        public int line;
        public List<int> assignmentLines = new List<int>();
        public List<SubChange> microHistory = new List<SubChange>();
        public DateTime lastTime;
        public bool isExpanded;

        // Propiedades de conveniencia para compatibilidad con DebugStackInfo
        public string oldValue => microHistory.Count > 0 ? microHistory[0].oldValue : "N/A";
        public string newValue => microHistory.Count > 0 ? microHistory[0].newValue : "N/A";
        public DateTime time => lastTime;
    }

    // En UDV_State (Región 1) actualiza/añade:
    public static int HistoryMacroLimit = 5;
    public static int HistoryMicroLimit = 5;
    #endregion

    #region 4. ON INSPECTOR GUI (MASTER)
    public override void OnInspectorGUI()
    {
        // 1. SINCRONIZACIÓN DE DATOS (Solo en Layout para evitar el bug de controles)
        if (Event.current.type == EventType.Layout)
        {
            if (GetFold("m_history")) MonitorChanges();
            historyToDraw = new List<ChangeRecord>(changeHistory);
        }

        DrawDefaultInspector();
        int targetID = target.GetInstanceID();
        bool isActive = UDV_State.ActiveInstances.Contains(targetID);

        EditorGUILayout.Space(12);
        DrawMasterHeader(targetID, isActive);

        if (!isActive) return;
        if (!isCacheInitialized) InitializeCache();

        DrawTabs();

        mainScroll = EditorGUILayout.BeginScrollView(mainScroll, GUILayout.MaxHeight(850));

        if (GetFold("m_data")) DrawDataModule();
        if (GetFold("m_voids")) DrawVoidsModule();
        if (GetFold("m_static")) DrawStaticModule();
        if (GetFold("m_history")) DrawHistoryModule();
        if (GetFold("m_refs")) DrawRefsModule();
        if (GetFold("m_calls")) DrawCallsModule();
        if (GetFold("m_inject")) DrawInjectModule();

        EditorGUILayout.EndScrollView();

        if (Application.isPlaying) Repaint();
    }

   

    private void DrawMasterHeader(int id, bool active)
    {
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = active ? new Color(1f, 0.4f, 0.4f) : ColorAccentVoids;
        if (GUILayout.Button(active ? "● HIDE VIEWER" : $"○ SHOW VIEWER ({UDV_State.ActiveInstances.Count})", GUILayout.Height(35)))
        {
            if (active) UDV_State.ActiveInstances.Remove(id);
            else UDV_State.ActiveInstances.Add(id);
            isCacheInitialized = false;
        }
        GUI.backgroundColor = Color.white;
        if (UDV_State.ActiveInstances.Count > 0 && GUILayout.Button("OFF ALL", GUILayout.Width(75), GUILayout.Height(35)))
        {
            UDV_State.ActiveInstances.Clear();
            isCacheInitialized = false;
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTabs()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        string[] labels = { "Data", "Voids", "Static", "History", "Refs", "Calls", "Inject" };
        string[] keys = { "m_data", "m_voids", "m_static", "m_history", "m_refs", "m_calls", "m_inject" };
        for (int i = 0; i < labels.Length; i++)
            SetFold(keys[i], GUILayout.Toggle(GetFold(keys[i]), labels[i], EditorStyles.toolbarButton));
        EditorGUILayout.EndHorizontal();
    }
    #endregion

    #region 5. MÓDULO DATA (RECURSIVIDAD PARA DICCIONARIO MAESTRO)
    private void DrawDataModule()
    {
        if (!DrawSectionHeader("📊 DATA EXPLORER", ColorAccentData, "m_data")) return;
        var advanced = cachedFields.Where(f => f.IsSpecial && !f.IsStatic).ToList();
        if (advanced.Count > 0)
        {
            foreach (var f in advanced)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                object val = f.FieldInfo.GetValue(target);
                DrawMemberLogic(f.Name, f.Type, val, "data_" + f.Name, 0);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }
        }
        else DrawNoDataLabel();
        EditorGUILayout.EndVertical();
    }

    private void DrawMemberLogic(string label, Type type, object val, string key, int depth)
    {
        if (depth > 15) return;
        if (val == null) { EditorGUILayout.LabelField($"{label}: null", EditorStyles.centeredGreyMiniLabel); return; }

        if (val is IDictionary dict) DrawDictionaryUI(label, dict, type, key + "_d", depth);
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Stack<>)) DrawStackUI(label, val, type, key + "_s");
        else if (val is IList list && !(val is string)) DrawListUI(label, list, type, key + "_l", depth);
        else if (type.IsClass && type != typeof(string) && !typeof(UnityEngine.Object).IsAssignableFrom(type)) DrawComplexObjectUI(val, depth, label, key + "_c");
        else DrawUniversalEditor(label, val, type, (nv) => { });
    }

    // Cache temporal para las nuevas entradas de diccionario (para no perder el foco al escribir)
    private static Dictionary<string, (object key, object val)> dictBuffers = new Dictionary<string, (object, object)>();

    private void DrawDictionaryUI(string label, IDictionary dict, Type type, string key, int depth)
    {
        bool folded = EditorGUILayout.Foldout(GetFold(key), $"🗂️ {label} [Dict: {dict.Count}]", true, new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold });
        SetFold(key, folded);

        if (folded)
        {
            EditorGUI.indentLevel++;
            Type keyType = type.GetGenericArguments()[0];
            Type valType = type.GetGenericArguments()[1];

            // --- SECCIÓN: AÑADIR NUEVO ELEMENTO ---
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("➕ Add New Entry", EditorStyles.miniBoldLabel);

            if (!dictBuffers.ContainsKey(key)) dictBuffers[key] = (null, null);
            var buffer = dictBuffers[key];

            buffer.key = DrawUniversalEditor("New Key", buffer.key, keyType, (nk) => {
                var b = dictBuffers[key]; b.key = nk; dictBuffers[key] = b;
            });

            buffer.val = DrawUniversalEditor("New Value", buffer.val, valType, (nv) => {
                var b = dictBuffers[key]; b.val = nv; dictBuffers[key] = b;
            });

            if (GUILayout.Button("Add to Dictionary", EditorStyles.miniButton))
            {
                if (buffer.key != null && !dict.Contains(buffer.key))
                {
                    dict.Add(buffer.key, buffer.val);
                    Debug.Log($"<color=#7f66ff><b>[UDV DICT]</b></color> Added Key: <b>{buffer.key}</b> to <i>{label}</i>");
                    dictBuffers[key] = (null, null);
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);

            // --- SECCIÓN: LISTADO Y EDICIÓN ---
            var keys = dict.Keys.Cast<object>().ToList();
            foreach (var k in keys)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();

                if (typeof(UnityEngine.Object).IsAssignableFrom(keyType))
                    EditorGUILayout.ObjectField((UnityEngine.Object)k, keyType, true, GUILayout.Width(150));
                else
                    EditorGUILayout.LabelField($"Key: {k}", EditorStyles.miniBoldLabel);

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    dict.Remove(k);
                    Debug.Log($"<color=#ff4444><b>[UDV DICT]</b></color> Removed Key: <b>{k}</b>");
                    break;
                }
                EditorGUILayout.EndHorizontal();

                // IMPORTANTE: Aquí inyectamos la lógica de actualización real
                object currentValue = dict[k];
                DrawMemberLogicWithCallback("Value", valType, currentValue, key + k.GetHashCode(), depth + 1, (newValue) =>
                {
                    if (currentValue != newValue)
                    {
                        dict[k] = newValue;
                        Debug.Log($"<color=#4CAF50><b>[UDV UPDATE]</b></color> Dict: <i>{label}</i> | Key: <b>{k}</b> | New Value: <b>{newValue}</b>");
                    }
                });

                EditorGUILayout.EndVertical();
            }
            EditorGUI.indentLevel--;
        }
    }

    // Helper necesario para que la recursividad soporte el guardado de datos
    private void DrawMemberLogicWithCallback(string label, Type type, object val, string key, int depth, Action<object> onValueUpdated)
    {
        if (depth > 15) return;
        if (val == null)
        {
            // Si es nulo pero queremos asignar algo (como un objeto de Unity)
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                object nv = DrawUniversalEditor(label, null, type, onValueUpdated);
                if (nv != null) onValueUpdated?.Invoke(nv);
            }
            else EditorGUILayout.LabelField($"{label}: null", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        if (val is IDictionary dict) DrawDictionaryUI(label, dict, type, key + "_d", depth);
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Stack<>)) DrawStackUI(label, val, type, key + "_s");
        else if (val is IList list && !(val is string)) DrawListUI(label, list, type, key + "_l", depth);
        else if (type.IsClass && type != typeof(string) && !typeof(UnityEngine.Object).IsAssignableFrom(type)) DrawComplexObjectUI(val, depth, label, key + "_c");
        else DrawUniversalEditor(label, val, type, onValueUpdated); // Aquí el callback ya no es vacío
    }

    private void DrawListUI(string label, IList list, Type type, string key, int depth)
    {
        SetFold(key, EditorGUILayout.Foldout(GetFold(key), $"📜 {label} [List/Array: {list.Count}]", true));
        if (GetFold(key))
        {
            EditorGUI.indentLevel++;
            Type elemType = type.IsArray ? type.GetElementType() : (type.IsGenericType ? type.GetGenericArguments()[0] : typeof(object));
            for (int i = 0; i < list.Count; i++)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"[{i}]", EditorStyles.miniLabel, GUILayout.Width(40));
                if (GUILayout.Button("X", GUILayout.Width(25))) { list.RemoveAt(i); break; }
                EditorGUILayout.EndHorizontal();
                DrawMemberLogic("Item", elemType, list[i], key + i, depth + 1);
                EditorGUILayout.EndVertical();
            }
            if (!type.IsArray && GUILayout.Button("+ Add Element", EditorStyles.miniButton))
            {
                object newObj = elemType == typeof(string) ? "" : (elemType.IsValueType ? Activator.CreateInstance(elemType) : null);
                list.Add(newObj);
            }
            EditorGUI.indentLevel--;
        }
    }

    private void DrawComplexObjectUI(object obj, int depth, string label, string key)
    {
        SetFold(key, EditorGUILayout.Foldout(GetFold(key), $"◆ {label} ({obj.GetType().Name})", true));
        if (GetFold(key))
        {
            EditorGUI.indentLevel++;
            var fields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var f in fields)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                DrawMemberLogic(f.Name, f.FieldType, f.GetValue(obj), key + f.Name, depth + 1);
                EditorGUILayout.EndVertical();
            }
            EditorGUI.indentLevel--;
        }
    }

    private void DrawStackUI(string label, object stack, Type type, string key)
    {
        var items = ((IEnumerable)stack).Cast<object>().ToList();
        SetFold(key, EditorGUILayout.Foldout(GetFold(key), $"🥞 {label} [Stack: {items.Count}]", true));
        if (GetFold(key))
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("POP", GUILayout.Width(70))) type.GetMethod("Pop").Invoke(stack, null);
            if (GUILayout.Button("CLEAR", GUILayout.Width(70))) type.GetMethod("Clear").Invoke(stack, null);
            EditorGUILayout.EndHorizontal();
            foreach (var item in items) EditorGUILayout.LabelField("-> " + item?.ToString() ?? "null");
        }
    }
    #endregion

    #region 6. MÓDULO VOIDS (ORDENADO Y PARAMETRIZADO)
    private void DrawVoidsModule()
    {
        if (!DrawSectionHeader("⚙️ VOID EXECUTION", ColorAccentVoids, "m_voids")) return;

        EditorGUILayout.BeginHorizontal();
        UDV_State.ClearConsoleOnRun = EditorGUILayout.ToggleLeft(" Clear Console", UDV_State.ClearConsoleOnRun, GUILayout.Width(110));
        if (GUILayout.Button("Refrescar Métodos", EditorStyles.miniButton, GUILayout.Width(120))) InitializeCache();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(5);

        if (cachedMethods.Count == 0) { DrawNoDataLabel(); EditorGUILayout.EndVertical(); return; }

        // Ordenar alfabéticamente por nombre
        var sortedMethods = cachedMethods.OrderBy(m => m.Name).ToList();

        string currentLabel = selectedMethodIndex >= 0 ? GetMethodDisplayName(cachedMethods[selectedMethodIndex]) : "Seleccionar Función...";

        if (EditorGUILayout.DropdownButton(new GUIContent(currentLabel), FocusType.Keyboard, GUILayout.Height(30)))
        {
            GenericMenu menu = new GenericMenu();
            foreach (var m in sortedMethods)
            {
                int realIndex = cachedMethods.IndexOf(m);
                menu.AddItem(new GUIContent(GetMethodDisplayName(m)), realIndex == selectedMethodIndex, () => selectedMethodIndex = realIndex);
            }
            menu.ShowAsContext();
        }

        if (selectedMethodIndex >= 0 && selectedMethodIndex < cachedMethods.Count)
        {
            var m = cachedMethods[selectedMethodIndex];
            var ps = m.GetParameters();
            string mKey = target.GetInstanceID() + "_" + m.Name + "_" + ps.Length;

            if (!UDV_State.MethodArgsStore.ContainsKey(mKey)) UDV_State.MethodArgsStore[mKey] = new object[ps.Length];
            object[] args = UDV_State.MethodArgsStore[mKey];

            if (ps.Length > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Parámetros requeridos:", EditorStyles.miniBoldLabel);
                for (int i = 0; i < ps.Length; i++)
                {
                    // Dibujamos la entrada específica según el tipo
                    args[i] = DrawUniversalEditor($"{ps[i].Name} ({ps[i].ParameterType.Name})", args[i], ps[i].ParameterType, (nv) => args[i] = nv);
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(5);
            GUI.backgroundColor = ColorAccentVoids;
            if (GUILayout.Button($"▶ EJECUTAR: {m.Name.ToUpper()}", GUILayout.Height(40)))
            {
                ExecuteVoidWithSafety(m, args);
            }
            GUI.backgroundColor = Color.white;
        }
        EditorGUILayout.EndVertical();
    }
    #endregion

    #region 7. MÓDULO HISTORY (SISTEMA DE MONITOREO COMPLETO)
    private void DrawHistoryModule()
    {
        if (!DrawSectionHeader("📜 DEEP HISTORY MONITOR", ColorAccentHistory, "m_history")) return;

        ChangeRecord toRemove = null;

        try
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            UDV_State.HistoryGlobalMonitor = EditorGUILayout.ToggleLeft(" GO Monitor", UDV_State.HistoryGlobalMonitor, GUILayout.Width(95));
            UDV_State.HistoryDebugConsole = EditorGUILayout.ToggleLeft(" Log Console", UDV_State.HistoryDebugConsole, GUILayout.Width(95));
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Macro:", GUILayout.Width(45));
            UDV_State.HistoryMacroLimit = EditorGUILayout.IntField(UDV_State.HistoryMacroLimit, GUILayout.Width(30));
            EditorGUILayout.LabelField("Micro:", GUILayout.Width(45));
            UDV_State.HistoryMicroLimit = EditorGUILayout.IntField(UDV_State.HistoryMicroLimit, GUILayout.Width(30));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Width(50))) { changeHistory.Clear(); historyToDraw.Clear(); }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            if (historyToDraw.Count == 0) { DrawNoDataLabel(); return; }

            // Ordenamos la lista congelada para el dibujo
            var sortedMacro = historyToDraw.OrderByDescending(x => x.lastTime).ToList();

            foreach (var log in sortedMacro)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                GUIStyle headStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11, normal = { textColor = ColorAccentHistory } };
                log.isExpanded = EditorGUILayout.Foldout(log.isExpanded, $"{log.className} > {log.varName}", true, headStyle);

                if (GUILayout.Button("📋", GUILayout.Width(25))) CopyHistoryToClipboard(log);
                if (GUILayout.Button("X", GUILayout.Width(20))) toRemove = log;
                EditorGUILayout.EndHorizontal();

                if (log.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    if (GUILayout.Button($"Ir a Declaración (L{log.line})", EditorStyles.miniButton))
                        OpenScriptAtLine(log.className, log.line);

                    var microSnapshot = log.microHistory.ToList();
                    for (int j = 0; j < microSnapshot.Count; j++)
                    {
                        var sub = microSnapshot[j];
                        EditorGUILayout.BeginVertical(EditorStyles.textArea);
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"{sub.time:HH:mm:ss}", EditorStyles.miniLabel, GUILayout.Width(55));

                        GUIStyle diffStyle = new GUIStyle(EditorStyles.miniLabel) { richText = true, wordWrap = true };
                        EditorGUILayout.LabelField($"<color=#FF5555>{sub.oldValue}</color> ➜ <color=#55FF55>{sub.newValue}</color>", diffStyle);

                        if (GUILayout.Button("Stack", EditorStyles.miniButton, GUILayout.Width(45))) DebugStackInfo(log, sub);

                        if (log.assignmentLines != null && log.assignmentLines.Count > 0)
                        {
                            GUI.color = new Color(0.7f, 1f, 0.7f);
                            foreach (int lineNum in log.assignmentLines)
                            {
                                if (GUILayout.Button($"L{lineNum}", EditorStyles.miniButton, GUILayout.Width(35)))
                                    OpenScriptAtLine(log.className, lineNum);
                            }
                            GUI.color = Color.white;
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();
            }
        }
        finally
        {
            EditorGUILayout.EndVertical(); // Cierre del SectionHeader
        }

        if (toRemove != null) changeHistory.Remove(toRemove);
    }
    #endregion

    #region 7.5 MODULO HISTORY EXTENSION
    private void MonitorChanges()
    {
        // Solo procesamos cambios durante Layout para evitar errores de GUI
        if (Event.current.type != EventType.Layout || !Application.isPlaying) return;

        Component[] targets = UDV_State.HistoryGlobalMonitor ? ((Component)target).GetComponents<Component>() : new[] { (Component)target };

        foreach (var comp in targets)
        {
            if (comp == null) continue;
            var fields = comp.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var f in fields)
            {
                string key = comp.GetType().Name + "." + f.Name;
                object rawVal = f.GetValue(comp);
                string cur = rawVal != null ? rawVal.ToString() : "null";

                if (!lastKnownValues.ContainsKey(key)) { lastKnownValues[key] = cur; continue; }

                string oldVal = lastKnownValues[key].ToString();
                if (oldVal != cur)
                {
                    UpdateHistoryRecord(comp, f.Name, oldVal, cur);
                    lastKnownValues[key] = cur;
                }
            }
        }
    }

    private void UpdateHistoryRecord(Component comp, string fieldName, string oldV, string newV)
    {
        string className = comp.GetType().Name;
        var existing = changeHistory.FirstOrDefault(x => x.className == className && x.varName == fieldName);

        if (existing == null)
        {
            existing = new ChangeRecord
            {
                className = className,
                varName = fieldName,
                line = GetLineOfField(comp, fieldName),
                assignmentLines = FindAssignmentLines(comp, fieldName), // Escaneamos el script una sola vez
                isExpanded = true,
                summary = className + "." + fieldName
            };
            changeHistory.Insert(0, existing);
            if (changeHistory.Count > UDV_State.HistoryMacroLimit) changeHistory.RemoveAt(changeHistory.Count - 1);
        }

        var sub = new SubChange
        {
            oldValue = oldV,
            newValue = newV,
            time = DateTime.Now,
            stackTrace = "Cambio detectado en: " + className + "." + fieldName
        };

        existing.microHistory.Insert(0, sub);
        existing.lastTime = sub.time;

        if (existing.microHistory.Count > UDV_State.HistoryMicroLimit)
            existing.microHistory.RemoveAt(existing.microHistory.Count - 1);

        if (UDV_State.HistoryDebugConsole) DebugStackInfo(existing, sub);
    }

    private void DebugStackInfo(ChangeRecord macro, SubChange micro = null)
    {
        var targetSub = micro ?? (macro.microHistory.Count > 0 ? macro.microHistory[0] : null);
        if (targetSub == null) return;

        // Limpiar el stack de llamadas de Unity/Sistema
        string[] lines = targetSub.stackTrace.Split('\n');
        StringBuilder sb = new StringBuilder();
        foreach (var line in lines)
        {
            if (line.Contains("UnityEngine") || line.Contains("UnityEditor") || line.Contains("System.Environment")) continue;
            if (string.IsNullOrWhiteSpace(line)) continue;
            sb.AppendLine("  > " + line.Trim());
        }

        string report = $"<color=#FF6600><b>[UDV HISTORY]</b></color> <b>{macro.varName}</b> modificado en <b>{macro.className}</b>\n" +
                        $"De: {targetSub.oldValue} ➜ A: {targetSub.newValue}\n" +
                        $"Hora: {targetSub.time:HH:mm:ss.fff}\n" +
                        $"<b>PILA DE LLAMADAS (STACK):</b>\n{sb}";

        Debug.Log(report);
    }

    private List<int> FindAssignmentLines(Component c, string fName)
    {
        List<int> linesFound = new List<int>();
        try
        {
            var ms = MonoScript.FromMonoBehaviour(c as MonoBehaviour);
            if (!ms) return linesFound;

            string path = AssetDatabase.GetAssetPath(ms);
            if (!File.Exists(path)) return linesFound;

            string[] lines = File.ReadAllLines(path);

            // Regex: Busca la palabra completa de la variable seguida de un '='
            // Ignora si la línea empieza con // (comentario)
            string pattern = @"^\s*(?!\/\/).*\b" + fName + @"\s*(\+|-|\*|/)?=[^=]";

            for (int i = 0; i < lines.Length; i++)
            {
                if (Regex.IsMatch(lines[i], pattern))
                {
                    linesFound.Add(i + 1);
                }
            }
        }
        catch { }
        return linesFound;
    }

    private void CopyHistoryToClipboard(ChangeRecord log)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"HISTORIAL DE CAMBIOS: {log.className}.{log.varName}");
        foreach (var sub in log.microHistory)
        {
            sb.AppendLine($"[{sub.time:HH:mm:ss}] {sub.oldValue} -> {sub.newValue}");
        }
        EditorGUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log("Historial copiado al portapapeles.");
    }
    #endregion

    #region 8. MÓDULO REFS (UI Y CÓDIGO)
    private void DrawRefsModule()
    {
        if (!DrawSectionHeader("🔍 REFERENCE DETECTIVE PRO", ColorAccentRefs, "m_refs")) return;
        if (GUILayout.Button("SCAN DEEP REFERENCES", GUILayout.Height(35))) PerformDetectiveSearch();

        foreach (var res in detectiveResults)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            res.isExpanded = EditorGUILayout.Foldout(res.isExpanded, $"{res.owner.name} ({res.sourceComponent.GetType().Name})", true);
            if (GUILayout.Button("Go", GUILayout.Width(45))) Selection.activeGameObject = res.owner;
            EditorGUILayout.EndHorizontal();

            if (res.isExpanded)
            {
                EditorGUI.indentLevel++;
                if (res.isUIEvent) EditorGUILayout.HelpBox($"UI: {res.uiMethodName}() en {res.uiTargetName}", MessageType.Info);
                foreach (var m in res.methods)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"ƒ {m.methodName}()", EditorStyles.boldLabel);
                    if (GUILayout.Button("Ir", GUILayout.Width(40))) OpenScriptAtLine(res.sourceComponent, m.startLine);
                    EditorGUILayout.EndHorizontal();
                    foreach (var l in m.lines)
                    {
                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button($"L{l.line}", GUILayout.Width(50))) OpenScriptAtLine(res.sourceComponent, l.line);
                        EditorGUILayout.LabelField(l.content.Trim(), EditorStyles.miniLabel);
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndVertical();
    }
    #endregion

    #region 9. STATIC, CALLS, INJECT
    private void DrawStaticModule()
    {
        if (!DrawSectionHeader("⚡ STATIC EXPLORER", ColorAccentData, "m_static")) return;
        foreach (var f in cachedFields.Where(f => f.IsStatic))
            DrawUniversalEditor(f.Name, f.FieldInfo.GetValue(null), f.Type, (nv) => f.FieldInfo.SetValue(null, nv));

        foreach (var t in cachedStaticClasses)
        {
            SetFold(t.Name, EditorGUILayout.Foldout(GetFold(t.Name), "🏛️ Class: " + t.Name, true));
            if (GetFold(t.Name))
            {
                EditorGUI.indentLevel++;
                foreach (var f in t.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    DrawUniversalEditor(f.Name, f.GetValue(null), f.FieldType, (nv) => f.SetValue(null, nv));
                foreach (var m in t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (m.IsSpecialName || m.DeclaringType == typeof(object)) continue;
                    if (GUILayout.Button("Call " + m.Name, GUILayout.Width(200))) m.Invoke(null, null);
                }
                EditorGUI.indentLevel--;
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawCallsModule()
    {
        if (!DrawSectionHeader("📡 CALL ANALYZER", ColorAccentRefs, "m_calls")) return;
        foreach (var call in callMap.Values.OrderByDescending(c => c.lastExecuted))
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"{call.methodName} (x{call.count})", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"{call.Age:F1}s ago | From: {call.callerMethod}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawInjectModule()
    {
        if (!DrawSectionHeader("💉 QUICK INJECTION", Color.red, "m_inject")) return;
        EditorGUILayout.BeginHorizontal();
        injectCode = EditorGUILayout.TextField(injectCode, GUILayout.Height(25));
        if (GUILayout.Button("EXEC", GUILayout.Width(60), GUILayout.Height(25))) ExecuteInject(injectCode);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField("Status: " + injectStatus);
        EditorGUILayout.EndVertical();
    }
    #endregion

    #region 10. HELPERS TÉCNICOS (REFLECTION)

    private void InitializeCache()
    {
        try
        {
            cachedFields.Clear(); cachedMethods.Clear(); cachedStaticClasses.Clear();
            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (var f in target.GetType().GetFields(flags))
                cachedFields.Add(new CachedFieldInfo { Name = f.Name, Type = f.FieldType, FieldInfo = f, IsStatic = f.IsStatic, IsSpecial = f.FieldType.IsGenericType || f.FieldType.IsArray });

            // Filtrar métodos: Solo los del script actual, ignorando Get/Set de propiedades y métodos base de Unity
            cachedMethods = target.GetType().GetMethods(flags)
                .Where(m => !m.IsSpecialName && m.DeclaringType != typeof(MonoBehaviour) && m.DeclaringType != typeof(Component) && m.DeclaringType != typeof(UnityEngine.Object) && m.DeclaringType != typeof(object))
                .ToList();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try { var types = assembly.GetTypes().Where(t => t.GetCustomAttributes(typeof(DisplayStaticInInspector), false).Any()); cachedStaticClasses.AddRange(types); } catch { }
            }
            isCacheInitialized = true;
        }
        catch (Exception e) { Debug.LogError($"[UDV Cache Error]: {e.Message}"); }
    }

    private string GetMethodDisplayName(MethodInfo m)
    {
        var ps = m.GetParameters();
        string paramsStr = string.Join(", ", ps.Select(p => $"{p.ParameterType.Name} {p.Name}"));
        string access = m.IsPublic ? "P" : "I";
        string scope = m.IsStatic ? "S" : "I";

        return $"{m.Name}({paramsStr}) | {access} | {scope}";
    }

    private void ExecuteVoidWithSafety(MethodInfo m, object[] args)
    {
        if (UDV_State.ClearConsoleOnRun) ClearConsole();

        try
        {
            RegisterCall(m.Name);
            object result = m.Invoke(m.IsStatic ? null : target, args);

            string resLog = result != null ? $" | Retorno: {result}" : "";
            Debug.Log($"<color=#4CAF50><b>[UDV EXEC]</b></color> Éxito al llamar a <b>{m.Name}</b>{resLog}");
        }
        catch (TargetInvocationException tie)
        {
            Debug.LogError($"<color=red><b>[UDV ERROR EN CÓDIGO]</b></color> El método <b>{m.Name}</b> lanzó una excepción:\n<color=yellow>{tie.InnerException?.Message}</color>\n{tie.InnerException?.StackTrace}");
        }
        catch (Exception e)
        {
            Debug.LogError($"<color=orange><b>[UDV ERROR REFLECTION]</b></color> No se pudo invocar <b>{m.Name}</b>. Motivo: {e.Message}");
        }
    }

    private object DrawUniversalEditor(string label, object val, Type t, Action<object> onCommit)
    {
        EditorGUILayout.BeginHorizontal();

        // Definimos que el campo de entrada ocupe el 60% del ancho para que haya espacio para el nombre después
        float inputWidth = EditorGUIUtility.currentViewWidth * 0.8f;

        object nv = val;
        EditorGUI.BeginChangeCheck();

        // Dibujamos el control SIN etiqueta (pasando un string vacío o directamente el valor)
        if (t == typeof(int)) nv = EditorGUILayout.IntField((int)(val ?? 0), GUILayout.Width(inputWidth));
        else if (t == typeof(float)) nv = EditorGUILayout.FloatField((float)(val ?? 0f), GUILayout.Width(inputWidth));
        else if (t == typeof(string)) nv = EditorGUILayout.TextField((string)(val ?? ""), GUILayout.Width(inputWidth));
        else if (t == typeof(bool)) nv = EditorGUILayout.Toggle((bool)(val ?? false), GUILayout.Width(inputWidth));
        else if (t.IsEnum) nv = EditorGUILayout.EnumPopup((Enum)(val ?? (Enum)Enum.ToObject(t, 0)), GUILayout.Width(inputWidth));
        else if (typeof(UnityEngine.Object).IsAssignableFrom(t)) nv = EditorGUILayout.ObjectField((UnityEngine.Object)val, t, true, GUILayout.Width(inputWidth));
        else if (t == typeof(Vector2)) nv = EditorGUILayout.Vector2Field("", (Vector2)(val ?? Vector2.zero), GUILayout.Width(inputWidth));
        else if (t == typeof(Vector3)) nv = EditorGUILayout.Vector3Field("", (Vector3)(val ?? Vector3.zero), GUILayout.Width(inputWidth));
        else if (t == typeof(Color)) nv = EditorGUILayout.ColorField((Color)(val ?? Color.white), GUILayout.Width(inputWidth));
        else
        {
            EditorGUILayout.SelectableLabel(val?.ToString() ?? "null", EditorStyles.miniLabel, GUILayout.Width(inputWidth), GUILayout.Height(18));
        }

        // Dibujamos el nombre (label) DESPUÉS del campo
        GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
        EditorGUILayout.LabelField($"← {label}", labelStyle);

        if (EditorGUI.EndChangeCheck()) { onCommit?.Invoke(nv); }

        EditorGUILayout.EndHorizontal();
        return nv;
    }

    private bool GetFold(string k) => foldouts.ContainsKey(k) && foldouts[k];
    private void SetFold(string k, bool v) => foldouts[k] = v;
    private void ClearConsole() { var log = Type.GetType("UnityEditor.LogEntries, UnityEditor.dll"); log.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public).Invoke(null, null); }
    private void RegisterCall(string n) { if (!callMap.ContainsKey(n)) callMap[n] = new MethodCallRecord { methodName = n }; callMap[n].count++; callMap[n].lastExecuted = DateTime.Now; callMap[n].callerMethod = "Manual Execute"; }



    private void TakeSnapshot() { foreach (var f in target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) lastKnownValues[target.GetType().Name + "." + f.Name] = f.GetValue(target)?.ToString() ?? "null"; }
    private void OpenScriptAtLine(object c, int l)
    {
        MonoScript ms = (c is string s) ? (AssetDatabase.FindAssets(s + " t:MonoScript").Select(g => AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault()) : MonoScript.FromMonoBehaviour(c as MonoBehaviour);
        if (ms) AssetDatabase.OpenAsset(ms, l);
    }
    private int GetLineOfField(Component c, string f)
    {
        var ms = MonoScript.FromMonoBehaviour(c as MonoBehaviour);
        if (!ms) return 0;
        var lines = File.ReadAllLines(AssetDatabase.GetAssetPath(ms));
        for (int i = 0; i < lines.Length; i++) if (lines[i].Contains(f)) return i + 1;
        return 0;
    }

    private void PerformDetectiveSearch()
    {
        detectiveResults.Clear();
        var all = FindObjectsOfType<MonoBehaviour>(true);
        foreach (var script in all)
        {
            if (script == null || script == target) continue;
            SerializedObject so = new SerializedObject(script);
            SerializedProperty prop = so.GetIterator();
            while (prop.NextVisible(true))
            {
                if (prop.propertyType == SerializedPropertyType.ObjectReference && prop.objectReferenceValue == target)
                {
                    var res = new DetectiveResult { owner = script.gameObject, sourceComponent = script, varName = prop.name };
                    if (prop.propertyPath.Contains("m_PersistentCalls"))
                    {
                        res.isUIEvent = true;
                        var callProp = so.FindProperty(prop.propertyPath.Replace(".m_Target", ""));
                        res.uiMethodName = callProp.FindPropertyRelative("m_MethodName").stringValue;
                        var tObj = callProp.FindPropertyRelative("m_Target").objectReferenceValue;
                        res.uiTargetName = tObj ? tObj.GetType().Name : "Null";
                    }
                    AnalyzeSourceCode(res); detectiveResults.Add(res);
                }
            }
        }
    }

    private void AnalyzeSourceCode(DetectiveResult res)
    {
        MonoScript ms = MonoScript.FromMonoBehaviour(res.sourceComponent as MonoBehaviour);
        if (!ms) return;
        string path = AssetDatabase.GetAssetPath(ms);
        if (!File.Exists(path)) return;
        string[] lines = File.ReadAllLines(path); DetectiveMethod curM = null;
        for (int i = 0; i < lines.Length; i++)
        {
            string l = lines[i].Trim();
            if (Regex.IsMatch(l, @"\b(void|IEnumerator|string|int|float|bool)\s+(\w+)\s*\("))
            {
                curM = new DetectiveMethod { methodName = l.Split('(')[0].Split(' ').Last(), startLine = i + 1 };
                res.methods.Add(curM);
            }
            if (l.Contains(res.varName) && !l.Contains("public") && !l.Contains("["))
            {
                if (curM == null) { curM = new DetectiveMethod { methodName = "Global", startLine = i + 1 }; res.methods.Add(curM); }
                curM.lines.Add(new DetectiveLine { line = i + 1, content = l });
            }
        }
        res.methods.RemoveAll(m => m.lines.Count == 0);
    }

    private void ExecuteInject(string code)
    {
        try
        {
            if (code.Contains("="))
            {
                var p = code.Split('=');
                target.GetType().GetField(p[0].Trim(), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(target, Convert.ChangeType(p[1].Trim(), typeof(string)));
                injectStatus = "Inject OK";
            }
            else
            {
                target.GetType().GetMethod(code.Replace("()", "").Trim(), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Invoke(target, null);
                injectStatus = "Call OK";
            }
        }
        catch (Exception e) { injectStatus = "Error: " + e.Message; }
    }

    private bool DrawSectionHeader(string title, Color color, string key)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        Rect r = EditorGUILayout.GetControlRect(false, 30);
        EditorGUI.DrawRect(r, ColorSectionBg);
        EditorGUI.LabelField(r, " " + title, new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, alignment = TextAnchor.MiddleLeft, normal = { textColor = color } });
        if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition)) { SetFold(key, !GetFold(key)); Event.current.Use(); }
        return GetFold(key);
    }
    private void DrawNoDataLabel() => EditorGUILayout.LabelField("No relevant data found.", EditorStyles.centeredGreyMiniLabel);
    #endregion

    #region 11. AUX CLASSES
    private class DetectiveResult { public GameObject owner; public Component sourceComponent; public string varName, uiMethodName, uiTargetName; public bool isUIEvent, isExpanded = true; public List<DetectiveMethod> methods = new List<DetectiveMethod>(); }
    private class DetectiveMethod { public string methodName; public int startLine; public List<DetectiveLine> lines = new List<DetectiveLine>(); }
    private class DetectiveLine { public int line; public string content; }
    #endregion
}