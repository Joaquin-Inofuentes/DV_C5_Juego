using UnityEngine;
using System.Collections;

namespace DebugSystem
{
    public class SimpleAnimator : MonoBehaviour
    {
        private SpriteRenderer spr;
        private Color originalColor;
        private Vector3 originalLocalPosition;

        private void Awake()
        {
            spr = GetComponent<SpriteRenderer>();
            if (spr != null)
            {
                originalColor = spr.color;
            }
            originalLocalPosition = transform.localPosition;
        }

        public void TriggerHit()
        {
            StopCoroutine("HitCoroutine");
            StartCoroutine("HitCoroutine");
        }

        public void TriggerShoot(Vector3 direction)
        {
            StopCoroutine("RecoilCoroutine");
            StartCoroutine("RecoilCoroutine", direction);
        }

        private IEnumerator HitCoroutine()
        {
            if (spr == null) yield break;

            spr.color = Color.white;
            yield return new WaitForSeconds(0.05f);
            spr.color = Color.red;
            yield return new WaitForSeconds(0.05f);
            spr.color = originalColor;
        }

        private IEnumerator RecoilCoroutine(Vector3 direction)
        {
            // Move opposite to aim direction
            Vector3 recoilVector = -direction.normalized * 0.5f;
            transform.localPosition = originalLocalPosition + recoilVector;

            float recoverySpeed = 5f;
            while (Vector3.Distance(transform.localPosition, originalLocalPosition) > 0.01f)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, originalLocalPosition, Time.deltaTime * recoverySpeed);
                yield return null;
            }
            transform.localPosition = originalLocalPosition;
        }
    }
}
