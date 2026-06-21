using UnityEngine;

namespace DebugSystem
{
    public class ImpactEffect : MonoBehaviour
    {
        private SpriteRenderer spr;
        private float lifeTime = 0.2f;
        private float timer = 0f;

        private void Awake()
        {
            spr = GetComponent<SpriteRenderer>();
        }

        public void PlayEffect(Vector3 position, Color color)
        {
            transform.position = position;
            transform.localScale = Vector3.one * 0.5f;
            if (spr != null)
            {
                spr.color = color;
            }
            timer = lifeTime;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (timer > 0)
            {
                timer -= Time.deltaTime;
                float normalizedTime = timer / lifeTime;

                // Shrink and fade
                transform.localScale = Vector3.one * (0.5f * normalizedTime);
                if (spr != null)
                {
                    Color c = spr.color;
                    c.a = normalizedTime;
                    spr.color = c;
                }

                if (timer <= 0)
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }
}
