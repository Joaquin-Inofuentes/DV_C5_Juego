using UnityEngine;
using UnityEditor;

namespace Redes.EditorTools
{
    [InitializeOnLoad]
    public class AddObstacleFeedbackToPrefabs
    {
        static AddObstacleFeedbackToPrefabs()
        {
            EditorApplication.delayCall += Run;
        }

        private static void Run()
        {
            string[] prefabs = new string[] {
                "Assets/_Redes/Prefabs/BombObstacle.prefab",
                "Assets/_Redes/Prefabs/CustomObstacle.prefab"
            };

            bool changedAny = false;

            foreach (var path in prefabs)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    var feedback = prefab.GetComponent<Redes.Combat.ObstacleHitFeedback>();
                    if (feedback == null)
                    {
                        prefab.AddComponent<Redes.Combat.ObstacleHitFeedback>();
                        EditorUtility.SetDirty(prefab);
                        PrefabUtility.SavePrefabAsset(prefab);
                        changedAny = true;
                        Debug.Log($"<color=#00FF00>[AUTO-SETUP]</color> Añadido ObstacleHitFeedback a {path}");
                    }
                }
            }

            if (changedAny)
            {
                AssetDatabase.SaveAssets();
            }
        }
    }
}
