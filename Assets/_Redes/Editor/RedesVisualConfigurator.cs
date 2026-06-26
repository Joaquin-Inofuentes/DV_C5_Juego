using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

namespace Redes.EditorTools
{
    public static class RedesVisualConfigurator
    {
        private const string TTFPath = "Assets/_Textos/Science_Gothic/static/ScienceGothic_UltraExpanded-Regular.ttf";
        private const string SDFPath = "Assets/_Textos/Science_Gothic/static/ScienceGothic_UltraExpanded-Regular_SDF.asset";
        private const string ScenePath = "Assets/_Redes/Scenes/__Redes_RedesGame.unity";
        private const string PrefabFolder = "Assets/_Redes/Prefabs";

        public static void ConfigureAllVisuals()
        {
            Debug.Log("[REDES][VISUAL] === INICIANDO CONFIGURACIÓN VISUAL (Fuentes y Botones) ===");

            // 1. Cargar fuentes
            Font ttfFont = AssetDatabase.LoadAssetAtPath<Font>(TTFPath);
            if (ttfFont == null)
            {
                Debug.LogError($"[REDES][VISUAL] ERROR: No se encontró la fuente TTF en {TTFPath}");
                return;
            }
            Debug.Log($"[REDES][VISUAL] Fuente TTF cargada con éxito: '{ttfFont.name}'");

            // Cargar o crear el SDF de TMPro
            TMPro.TMP_FontAsset sdfAsset = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(SDFPath);
            if (sdfAsset == null)
            {
                Debug.Log($"[REDES][VISUAL] No se encontró el Asset de Fuente SDF en '{SDFPath}'. Creándolo...");
                sdfAsset = TMPro.TMP_FontAsset.CreateFontAsset(ttfFont);
                AssetDatabase.CreateAsset(sdfAsset, SDFPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[REDES][VISUAL] Asset de Fuente SDF creado y guardado en: '{SDFPath}'");
            }
            else
            {
                Debug.Log($"[REDES][VISUAL] Asset de Fuente SDF cargado correctamente.");
            }

            // 2. Cargar escena
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
            Debug.Log($"[REDES][VISUAL] Escena '{ScenePath}' abierta.");

            // 3. Obtener el Sprite de fondo (Background)
            Sprite backgroundSprite = null;
            var ramboGo = GameObject.Find("RamboButton");
            if (ramboGo != null)
            {
                var img = ramboGo.GetComponent<Image>();
                if (img != null)
                {
                    backgroundSprite = img.sprite;
                    Debug.Log($"[REDES][VISUAL] Sprite de fondo obtenido de 'RamboButton': {backgroundSprite.name}");
                }
            }
            if (backgroundSprite == null)
            {
                string[] guids = AssetDatabase.FindAssets("Background t:Sprite");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    Debug.Log($"[REDES][VISUAL] Sprite de fondo obtenido del AssetDatabase en '{path}': {backgroundSprite.name}");
                }
            }
            if (backgroundSprite == null)
            {
                Debug.LogWarning("[REDES][VISUAL] CUIDADO: No se encontró un Sprite de fondo llamado 'Background'. Se usará el default.");
            }

            // 4. Modificar Textos de la escena
            int sceneTextCount = 0;
            var legacyTexts = Object.FindObjectsOfType<Text>(true);
            foreach (var txt in legacyTexts)
            {
                txt.font = ttfFont;
                EditorUtility.SetDirty(txt);
                sceneTextCount++;
                Debug.Log($"[REDES][VISUAL] Fuente asignada en escena -> Legacy Text: '{txt.gameObject.name}'");
            }

            int sceneTmpCount = 0;
            var tmpTexts = Object.FindObjectsOfType<TMPro.TMP_Text>(true);
            foreach (var tmp in tmpTexts)
            {
                tmp.font = sdfAsset;
                EditorUtility.SetDirty(tmp);
                sceneTmpCount++;
                Debug.Log($"[REDES][VISUAL] Fuente asignada en escena -> TextMeshPro: '{tmp.gameObject.name}'");
            }

            // 5. Modificar Botones de la escena
            int sceneBtnCount = 0;
            var buttons = Object.FindObjectsOfType<Button>(true);
            foreach (var btn in buttons)
            {
                ConfigureButton(btn, backgroundSprite);
                sceneBtnCount++;
            }

            // Sincronizar LobbyView en la escena
            var lobbyView = Object.FindObjectOfType<Views.LobbyView>(true);
            if (lobbyView != null)
            {
                // Asignar el font de los botones dinámicos por medio de reflexión para modificar el campo serializado
                var field = typeof(Views.LobbyView).GetField("_buttonFont", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(lobbyView, ttfFont);
                    EditorUtility.SetDirty(lobbyView);
                    Debug.Log("[REDES][VISUAL] Asignada fuente '_buttonFont' en el LobbyView de la escena.");
                }
            }

            // Guardar escena
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
            Debug.Log($"[REDES][VISUAL] Escena guardada con éxito.");

            // 6. Modificar Prefabs
            if (Directory.Exists(PrefabFolder))
            {
                string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder });
                Debug.Log($"[REDES][VISUAL] Se encontraron {prefabGuids.Length} prefabs en '{PrefabFolder}'");

                foreach (var guid in prefabGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject root = PrefabUtility.LoadPrefabContents(path);
                    bool changed = false;

                    // Textos legacy
                    var prefLegacyTexts = root.GetComponentsInChildren<Text>(true);
                    foreach (var txt in prefLegacyTexts)
                    {
                        txt.font = ttfFont;
                        changed = true;
                        Debug.Log($"[REDES][VISUAL] Fuente asignada en prefab -> '{root.name}/{txt.gameObject.name}' (Legacy Text)");
                    }

                    // TMPro
                    var prefTmpTexts = root.GetComponentsInChildren<TMPro.TMP_Text>(true);
                    foreach (var tmp in prefTmpTexts)
                    {
                        tmp.font = sdfAsset;
                        changed = true;
                        Debug.Log($"[REDES][VISUAL] Fuente asignada en prefab -> '{root.name}/{tmp.gameObject.name}' (TextMeshPro)");
                    }

                    // Botones
                    var prefButtons = root.GetComponentsInChildren<Button>(true);
                    foreach (var btn in prefButtons)
                    {
                        ConfigureButton(btn, backgroundSprite);
                        changed = true;
                    }

                    // Si es LobbyView prefab, asignar también la fuente
                    var pLobbyView = root.GetComponentInChildren<Views.LobbyView>(true);
                    if (pLobbyView != null)
                    {
                        var field = typeof(Views.LobbyView).GetField("_buttonFont", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field != null)
                        {
                            field.SetValue(pLobbyView, ttfFont);
                            changed = true;
                            Debug.Log($"[REDES][VISUAL] Asignada fuente '_buttonFont' en prefab '{root.name}'");
                        }
                    }

                    if (changed)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        Debug.Log($"[REDES][VISUAL] Prefab actualizado y guardado: '{path}'");
                    }
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[REDES][VISUAL] === PROCESO VISUAL COMPLETADO ===");
            Debug.Log($"[REDES][VISUAL] Resumen escena: {sceneTextCount} textos legacy, {sceneTmpCount} TMP, {sceneBtnCount} botones actualizados.");
        }

        private static void ConfigureButton(Button btn, Sprite backgroundSprite)
        {
            var img = btn.GetComponent<Image>();
            if (img == null) img = btn.gameObject.AddComponent<Image>();

            if (backgroundSprite != null)
            {
                img.sprite = backgroundSprite;
            }
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 0.54f;

            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.ColorTint;

            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(1f, 1f, 1f, 1f);
            cb.highlightedColor = new Color(1f, 0.2f, 0.2f, 1f);
            cb.pressedColor = new Color(0.08f, 0.1f, 0.16f, 1f);
            cb.selectedColor = new Color(1f, 0.2f, 0.2f, 1f);
            cb.disabledColor = new Color(0.15f, 0.15f, 0.15f, 0.5f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.1f;
            btn.colors = cb;

            // Configurar textos hijos del botón (tamaño automático, anchors estirados y alineación al centro)
            var childTexts = btn.GetComponentsInChildren<Text>(true);
            foreach (var txt in childTexts)
            {
                var rt = txt.rectTransform;
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0, 0);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = Vector2.zero;
                    rt.pivot = new Vector2(0.5f, 0.5f);
                }
                txt.resizeTextForBestFit = true;
                txt.resizeTextMinSize = 10;
                txt.resizeTextMaxSize = 40;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.horizontalOverflow = HorizontalWrapMode.Wrap;
                txt.verticalOverflow = VerticalWrapMode.Truncate;
                EditorUtility.SetDirty(txt);
                Debug.Log($"[REDES][VISUAL] Configurado texto hijo (Legacy) en botón '{btn.gameObject.name}': BestFit=true, Min=10, Max=40");
            }

            var childTmpTexts = btn.GetComponentsInChildren<TMPro.TMP_Text>(true);
            foreach (var tmp in childTmpTexts)
            {
                var rt = tmp.rectTransform;
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0, 0);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = Vector2.zero;
                    rt.pivot = new Vector2(0.5f, 0.5f);
                }
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 10f;
                tmp.fontSizeMax = 40f;
                tmp.alignment = TMPro.TextAlignmentOptions.Center;
                EditorUtility.SetDirty(tmp);
                Debug.Log($"[REDES][VISUAL] Configurado texto hijo (TMP) en botón '{btn.gameObject.name}': AutoSizing=true, Min=10, Max=40");
            }

            EditorUtility.SetDirty(btn);
            EditorUtility.SetDirty(img);

            Debug.Log($"[REDES][VISUAL] Botón configurado -> '{btn.gameObject.name}' (Sprite={img.sprite?.name}, Type=Sliced, PPUMultiplier=0.54)");
        }
    }
}
