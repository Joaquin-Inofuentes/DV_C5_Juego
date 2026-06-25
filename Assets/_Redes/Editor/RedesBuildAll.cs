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
        private const string ScenePath = "Assets/_Redes/Scenes/__Redes_RedesGame.unity";
        private const string BuildPath = "Builds/RedesGame_Win64/RedesGame.exe";

        // Removed Tools/Redes/4. Full Build MenuItem
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
            var scenes = EditorBuildSettings.scenes;
            bool found = false;
            foreach (var s in scenes) { if (s.path == ScenePath) found = true; }
            if (!found) {
                var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
                System.Array.Copy(scenes, newScenes, scenes.Length);
                newScenes[scenes.Length] = new EditorBuildSettingsScene(ScenePath, true);
                EditorBuildSettings.scenes = newScenes;
                Debug.Log("[REDES][BUILD] Escena añadida a Build Settings: " + ScenePath);
            }
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

        [MenuItem("Redes/Corregir", priority = 1)]
        public static void Corregir()
        {
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
            Debug.Log("[REDES][CORREGIR] PlayerSettings.insecureHttpOption set to AlwaysAllowed.");

            Debug.Log("[REDES][CORREGIR] === Paso 0: Crear Mixer de Audio e Inicializar Variables ===");
            RedesAudioSetup.CreateAudioMixerAndSetup();

            Debug.Log("[REDES][CORREGIR] === Paso 1: Crear escena de juego ===");
            RedesSceneCreator.CreateScene();

            Debug.Log("[REDES][CORREGIR] === Paso 2: Crear prefabs ===");
            RedesPrefabCreator.CreatePrefabs();

            Debug.Log("[REDES][CORREGIR] === Paso 3: Enlazar referencias ===");
            RedesSceneLinker.LinkAll();

            Debug.Log("[REDES][CORREGIR] === Paso 4: Crear escena de test offline ===");
            RedesTestSceneBuilder.BuildTestScene();

            Debug.Log("[REDES][CORREGIR] === Paso 5: Añadiendo escena a Build Settings ===");
            EnsureRedesSceneIsFirst();

            Debug.Log("[REDES][CORREGIR] === Paso 6: Abriendo escena de juego principal ===");
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Debug.Log("[REDES][CORREGIR] === COMPLETADO ===");
        }
    }
}
