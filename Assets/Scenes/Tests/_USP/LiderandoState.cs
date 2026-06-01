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
        }

        public void Update(UnitController unit)
        {
            if (GEN_Inputs.Instance == null) return;

            // Rotar hacia el mouse
            Vector3 mousePos = GEN_Inputs.Instance.MouseWorldPosition;
            Vector3 dir = (mousePos - unit.transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            unit.transform.rotation = Quaternion.Euler(0, 0, angle);

            // Disparo manual
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
        public void Enter(UnitController unit) { }

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

        public void Enter(UnitController unit) => unit.agent.StopAgent();

        public void Update(UnitController unit)
        {
            if (unit.target == null)
            {
                unit.CambiarEstado(new SeguirFormacionState());
                return;
            }

            // Mirar al objetivo
            Vector3 dir = (unit.target.position - unit.transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            unit.transform.rotation = Quaternion.Slerp(unit.transform.rotation, Quaternion.Euler(0, 0, angle), Time.deltaTime * 10f);

            if (Time.time >= nextFireTime && unit.model.CanFire())
            {
                unit.shooter.Disparar();
                unit.model.ConsumeAmmo();
                nextFireTime = Time.time + unit.model.fireRate;
            }

            // Si se aleja mucho, perseguir
            if (Vector3.Distance(unit.transform.position, unit.target.position) > unit.model.attackRange)
            {
                unit.CambiarEstado(new PerseguirState());
            }
        }

        public void FixedUpdate(UnitController unit) { }
        public void Exit(UnitController unit) { }
    }

    // ==========================================
    // ESTADO: PERSEGUIR (IA)
    // ==========================================
    public class PerseguirState : IUnitState
    {
        public void Enter(UnitController unit) { }

        public void Update(UnitController unit)
        {
            if (unit.target == null)
            {
                unit.CambiarEstado(new SeguirFormacionState());
                return;
            }

            unit.agent.GoTo(unit.target.position);

            if (Vector3.Distance(unit.transform.position, unit.target.position) <= unit.model.attackRange)
            {
                unit.CambiarEstado(new AtacarState());
            }
        }

        public void FixedUpdate(UnitController unit) { }
        public void Exit(UnitController unit) { }
    }

    // ==========================================
    // ESTADO: ESPERANDO
    // ==========================================
    public class EsperandoState : IUnitState
    {
        public void Enter(UnitController unit) => unit.agent.StopAgent();
        public void Update(UnitController unit) { }
        public void FixedUpdate(UnitController unit) { }
        public void Exit(UnitController unit) { }
    }

    // ==========================================
    // ESTADO: HUIR DETRÁS DEL LÍDER (COBERTURA)
    // ==========================================
    public class HuirDetrasLiderState : IUnitState
    {
        public void Enter(UnitController unit) { }

        public void Update(UnitController unit)
        {
            // AQUÍ ESTABA TU ERROR: Ahora usamos UnitController correctamente
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

            // Posicionarse en el lado opuesto al enemigo respecto al líder
            Vector3 dirEnemigoAlLider = (leader.transform.position - unit.target.position).normalized;
            Vector3 puntoCobertura = leader.transform.position + dirEnemigoAlLider * 2.5f;

            unit.agent.GoTo(puntoCobertura);

            // Si recupera vida o el enemigo muere, vuelve a formación
            if (unit.model.healthActual / unit.model.healthMax > 0.5f)
            {
                unit.CambiarEstado(new SeguirFormacionState());
            }
        }

        public void FixedUpdate(UnitController unit) { }
        public void Exit(UnitController unit) { }
    }
}