using UnityEngine;
using Game.Core;
using Game.Squad;
using Game.Sensors;

namespace Game.Squad
{
    /// <summary>
    /// ESTADO: CAIDO (Dañado - Down)
    /// El soldado está caído y no puede moverse, disparar ni realizar acciones.
    /// Solo puede ser revivido por un aliado cercano (el líder) manteniando barra espaciadora por 3 segundos.
    /// Mientras está caído es invisible para los detectores enemigos.
    /// </summary>
    public class DamagedState : IUnitState
    {
        private GameObject aliveGameObject;
        private GameObject downGameObject;
        private DamagedStateHandler stateHandler;

        public void Enter(UnitController unit)
        {
            LogMethodEntry($"[Enter] Soldado {unit.name} entrando en estado CAIDO");

            // Detener agente y liberar slot de formación
            if (unit.agent != null) unit.agent.StopAgent();
            unit.ReleaseSlot();

            // Desactivar disparo
            if (unit.shooter != null) unit.shooter.enabled = false;

            // Buscar o crear el handler para este estado
            stateHandler = unit.GetComponent<DamagedStateHandler>();
            if (stateHandler == null)
            {
                stateHandler = unit.gameObject.AddComponent<DamagedStateHandler>();
            }

            // Intercambiar GameObjects (vivo -> caído)
            SwapGameObjects(unit);

            // Marcar como no detectable por enemigos
            SetUndetectable(unit, true);

            // Mostrar UI de caído
            if (unit.view != null)
            {
                unit.view.StopAllBlinks();
                unit.view.HideLine();
                LogMethodEntry($"[Enter] Vista actualizada para modo caído");
            }

            // Dibujar líneas de debug entre aliados
            if (stateHandler != null)
            {
                stateHandler.StartDamagedVisualization(unit);
                LogMethodEntry($"[Enter] Visualización de caído iniciada");
            }

            LogMethodEntry($"[Enter] Soldado {unit.name} completamente en estado CAIDO - INDETECTABLE");
        }

        public void Update(UnitController unit)
        {
            // El soldado caído no puede hacer nada, solo esperar revivimiento
            // Mantener visualización de líneas de debug
            if (stateHandler != null)
            {
                stateHandler.UpdateDamagedVisualization(unit);
            }
        }

        public void FixedUpdate(UnitController unit)
        {
            // Sin movimiento posible mientras está caído
        }

        public void Exit(UnitController unit)
        {
            LogMethodEntry($"[Exit] Soldado {unit.name} saliendo de estado CAIDO");

            // Restaurar GameObjects (caído -> vivo)
            RestoreGameObjects(unit);

            // Marcar como detectable nuevamente
            SetUndetectable(unit, false);

            // Restaurar disparo si es necesario
            if (unit.shooter != null) unit.shooter.enabled = true;

            // Limpiar visualización
            if (stateHandler != null)
            {
                stateHandler.StopDamagedVisualization();
            }

            LogMethodEntry($"[Exit] Soldado {unit.name} revivido y operativo nuevamente");
        }

        // =====================================================
        // MÉTODOS AUXILIARES
        // =====================================================

        private void SwapGameObjects(UnitController unit)
        {
            aliveGameObject = unit.vivoGO;
            downGameObject = unit.caidoGO;

            if (aliveGameObject != null)
            {
                aliveGameObject.SetActive(false);
                LogMethodEntry($"[SwapGameObjects] Desactivado GameObject 'Vivo'");
            }
            else
                Debug.LogWarning($"[DamagedState] {unit.name}: vivoGO no asignado en UnitController.");

            if (downGameObject != null)
            {
                downGameObject.SetActive(true);
                LogMethodEntry($"[SwapGameObjects] Activado GameObject 'Caido'");
            }
            else
                Debug.LogWarning($"[DamagedState] {unit.name}: caidoGO no asignado en UnitController.");
        }

        private void RestoreGameObjects(UnitController unit)
        {
            if (downGameObject != null)
            {
                downGameObject.SetActive(false);
                LogMethodEntry($"[RestoreGameObjects] Desactivado GameObject 'Caido'");
            }

            if (aliveGameObject != null)
            {
                aliveGameObject.SetActive(true);
                LogMethodEntry($"[RestoreGameObjects] Activado GameObject 'Vivo'");
            }
        }

        private void SetUndetectable(UnitController unit, bool isUndetectable)
        {
            // Obtener el componente IDetectable
            if (unit is IDetectable detectable)
            {
                // Aquí deberías marcar el unit como indetectable
                // Esto se usa en detectores enemigos para ignorar a caídos
                unit.gameObject.tag = isUndetectable ? "Undetectable" : "Detectable";
                LogMethodEntry($"[SetUndetectable] Soldado {unit.name} es indetectable: {isUndetectable}");
            }
        }

        private void LogMethodEntry(string message)
        {
            Debug.Log($"<color=yellow>[DamagedState]</color> {message}");
        }
    }

    /// <summary>
    /// Manejador auxiliar para las visualizaciones y lógica del estado caído.
    /// Se agrega como componente dinámicamente cuando el soldado entra en DamagedState.
    /// </summary>
    public class DamagedStateHandler : MonoBehaviour
    {
        private UnitController cachedController;
        private float[] allyDistances; // Para detectar quién está lo suficientemente cerca
        private UnitController reviverAlly; // Quién está reviviendo actualmente

        public void StartDamagedVisualization(UnitController unit)
        {
            cachedController = unit;
            LogMethodEntry($"[StartDamagedVisualization] Iniciando visualización de caído para {unit.name}");
        }

        public void UpdateDamagedVisualization(UnitController unit)
        {
            if (unit == null) return;

            // Dibujar líneas a todos los aliados
            DrawLinesToAllies(unit);

            // Detectar quién está cerca y puede revivir
            DetectRevivalCandidates(unit);
        }

        public void StopDamagedVisualization()
        {
            LogMethodEntry($"[StopDamagedVisualization] Deteniendo visualización");
        }

        private void DrawLinesToAllies(UnitController damagedUnit)
        {
            // Buscar todos los UnitController en la escena
            UnitController[] allUnits = FindObjectsOfType<UnitController>();

            foreach (UnitController ally in allUnits)
            {
                // Solo dibujar líneas a aliados vivos que no sean el mismo
                if (ally == damagedUnit || !ally.isActiveAndEnabled) continue;
                if (ally.model.team != damagedUnit.model.team) continue; // Solo aliados
                if (ally.model.IsDown) continue; // No línea a otros caídos

                float distance = Vector3.Distance(damagedUnit.transform.position, ally.transform.position);
                float revivalRange = 3f; // Rango de revivimiento

                Color lineColor = distance <= revivalRange ? Color.yellow : Color.blue;
                Debug.DrawLine(
                    damagedUnit.transform.position,
                    ally.transform.position,
                    lineColor,
                    Time.deltaTime * 2f
                );

                // Log si está lo suficientemente cerca
                if (distance <= revivalRange && ally.model.IsLeader)
                {
                    LogMethodEntry($"[DrawLinesToAllies] Soldado {ally.name} (LIDER) está cerca para revivir. Distancia: {distance:F2}");
                }
            }
        }

        private void DetectRevivalCandidates(UnitController damagedUnit)
        {
            UnitController[] allUnits = FindObjectsOfType<UnitController>();
            float revivalRange = 3f;

            foreach (UnitController ally in allUnits)
            {
                if (ally == damagedUnit || !ally.isActiveAndEnabled) continue;
                if (ally.model.team != damagedUnit.model.team) continue;
                if (ally.model.IsDown) continue;

                float distance = Vector3.Distance(damagedUnit.transform.position, ally.transform.position);

                if (distance <= revivalRange)
                {
                    // Determinar si puede revivir
                    CanReviveCheck(ally, damagedUnit);
                }
            }
        }

        private void CanReviveCheck(UnitController potentialReviver, UnitController damagedUnit)
        {
            // Solo el líder puede revivir en esta versión
            if (!potentialReviver.model.IsLeader)
            {
                LogMethodEntry($"[CanReviveCheck] Soldado {potentialReviver.name} está cerca pero NO es líder");
                return;
            }

            LogMethodEntry($"[CanReviveCheck] Soldado {potentialReviver.name} ES LIDER y está lo suficientemente cerca para revivir");
        }

        private void LogMethodEntry(string message)
        {
            Debug.Log($"<color=cyan>[DamagedStateHandler]</color> {message}");
        }
    }
}
