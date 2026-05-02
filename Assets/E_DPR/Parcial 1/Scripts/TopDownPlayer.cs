using Fusion;
using UnityEngine;

public class TopDownPlayer : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private NetworkObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    [Networked] public int Ammo { get; set; }
    [Networked] private TickTimer shootCooldown { get; set; }
    private bool _wasShootPressed;

    public override void Spawned()
    {
        if (Object.HasStateAuthority) Ammo = 20;
        if (Object.HasInputAuthority && TopDownCameraFollow.Instance != null)
            TopDownCameraFollow.Instance.SetTarget(transform);
    }

    private void Update()
    {
        if (Object.HasInputAuthority && Input.GetMouseButtonDown(0)) _wasShootPressed = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (TopDownGameManager.Instance == null || !TopDownGameManager.Instance.MatchStarted) return;

        // Movimiento
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        transform.position += new Vector3(h, 0, v).normalized * moveSpeed * Runner.DeltaTime;

        // Rotación (HandleAiming)
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 dir = hit.point - transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.1f) transform.rotation = Quaternion.LookRotation(dir);
        }

        // Disparo
        if (_wasShootPressed && Ammo > 0 && shootCooldown.ExpiredOrNotRunning(Runner))
        {
            Ammo--;
            shootCooldown = TickTimer.CreateFromSeconds(Runner, 0.3f);
            Runner.Spawn(projectilePrefab, firePoint.position, firePoint.rotation, Object.InputAuthority, (r, obj) => {
                obj.GetComponent<TopDownProjectile>().Initialize(Object.InputAuthority.PlayerId, 25);
            });
        }
        _wasShootPressed = false;
    }
}