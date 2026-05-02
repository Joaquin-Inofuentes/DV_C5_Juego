using Fusion;
using UnityEngine;

public class TopDownPlayer : NetworkBehaviour
{
    [Header("Configuración Básica")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private NetworkObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private NetworkMecanimAnimator networkAnimator;

    [Header("Teletransporte")]
    [SerializeField] private float teleportCooldownTime = 2f;
    [SerializeField] private float mobileTeleportDistance = 5f; // Distancia fija para táctil
    [Networked] private TickTimer teleportCooldown { get; set; }

    [Networked] public int Ammo { get; set; }
    [Networked] private TickTimer shootCooldown { get; set; }

    private Vector2 _moveInput;
    private Vector2 _shootInput;
    private bool _mouseShootRequest;
    private bool _teleportRequest; // Flag para el Update -> FixedUpdate

    public override void Spawned()
    {
        if (Object.HasStateAuthority) Ammo = 20;

        if (Object.HasInputAuthority)
        {
            JoystickController.OnJoystickAction += HandleJoystickInput;
            if (TopDownCameraFollow.Instance != null)
                TopDownCameraFollow.Instance.SetTarget(transform);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        JoystickController.OnJoystickAction -= HandleJoystickInput;
    }

    private void HandleJoystickInput(string action, Vector2 value)
    {
        if (!Object.HasInputAuthority) return;
        if (action == "Move") _moveInput = value;
        else if (action == "Shoot") _shootInput = value;
    }

    private void Update()
    {
        if (!Object.HasInputAuthority) return;

        // Disparo con Mouse
        if (Input.GetMouseButtonDown(0) && !Application.isMobilePlatform)
            _mouseShootRequest = true;

        // Teletransporte con Espacio
        if (Input.GetKeyDown(KeyCode.Space))
            _teleportRequest = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (TopDownGameManager.Instance == null || !TopDownGameManager.Instance.MatchStarted) return;

        // 1. MOVIMIENTO NORMAL
        float h = Input.GetAxisRaw("Horizontal") + _moveInput.x;
        float v = Input.GetAxisRaw("Vertical") + _moveInput.y;
        Vector3 moveDir = new Vector3(h, 0, v);
        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

        transform.position += moveDir * moveSpeed * Runner.DeltaTime;

        // 2. LÓGICA DE TELETRANSPORTE
        if (_teleportRequest)
        {
            if (teleportCooldown.ExpiredOrNotRunning(Runner))
            {
                ExecuteTeleport(moveDir);
            }
            _teleportRequest = false;
        }

        // 3. LÓGICA DE DISPARO
        bool isShootingJoystick = _shootInput.magnitude > 0.5f;
        if ((_mouseShootRequest || isShootingJoystick) && Ammo > 0 && shootCooldown.ExpiredOrNotRunning(Runner))
        {
            ExecuteShoot();
        }
        _mouseShootRequest = false;
    }

    private void ExecuteTeleport(Vector3 currentMoveDir)
    {
        Vector3 targetPos = transform.position;

        // Si es PC: Teletransportar al Mouse
        if (!Application.isMobilePlatform && Input.mousePresent)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                targetPos = hit.point;
            }
        }
        // Si es Móvil: Teletransportar hacia donde se mueve
        else
        {
            if (currentMoveDir.sqrMagnitude > 0.1f)
                targetPos = transform.position + (currentMoveDir.normalized * mobileTeleportDistance);
            else
                targetPos = transform.position + (transform.forward * mobileTeleportDistance);
        }

        // Ajustar altura para que no aparezca debajo del suelo
        targetPos.y = transform.position.y;

        // Aplicar posición
        transform.position = targetPos;

        // Iniciar Cooldown
        teleportCooldown = TickTimer.CreateFromSeconds(Runner, teleportCooldownTime);

        // Opcional: Feedback visual (puedes disparar un trigger de animación o partículas)
        Debug.Log("[Teleport] Jugador teletransportado");
    }

    public override void Render()
    {
        if (!Object.HasInputAuthority) return;

        // ROTACIÓN VISUAL
        if (_shootInput.sqrMagnitude > 0.1f)
        {
            RotatePlayer(new Vector3(_shootInput.x, 0, _shootInput.y));
        }
        else if (!Application.isMobilePlatform && Input.mousePresent)
        {
            HandleMouseRotation();
        }
        else if (_moveInput.sqrMagnitude > 0.1f)
        {
            RotatePlayer(new Vector3(_moveInput.x, 0, _moveInput.y));
        }
    }

    private void HandleMouseRotation()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 dir = hit.point - transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.1f) RotatePlayer(dir);
        }
    }

    private void RotatePlayer(Vector3 direction)
    {
        if (direction.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    private void ExecuteShoot()
    {
        Ammo--;
        shootCooldown = TickTimer.CreateFromSeconds(Runner, 0.3f);
        if (networkAnimator != null) networkAnimator.SetTrigger("OnShoot");

        Runner.Spawn(projectilePrefab, firePoint.position, firePoint.rotation, Object.InputAuthority, (r, obj) => {
            obj.GetComponent<TopDownProjectile>().Initialize(Object.InputAuthority, 25);
        });
    }
}