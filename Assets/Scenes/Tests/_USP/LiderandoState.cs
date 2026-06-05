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

        public void Enter(UnitController unit)
        {
            if (unit.agent != null) unit.agent.StopAgent();
            unit.view.SetSelectionRing(true);
            unit.view.StopAllBlinks();
            unit.view.HideLine();
        }

        public void Update(UnitController unit)
        {
            if (GEN_Inputs.Instance == null) return;

            Vector3 mousePos = GEN_Inputs.Instance.MouseWorldPosition;
            Vector3 dir = (mousePos - unit.transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            unit.view.RotateGraphics(angle);

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

        public void FixedUpdate(UnitController unit)
        {
            Vector2 moveDir2D = GEN_Inputs.Instance.MovimientoInput;
            Vector3 moveDir = new Vector3(moveDir2D.x, moveDir2D.y, 0f);
            unit.transform.position += moveDir * unit.model.speedChase * Time.deltaTime;
        }

        public void Exit(UnitController unit) => unit.view.SetSelectionRing(false);
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
            if (unit.currentSlot != null)
            {
                unit.agent.GoTo(unit.currentSlot.position);
            }
            else
            {
                unit.CambiarEstado(new EsperandoState());
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
                unit.CambiarEstado(new SeguirFormacionState());
                return;
            }

            // Rotar gráfica hacia el enemigo
            Vector3 dir = (unit.target.position - unit.transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            unit.view.RotateGraphicsSmooth(angle, 10f);

            // Línea roja al objetivo
            unit.view.ShowLineToTarget(unit.transform.position, unit.target.position);

            // Comprobar visibilidad: raycast/linecast contra obstáculos
            bool visionDirecta = true;
            LayerMask mask = LayerMask.GetMask("Obstacles", "Obstaculos");
            if (mask == 0) mask = (1 << 6) | (1 << 14); // Fallback a capa 6 y 14

            RaycastHit2D hit = Physics2D.Linecast(unit.transform.position, unit.target.position, mask);
            if (hit.collider != null)
            {
                visionDirecta = false;
            }

            if (visionDirecta && Time.time >= nextFireTime && unit.model.CanFire())
            {
                unit.shooter.Disparar();
                unit.model.ConsumeAmmo();
                nextFireTime = Time.time + unit.model.fireRate;
            }

            if (Vector3.Distance(unit.transform.position, unit.target.position) > unit.model.attackRange || !visionDirecta)
            {
                unit.CambiarEstado(new PerseguirState());
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
        public void Enter(UnitController unit)
        {
            unit.view.StartBlink(IndicatorType.Combat);
        }

        public void Update(UnitController unit)
        {
            if (unit.target == null)
            {
                unit.ResetHelpPriority();
                unit.CambiarEstado(new SeguirFormacionState());
                return;
            }

            unit.agent.GoTo(unit.target.position);

            // Línea roja al enemigo
            unit.view.ShowLineToTarget(unit.transform.position, unit.target.position);

            // Comprobar visibilidad: raycast/linecast contra obstáculos
            bool visionDirecta = true;
            LayerMask mask = LayerMask.GetMask("Obstacles", "Obstaculos");
            if (mask == 0) mask = (1 << 6) | (1 << 14); // Fallback a capa 6 y 14

            RaycastHit2D hit = Physics2D.Linecast(unit.transform.position, unit.target.position, mask);
            if (hit.collider != null)
            {
                visionDirecta = false;
            }

            if (Vector3.Distance(unit.transform.position, unit.target.position) <= unit.model.attackRange && visionDirecta)
            {
                unit.CambiarEstado(new AtacarState());
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
