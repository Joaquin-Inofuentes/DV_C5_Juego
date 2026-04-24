using UnityEngine;
using System.Collections.Generic;

public class LeaderManager : MonoBehaviour
{
    public List<FSMController> unidades;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha4)) CambiarLider(0);
        if (Input.GetKeyDown(KeyCode.Alpha5)) CambiarLider(1);
        if (Input.GetKeyDown(KeyCode.Alpha6)) CambiarLider(2);

        // El PositionManager sigue al líder actual
        if (GlobalData.liderActual != null)
        {
            transform.position = GlobalData.liderActual.transform.position;
            // Opcional: transform.rotation = GlobalData.liderActual.transform.rotation;
        }
    }

    void CambiarLider(int index)
    {
        if (index >= unidades.Count) return;

        for (int i = 0; i < unidades.Count; i++)
        {
            if (i == index)
            {
                unidades[i].currentState = FSMController.State.Liderando;
                unidades[i].GetComponent<PlayerController2D>().enabled = true;
                GlobalData.liderActual = unidades[i];
            }
            else
            {
                // Si antes era líder, lo devolvemos a formación
                if (unidades[i].currentState == FSMController.State.Liderando)
                {
                    unidades[i].currentState = FSMController.State.IrAFormacion;
                }
                unidades[i].GetComponent<PlayerController2D>().enabled = false;
            }
        }
        Debug.Log("<color=cyan>Líder cambiado a: </color>" + unidades[index].name);
    }
}