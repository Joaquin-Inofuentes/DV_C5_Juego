using UnityEngine;
using USP.Entities;

namespace Game.Squad
{
    // ==========================================
    // ESTADO: LIDERANDO (Control Manual)
    // ==========================================
    public class LiderandoState : ISoldierState
    {
        private float nextFireTime;
        private bool loggedMissingInputs = false;

        public void Enter(SoldierController controller)
        {
            if (controller.agent != null) controller.agent.enabled = false;
            controller.SetSelectionRing(true);
            loggedMissingInputs = false;
        }

        public void Update(SoldierController controller)
        {
            if (GEN_Inputs.Instance == null)
            {
                if (!loggedMissingInputs)
                {
                    Debug.LogError($"[LiderandoState] GEN_Inputs.Instance es NULL en '{controller.name}'. ¿El objeto Formacion está activo en escena?");
                    loggedMissingInputs = true;
                }
                return;
            }

            // Rotar hacia el mouse
            Vector3 mousePos = GEN_Inputs.Instance.MouseWorldPosition;
            Vector3 dir = mousePos - controller.transform.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            controller.transform.rotation = Quaternion.Euler(0, 0, angle);

            // Disparo manual
            if (GEN_Inputs.Instance.DisparoSostenido && Time.time >= nextFireTime)
            {
                controller.DispararProyectil();
                if (controller.model != null)
                    nextFireTime = Time.time + controller.model.fireRate;
            }
        }

        public void FixedUpdate(SoldierController controller)
        {
            if (controller.model == null || GEN_Inputs.Instance == null) return;

            Vector2 moveDir2D = GEN_Inputs.Instance.MovimientoInput;
            Vector3 moveDir = new Vector3(moveDir2D.x, moveDir2D.y, 0f);
            controller.transform.position += moveDir * controller.model.velocidad * Time.deltaTime;
        }

        public void Exit(SoldierController controller)
        {
            controller.SetSelectionRing(false);
        }
    }

    // ==========================================
    // ESTADO: IR A FORMACIÓN (Seguir líder)
    // ==========================================
    public class IrAFormacionState : ISoldierState
    {
        public void Enter(SoldierController controller)
        {
            if (controller.agent != null) controller.agent.enabled = true;
        }

        public void Update(SoldierController controller)
        {
            if (controller.slotAsignado != null)
            {
                controller.MoverAgenteA(controller.slotAsignado.position);
                Debug.DrawLine(controller.transform.position, controller.slotAsignado.position, Color.blue);
            }
        }

        public void FixedUpdate(SoldierController controller) { }

        public void Exit(SoldierController controller) { }
    }

    // ==========================================
    // ESTADO: ATACAR (IA Disparo)
    // ==========================================
    public class AtacarState : ISoldierState
    {
        private float nextFireTime;

        public void Enter(SoldierController controller)
        {
            controller.StopAgente();
        }

        public void Update(SoldierController controller)
        {
            if (controller.objetivo == null) return;

            // Mirar al objetivo
            Vector3 diferencia = controller.objetivo.position - controller.transform.position;
            float anguloZ = Mathf.Atan2(diferencia.y, diferencia.x) * Mathf.Rad2Deg;
            controller.transform.rotation = Quaternion.Slerp(controller.transform.rotation, Quaternion.Euler(0, 0, anguloZ), Time.deltaTime * 10f);

            // Disparo IA
            if (Time.time >= nextFireTime)
            {
                controller.DispararProyectil();
                if (controller.model != null)
                {
                    nextFireTime = Time.time + controller.model.fireRate;
                }
            }
        }

        public void FixedUpdate(SoldierController controller) { }

        public void Exit(SoldierController controller) { }
    }

    // ==========================================
    // ESTADO: IR A ATACAR (IA Persecución)
    // ==========================================
    public class IrAAtacarState : ISoldierState
    {
        public void Enter(SoldierController controller)
        {
            if (controller.agent != null) controller.agent.enabled = true;
        }

        public void Update(SoldierController controller)
        {
            if (controller.objetivo != null)
            {
                controller.MoverAgenteA(controller.objetivo.position);
            }
        }

        public void FixedUpdate(SoldierController controller) { }

        public void Exit(SoldierController controller) { }
    }

    // ==========================================
    // ESTADO: INVESTIGAR (IA Disparo Escuchado)
    // ==========================================
    public class InvestigarState : ISoldierState
    {
        public void Enter(SoldierController controller)
        {
            if (controller.agent != null) controller.agent.enabled = true;
        }

        public void Update(SoldierController controller)
        {
            if (controller.investigarPos.HasValue)
            {
                controller.MoverAgenteA(controller.investigarPos.Value);
                if (Vector3.Distance(controller.transform.position, controller.investigarPos.Value) < 1.5f)
                {
                    controller.investigarPos = null;
                    controller.waitTimer = 2f;
                    controller.CambiarEstado(new EsperandoState());
                }
            }
        }

        public void FixedUpdate(SoldierController controller) { }

        public void Exit(SoldierController controller) { }
    }

    // ==========================================
    // ESTADO: IR A OBJETIVO (Orden Manual)
    // ==========================================
    public class IrAObjetivoState : ISoldierState
    {
        public void Enter(SoldierController controller)
        {
            if (controller.agent != null) controller.agent.enabled = true;
        }

        public void Update(SoldierController controller)
        {
            controller.MoverAgenteA(controller.destinoPos);
            // Dibujar línea cian indicando el objetivo de movimiento ordenado manualmente
            Debug.DrawLine(controller.transform.position, controller.destinoPos, Color.cyan);

            if (Vector3.Distance(controller.transform.position, controller.destinoPos) < 0.8f)
            {
                controller.tieneOrdenManual = false;
                controller.waitTimer = 3f;
                controller.CambiarEstado(new EsperandoState());
            }
        }

        public void FixedUpdate(SoldierController controller) { }

        public void Exit(SoldierController controller) { }
    }

    // ==========================================
    // ESTADO: ESPERANDO (Cooldown / Pausa)
    // ==========================================
    public class EsperandoState : ISoldierState
    {
        public void Enter(SoldierController controller)
        {
            controller.StopAgente();
        }

        public void Update(SoldierController controller)
        {
            controller.waitTimer -= Time.deltaTime;
            if (controller.waitTimer <= 0f)
            {
                controller.CambiarEstado(new IrAFormacionState());
            }
        }

        public void FixedUpdate(SoldierController controller) { }

        public void Exit(SoldierController controller) { }
    }

    // ==========================================
    // ESTADO: INTERACTUANDO (IA Recoger Ítem)
    // ==========================================
    public class InteractuandoState : ISoldierState
    {
        public void Enter(SoldierController controller)
        {
            if (controller.agent != null) controller.agent.enabled = true;
        }

        public void Update(SoldierController controller)
        {
            IInteractable target = controller.ObtenerObjetoAInteractuar();
            if (target == null || (target is MonoBehaviour mb && mb == null))
            {
                controller.LimpiarInteraccion();
                controller.CambiarEstado(new IrAFormacionState());
                return;
            }

            controller.MoverAgenteA(target.GetTransform().position);

            if (Vector3.Distance(controller.transform.position, target.GetTransform().position) < 1.2f)
            {
                target.Interact(controller.gameObject);
                controller.LimpiarInteraccion();
                controller.CambiarEstado(new IrAFormacionState());
            }
        }

        public void FixedUpdate(SoldierController controller) { }

        public void Exit(SoldierController controller) { }
    }

    // ==========================================
    // ESTADO: HUIR DETRÁS DEL LÍDER
    // ==========================================
    public class HuirDetrasLiderState : ISoldierState
    {
        public void Enter(SoldierController controller)
        {
            if (controller.agent != null) controller.agent.enabled = true;
            Debug.Log($"[HuirDetrasLiderState] {controller.name} tiene poca vida y huye a cubrirse detrás del líder.");
        }

        public void Update(SoldierController controller)
        {
            SoldierController leader = GlobalData.liderActual;
            if (leader == null || leader == controller)
            {
                controller.CambiarEstado(new IrAFormacionState());
                return;
            }

            // Buscar enemigo más cercano
            GameObject closestEnemy = null;
            float closestDist = float.MaxValue;
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (GameObject enemy in enemies)
            {
                if (enemy != null)
                {
                    float d = Vector3.Distance(controller.transform.position, enemy.transform.position);
                    if (d < closestDist)
                    {
                        closestDist = d;
                        closestEnemy = enemy;
                    }
                }
            }

            // Vector del enemigo al líder
            Vector3 directionBehind;
            if (closestEnemy != null)
            {
                directionBehind = (leader.transform.position - closestEnemy.transform.position).normalized;
            }
            else
            {
                // Si no hay enemigo, situarse detrás de donde apunta el líder
                directionBehind = -leader.transform.right;
            }

            // Colocarse a 2.5 unidades detrás del líder
            Vector3 targetPos = leader.transform.position + directionBehind * 2.5f;

            controller.MoverAgenteA(targetPos);

            // Dibujos de Debug para verificar cobertura
            Debug.DrawLine(controller.transform.position, targetPos, Color.yellow);
            if (closestEnemy != null)
            {
                Debug.DrawLine(leader.transform.position, closestEnemy.transform.position, Color.red * 0.5f);
            }
        }

        public void FixedUpdate(SoldierController controller) { }

        public void Exit(SoldierController controller) { }
    }
}
