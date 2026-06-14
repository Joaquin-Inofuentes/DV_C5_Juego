using UnityEngine;
using Game.Core;
using Game.Squad;

namespace Game.Squad
{
    // ==========================================
    // ESTADO: LIDERANDO (Control Manual)
    // ==========================================
    public class LiderandoState : IUnitState
    {
        private float nextFireTime;
        private const float CAM_NORMAL = 8f;
        private const float CAM_SNIPER = 16f;

        public void Enter(UnitController unit)
        {
            if (unit.agent != null) unit.agent.StopAgent();
            unit.view.SetSelectionRing(true);
            unit.view.StopAllBlinks();
            unit.view.HideLine();

            float targetSize = unit.model.specialization == UnitSpecialization.Flancotirador ? CAM_SNIPER : CAM_NORMAL;
            if (LeaderManager.Instance != null)
                LeaderManager.Instance.LerpCameraSize(targetSize, 0.5f);
            else if (Camera.main != null)
                Camera.main.orthographicSize = targetSize;
        }

        public void Update(UnitController unit)
        {
            if (GEN_Inputs.Instance == null) return;

            Vector3 mousePos = GEN_Inputs.Instance.MouseWorldPosition;
            Vector3 dir = (mousePos - unit.transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            unit.view.RotateGraphics(angle);

            // Lógica de Sprint y Estamina
            bool isMoving = GEN_Inputs.Instance.MovimientoInput.sqrMagnitude > 0.01f;
            bool isSprinting = isMoving && GEN_Inputs.Instance.SprintInput;

            if (isSprinting && unit.model.currentStamina > 0f)
            {
                unit.model.currentStamina -= Time.deltaTime;
                if (unit.model.currentStamina < 0f) unit.model.currentStamina = 0f;
            }
            else
            {
                if (unit.model.currentStamina < unit.model.maxStamina)
                {
                    // Recuperar en 4 segundos
                    unit.model.currentStamina += Time.deltaTime * (unit.model.maxStamina / 4f);
                    if (unit.model.currentStamina > unit.model.maxStamina)
                        unit.model.currentStamina = unit.model.maxStamina;
                }
            }

            if (unit.model.specialization == UnitSpecialization.Medico)
            {
                HandleMedicHeal(unit, mousePos);
                return; // Médico no dispara
            }

            if (GEN_Inputs.Instance.DisparoSostenido && Time.time >= nextFireTime)
            {
                if (unit.model.CanFire())
                {
                    unit.shooter.Disparar();
                    unit.model.ConsumeAmmo();
                    nextFireTime = Time.time + unit.model.fireRate;
                }
            }
        }

        private static readonly string[] _medicHealLines =
            { "¡Calma, ya estás bien!", "Tranquilo, camarada.", "¡Ahí va, amigo!" };
        private static readonly string[] _medicGoLines =
            { "¡Ahora voy, camarada!", "¡Enseguida te atiendo!", "¡Voy, aguantá!" };

        private void HandleMedicHeal(UnitController medic, Vector3 mousePos)
        {
            if (!GEN_Inputs.Instance.HealPresionado) return;

            LayerMask allyMask = LayerMask.GetMask("Soldado");
            Collider2D hit = Physics2D.OverlapPoint(mousePos, allyMask);
            if (hit == null) return;

            var ally = hit.GetComponent<UnitController>();
            if (ally == null || ally == medic || ally.model.team != medic.model.team || ally.model.IsDown) return;
            if (ally.model.healthActual >= ally.model.healthMax) return;

            ally.model.AddHealth(15f);
            ally.view.TriggerHealEffect();
            ally.view.ShowSpeech(_medicHealLines[UnityEngine.Random.Range(0, _medicHealLines.Length)], 2f);
            medic.view.ShowSpeech(_medicGoLines[UnityEngine.Random.Range(0, _medicGoLines.Length)], 2f);
            Debug.Log($"<color=green>[Medico]</color> {medic.name} curó a {ally.name}. HP: {ally.model.healthActual:F0}/{ally.model.healthMax:F0}");
        }

        public void FixedUpdate(UnitController unit)
        {
            if (GEN_Inputs.Instance == null) return;
            Vector2 moveDir2D = GEN_Inputs.Instance.MovimientoInput;
            Vector3 moveDir = new Vector3(moveDir2D.x, moveDir2D.y, 0f);

            bool isMoving = moveDir2D.sqrMagnitude > 0.01f;
            bool isSprinting = isMoving && GEN_Inputs.Instance.SprintInput && unit.model.currentStamina > 0.01f;

            float speed = unit.model.speedChase * 2f; // Doble de la velocidad natural
            if (isSprinting)
            {
                speed *= 1.8f;
            }

            Rigidbody2D rb = unit.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.MovePosition(rb.position + (Vector2)moveDir * speed * Time.fixedDeltaTime);
            }
            else
            {
                unit.transform.position += moveDir * speed * Time.fixedDeltaTime;
            }
        }

        public void Exit(UnitController unit)
        {
            unit.view.SetSelectionRing(false);
            if (LeaderManager.Instance != null)
                LeaderManager.Instance.LerpCameraSize(CAM_NORMAL, 0.5f);
            else if (Camera.main != null)
                Camera.main.orthographicSize = CAM_NORMAL;
        }
    }

    // ==========================================
    // ESTADO: SEGUIR FORMACIÓN (Aliados)
    // ==========================================
    public class SeguirFormacionState : IUnitState
    {
        public void Enter(UnitController unit)
        {
            unit.view.StopAllBlinks();
            unit.view.HideLine();
        }

        public void Update(UnitController unit)
        {
            if (GlobalData.liderActual == null)
            {
                unit.CambiarEstado(new EsperandoState());
                return;
            }

            Vector3 posicionLider = GlobalData.liderActual.transform.position;
            float dist = Vector3.Distance(unit.transform.position, posicionLider);
            float distanciaSeguimiento = 3.5f;

            if (dist > distanciaSeguimiento + 0.3f)
            {
                unit.agent.GoTo(posicionLider);
            }
            else if (dist <= distanciaSeguimiento)
            {
                unit.agent.StopAgent();
            }
        }

        public void FixedUpdate(UnitController unit) { }
        public void Exit(UnitController unit) { }
    }

    // ==========================================
    // ESTADO: ATACAR (IA Combate)
    // ==========================================
    public class AtacarState : IUnitState
    {
        private float nextFireTime;

        public void Enter(UnitController unit)
        {
            unit.agent.StopAgent();
            unit.view.StartBlink(IndicatorType.Combat);
        }

        public void Update(UnitController unit)
        {
            if (unit.target == null) 
            { 
                unit.ResetHelpPriority(); 
                unit.CambiarEstado(unit.model.team == UnitTeam.PlayerTeam ? (IUnitState)new SeguirFormacionState() : new EsperandoState()); 
                return; 
            }

            // Si el objetivo cayó, limpiar
            var targetUnit = unit.target.GetComponent<UnitController>();
            if (targetUnit != null && targetUnit.model.IsDown) 
            { 
                unit.target = null; 
                unit.ResetHelpPriority(); 
                unit.CambiarEstado(unit.model.team == UnitTeam.PlayerTeam ? (IUnitState)new SeguirFormacionState() : new EsperandoState()); 
                return; 
            }

            Vector3 dir = (unit.target.position - unit.transform.position).normalized;
            unit.view.RotateGraphicsSmooth(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, 10f);
            unit.view.ShowLineToTarget(unit.transform.position, unit.target.position);

            // LOS solo contra obstáculos (no aliados)
            LayerMask obsMask = LayerMask.GetMask("Obstacles", "Obstaculos");
            if (obsMask == 0) obsMask = (1 << 6) | (1 << 14);
            bool hasLOS = Physics2D.Linecast(unit.transform.position, unit.target.position, obsMask).collider == null;

            float dist = Vector3.Distance(unit.transform.position, unit.target.position);
            if (!hasLOS || dist > unit.model.attackRange)
            {
                unit.CambiarEstado(new PerseguirState());
                return;
            }

            if (Time.time >= nextFireTime && unit.model.CanFire())
            {
                unit.shooter.Disparar();
                unit.model.ConsumeAmmo();
                nextFireTime = Time.time + unit.model.fireRate;
            }
        }

        public void FixedUpdate(UnitController unit) { }

        public void Exit(UnitController unit)
        {
            unit.view.StopBlink(IndicatorType.Combat);
            unit.view.HideLine();
        }
    }

    // ==========================================
    // ESTADO: PERSEGUIR (IA)
    // ==========================================
    public class PerseguirState : IUnitState
    {
        public void Enter(UnitController unit) => unit.view.StartBlink(IndicatorType.Combat);

        public void Update(UnitController unit)
        {
            if (unit.target == null) 
            { 
                unit.ResetHelpPriority(); 
                unit.CambiarEstado(unit.model.team == UnitTeam.PlayerTeam ? (IUnitState)new SeguirFormacionState() : new EsperandoState()); 
                return; 
            }

            var targetUnit = unit.target.GetComponent<UnitController>();
            if (targetUnit != null && targetUnit.model.IsDown) 
            { 
                unit.target = null; 
                unit.ResetHelpPriority(); 
                unit.CambiarEstado(unit.model.team == UnitTeam.PlayerTeam ? (IUnitState)new SeguirFormacionState() : new EsperandoState()); 
                return; 
            }

            LayerMask obsMask = LayerMask.GetMask("Obstacles", "Obstaculos");
            if (obsMask == 0) obsMask = (1 << 6) | (1 << 14);
            
            RaycastHit2D hit = Physics2D.Linecast(unit.transform.position, unit.target.position, obsMask);
            bool hasLOS = hit.collider == null || hit.collider.transform == unit.target || hit.collider.transform.IsChildOf(unit.target);
            
            float dist = Vector3.Distance(unit.transform.position, unit.target.position);

            if (hasLOS && dist <= unit.model.attackRange)
            {
                unit.CambiarEstado(new AtacarState());
                return;
            }

            // Evitar que se peguen si por alguna razón no tienen LOS pero están muy cerca
            if (dist > unit.model.attackRange * 0.8f || !hasLOS && dist > 2f)
            {
                unit.agent.GoTo(unit.target.position);
            }
            else
            {
                unit.agent.StopAgent();
            }

            unit.view.ShowLineToTarget(unit.transform.position, unit.target.position);
        }

        public void FixedUpdate(UnitController unit) { }

        public void Exit(UnitController unit)
        {
            unit.view.StopBlink(IndicatorType.Combat);
            unit.view.HideLine();
        }
    }

    // ==========================================
    // ESTADO: ESPERANDO (tras orden, 5 segundos)
    // ==========================================
    public class EsperandoState : IUnitState
    {
        private float waitTimer;
        private float waitDuration;
        private bool timed;

        public EsperandoState() { timed = false; }

        public EsperandoState(float duration)
        {
            timed = true;
            waitDuration = duration;
        }

        public void Enter(UnitController unit)
        {
            unit.agent.StopAgent();
            waitTimer = 0f;
            if (timed)
            {
                unit.isWaitingOrder = true;
                unit.view.StartBlink(IndicatorType.Moving);
            }
        }

        public void Update(UnitController unit)
        {
            if (!timed) return;

            waitTimer += Time.deltaTime;
            if (waitTimer >= waitDuration)
            {
                unit.CambiarEstado(new SeguirFormacionState());
            }
        }

        public void FixedUpdate(UnitController unit) { }

        public void Exit(UnitController unit)
        {
            unit.isWaitingOrder = false;
            unit.view.StopBlink(IndicatorType.Moving);
        }
    }

    // ==========================================
    // ESTADO: HUIR DETRÁS DEL LÍDER (COBERTURA)
    // ==========================================
    public class HuirDetrasLiderState : IUnitState
    {
        public void Enter(UnitController unit) { }

        public void Update(UnitController unit)
        {
            UnitController leader = GlobalData.liderActual;

            if (leader == null || leader == unit)
            {
                unit.CambiarEstado(new SeguirFormacionState());
                return;
            }

            if (unit.target == null)
            {
                unit.CambiarEstado(new SeguirFormacionState());
                return;
            }

            Vector3 dirEnemigoAlLider = (leader.transform.position - unit.target.position).normalized;
            Vector3 puntoCobertura = leader.transform.position + dirEnemigoAlLider * 2.5f;

            unit.agent.GoTo(puntoCobertura);

            if (unit.model.healthActual / unit.model.healthMax > 0.5f)
            {
                unit.CambiarEstado(new SeguirFormacionState());
            }
        }

        public void FixedUpdate(UnitController unit) { }
        public void Exit(UnitController unit) { }
    }

    // ==========================================
    // ESTADO: IR A DESTINO (Orden manual)
    // ==========================================
    public class IrADestinoState : IUnitState
    {
        public void Enter(UnitController unit)
        {
            Debug.Log($"<color=cyan>[Estado]</color> {unit.name} → IrADestino hacia {unit.GetTargetPoint()}");
            unit.isWaitingOrder = true;
            unit.agent.GoTo(unit.GetTargetPoint());
            unit.view.StartBlink(IndicatorType.Moving);
        }

        public void Update(UnitController unit)
        {
            // Línea verde al destino
            unit.view.ShowLineToDestination(unit.transform.position, unit.GetTargetPoint());

            if (unit.ReachedDestination())
            {
                Debug.Log($"<color=cyan>[Estado]</color> {unit.name} llegó a destino. → Esperando 5s");
                unit.CambiarEstado(new EsperandoState(5f));
            }
        }

        public void FixedUpdate(UnitController unit) { }

        public void Exit(UnitController unit)
        {
            unit.isWaitingOrder = false;
            unit.view.StopBlink(IndicatorType.Moving);
            unit.view.HideLine();
        }
    }
}
