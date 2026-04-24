using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class MissingScriptsCleaner : Editor
{
    [MenuItem("Tools/Limpiar Scripts Faltantes (Missing)")]
    public static void CleanAllMissingScripts()
    {
        int totalEliminados = 0;
        int objetosAfectados = 0;

        // Obtener todos los GameObjects de la escena activa (incluyendo inactivos)
        GameObject[] todosLosObjetos = Resources.FindObjectsOfTypeAll<GameObject>();

        foreach (GameObject go in todosLosObjetos)
        {
            // Evitar limpiar objetos que son parte de la interfaz de Unity o assets del proyecto
            if (go.hideFlags != HideFlags.None || EditorUtility.IsPersistent(go))
                continue;

            // GameObjectUtility.RemoveMonoBehavioursWithMissingScript es el método oficial de Unity
            int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);

            if (count > 0)
            {
                totalEliminados += count;
                objetosAfectados++;
                Debug.Log($"<color=yellow>[MissingScript]</color> Se eliminaron <b>{count}</b> referencias nulas en: <b>{go.name}</b>", go);
            }
        }

        if (totalEliminados > 0)
        {
            // Marcar la escena como "sucia" para que Unity pida guardar los cambios
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            Debug.Log($"<color=green><b>[Limpieza Completada]</b></color> Se eliminaron {totalEliminados} scripts perdidos en {objetosAfectados} GameObjects.");
        }
        else
        {
            Debug.Log("<color=white>[Limpieza]</color> No se encontraron scripts 'Missing' en la escena.");
        }
    }
}