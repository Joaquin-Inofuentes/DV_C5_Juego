using Fusion;
using UnityEngine;

public class TopDownPlayer : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private NetworkObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    public override void Spawned()
    {
        Debug.Log($"<color=magenta>[Player] Spawned ID: {Object.InputAuthority.PlayerId}</color>");
        if (Object.HasInputAuthority && TopDownCameraFollow.Instance != null)
        {
            TopDownCameraFollow.Instance.SetTarget(transform);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasInputAuthority) return;

        if (TopDownGameManager.Instance == null || !TopDownGameManager.Instance.MatchStarted) return;

        HandleMovement();
        HandleAiming();
        HandleShooting();
    }

    private void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        if (h != 0 || v != 0)
        {
            Vector3 move = new Vector3(h, 0, v).normalized;
            transform.position += move * moveSpeed * Runner.DeltaTime;
        }
    }

    private void HandleAiming()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 targetPos = hit.point;
            targetPos.y = transform.position.y;
            Vector3 dir = targetPos - transform.position;
            if (dir.sqrMagnitude > 0.1f) transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    private void HandleShooting()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log($"<color=orange>[Acción] Disparo de P{Object.InputAuthority.PlayerId}</color>");

            Runner.Spawn(projectilePrefab, firePoint.position, firePoint.rotation, Object.InputAuthority, (runner, obj) =>
            {
                var proj = obj.GetComponent<TopDownProjectile>();
                if (proj != null)
                    proj.Initialize(Object.InputAuthority.PlayerId, gameObject, 25);
            });
        }
    }

    public void EnableInput() { }
}