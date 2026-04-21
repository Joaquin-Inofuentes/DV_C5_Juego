using UnityEngine;
using System.Collections.Generic;

public class UnitFSM : MonoBehaviour
{
    public enum State { Controlado, Atacando, SeguirLider, IrADestino }

    [Header("Estado Actual")]
    public State currentState = State.SeguirLider;
    private State lastState;

    [Header("Referencias")]
    public UnitController controller;
    public Transform groupTransform;
    public List<Transform> formationPositions;

    [Header("Debug")]
    public bool showDebug = true;

    void Start()
    {
        lastState = currentState;
        ApplyStateChange();
    }

    void Update()
    {
        // Ejecución de la lógica según estado
        switch (currentState)
        {
            case State.Controlado:
                // En modo controlado, se queda en su slot asignado
                controller.FollowLeader(formationPositions);
                break;

            case State.Atacando:
                controller.Attack(controller.GetEnemy());
                break;

            case State.SeguirLider:
                controller.FollowLeader(formationPositions);
                break;

            case State.IrADestino:
                if (controller.ReachedDestination())
                {
                    SetState(State.Controlado);
                }
                break;
        }
    }

    // --- MÉTODOS DE TRANSICIÓN ---

    public void SetState(State newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        Debug.Log($"<color=cyan>[FSM]</color> {gameObject.name} cambió a {newState}");
        ApplyStateChange();
    }

    private void ApplyStateChange()
    {
        if (currentState != State.SeguirLider && currentState != State.Controlado)
        {
            controller.ReleaseSlot();
        }
    }

    public void SetEnemy(Transform enemy)
    {
        controller.Attack(enemy);
        SetState(State.Atacando);
    }

    public void SetDestination(Vector3 pos)
    {
        controller.MoveToPoint(pos);
        SetState(State.IrADestino);
    }

    // --- DEBUG Y VALIDACIÓN ---

    private void OnValidate()
    {
        if (Application.isPlaying && currentState != lastState)
        {
            lastState = currentState;
            ApplyStateChange();
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebug || !Application.isPlaying) return;

        switch (currentState)
        {
            case State.SeguirLider:
                Gizmos.color = Color.blue;
                if (controller.currentSlot) Gizmos.DrawLine(transform.position, controller.currentSlot.position);
                break;
            case State.Atacando:
                Gizmos.color = Color.red;
                if (controller.GetEnemy()) Gizmos.DrawLine(transform.position, controller.GetEnemy().position);
                break;
            case State.IrADestino:
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, controller.GetTargetPoint());
                break;
            case State.Controlado:
                Gizmos.color = Color.yellow;
                if (controller.currentSlot) Gizmos.DrawWireSphere(controller.currentSlot.position, 0.3f);
                break;
        }
    }
}