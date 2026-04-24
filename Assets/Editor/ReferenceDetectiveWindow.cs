using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Tools.Detective
{
    public class ReferenceDetectiveUltimate : EditorWindow
    {
        // --- ESTRUCTURAS DE DATOS JERÁRQUICAS ---

        // Nivel 4: La línea de código específica
        private class CodeLine
        {
            public int LineNumber;
            public string Content;
            public bool IsModification; // Si detectamos un "=" cerca
        }

        // Nivel 3: El método o función
        private class MethodUsage
        {
            public string MethodName;
            public int MethodStartLine;
            public List<CodeLine> Lines = new List<CodeLine>();
            public bool IsExpanded = true;
        }

        // Nivel 2: El Componente que tiene la referencia
        private class ComponentResult
        {
            public Component SourceComponent;
            public string VariableName;
            public string VariablePath;
            public List<MethodUsage> Methods = new List<MethodUsage>();
            public bool IsExpanded = true;
        }

        // Nivel 1: El GameObject dueño del componente
        private class ObjectResult
        {
            public GameObject OwnerObject;
            public List<ComponentResult> Components = new List<ComponentResult>();
            public bool IsExpanded = true;
        }

        // --- ESTADO ---
        private Object _targetObject;     // Si buscamos referencias a un GO
        private Component _targetComponent; // Si buscamos referencias a un Componente especifico
        private List<ObjectResult> _results = new List<ObjectResult>();
        private Vector2 _scrollPos;
        private bool _isolationMode = false;
        private List<GameObject> _hiddenObjects = new List<GameObject>(); // Para restaurar visibilidad

        // --- ESTILOS GUI ---
        private GUIStyle _styleHeader;
        private GUIStyle _styleObjectBar;
        private GUIStyle _styleComponentBar;
        private GUIStyle _styleMethodLabel;
        private GUIStyle _styleCodeLine;
        private GUIStyle _styleButtonIcon;
        public bool _stylesInitialized = false;
        public bool _autoRefresh = false; // <--- AGREGA ESTO

        private string _simString = "Test";
        private int _simInt = 1;
        private float _simFloat = 1.0f;
        private bool _simBool = true;

        [MenuItem("Tools/🕵️ Detective Ultimate _F4")]
        public static void ShowWindow()
        {
            // Lógica de Toggle: Si ya está abierta y tiene foco, la cerramos.
            if (HasOpenInstances<ReferenceDetectiveUltimate>())
            {
                var window = GetWindow<ReferenceDetectiveUltimate>();
                if (window == EditorWindow.focusedWindow)
                {
                    window.Close();
                    return;
                }
                else
                {
                    window.Focus();
                    // Si solo la estamos enfocando, opcionalmente podríamos refrescar aquí también
                }
            }
            else
            {
                // Si no está abierta, la creamos
                var window = GetWindow<ReferenceDetectiveUltimate>("Detective Pro");
                // ANCHO REDUCIDO A LA MITAD (450 en vez de 900)
                window.minSize = new Vector2(450, 600);
                window.Show();

                // Auto-buscar al abrir
                if (Selection.activeGameObject != null)
                {
                    window._targetObject = Selection.activeGameObject;
                    window._targetComponent = null;
                    window.PerformDeepSearch();
                }
            }
        }

        private void OnEnable()
        {
            // Suscribirse al evento de cambio de selección
            Selection.selectionChanged += OnSelectionChanged;
        }

        private void OnDisable()
        {
            // Importante: Desuscribirse para evitar errores de memoria
            Selection.selectionChanged -= OnSelectionChanged;
        }

        // Método nuevo para manejar el cambio de selección
        private void OnSelectionChanged()
        {
            if (_autoRefresh && _targetComponent == null) // Solo auto-refresca si no estamos en modo "componente específico"
            {
                if (Selection.activeGameObject != null)
                {
                    _targetObject = Selection.activeGameObject;
                    PerformDeepSearch();
                    Repaint();
                }
            }
        }

        // --- CONTEXT MENU (Click derecho en componente) ---
        [MenuItem("CONTEXT/Component/🔍 Rastrear referencias a este Componente", false, 150)]
        public static void SearchSpecificComponent(MenuCommand command)
        {
            var window = GetWindow<ReferenceDetectiveUltimate>("Detective Pro");
            window.Show();

            Component c = (Component)command.context;
            window._targetComponent = c;
            window._targetObject = c.gameObject; // Referencia visual

            window.PerformDeepSearch();
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            // CABECERA GIGANTE
            _styleHeader = new GUIStyle(EditorStyles.boldLabel);
            _styleHeader.fontSize = 24; // Duplicado
            _styleHeader.alignment = TextAnchor.MiddleCenter;
            _styleHeader.normal.textColor = new Color(0.3f, 1f, 1f); // Cyan

            // BARRA DE OBJETO (Nivel 1) - Grande y clara
            _styleObjectBar = new GUIStyle(EditorStyles.helpBox);
            _styleObjectBar.fontSize = 18; // Fuente grande
            _styleObjectBar.fontStyle = FontStyle.Bold;
            _styleObjectBar.normal.textColor = Color.white;
            _styleObjectBar.alignment = TextAnchor.MiddleLeft;
            _styleObjectBar.fixedHeight = 50; // Altura duplicada (antes era auto o 20)

            var texObj = new Texture2D(1, 1);
            texObj.SetPixel(0, 0, new Color(0.25f, 0.25f, 0.35f));
            texObj.Apply();
            _styleObjectBar.normal.background = texObj;

            // BARRA DE COMPONENTE (Nivel 2)
            _styleComponentBar = new GUIStyle(EditorStyles.label);
            _styleComponentBar.fontSize = 16; // Fuente grande
            _styleComponentBar.fontStyle = FontStyle.Bold;
            _styleComponentBar.alignment = TextAnchor.MiddleLeft;
            _styleComponentBar.normal.textColor = new Color(1f, 0.8f, 0.4f); // Dorado
            _styleComponentBar.fixedHeight = 40; // Más alto

            // ETIQUETA DE MÉTODO (Nivel 3)
            _styleMethodLabel = new GUIStyle(EditorStyles.label);
            _styleMethodLabel.fontSize = 14;
            _styleMethodLabel.fontStyle = FontStyle.Italic;
            _styleMethodLabel.normal.textColor = new Color(0.6f, 1f, 0.6f); // Verde

            // LÍNEA DE CÓDIGO (Nivel 4)
            _styleCodeLine = new GUIStyle(EditorStyles.label);
            _styleCodeLine.fontSize = 14; // Código más legible
            _styleCodeLine.font = EditorStyles.standardFont;
            _styleCodeLine.richText = true;

            // BOTONES ICONOS (GIGANTES)
            _styleButtonIcon = new GUIStyle(EditorStyles.miniButton);
            _styleButtonIcon.fixedWidth = 100;  // Ancho x2
            _styleButtonIcon.fixedHeight = 35; // Alto x2
            _styleButtonIcon.margin = new RectOffset(5, 5, 5, 5);

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            // --- DETECTAR INPUT F5 o F6 (PRIORIDAD ABSOLUTA) ---
            Event e = Event.current;

            // Verificamos si es KeyDown y si es F5 o F6
            bool isRefreshKey = e.type == EventType.KeyDown && (e.keyCode == KeyCode.F5 || e.keyCode == KeyCode.F6);

            if (isRefreshKey)
            {
                // Solo ejecutamos si hay algo seleccionado
                if (Selection.activeGameObject != null)
                {
                    _targetObject = Selection.activeGameObject;
                    _targetComponent = null; // Resetear filtro para ver todo el objeto

                    PerformDeepSearch();

                    // Forzamos el repintado inmediato visual
                    Repaint();

                    // IMPORTANTE: Consumimos el evento para que Unity no haga su "Refresh" nativo de assets
                    e.Use();
                }
            }

            InitStyles();

            GUILayout.Space(10);

            // --- HEADER ---
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            string titleHeader = "NINGUNA SELECCIÓN";
            if (_targetComponent != null) titleHeader = $"🔍 COMP: [{_targetComponent.GetType().Name}]";
            else if (_targetObject != null) titleHeader = $"🔍 GO: '{_targetObject.name}'";

            GUILayout.Label(titleHeader, _styleHeader);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            // --- CONTROLES SUPERIORES (Toolbar) ---
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Botón Refresh Manual
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_Refresh"), EditorStyles.toolbarButton, GUILayout.Width(30)))
            {
                if (Selection.activeGameObject != null) _targetObject = Selection.activeGameObject;
                PerformDeepSearch();
            }

            // Checkbox Auto Refresh
            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", EditorStyles.toolbarButton, GUILayout.Width(90));

            GUILayout.FlexibleSpace();


            GUILayout.FlexibleSpace();

            // Botón Aislamiento
            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = _isolationMode ? Color.red : oldColor;
            if (GUILayout.Button(_isolationMode ? "Salir Aisla." : "👁️ Aislar", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                ToggleIsolation();
            }
            GUI.backgroundColor = oldColor;

            EditorGUILayout.EndHorizontal();

            // --- LISTADO DE RESULTADOS ---
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            if (_results.Count == 0 && _targetObject != null)
            {
                GUILayout.Space(20);
                EditorGUILayout.HelpBox("No se encontraron referencias 👻.", MessageType.Info);
            }
            else if (_targetObject == null)
            {
                GUILayout.Space(20);
                EditorGUILayout.HelpBox("Selecciona un objeto y presiona F5 / F6.", MessageType.Warning);
            }

            foreach (var objResult in _results)
            {
                DrawObjectResult(objResult);
            }

            EditorGUILayout.EndScrollView();
        }

        // --- DIBUJADO RECURSIVO (UI) ---

        private void DrawObjectResult(ObjectResult res)
        {
            // NIVEL 1: GAMEOBJECT (Contenedor principal)
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Fila de altura 50px
            EditorGUILayout.BeginHorizontal(_styleObjectBar, GUILayout.Height(50));

            // 1. BOTÓN SELECCIONAR (Pegado a la izquierda)
            GUILayout.BeginVertical();
            GUILayout.Space(7);
            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("d_scenepicking_notpickable").image, "Seleccionar"), _styleButtonIcon))
            {
                Selection.activeGameObject = res.OwnerObject;
                EditorGUIUtility.PingObject(res.OwnerObject);
            }
            GUILayout.EndVertical();

            // ELIMINADO: El GUILayout.Space(10) o cualquier FlexibleSpace intermedio
            GUILayout.Space(5); // Solo un pequeño aire entre botón y texto

            // 2. FOLDOUT + TEXTO (Ajustado para nacer inmediatamente después del botón)
            GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout);
            foldoutStyle.fontSize = 18;
            foldoutStyle.fontStyle = FontStyle.Bold;
            foldoutStyle.alignment = TextAnchor.MiddleLeft;
            foldoutStyle.normal.textColor = Color.white;
            foldoutStyle.fixedHeight = 50;

            // Forzamos que el contenido empiece desde la izquierda del área del foldout
            foldoutStyle.padding.left = 18;

            string titulo = $"📦 {res.OwnerObject.name} ({res.Components.Count} scripts)";

            // Dibujamos el foldout. Al estar en un BeginHorizontal sin espacios flexibles,
            // se pegará automáticamente al elemento anterior (el botón).
            res.IsExpanded = EditorGUILayout.Foldout(res.IsExpanded, titulo, true, foldoutStyle);

            // Agregamos un espacio flexible al FINAL para empujar todo lo anterior hacia la izquierda
            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            if (res.IsExpanded)
            {
                EditorGUI.indentLevel++;
                foreach (var compResult in res.Components)
                {
                    DrawComponentResult(compResult);
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private void DrawComponentResult(ComponentResult res)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Height(40));
            GUILayout.Space(5);

            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("d_Settings").image, "Ping Script"), _styleButtonIcon))
            {
                Selection.activeGameObject = res.SourceComponent.gameObject;
                MonoScript scriptObj = MonoScript.FromMonoBehaviour((MonoBehaviour)res.SourceComponent);
                if (scriptObj != null) EditorGUIUtility.PingObject(scriptObj);
            }
            GUILayout.Space(5);

            GUILayout.BeginVertical(); GUILayout.Space(5);
            GUILayout.Label(EditorGUIUtility.IconContent("cs Script Icon"), GUILayout.Width(30), GUILayout.Height(30));
            GUILayout.EndVertical();

            GUIStyle compStyle = new GUIStyle(EditorStyles.foldout);
            compStyle.fontSize = 15;
            compStyle.alignment = TextAnchor.LowerLeft;
            compStyle.normal.textColor = new Color(1f, 0.8f, 0.4f);
            compStyle.fixedHeight = 40;
            compStyle.padding.left = 15;

            string label = $"📜 {res.SourceComponent.GetType().Name}  ➜  {res.VariableName}";
            EditorGUILayout.EndHorizontal();
            res.IsExpanded = EditorGUILayout.Foldout(res.IsExpanded, label, true, compStyle);
            GUILayout.Space(20);

            if (res.IsExpanded)
            {
                EditorGUI.indentLevel++;
                if (res.VariablePath.Contains("m_PersistentCalls"))
                {
                    DrawUIEventInfo(res);
                }
                else if (res.Methods.Count == 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(60);
                    EditorGUILayout.HelpBox("Asignado en Inspector (Variable), pero el nombre exacto no se encontró en el texto del código .cs.", MessageType.None);
                    GUILayout.EndHorizontal();
                }
                else
                {
                    foreach (var method in res.Methods)
                    {
                        DrawMethodUsage(method, res.SourceComponent);
                    }
                }
                EditorGUI.indentLevel--;
                GUILayout.Space(2);
            }
        }

        private void DrawUIEventInfo(ComponentResult res)
        {
            SerializedObject so = new SerializedObject(res.SourceComponent);
            string callPath = res.VariablePath.Replace(".m_Target", "");
            SerializedProperty callProp = so.FindProperty(callPath);

            if (callProp != null)
            {
                string methodName = callProp.FindPropertyRelative("m_MethodName").stringValue;
                int mode = callProp.FindPropertyRelative("m_Mode").enumValueIndex;
                SerializedProperty args = callProp.FindPropertyRelative("m_Arguments");

                string tipoUI = "Evento Genérico";
                if (res.SourceComponent is UnityEngine.UI.Button) tipoUI = "🔘 BOTÓN (OnClick)";
                else if (res.SourceComponent is UnityEngine.UI.Slider) tipoUI = "🎚️ SLIDER (OnValueChanged)";
                else if (res.SourceComponent is UnityEngine.UI.Toggle) tipoUI = "☑️ TOGGLE (OnValueChanged)";
                else if (res.SourceComponent is UnityEngine.UI.InputField) tipoUI = "⌨️ INPUT FIELD";

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Cabecera del Evento
                GUIStyle headerUI = new GUIStyle(EditorStyles.boldLabel);
                headerUI.normal.textColor = new Color(0.5f, 0.8f, 1f);
                GUILayout.Label($" {tipoUI}", headerUI);

                // --- CORRECCIÓN DE DESPLAZAMIENTO ---
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);

                GUIStyle funcStyle = new GUIStyle(EditorStyles.label) { richText = true, fixedHeight = 30, alignment = TextAnchor.MiddleLeft };
                string infoStr = $"<color=#AAAAAA>ƒ Función:</color> <color=#FFD700><b>{methodName}()</b></color>";


                //GUILayout.FlexibleSpace(); // Empuja el botón a la derecha del texto, no al infinito

                if (GUILayout.Button("⚡ SIMULAR LLAMADA", GUI.skin.button, GUILayout.Width(180), GUILayout.Height(30)))
                {
                    SimulateEventCall(res.SourceComponent, methodName, mode, args);
                }

                // Usamos GUILayout.Label con ancho flexible para que no empuje al botón
                GUILayout.Label(infoStr, funcStyle);
                GUILayout.EndHorizontal();
                DrawSimulationControls(mode, args);

                EditorGUILayout.EndVertical();
            }
        }


        private void DrawSimulationControls(int mode, SerializedProperty args)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(40);
            EditorGUILayout.BeginVertical();

            switch (mode)
            {
                case 1: // Void
                    GUILayout.Label("<color=#888888><i>Sin parámetros (Void)</i></color>", new GUIStyle(EditorStyles.miniLabel) { richText = true });
                    break;
                case 2: // Object
                    Object obj = args.FindPropertyRelative("m_ObjectArgument").objectReferenceValue;
                    GUILayout.Label($"<color=#AAAAAA>📦 Ref actual:</color> {(obj != null ? obj.name : "NULL")}", new GUIStyle(EditorStyles.miniLabel) { richText = true });
                    break;
                case 3: // Int
                    _simInt = EditorGUILayout.IntField("Valor Int a enviar:", _simInt);
                    break;
                case 4: // Float
                    _simFloat = EditorGUILayout.FloatField("Valor Float a enviar:", _simFloat);
                    break;
                case 5: // String
                    _simString = EditorGUILayout.TextField("Texto a enviar:", _simString);
                    break;
                case 6: // Bool
                    _simBool = EditorGUILayout.Toggle("Estado a enviar:", _simBool);
                    break;
            }

            EditorGUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private void SimulateEventCall(Component source, string methodName, int mode, SerializedProperty args)
        {
            // 1. Limpiar consola
            var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
            var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            clearMethod.Invoke(null, null);

            Debug.Log($"<color=cyan><b>[Detective]</b></color> Iniciando simulación de <b>{methodName}</b> en {source.gameObject.name}...");

            try
            {
                // Buscamos el método en el componente
                var targetMethod = source.GetType().GetMethod(methodName,
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

                if (targetMethod == null)
                {
                    Debug.LogError($"No se encontró el método '{methodName}' mediante Reflection. Asegúrate de que no sea dinámico.");
                    return;
                }

                object[] parameters = null;

                // Preparar parámetros según el modo del UnityEvent
                switch (mode)
                {
                    case 3: parameters = new object[] { _simInt }; break;
                    case 4: parameters = new object[] { _simFloat }; break;
                    case 5: parameters = new object[] { _simString }; break;
                    case 6: parameters = new object[] { _simBool }; break;
                    case 2: parameters = new object[] { args.FindPropertyRelative("m_ObjectArgument").objectReferenceValue }; break;
                }

                // Invocación
                targetMethod.Invoke(source, parameters);

                Debug.Log($"<color=green><b>✔ ÉXITO:</b></color> Se ejecutó {methodName} correctamente.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"<color=red><b>✘ ERROR en {methodName}:</b></color> {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("Error de Simulación", $"Hubo un fallo al llamar a {methodName}:\n\n{ex.InnerException?.Message ?? ex.Message}", "Entendido");
            }
        }

        private void DrawMethodUsage(MethodUsage method, Component component)
        {
            // NIVEL 3: MÉTODO (Cabecera)
            // Altura 55 para dar un pequeño respiro al botón de 10
            EditorGUILayout.BeginHorizontal(GUILayout.Height(10));

            GUILayout.Space(80);

            // Botón "Ir a Func" (Grande)
            // Usamos GUI.skin.button para asegurar que tome el tamaño
            if (GUILayout.Button("Ir a Func", GUI.skin.button, GUILayout.Width(100), GUILayout.Height(50)))
            {
                OpenScriptAtLine(component, method.MethodStartLine);
            }

            // Texto del Nombre de Función
            GUIStyle funcStyle = new GUIStyle(EditorStyles.label);
            funcStyle.fontSize = 40;
            funcStyle.fontStyle = FontStyle.Italic;
            funcStyle.alignment = TextAnchor.MiddleLeft;
            funcStyle.normal.textColor = new Color(0.6f, 1f, 0.6f); // Verde Matrix
            funcStyle.fixedHeight = 50; // Centrado con el botón
            funcStyle.padding = new RectOffset(10, 0, 0, 0);

            GUILayout.Label($"ƒ  {method.MethodName}()", funcStyle);

            EditorGUILayout.EndHorizontal();

            // NIVEL 4: LÍNEAS DE CÓDIGO
            foreach (var line in method.Lines)
            {
                // CORRECCIÓN DE ESPACIO:
                // Antes era 100 (demasiado aire). Ahora 50 (ajustado al botón).
                EditorGUILayout.BeginHorizontal(GUILayout.Height(20));

                // Identación
                GUILayout.Space(120);

                // CORRECCIÓN DE BOTÓN:
                // 1. Usamos GUI.skin.button (El miniButton no se agranda)
                // 2. Width 50 / Height 50 (Cuadrado grande, misma altura que el de función)
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("d_editicon.sml").image, "Editar"),
                    GUI.skin.button,
                    GUILayout.Width(150),
                    GUILayout.Height(50)))
                {
                    OpenScriptAtLine(component, line.LineNumber);
                }

                // --- COLORES ---
                string cleanCode = line.Content.Trim();
                if (cleanCode.Length > 90) cleanCode = cleanCode.Substring(0, 90) + "...";

                Color colorTexto;

                // AZUL (Lógica)
                if (cleanCode.StartsWith("if") || cleanCode.StartsWith("else") || cleanCode.StartsWith("for") || cleanCode.StartsWith("foreach") || cleanCode.StartsWith("switch") || cleanCode.StartsWith("while"))
                {
                    colorTexto = new Color(0.3f, 0.7f, 1f); // Azul Cyan
                }
                // AMARILLO PATITO (Acciones)
                else
                {
                    colorTexto = new Color(1f, 0.84f, 0.0f); // Amarillo Patito
                }

                // Estilo para la línea de código
                GUIStyle lineStyle = new GUIStyle(EditorStyles.label);
                lineStyle.font = EditorStyles.standardFont;
                lineStyle.fontSize = 20;
                lineStyle.richText = true;

                // Alineación y altura ajustada a 50
                lineStyle.alignment = TextAnchor.MiddleLeft;
                lineStyle.normal.textColor = colorTexto;
                lineStyle.fixedHeight = 50;
                lineStyle.padding = new RectOffset(10, 0, 0, 0);
                lineStyle.clipping = TextClipping.Clip;

                GUILayout.Label($"L{line.LineNumber}:  {cleanCode}", lineStyle, GUILayout.ExpandWidth(true));

                EditorGUILayout.EndHorizontal();
            }
            // Reduje el espacio final también
            GUILayout.Space(2);
        }

        // --- LÓGICA DE AISLAMIENTO (TU FEATURE EXTRA) ---
        private void ToggleIsolation()
        {
            _isolationMode = !_isolationMode;

            if (_isolationMode)
            {
                // Recopilar todos los objetos involucrados
                HashSet<GameObject> relevantObjects = new HashSet<GameObject>();

                // CORRECCION: Casteamos _targetObject a GameObject antes de añadirlo
                if (_targetObject != null && _targetObject is GameObject goTarget)
                {
                    relevantObjects.Add(goTarget);
                }

                if (_targetComponent != null) relevantObjects.Add(_targetComponent.gameObject);

                foreach (var res in _results)
                {
                    if (res.OwnerObject != null) relevantObjects.Add(res.OwnerObject);
                }

                // Ocultar el resto
                _hiddenObjects.Clear();
                var allRenderers = FindObjectsOfType<Renderer>();
                foreach (var r in allRenderers)
                {
                    // Verificamos que r y su gameObject existan
                    if (r != null && r.gameObject != null && !relevantObjects.Contains(r.gameObject))
                    {
                        if (r.enabled)
                        {
                            r.enabled = false;
                            _hiddenObjects.Add(r.gameObject);
                        }
                    }
                }
                SceneView.RepaintAll();
            }
            else
            {
                // Restaurar
                foreach (var go in _hiddenObjects)
                {
                    if (go != null)
                    {
                        var r = go.GetComponent<Renderer>();
                        if (r != null) r.enabled = true;
                    }
                }
                _hiddenObjects.Clear();
                SceneView.RepaintAll();
            }
        }

        // --- CORE LOGIC ---

        private void PerformDeepSearch()
        {
            _results.Clear();
            if (_targetObject == null && _targetComponent == null) return;

            // 1. Identidades
            HashSet<Object> identitiesToFind = new HashSet<Object>();
            if (_targetComponent != null)
            {
                identitiesToFind.Add(_targetComponent);
            }
            else if (_targetObject != null) // Agregamos verificación de null
            {
                identitiesToFind.Add(_targetObject);

                // CORRECCIÓN AQUÍ: Intentamos tratarlo como GameObject
                GameObject go = _targetObject as GameObject;
                if (go != null)
                {
                    foreach (var c in go.GetComponents<Component>())
                    {
                        if (c != null) identitiesToFind.Add(c);
                    }
                }
            }

            // 2. Escanear
            MonoBehaviour[] allScripts = FindObjectsOfType<MonoBehaviour>();
            Dictionary<GameObject, ObjectResult> tempResults = new Dictionary<GameObject, ObjectResult>();

            int count = 0;
            foreach (var script in allScripts)
            {
                if (script == null) continue;
                count++;
                if (count % 20 == 0) EditorUtility.DisplayProgressBar("Detective", "Escaneando Inputs y Scripts...", (float)count / allScripts.Length);

                SerializedObject so = new SerializedObject(script);
                SerializedProperty prop = so.GetIterator();

                // IMPORTANTE: enterChildren = true permite entrar en UnityEvents (Buttons, etc)
                bool enterChildren = true;
                while (prop.Next(enterChildren))
                {
                    if (prop.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        if (prop.objectReferenceValue != null && identitiesToFind.Contains(prop.objectReferenceValue))
                        {
                            // Encontrado!
                            if (!tempResults.ContainsKey(script.gameObject))
                            {
                                tempResults[script.gameObject] = new ObjectResult { OwnerObject = script.gameObject };
                            }

                            // Detección especial para UI (Botones, Toggles, Sliders)
                            // Los eventos de UI suelen tener rutas como "m_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_Target"
                            string cleanName = prop.displayName;
                            bool isUIEvent = prop.propertyPath.Contains("m_PersistentCalls");

                            if (isUIEvent)
                            {
                                // Intentamos deducir el nombre del evento (ej: OnClick)
                                if (prop.propertyPath.Contains("m_OnClick")) cleanName = "On Click (Botón)";
                                else if (prop.propertyPath.Contains("m_OnValueChanged")) cleanName = "On Value Changed (Toggle/Slider)";
                                else if (prop.propertyPath.Contains("m_OnEndEdit")) cleanName = "On End Edit (Input)";
                                else cleanName = "Unity Event (Inspector)";
                            }

                            var objRes = tempResults[script.gameObject];

                            // Evitar duplicados si el mismo botón llama 2 veces
                            bool alreadyExists = objRes.Components.Any(c => c.VariableName == cleanName && c.SourceComponent == script);

                            if (!alreadyExists)
                            {
                                ComponentResult compRes = new ComponentResult
                                {
                                    SourceComponent = script,
                                    VariableName = cleanName,
                                    VariablePath = prop.propertyPath // Guardamos la ruta técnica para verificar luego
                                };

                                // Si NO es un evento de UI, buscamos en el código.
                                // Si ES un evento de UI, no hace falta buscar en código C# porque la conexión está en el inspector.
                                if (!isUIEvent)
                                {
                                    AnalyzeSourceCode(compRes);
                                }

                                objRes.Components.Add(compRes);
                            }
                        }
                    }
                    // Optimizacion: Solo entramos a hijos si es genérico (Listas, Eventos)
                    enterChildren = (prop.propertyType == SerializedPropertyType.Generic);
                }
            }

            _results = tempResults.Values.ToList();
            EditorUtility.ClearProgressBar();
        }

        private void AnalyzeSourceCode(ComponentResult compRes)
        {
            MonoScript ms = MonoScript.FromMonoBehaviour((MonoBehaviour)compRes.SourceComponent);
            if (ms == null) return;
            string path = AssetDatabase.GetAssetPath(ms);
            if (string.IsNullOrEmpty(path)) return;

            string[] lines = File.ReadAllLines(path);
            string currentMethod = "Fuera de método";
            int currentMethodLine = 1;

            // Regex simple para detectar métodos (void Update(), private IEnumerator Co(), etc)
            // Soporta espacios, tabs, saltos de línea en argumentos
            Regex methodRegex = new Regex(@"\b(void|IEnumerator|string|int|float|bool|Vector\d|GameObject|Transform)\s+(\w+)\s*\(", RegexOptions.IgnoreCase);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("//") || line.Length == 0) continue;

                // Detectar método
                Match match = methodRegex.Match(line);
                if (match.Success)
                {
                    currentMethod = match.Groups[2].Value;
                    currentMethodLine = i + 1;
                }

                // Detectar uso de variable
                // Usamos Regex boundary \b para asegurar palabra completa
                if (Regex.IsMatch(line, $@"\b{compRes.VariablePath}\b"))
                {
                    // Ignorar declaración
                    if (line.StartsWith("public") || line.StartsWith("private") || line.StartsWith("[Serialize")) continue;

                    // Buscar si ya tenemos este método registrado en los resultados
                    MethodUsage mUsage = compRes.Methods.FirstOrDefault(m => m.MethodName == currentMethod);
                    if (mUsage == null)
                    {
                        mUsage = new MethodUsage { MethodName = currentMethod, MethodStartLine = currentMethodLine };
                        compRes.Methods.Add(mUsage);
                    }

                    mUsage.Lines.Add(new CodeLine
                    {
                        LineNumber = i + 1,
                        Content = line,
                        IsModification = line.Contains("=") && !line.Contains("==") // Heurística simple de asignación
                    });
                }
            }
        }

        private void OpenScriptAtLine(Component c, int line)
        {
            MonoScript ms = MonoScript.FromMonoBehaviour((MonoBehaviour)c);
            AssetDatabase.OpenAsset(ms, line);
        }
    }
}