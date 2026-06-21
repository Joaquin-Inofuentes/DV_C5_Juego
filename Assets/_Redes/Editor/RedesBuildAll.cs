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

            Debug.Log("[REDES][BUILD] === Paso 4: Agregar a Build Settings ===");
            AddSceneToBuildSettings("Assets/_Redes/Scenes/RedesGame.unity");

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

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
                if (s.path == scenePath) { Debug.Log("[REDES][BUILD] Escena ya esta en BuildSettings"); return; }

            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes)
            {
                new EditorBuildSettingsScene(scenePath, true)
            };
            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log("[REDES][BUILD] Escena agregada a BuildSettings: " + scenePath);
        }

        private static void DoBuild()
        {
            string dir = Path.GetDirectoryName(BuildPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var scenes = new System.Collections.Generic.List<string>();
            foreach (var s in EditorBuildSettings.scenes)
                if (s.enabled) scenes.Add(s.path);

            var report = BuildPipeline.BuildPlayer(
                scenes.ToArray(),
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
