using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CustomInspector;

public class IA_F_ChangeMode : MonoBehaviour
{
    [Button(nameof(OrdenarAtaque))]
    public IA_P2_AgentIA agentIA;

    void Update()
    {
        if (agentIA == null) return;

        if (Input.GetKeyDown(KeyCode.W)
            || Input.GetKeyDown(KeyCode.A)
            || Input.GetKeyDown(KeyCode.S)
            || Input.GetKeyDown(KeyCode.D)
            )
        {
            CambiarDeModo(true);
        }

        // Detecta click izquierdo del mouse
        if (Input.GetMouseButtonDown(0))
        {
            CambiarDeModo(false);
            // Obtiene posicion en el mundo 2D
            Vector3 clickPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            clickPoint.z = 0;
            // Mueve el agente a la posicion del click
            agentIA.GoTo(clickPoint);
            agentIA.LookAtTarget(clickPoint);
        }
        if (Input.GetMouseButtonDown(1))
        {
            CambiarDeModo(false);
            // Obtiene posicion en el mundo 2D
            Vector3 clickPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            clickPoint.z = 0;
            // Mueve el agente a la posicion del click
            agentIA.LookAtTarget(clickPoint);
        }
    }

    public void CambiarDeModo()
    {
        if (agentIA == null)
        {
            Debug.LogWarning("AgentIA o SoldadoJugador no asignados.");
            return;
        }

        if (agentIA.enabled)
        {
            agentIA.enabled = false;
            Debug.Log("Modo Jugador activado.");
        }
        else
        {
            agentIA.enabled = true;
            Debug.Log("Modo IA activado.");
        }
    }
    
    public void CambiarDeModo(bool ModoIA)
    {
        if (agentIA == null)
        {
            Debug.LogWarning("AgentIA o SoldadoJugador no asignados.");
            return;
        }

        if (ModoIA)
        {
            agentIA.enabled = false;
        }
        else
        {
            agentIA.enabled = true;
        }
    }

    public GameObject objetivoEnemigo;

    [ContextMenu("Forzar Ataque Global")]
    public void OrdenarAtaque()
    {
        IA_P2_BusEvent_Manager.NotificarEncontrado(objetivoEnemigo, "Jugador");
    }
}
