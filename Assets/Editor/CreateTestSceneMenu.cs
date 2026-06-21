using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DebugSystem;

namespace DebugSystem.Editor
{
    public class CreateTestSceneMenu
    {
        [MenuItem("Tools/Pruebas/CrearEscenaDeTests")]
        public static void CreateScene()
        {
            // 1. Create a new active scene
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            newScene.name = "Scene_DebugTests";

            // Remove default direction light and camera to construct our own structure cleanly or modify them
            GameObject defaultCamera = GameObject.Find("Main Camera");
            if (defaultCamera != null)
            {
                // Set orthographic for 2D game testing
                Camera cam = defaultCamera.GetComponent<Camera>();
                if (cam != null)
                {
                    cam.orthographic = true;
                    cam.orthographicSize = 10f;
                }
            }

            // 2. Managers Container
            GameObject managersGo = new GameObject("Managers");
            managersGo.AddComponent<GameManager>();
            managersGo.AddComponent<DebugLogger>();
            managersGo.AddComponent<GameLoopReferee>();

            // BulletPool
            GameObject bulletPoolGo = new GameObject("PoolManager");
            bulletPoolGo.transform.SetParent(managersGo.transform);
            BulletPool bulletPool = bulletPoolGo.AddComponent<BulletPool>();

            // 3. Bullet Prefab Creation (in memory/temporary or saved under Assets/Prefabs if needed, let's create a minimal hierarchy sprite bullet)
            GameObject bulletPrefabGo = new GameObject("BulletPrefabTemplate");
            bulletPrefabGo.SetActive(false);
            
            // Add SpriteRenderer to bullet with a simple Circle representation if possible (we can configure components)
            SpriteRenderer bulletRenderer = bulletPrefabGo.AddComponent<SpriteRenderer>();
            // Use built-in unity sprite (Knob or InputFieldBackground or background)
            bulletRenderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            bulletRenderer.color = Color.yellow;
            bulletPrefabGo.transform.localScale = new Vector3(0.3f, 0.3f, 1f);

            CircleCollider2D bulletCol = bulletPrefabGo.AddComponent<CircleCollider2D>();
            bulletCol.isTrigger = true;
            Rigidbody2D bulletRb = bulletPrefabGo.AddComponent<Rigidbody2D>();
            bulletRb.bodyType = RigidbodyType2D.Kinematic;

            Bullet bulletComponent = bulletPrefabGo.AddComponent<Bullet>();
            bulletComponent.Speed = 20f;
            bulletComponent.Damage = 20f;
            bulletComponent.Lifetime = 3f;

            // Link prefab template to bullet pool
            // For simple scene execution, we can just instantiate it. We'll use reflection/serialized fields or make a public field set
            SerializedObject poolSO = new SerializedObject(bulletPool);
            poolSO.FindProperty("bulletPrefab").objectReferenceValue = bulletComponent;
            poolSO.ApplyModifiedProperties();

            // 4. Obstacle environment
            GameObject environmentGo = new GameObject("Environment");
            GameObject wall = new GameObject("Wall");
            wall.transform.SetParent(environmentGo.transform);
            wall.transform.position = new Vector3(0f, 6f, 0f);
            wall.transform.localScale = new Vector3(10f, 1f, 1f);
            BoxCollider2D wallCol = wall.AddComponent<BoxCollider2D>();
            wallCol.isTrigger = true;
            wall.tag = "Obstacle";
            // Visual for Wall
            SpriteRenderer wallRenderer = wall.AddComponent<SpriteRenderer>();
            wallRenderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            wallRenderer.color = Color.gray;

            // 5. Create Player GameObject
            GameObject playerGo = new GameObject("Player");
            playerGo.transform.position = new Vector3(-4f, 0f, 0f);
            PlayerModel playerModel = playerGo.AddComponent<PlayerModel>();
            playerModel.Initialize(1, "Joaco");
            playerGo.AddComponent<PlayerView>();
            playerGo.AddComponent<PlayerController>();

            BoxCollider2D playerCol = playerGo.AddComponent<BoxCollider2D>();
            playerCol.size = new Vector2(1f, 1f);
            Rigidbody2D playerRb = playerGo.AddComponent<Rigidbody2D>();
            playerRb.bodyType = RigidbodyType2D.Kinematic;

            // Sprite representation for player
            SpriteRenderer playerRenderer = playerGo.AddComponent<SpriteRenderer>();
            playerRenderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            playerRenderer.color = Color.cyan;

            // Weapon mount for Player
            GameObject playerWeaponGo = new GameObject("WeaponMount");
            playerWeaponGo.transform.SetParent(playerGo.transform);
            playerWeaponGo.transform.localPosition = new Vector3(0.6f, 0f, 0f);
            Weapon playerWeapon = playerWeaponGo.AddComponent<Weapon>();
            playerWeapon.WeaponName = "Pistol";
            playerWeapon.ClipSize = 12;
            playerWeapon.MaxReserve = 24;

            // Aim Indicator
            GameObject playerAimGo = new GameObject("AimIndicator");
            playerAimGo.transform.SetParent(playerGo.transform);
            playerAimGo.transform.localPosition = new Vector3(1.5f, 0f, 0f);
            SpriteRenderer aimRenderer = playerAimGo.AddComponent<SpriteRenderer>();
            aimRenderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            aimRenderer.color = Color.red;
            playerAimGo.transform.localScale = new Vector3(0.2f, 0.2f, 1f);

            // 6. Create Enemy GameObject
            GameObject enemyGo = new GameObject("Enemy");
            enemyGo.transform.position = new Vector3(4f, 0f, 0f);
            PlayerModel enemyModel = enemyGo.AddComponent<PlayerModel>();
            enemyModel.Initialize(2, "EnemyBot");
            enemyGo.AddComponent<PlayerView>();
            enemyGo.AddComponent<SimpleEnemyAI>();

            BoxCollider2D enemyCol = enemyGo.AddComponent<BoxCollider2D>();
            enemyCol.size = new Vector2(1f, 1f);
            Rigidbody2D enemyRb = enemyGo.AddComponent<Rigidbody2D>();
            enemyRb.bodyType = RigidbodyType2D.Kinematic;

            // Sprite representation for enemy
            SpriteRenderer enemyRenderer = enemyGo.AddComponent<SpriteRenderer>();
            enemyRenderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            enemyRenderer.color = Color.red;

            // Weapon mount for Enemy
            GameObject enemyWeaponGo = new GameObject("WeaponMount");
            enemyWeaponGo.transform.SetParent(enemyGo.transform);
            enemyWeaponGo.transform.localPosition = new Vector3(0.6f, 0f, 0f);
            Weapon enemyWeapon = enemyWeaponGo.AddComponent<Weapon>();
            enemyWeapon.WeaponName = "Pistol";
            enemyWeapon.ClipSize = 12;
            enemyWeapon.MaxReserve = 24;

            // 7. Mark scene dirty to allow saving
            EditorSceneManager.MarkSceneDirty(newScene);

            // Save the scene under Assets/Scenes/Scene_DebugTests.unity
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
            EditorSceneManager.SaveScene(newScene, "Assets/Scenes/Scene_DebugTests.unity");

            Debug.Log("Scene_DebugTests created successfully with full components, scripts, hierarchy, and links!");
        }
    }
}
