using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;

public class CentralizadorScripts : EditorWindow
{
    [MenuItem("Tools/Centralizar")]
    public static void CentralizarScriptsEnEscena()
    {
        // 1. Obtener la escena activa
        Scene currentScene = SceneManager.GetActiveScene();

        if (string.IsNullOrEmpty(currentScene.path))
        {
            Debug.LogError("Error: La escena actual no ha sido guardada. Guárdala antes de centralizar.");
            return;
        }

        Debug.Log($"<color=cyan>--- Iniciando centralización para la escena: {currentScene.name} ---</color>");

        // 2. Obtener la ruta de la carpeta de la escena
        string sceneFolderPath = Path.GetDirectoryName(currentScene.path);
        Debug.Log($"Carpeta destino: {sceneFolderPath}");

        // 3. Buscar todos los MonoBehaviours en la escena (incluyendo desactivados)
        MonoBehaviour[] allScripts = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        // Usamos un HashSet para evitar procesar el mismo archivo de script varias veces
        HashSet<string> scriptsProcesados = new HashSet<string>();
        int movidos = 0;
        int errores = 0;

        foreach (MonoBehaviour mb in allScripts)
        {
            if (mb == null) continue;

            // Obtener el objeto script (MonoScript) asociado al componente
            MonoScript scriptAsset = MonoScript.FromMonoBehaviour(mb);
            if (scriptAsset == null) continue;

            // Obtener la ruta del archivo del script
            string oldPath = AssetDatabase.GetAssetPath(scriptAsset);

            // Validaciones de seguridad
            if (string.IsNullOrEmpty(oldPath)) continue;
            if (scriptsProcesados.Contains(oldPath)) continue;

            // Ignorar scripts que vienen de Packages o que son internos de Unity
            if (!oldPath.StartsWith("Assets/"))
            {
                Debug.LogWarning($"Saltado: {scriptAsset.name} está fuera de la carpeta Assets (posiblemente un Package).");
                continue;
            }

            string fileName = Path.GetFileName(oldPath);
            string newPath = Path.Combine(sceneFolderPath, fileName).Replace("\\", "/");

            // Si el script ya está en esa carpeta, lo ignoramos
            if (oldPath == newPath)
            {
                scriptsProcesados.Add(oldPath);
                continue;
            }

            // 4. Mover el archivo
            Debug.Log($"Moviendo: <b>{fileName}</b> de {oldPath} a {newPath}");

            string validation = AssetDatabase.ValidateMoveAsset(oldPath, newPath);
            if (string.IsNullOrEmpty(validation))
            {
                string result = AssetDatabase.MoveAsset(oldPath, newPath);
                if (string.IsNullOrEmpty(result))
                {
                    movidos++;
                }
                else
                {
                    Debug.LogError($"Error al mover {fileName}: {result}");
                    errores++;
                }
            }
            else
            {
                Debug.LogWarning($"No se pudo mover {fileName}: {validation}");
                errores++;
            }

            scriptsProcesados.Add(oldPath);
        }

        // 5. Refrescar la base de datos de Assets
        if (movidos > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"<color=green>--- Proceso finalizado ---</color>");
        Debug.Log($"Scripts movidos: {movidos} | Errores o saltados: {errores}");
        EditorUtility.DisplayDialog("Centralizar Scripts", $"Se han movido {movidos} scripts a la carpeta de la escena.\nErrores: {errores}", "OK");
    }
}