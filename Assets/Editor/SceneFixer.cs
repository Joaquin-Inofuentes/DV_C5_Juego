using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using USP.Entities;
using Game.Sensors;
using System.Collections.Generic;

public class SceneFixer : MonoBehaviour
{
    [MenuItem("Tools/Fix USP Separada Scene Components")]
    public static void FixScene()
    {
        string scenePath = "Assets/Scenes/Tests/_USP Separada.unity";
        Debug.Log($"--- Iniciando corrección de componentes en: {scenePath} ---");

        // Abrir la escena
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"No se pudo cargar la escena en la ruta: {scenePath}");
            return;
        }

        bool changed = false;

        // --- 1. CONFIGURACIÓN EN ENEMIGOS (E1, etc.) ---
        EnemyController[] enemies = Object.FindObjectsOfType<EnemyController>();
        foreach (var enemy in enemies)
        {
            // Remover EnemySensors del hijo "Sensores"
            Transform sensoresTransform = enemy.transform.Find("Sensores");
            GameObject sensoresGo;
            if (sensoresTransform == null)
            {
                sensoresGo = new GameObject("Sensores");
                sensoresGo.transform.SetParent(enemy.transform);
                sensoresGo.transform.localPosition = Vector3.zero;
                changed = true;
            }
            else
            {
                sensoresGo = sensoresTransform.gameObject;
            }

            // Quitar EnemySensors si existe
            var oldSensors = sensoresGo.GetComponent("EnemySensors");
            if (oldSensors != null)
            {
                DestroyImmediate(oldSensors, true);
                Debug.Log($"[ENEMIGO] Removido EnemySensors viejo de {enemy.name}/Sensores");
                changed = true;
            }

            // Agregar y configurar GenericDetector
            var detector = sensoresGo.GetComponent<GenericDetector>();
            if (detector == null)
            {
                detector = sensoresGo.AddComponent<GenericDetector>();
                changed = true;
            }

            
            detector.obstacleMask = 1 << 6; // Capa 6: Obstaculo
            detector.typesToDetect = new List<DetectableType> { DetectableType.Aliado, DetectableType.Proyectil };

            var circle = sensoresGo.GetComponent<CircleCollider2D>();
            if (circle == null)
            {
                circle = sensoresGo.AddComponent<CircleCollider2D>();
                changed = true;
            }
            circle.isTrigger = true;
            

            Debug.Log($"[ENEMIGO] Configurado GenericDetector en {enemy.name}/Sensores");
        }

        // --- 2. CONFIGURACIÓN EN ALIADOS (J1, J2, J3, etc.) ---
        SoldierController[] soldiers = Object.FindObjectsOfType<SoldierController>();
        foreach (var soldier in soldiers)
        {
            // Quitar EnemyDetector viejo si existe
            var oldDetector = soldier.GetComponent("EnemyDetector");
            if (oldDetector != null)
            {
                DestroyImmediate(oldDetector, true);
                Debug.Log($"[ALIADO] Removido EnemyDetector viejo de {soldier.name}");
                changed = true;
            }

            // El aliado ahora debe tener un hijo "Detector" con un CircleCollider y GenericDetector
            Transform detectorTransform = soldier.transform.Find("Detector");
            GameObject detectorGo;
            if (detectorTransform == null)
            {
                detectorGo = new GameObject("Detector");
                detectorGo.transform.SetParent(soldier.transform);
                detectorGo.transform.localPosition = Vector3.zero;
                changed = true;
            }
            else
            {
                detectorGo = detectorTransform.gameObject;
            }

            var detector = detectorGo.GetComponent<GenericDetector>();
            if (detector == null)
            {
                detector = detectorGo.AddComponent<GenericDetector>();
                changed = true;
            }

            
            detector.obstacleMask = 1 << 6; // Capa 6: Obstaculo
            detector.typesToDetect = new List<DetectableType> { DetectableType.Enemigo };

            var circle = detectorGo.GetComponent<CircleCollider2D>();
            if (circle == null)
            {
                circle = detectorGo.AddComponent<CircleCollider2D>();
                changed = true;
            }
            circle.isTrigger = true;
            

            Debug.Log($"[ALIADO] Configurado GenericDetector en {soldier.name}/Detector");
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("--- Escena '_USP Separada' corregida y guardada con GenericDetector exitosamente ---");
        }
        else
        {
            Debug.Log("No se requirieron cambios en la escena.");
        }
    }
}
