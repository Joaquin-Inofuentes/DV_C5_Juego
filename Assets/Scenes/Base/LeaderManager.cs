using UnityEngine;
using System.Collections.Generic;

public class LeaderManager : MonoBehaviour
{
    public List<FSMController> unidades; // La lista mantiene su tamaño original
    public GameObject MensajeDeQueEstaMuerto;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha4)) CambiarLider(0);
        if (Input.GetKeyDown(KeyCode.Alpha5)) CambiarLider(1);
        if (Input.GetKeyDown(KeyCode.Alpha6)) CambiarLider(2);

        // Si el líder actual muere, limpiamos la referencia global
        if (GlobalData.liderActual == null)
        {
            GlobalData.liderActual = null;
        }
        else
        {
            transform.position = GlobalData.liderActual.transform.position;
        }
    }

    void CambiarLider(int index)
    {
        // Validar que el índice exista en la lista
        if (index < 0 || index >= unidades.Count) return;

        // VERIFICACIÓN: Si el soldado en ese slot está muerto (es null)
        if (unidades[index] == null)
        {
            Debug.LogError($"<color=red>ACCESO DENEGADO:</color> El soldado en el slot {index + 1} ha muerto y no puede ser líder.");
            MensajeDeQueEstaMuerto.SetActive(true);
            return;
        }

        for (int i = 0; i < unidades.Count; i++)
        {
            // Si este miembro de la lista está muerto, lo saltamos para no tirar error
            if (unidades[i] == null) continue;

            if (i == index)
            {
                unidades[i].currentState = FSMController.State.Liderando;

                // Activamos control manual
                PlayerController2D pc = unidades[i].GetComponent<PlayerController2D>();
                if (pc != null) pc.enabled = true;

                GlobalData.liderActual = unidades[i];
            }
            else
            {
                // Si antes era líder, lo devolvemos a formación
                if (unidades[i].currentState == FSMController.State.Liderando)
                {
                    unidades[i].currentState = FSMController.State.IrAFormacion;
                }

                // Desactivamos control manual
                PlayerController2D pc = unidades[i].GetComponent<PlayerController2D>();
                if (pc != null) pc.enabled = false;
            }
        }
        Debug.Log("<color=cyan>Líder cambiado a: </color>" + unidades[index].name);
    }

    public void DesactivarMensaje()
    {
        MensajeDeQueEstaMuerto.SetActive(false);
    }
}