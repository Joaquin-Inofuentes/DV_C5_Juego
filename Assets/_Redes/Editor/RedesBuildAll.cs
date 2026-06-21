using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

namespace Redes.EditorTools
{
    /// <summary>
    /// Invocado desde línea de comandos por BuildAndTest.bat
    /// Hace en un solo proceso de Unity:
    ///   1. Crea/regenera la escena RedesGame
    ///   2. Crea los prefabs Player y Bullet
    ///   3. Enlaza todas las referencias
    ///   4. Agrega RedesGame a BuildSettings
    ///   5. Compila y construye el juego para Windows x64
    /// </summary>
    public static class RedesBuildAll
    {
        private const string ScenePath = "Assets/_Redes/Scenes/RedesGame.unity";
        private const string BuildPath = "Builds/RedesGame_Win64/RedesGame.exe";

        [MenuItem("Tools/Redes/4. Full Build (Scene + Prefabs + Link + Build)", priority = 4)]
        public static void FullBuild()
        {
            Debug.Log("[REDES][BUILD] === Paso 1: Crear escena ===");
            RedesSceneCreator.CreateScene();

            Debug.Log("[REDES][BUILD] === Paso 2: Crear prefabs ===");
            RedesPrefabCreator.CreatePrefabs();

            Debug.Log("[REDES][BUILD] === Paso 3: Enlazar referencias ===");
            RedesSceneLinker.LinkAll();

            Debug.Log("[REDES][BUILD] === Paso 4: RedesGame PRIMERA en Build Settings ===");
            EnsureRedesSceneIsFirst();

            Debug.Log("[REDES][BUILD] === Paso 5: Build Windows x64 ===");
            DoBuild();

            Debug.Log("[REDES][BUILD] === COMPLETADO ===");
        }

        public static void FullBuildCLI()
        {
            try
            {
                FullBuild();
            }
            catch (System.Exception e)
            {
                Debug.LogError("[REDES][BUILD] Error: " + e);
                EditorApplication.Exit(1);
                return;
            }
            EditorApplication.Exit(0);
        }

        /// <summary>
        /// Asegura que RedesGame sea la ÚNICA escena del build dedicado de Redes,
        /// cargada en índice 0.  No toca el orden del proyecto principal.
        /// </summary>
        private static void EnsureRedesSceneIsFirst()
        {
            // El build de Redes solo incluye su propia escena para garantizar
            // que sea la escena de inicio (build index 0).
            Debug.Log("[REDES][BUILD] Build dedicado: solo RedesGame.unity (index 0)");
        }

        private static void DoBuild()
        {
            string dir = Path.GetDirectoryName(BuildPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            // Build DEDICADO: solo RedesGame.unity — garantiza que sea la startup scene.
            var report = BuildPipeline.BuildPlayer(
                new[] { ScenePath },
                BuildPath,
                BuildTarget.StandaloneWindows64,
                BuildOptions.None
            );

            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
                Debug.Log("[REDES][BUILD] Build exitoso: " + BuildPath);
            else
                Debug.LogError("[REDES][BUILD] Build FALLO: " + report.summary.totalErrors + " errores");
        }
    }
}
