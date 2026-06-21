using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
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
        public const string EntityDisplayViewPrefabPath = PrefabFolder + "/EntityDisplayView.prefab";

        [MenuItem("Tools/Redes/2. Create Prefabs", priority = 2)]
        public static void CreatePrefabs()
        {
            EnsureFolder("Assets/_Redes", "Prefabs");

            CreateGameEventBusAsset();
            CreateBulletPrefab();
            CreatePlayerPrefab();
            CreateEntityDisplayViewPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#9E9E9E>[REDES][BOOT]</color> Prefabs creados. Ahora ejecuta '3. Link & Assign All'.");
        }

        private static void CreateGameEventBusAsset()
        {
            string path = "Assets/_Redes/Scripts/Core/GameEventBus.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Redes.Core.GameEventBus>(path);
            if (existing == null)
            {
                EnsureFolder("Assets/_Redes/Scripts", "Core");
                var asset = ScriptableObject.CreateInstance<Redes.Core.GameEventBus>();
                AssetDatabase.CreateAsset(asset, path);
                Debug.Log($"[REDES][PREFABS] GameEventBus asset created at {path}");
            }
        }

        private static void CreatePlayerPrefab()
        {
            // Primitive body (capsule).
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Player";

            // Physics + Fusion identity.
            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            go.AddComponent<NetworkObject>();
            go.AddComponent<NetworkTransform>();

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
            go.AddComponent<NetworkTransform>();
            go.AddComponent<Projectile>();

            PrefabUtility.SaveAsPrefabAsset(go, BulletPrefabPath);
            Object.DestroyImmediate(go);
        }

        private static void CreateEntityDisplayViewPrefab()
        {
            var go = new GameObject("EntityDisplayView", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100f, 15f);

            var sliderGo = new GameObject("Slider", typeof(RectTransform));
            sliderGo.transform.SetParent(go.transform, false);
            var sliderRect = sliderGo.GetComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = Vector2.one;
            sliderRect.sizeDelta = Vector2.zero;

            var bgGo = new GameObject("Background", typeof(RectTransform));
            bgGo.transform.SetParent(sliderGo.transform, false);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.3f, 0.3f, 0.3f, 0.7f);

            var fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaGo.transform.SetParent(sliderGo.transform, false);
            var fillAreaRect = fillAreaGo.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;

            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.sizeDelta = Vector2.zero;
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = Color.green;

            var slider = sliderGo.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.targetGraphic = fillImg;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            var nicknameGo = new GameObject("NicknameText", typeof(RectTransform));
            nicknameGo.transform.SetParent(go.transform, false);
            var nicknameRect = nicknameGo.GetComponent<RectTransform>();
            nicknameRect.anchorMin = new Vector2(0f, 1f);
            nicknameRect.anchorMax = new Vector2(1f, 1f);
            nicknameRect.pivot = new Vector2(0.5f, 0f);
            nicknameRect.anchoredPosition = new Vector2(0f, 10f); // Adjust position slightly higher for larger font
            nicknameRect.sizeDelta = new Vector2(240f, 40f);

            var nicknameTxt = nicknameGo.AddComponent<Text>();
            nicknameTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            nicknameTxt.fontSize = 24;
            nicknameTxt.color = Color.white;
            nicknameTxt.alignment = TextAnchor.MiddleCenter;
            nicknameTxt.fontStyle = FontStyle.Bold;

            var view = go.AddComponent<Views.EntityDisplayView>();
            AssignRefs(view, ("_healthSlider", (Object)slider), ("_nicknameText", (Object)nicknameTxt));

            PrefabUtility.SaveAsPrefabAsset(go, EntityDisplayViewPrefabPath);
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

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
