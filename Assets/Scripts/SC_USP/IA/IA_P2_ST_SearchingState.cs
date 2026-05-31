using UnityEngine;

public class IA_P2_ST_SearchingState : IA_P2_INT_gentState
{
    private float _alertTimer;
    private const float ALERT_TIME = 4f;

    public void Enter(IA_P2_FSM context)
    {
        // Si entramos aquí y por alguna razón el target es null, abortamos de inmediato
        if (context.target == null)
        {
            context.TransitionTo(AgentState.ReturningToPatrol);
            return;
        }

        Debug.Log("<color=yellow>ALERTA:</color> Buscando en la zona...");
        context.agent.StopAgent();
        context.agent.AsignarColor(Color.yellow);
        _alertTimer = ALERT_TIME;
    }

    public void Execute(IA_P2_FSM context)
    {
        if (context.target == null)
        {
            context.TransitionTo(AgentState.ReturningToPatrol);
            return;
        }

        bool loVeo = IA_P2_LineOfSight3D.Check(
            context.agent.transform.position,
            context.target.transform.position,
            context.NotificacionDeEnemigoVisible.visionObstacles);

        if (loVeo)
        {
            context.TransitionTo(AgentState.Chasing);
            return;
        }

        // MODIFICADO: Ahora rota en el eje Z (Vector3.forward) para el modo YX
        context.agent.transform.Rotate(Vector3.forward, 120f * Time.deltaTime);

        _alertTimer -= Time.deltaTime;
        if (_alertTimer <= 0)
        {
            context.TransitionTo(AgentState.ReturningToPatrol);
        }
    }

    public void Exit(IA_P2_FSM context) { }
}