using System.Collections;
using UnityEngine;

namespace DebugSystem
{
    public class Weapon : MonoBehaviour
    {
        public string WeaponName = "Pistol";
        public int WeaponIndex = 0;
        public int ClipSize = 12;
        public int MaxReserve = 24;

        [Header("Runtime Stats")]
        [SerializeField] private int currentClip;
        [SerializeField] private int currentReserve;
        [SerializeField] private bool isReloading = false;

        private PlayerModel playerModel;
        private PlayerView playerView;

        private void Awake()
        {
            playerModel = GetComponentInParent<PlayerModel>();
            playerView = GetComponentInParent<PlayerView>();
            currentClip = ClipSize;
            currentReserve = MaxReserve;
        }

        private void Start()
        {
            if (playerModel != null)
            {
                EventBus.TriggerWeaponEquipped(playerModel.ActorID, WeaponName, WeaponIndex);
                TriggerAmmoUpdate();
            }
        }

        public bool TryShoot()
        {
            if (isReloading) return false;
            if (currentClip <= 0)
            {
                EventBus.TriggerShootNoAmmo();
                return false;
            }

            currentClip--;
            EventBus.TriggerShootStart(playerModel.ActorID, WeaponName, currentClip);
            EventBus.TriggerShootConfirmed(playerModel.ActorID, Time.time);
            TriggerAmmoUpdate();

            // Spawn bullet from pool
            Bullet b = BulletPool.Instance.Get();
            b.transform.position = transform.position;
            b.transform.rotation = transform.rotation;
            b.Setup(playerModel.ActorID, transform.right);

            return true;
        }

        public bool CanReload()
        {
            return !isReloading && currentClip < ClipSize && currentReserve > 0;
        }

        public void Reload()
        {
            if (!CanReload()) return;
            StartCoroutine(ReloadRoutine());
        }

        private IEnumerator ReloadRoutine()
        {
            isReloading = true;
            EventBus.TriggerReloadStart(playerModel.ActorID, WeaponName);

            yield return new WaitForSeconds(1.5f); // Reload time

            int needed = ClipSize - currentClip;
            int toLoad = Mathf.Min(needed, currentReserve);
            currentClip += toLoad;
            currentReserve -= toLoad;

            isReloading = false;
            if (playerView != null)
            {
                playerView.StopReloadVFX();
            }

            EventBus.TriggerReloadComplete();
            TriggerAmmoUpdate();
        }

        private void TriggerAmmoUpdate()
        {
            EventBus.TriggerAmmoUpdated(playerModel.ActorID, WeaponName, currentClip, ClipSize, currentReserve);
        }
    }
}
