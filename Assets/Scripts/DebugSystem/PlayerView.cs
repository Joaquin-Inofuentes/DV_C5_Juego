using UnityEngine;

namespace DebugSystem
{
    public class PlayerView : MonoBehaviour
    {
        private PlayerModel model;
        private SimpleAnimator anim;

        private void Awake()
        {
            model = GetComponent<PlayerModel>();
            // Animator can be on the same object or on a child 'Visuals'
            anim = GetComponentInChildren<SimpleAnimator>();
        }

        private void OnEnable()
        {
            if (model != null)
            {
                model.OnHealthChanged += HandleHealthChanged;
            }
        }

        private void OnDisable()
        {
            if (model != null)
            {
                model.OnHealthChanged -= HandleHealthChanged;
            }
        }

        private void HandleHealthChanged(float hp, float shield)
        {
            if (hp > 0)
            {
                if (anim != null) anim.TriggerHit();
                
                EventBus.TriggerAnimTrigger(model.ActorID, "Hit");
                EventBus.TriggerAnimDamage(model.ActorID, "hit", transform.position.x, transform.position.y, transform.position.z);
                EventBus.TriggerDamageUI(model.ActorID, "front");
                EventBus.TriggerSFX("PlayerHurt", model.ActorID, 1.0f);
            }
            else
            {
                EventBus.TriggerAnimTrigger(model.ActorID, "Die");
                EventBus.TriggerAnimDeath(model.ActorID, "normal");
                EventBus.TriggerSFX("PlayerDeath", model.ActorID, 1.0f);
                if (anim != null) anim.TriggerHit(); // Final flash
            }
        }

        public void PlayShootVFX(Vector3 weaponPos)
        {
            Vector3 shootDir = (weaponPos - transform.position).normalized;
            if (anim != null) anim.TriggerShoot(shootDir);

            EventBus.TriggerAnimTrigger(model.ActorID, "Shoot");
            EventBus.TriggerVFX("MuzzleFlash", weaponPos.x, weaponPos.y, weaponPos.z, model.ActorID);
            EventBus.TriggerSFX("PistolShot", model.ActorID, 1.0f);
            
            AudioClip clip = ProceduralAudioGenerator.GetShootSound();
            ProceduralAudioGenerator.PlayClipAtPoint(clip, weaponPos, 0.4f);
        }

        public void PlayReloadVFX()
        {
            EventBus.TriggerAnimTrigger(model.ActorID, "Reload");
            EventBus.TriggerAnimReload(model.ActorID, "normal");
            EventBus.TriggerWeaponAnimBool(model.ActorID, "isReloading", true);
            EventBus.TriggerSFX("PistolReload", model.ActorID, 1.0f);
        }

        public void StopReloadVFX()
        {
            EventBus.TriggerWeaponAnimBool(model.ActorID, "isReloading", false);
        }
    }
}
