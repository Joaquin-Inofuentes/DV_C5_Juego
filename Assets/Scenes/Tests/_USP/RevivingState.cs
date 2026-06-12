using UnityEngine;
using Game.Core;
using Game.Squad;

namespace Game.Squad
{
    /// <summary>
    /// ESTADO: REVIVIENDO
    /// El aliado está cerca de un compañero caído y puede revivir.
    /// Si está cercano, no está atacando/siendo atacado, va directamente a revivir.
    /// Una vez que comienza el revivimiento, mantiene el contacto por 3 segundos.
    /// Si se aleja o el caído se mueve, el revivimiento se cancela.
    /// </summary>
    public class RevivingState : IUnitState
    {
        private UnitController damagedAlly;
        private float revivalDuration = 3f;
        private float revivalTimer = 0f;
        private float revivalRange = 3f;
        private bool isActivelyReviving = false;

        public RevivingState(UnitController damagedTarget)
        {
            damagedAlly = damagedTarget;
            LogMethodEntry($"[Constructor] RevivingState creado para revivir a {damagedTarget.name}");
        }

        public void Enter(UnitController unit)
        {
            LogMethodEntry($"[Enter] Soldado {unit.name} entrando en estado REVIVIENDO");
            LogMethodEntry($"[Enter] Objetivo a revivir: {damagedAlly.name}");

            // Detener el agente
            if (unit.agent != null) unit.agent.StopAgent();

            // Marcar que comienza el revivimiento
            isActivelyReviving = true;
            revivalTimer = 0f;

            // Mostrar que está reviviendo
            unit.view.StartBlink(IndicatorType.Reviving);
            unit.view.HideLine();

            LogMethodEntry($"[Enter] Revivimiento iniciado. Presionar barra espaciadora durante {revivalDuration} segundos");
        }

        public void Update(UnitController unit)
        {
            if (damagedAlly == null || !damagedAlly.isActiveAndEnabled)
            {
                LogMethodEntry($"[Update] Objetivo caído fue destruido o desactivado. Abortando revivimiento");
                isActivelyReviving = false;
                unit.CambiarEstado(new SeguirFormacionState());
                return;
            }

            // Verificar distancia
            float distance = Vector3.Distance(unit.transform.position, damagedAlly.transform.position);

            if (distance > revivalRange)
            {
                LogMethodEntry($"[Update] {unit.name} se alejó del caído (distancia: {distance:F2}). Abortando revivimiento");
                isActivelyReviving = false;
                unit.CambiarEstado(new SeguirFormacionState());
                return;
            }

            // Incrementar timer solo si se mantiene presionada la barra espaciadora
            if (GEN_Inputs.Instance != null && GEN_Inputs.Instance.RavivicionInput)
            {
                revivalTimer += Time.deltaTime;
                LogMethodEntry($"[Update] Reviviendo a {damagedAlly.name}. Progreso: {revivalTimer:F2}/{revivalDuration:F2} segundos");

                damagedAlly.view.revivalProgress = revivalTimer / revivalDuration;

                // Si se completa el revivimiento
                if (revivalTimer >= revivalDuration)
                {
                    CompleteRevival(unit);
                    return;
                }
            }
            else
            {
                // Si soltó la tecla, cancelar revivimiento
                if (revivalTimer > 0)
                {
                    LogMethodEntry($"[Update] {unit.name} soltó la barra espaciadora. Revivimiento CANCELADO");
                    revivalTimer = 0f;
                    damagedAlly.view.revivalProgress = 0f;
                    isActivelyReviving = false;
                    unit.CambiarEstado(new SeguirFormacionState());
                }
            }

            // Mantener rotación hacia el objetivo
            Vector3 dir = (damagedAlly.transform.position - unit.transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            unit.view.RotateGraphicsSmooth(angle, 10f);
        }

        public void FixedUpdate(UnitController unit)
        {
            // No se mueve mientras revive
        }

        public void Exit(UnitController unit)
        {
            LogMethodEntry($"[Exit] Soldado {unit.name} saliendo del estado REVIVIENDO");
            unit.view.StopBlink(IndicatorType.Reviving);
            isActivelyReviving = false;
            revivalTimer = 0f;
        }

        private void CompleteRevival(UnitController unit)
        {
            LogMethodEntry($"[CompleteRevival] ¡Revivimiento COMPLETADO! {unit.name} ha revivido a {damagedAlly.name}");

            // Restaurar salud al caído
            damagedAlly.model.ReviveHealth();
            LogMethodEntry($"[CompleteRevival] Salud de {damagedAlly.name} restaurada a {damagedAlly.model.healthActual}/{damagedAlly.model.healthMax}");

            damagedAlly.view.OnRevivalComplete();
            damagedAlly.ExitDamagedState();
            damagedAlly.CambiarEstado(new SeguirFormacionState());

            // Hacer que el revividor vuelva a formación
            unit.CambiarEstado(new SeguirFormacionState());

            // Marcar que ya no está reviviendo
            isActivelyReviving = false;

            LogMethodEntry($"[CompleteRevival] {damagedAlly.name} ahora es OPERATIVO nuevamente");
        }

        private void LogMethodEntry(string message)
        {
            Debug.Log($"<color=green>[RevivingState]</color> {message}");
        }
    }

}
