using UnityEngine;
using System.Collections.Generic;

namespace DebugSystem
{
    public class EffectPool : MonoBehaviour
    {
        public static EffectPool Instance { get; private set; }

        public int initialSize = 20;
        private List<ImpactEffect> pool = new List<ImpactEffect>();
        private GameObject impactPrefab;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                CreatePrefab();
                InitializePool();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void CreatePrefab()
        {
            impactPrefab = new GameObject("ImpactPrefab");
            impactPrefab.SetActive(false);
            SpriteRenderer sr = impactPrefab.AddComponent<SpriteRenderer>();
            
            // Generate a simple circular sprite procedurally
            Texture2D texture = new Texture2D(32, 32);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                    if (dist <= 15.5f)
                        texture.SetPixel(x, y, Color.white);
                    else
                        texture.SetPixel(x, y, Color.clear);
                }
            }
            texture.Apply();
            Sprite knob = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
            
            sr.sprite = knob;
            sr.sortingOrder = 10;
            
            impactPrefab.AddComponent<ImpactEffect>();
            impactPrefab.transform.SetParent(transform);
        }

        private void InitializePool()
        {
            for (int i = 0; i < initialSize; i++)
            {
                CreateNewEffect();
            }
        }

        private ImpactEffect CreateNewEffect()
        {
            GameObject go = Instantiate(impactPrefab, transform);
            go.SetActive(false);
            ImpactEffect effect = go.GetComponent<ImpactEffect>();
            pool.Add(effect);
            return effect;
        }

        public ImpactEffect GetEffect()
        {
            foreach (var effect in pool)
            {
                if (!effect.gameObject.activeInHierarchy)
                {
                    return effect;
                }
            }
            return CreateNewEffect(); // Expand pool if needed
        }

        public void SpawnImpact(Vector3 position, Color color)
        {
            ImpactEffect fx = GetEffect();
            fx.PlayEffect(position, color);
            
            // Play Audio
            AudioClip clip = ProceduralAudioGenerator.GetImpactSound();
            ProceduralAudioGenerator.PlayClipAtPoint(clip, position, 0.5f);
        }
    }
}
