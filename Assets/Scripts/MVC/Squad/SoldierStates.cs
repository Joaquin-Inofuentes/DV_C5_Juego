using UnityEngine;

namespace Game.Squad
{
    // ==========================================
    // ESTADO: LIDERANDO (Control Manual)
    // ==========================================
    public class LiderandoState : ISoldierState
    {
        private float nextFireTime;

        public void Enter(SoldierController controller)
        {
            if (controller.agent != null) controller.agent.enabled = false;
            controller.SetSelectionRing(true);
        }

        public void Update(SoldierController controller)
        {
            if (Camera.main == null) return;

            // Rotar hacia el mouse
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 dir = mousePos - controller.transform.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            controller.transform.rotation = Quaternion.Euler(0, 0, angle);

            // Disparo Manual
            if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
            {
                controller.DispararProyectil();
                if (controller.model != null)
                {
                    nextFireTime = Time.time + controller.model.fireRate;
                }
            }
        }

        public void FixedUpdate(SoldierController controller)
        {
            if (controller.model == null) return;

            // Movimiento Manual WASD
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");
            Vector3 moveDir = new Vector3(moveX, moveY, 0).normalized;
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
}
