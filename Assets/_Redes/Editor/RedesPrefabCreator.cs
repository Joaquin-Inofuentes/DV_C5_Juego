using UnityEditor;
using UnityEngine;
using Fusion;
using Redes.Player;
using Redes.Combat;

namespace Redes.EditorTools
{
    /// <summary>
    /// Tools > Redes > 2. Create Prefabs
    ///
    /// Creates the primitive prefabs (Player capsule + Bullet sphere) with ALL
    /// their network components attached. References between components on the
    /// SAME prefab are wired here; cross-asset wiring (bullet prefab into the
    /// player, etc.) is done by "3. Link & Assign All".
    /// </summary>
    public static class RedesPrefabCreator
    {
        private const string PrefabFolder = "Assets/_Redes/Prefabs";
        public const string PlayerPrefabPath = PrefabFolder + "/Player.prefab";
        public const string BulletPrefabPath = PrefabFolder + "/Bullet.prefab";

        [MenuItem("Tools/Redes/2. Create Prefabs", priority = 2)]
        public static void CreatePrefabs()
        {
            EnsureFolder();
            CreatePlayerPrefab();
            CreateBulletPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#9E9E9E>[REDES][BOOT]</color> Prefabs creados (Player, Bullet). " +
                      "Ahora ejecuta 'Tools > Redes > 3. Link & Assign All'.");
        }

        private static void CreatePlayerPrefab()
        {
            // Primitive body (capsule).
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Player";

            // Physics + Fusion identity.
            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            go.AddComponent<NetworkObject>();

            // Animation (PlayerAnimationController requires an Animator).
            go.AddComponent<Animator>();

            // Player systems.
            var net   = go.AddComponent<Player.NetworkPlayer>();
            var move  = go.AddComponent<PlayerMovement>();
            var shoot = go.AddComponent<PlayerShooting>();
            var hp    = go.AddComponent<PlayerHealth>();
            var ammo  = go.AddComponent<AmmoSystem>();
            var anim  = go.AddComponent<PlayerAnimationController>();

            // Muzzle (where bullets will spawn).
            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(go.transform, false);
            muzzle.transform.localPosition = new Vector3(0, 0, 0.8f);

            // Wire SAME-PREFAB references via SerializedObject.
            AssignRefs(net,   ("_movement", move), ("_shooting", shoot), ("_health", hp),
                              ("_ammo", ammo), ("_animation", anim));
            AssignRefs(shoot, ("_ammo", ammo), ("_muzzle", muzzle.transform));
            AssignRefs(move,  ("_body", rb));
            AssignRefs(anim,  ("_animator", go.GetComponent<Animator>()));

            PrefabUtility.SaveAsPrefabAsset(go, PlayerPrefabPath);
            Object.DestroyImmediate(go);
        }

        private static void CreateBulletPrefab()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Bullet";
            go.transform.localScale = Vector3.one * 0.3f;

            var col = go.GetComponent<SphereCollider>();
            col.isTrigger = true;

            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;

            go.AddComponent<NetworkObject>();
            go.AddComponent<Projectile>();

            PrefabUtility.SaveAsPrefabAsset(go, BulletPrefabPath);
            Object.DestroyImmediate(go);
        }

        // Assigns (fieldName, value) pairs onto a component via SerializedObject.
        private static void AssignRefs(Object target, params (string field, Object value)[] pairs)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in pairs)
            {
                var prop = so.FindProperty(field);
                if (prop != null) prop.objectReferenceValue = value;
                else Debug.LogWarning($"[REDES][LINK] Campo '{field}' no encontrado en {target.GetType().Name}");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder("Assets/_Redes", "Prefabs");
        }
    }
}
