using UnityEngine;
using UnityEngine.UI;

namespace DebugSystem
{
    public class FloatingHealthBar : MonoBehaviour
    {
        private PlayerModel targetModel;
        private Image fillImage;
        private Text nameText;

        public void Setup(PlayerModel model, string username)
        {
            targetModel = model;
            
            // Generate simple Canvas hierarchy if it doesn't exist
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rt = GetComponent<RectTransform>();
            
            // Counter-scale so the health bar maintains consistent size and position
            Vector3 parentScale = transform.parent != null ? transform.parent.localScale : Vector3.one;
            transform.localScale = new Vector3(1f / parentScale.x, 1f / parentScale.y, 1f);
            
            rt.sizeDelta = new Vector2(2f, 0.5f);
            rt.localPosition = new Vector3(0f, 1.5f / parentScale.y, 0f); // 1.5 units above the center in world space

            // Background
            GameObject bgGo = new GameObject("BG");
            bgGo.transform.SetParent(transform, false);
            Image bg = bgGo.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            RectTransform bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0f, 0f);
            bgRt.anchorMax = new Vector2(1f, 0.5f);
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            // Fill
            GameObject fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(bgGo.transform, false);
            fillImage = fillGo.AddComponent<Image>();
            fillImage.color = Color.green;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = 1f;
            RectTransform fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            // Text
            GameObject textGo = new GameObject("NameText");
            textGo.transform.SetParent(transform, false);
            nameText = textGo.AddComponent<Text>();
            Font defaultFont = null;
            try
            {
                defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch {}

            if (defaultFont == null)
            {
                try
                {
                    defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                catch {}
            }

            if (defaultFont == null)
            {
                Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
                if (fonts != null && fonts.Length > 0)
                {
                    defaultFont = fonts[0];
                }
            }

            nameText.font = defaultFont;
            
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.color = Color.white;
            nameText.text = username;
            
            // Font size handling for world space
            nameText.fontSize = 20;
            nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
            nameText.verticalOverflow = VerticalWrapMode.Overflow;
            nameText.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);

            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0f, 0.5f);
            textRt.anchorMax = new Vector2(1f, 1f);
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            if (targetModel != null)
            {
                targetModel.OnHealthChanged += HandleHealthChanged;
            }
        }

        private void OnDestroy()
        {
            if (targetModel != null)
            {
                targetModel.OnHealthChanged -= HandleHealthChanged;
            }
        }

        private void HandleHealthChanged(float hp, float shield)
        {
            if (targetModel != null && fillImage != null)
            {
                float ratio = Mathf.Clamp01(hp / targetModel.MaxHP);
                StartCoroutine(SmoothFill(ratio));
            }
        }

        private System.Collections.IEnumerator SmoothFill(float targetFill)
        {
            float startFill = fillImage.fillAmount;
            float time = 0f;
            float duration = 0.2f;

            while (time < duration)
            {
                time += Time.deltaTime;
                fillImage.fillAmount = Mathf.Lerp(startFill, targetFill, time / duration);
                
                // Color Lerp (Green -> Yellow -> Red)
                if (fillImage.fillAmount > 0.5f)
                    fillImage.color = Color.Lerp(Color.yellow, Color.green, (fillImage.fillAmount - 0.5f) * 2f);
                else
                    fillImage.color = Color.Lerp(Color.red, Color.yellow, fillImage.fillAmount * 2f);

                yield return null;
            }
            fillImage.fillAmount = targetFill;
        }

        private void LateUpdate()
        {
            // Lock rotation so it always faces properly (useful if characters rotate)
            transform.rotation = Quaternion.identity;
        }
    }
}
