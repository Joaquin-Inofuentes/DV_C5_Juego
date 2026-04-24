using UnityEngine;
/*
using System.Diagnostics;
using System.Text;
using System.Reflection;
using HarmonyLib;
*/
public class VariableWatcherInyectable : MonoBehaviour
{
    /*
    [Header("Configuración")]
    public Notes_Draw targetComponent;
    public string nombreVariable = "ultimaPosicionMundo";

    private static string varName;
    private static object lastValue;
    private static FieldInfo fieldInfo;
    private static Harmony harmony;

    void OnEnable()
    {
        if (targetComponent == null) return;

        varName = nombreVariable;
        // Obtenemos el campo por reflexión
        fieldInfo = typeof(Notes_Draw).GetField(varName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (fieldInfo == null)
        {
            UnityEngine.Debug.LogError($"No se encontró el campo {varName}");
            return;
        }

        // Guardamos el valor inicial
        lastValue = fieldInfo.GetValue(targetComponent);

        // --- INYECCIÓN LETAL ---
        if (harmony == null)
        {
            harmony = new Harmony("com.peer.debug.watcher");

            // Buscamos todos los métodos en Notes_Draw
            var methods = typeof(Notes_Draw).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            // Nuestro método que hará el Log
            var postfix = new HarmonyMethod(typeof(VariableWatcherInyectable).GetMethod(nameof(FinalizadorDeMetodo)));

            foreach (var m in methods)
            {
                // Evitamos parchear propiedades o métodos de sistema de Unity si dieran error
                if (m.IsSpecialName || m.Name.Contains("OnValidate")) continue;

                try
                {
                    harmony.Patch(m, postfix: postfix);
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogWarning($"No se pudo parchear {m.Name}: {e.Message}");
                }
            }
            UnityEngine.Debug.Log("<color=#00FF00><b>[HARMONY]</b> Notes_Draw ha sido infectado. Vigilando cambios...</color>");
        }
    }

    // Este es el método que Harmony "pega" al final de cada función de Notes_Draw
    public static void FinalizadorDeMetodo(object __instance, MethodBase __originalMethod)
    {
        if (fieldInfo == null || __instance == null) return;

        // Leemos el valor actual del campo
        object currentValue = fieldInfo.GetValue(__instance);

        // Comparamos (usando Equals para que funcione con Vector2)
        if (!currentValue.Equals(lastValue))
        {
            LoggearCambio(__originalMethod.Name, lastValue, currentValue);
            lastValue = currentValue;
        }
    }

    private static void LoggearCambio(string metodoQueCambio, object viejo, object nuevo)
    {
        // Forzamos la captura con fNeedFileInfo = true
        StackTrace st = new StackTrace(true);
        StringBuilder sb = new StringBuilder();

        sb.Append($"<b><color=#FF4444>[CAMBIO DETECTADO]</color></b>\n");
        sb.Append($"Culpable Directo: <color=#FFFF00>{metodoQueCambio}()</color>\n");
        sb.Append($"Valor: {viejo} <b> <color=#00FF00>--></color> </b> {nuevo}\n");
        sb.Append("<color=#888888>Pila de ejecución (Trace Limpio):</color>\n");

        for (int i = 0; i < st.FrameCount; i++)
        {
            StackFrame sf = st.GetFrame(i);
            MethodBase m = sf.GetMethod();
            if (m == null) continue;

            string cName = m.DeclaringType != null ? m.DeclaringType.Name : "Unknown";
            string mName = m.Name;

            // --- FILTRO DE RUIDO ---
            // Ignoramos todo lo que sea de Harmony, MonoMod o este mismo script de Debug
            if (cName.Contains("Harmony") || cName.Contains("MonoMod") ||
                cName.Contains("VariableWatcher") || mName.Contains("Finalizador"))
                continue;

            // Intentamos obtener la línea. Si da 0, es que Unity no tiene el .pdb cargado en ese frame
            int line = sf.GetFileLineNumber();
            string file = sf.GetFileName();

            // Si la línea es 0, a veces es porque es un método Patch. 
            // Intentamos mostrar al menos la clase y el método que sí tenemos.
            string lineStr = (line > 0) ? $"<color=#FFFB00>L{line}</color>" : "<color=#FF0000>L?</color>";

            sb.Append($"\t<b>{cName}</b> :: {mName}() \t {lineStr}\n");
        }

        //UnityEngine.Debug.Log(sb.ToString());
    }

    void OnApplicationQuit()
    {
        // Limpiamos al salir para no dejar rastros en el editor
        harmony?.UnpatchAll("com.peer.debug.watcher");
    }
    */
}