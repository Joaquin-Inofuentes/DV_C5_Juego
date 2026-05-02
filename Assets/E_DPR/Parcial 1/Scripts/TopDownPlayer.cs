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
        // Solo el dueño mueve el objeto físicamente
        if (!Object.HasStateAuthority) return;
        if (TopDownGameManager.Instance == null || !TopDownGameManager.Instance.MatchStarted) return;

        // MOVIMIENTO FÍSICO
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 moveDir = new Vector3(h, 0, v).normalized;
        transform.position += moveDir * moveSpeed * Runner.DeltaTime;

        // DISPARO
        if (_wasShootPressed && Ammo > 0 && shootCooldown.ExpiredOrNotRunning(Runner))
        {
            ExecuteShoot();
        }
        _wasShootPressed = false;
    }

    // NUEVO: Usamos Render para la rotación visual para eliminar el delay del ratón
    public override void Render()
    {
        // Solo rotamos localmente para el que controla al personaje
        if (Object.HasInputAuthority)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Vector3 dir = hit.point - transform.position;
                dir.y = 0;
                if (dir.sqrMagnitude > 0.1f)
                {
                    // Rotación suave pero instantánea visualmente
                    transform.rotation = Quaternion.LookRotation(dir);
                }
            }
        }
    }
    // Añade esta referencia
    [SerializeField] private NetworkMecanimAnimator networkAnimator;

    private void ExecuteShoot()
    {
        Ammo--;
        shootCooldown = TickTimer.CreateFromSeconds(Runner, 0.3f);

        // En lugar de usar playerAnimator.SetTrigger:
        if (networkAnimator != null)
        {
            networkAnimator.SetTrigger("OnShoot");
        }

        Runner.Spawn(projectilePrefab, firePoint.position, firePoint.rotation, Object.InputAuthority, (r, obj) => {
            obj.GetComponent<TopDownProjectile>().Initialize(Object.InputAuthority, 25);
        });
    }
}