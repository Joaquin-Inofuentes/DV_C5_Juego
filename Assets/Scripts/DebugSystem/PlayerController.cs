using UnityEngine;

namespace DebugSystem
{
    public class PlayerController : MonoBehaviour
    {
        private PlayerModel model;
        private PlayerView view;
        private Weapon weapon;

        [Header("Movement")]
        public float MoveSpeed = 5f;

        private void Awake()
        {
            model = GetComponent<PlayerModel>();
            view = GetComponent<PlayerView>();
            weapon = GetComponentInChildren<Weapon>();
        }

        private void Start()
        {
            if (model != null && model.ActorID != LocalNetworkMock.LocalActorID)
            {
                enabled = false;
            }
        }

        private void Update()
        {
            // Simple keyboard movement
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 moveDir = new Vector3(h, v, 0).normalized;
            if (moveDir.magnitude > 0)
            {
                transform.position += moveDir * MoveSpeed * Time.deltaTime;
                EventBus.TriggerInputReceived("Move", Time.frameCount);
            }

            // Aim towards mouse cursor
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            Vector3 aimDir = (mousePos - transform.position).normalized;
            if (aimDir.magnitude > 0)
            {
                float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }

            // Shoot with Left Click
            if (Input.GetMouseButtonDown(0))
            {
                EventBus.TriggerInputReceived("ShootPress", Time.frameCount);
                if (weapon != null)
                {
                    if (weapon.TryShoot())
                    {
                        view.PlayShootVFX(weapon.transform.position);
                    }
                }
            }

            // Reload with R
            if (Input.GetKeyDown(KeyCode.R))
            {
                EventBus.TriggerInputReceived("ReloadPress", Time.frameCount);
                if (weapon != null && weapon.CanReload())
                {
                    view.PlayReloadVFX();
                    weapon.Reload();
                }
            }
        }
    }
}
