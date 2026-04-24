using UnityEngine;

public class UnitCommander : MonoBehaviour
{
    public FSMController[] units;
    public Transform targetMarker;

    void Update()
    {
        if (Input.GetMouseButtonDown(1)) // Click derecho
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            if (targetMarker != null) targetMarker.position = mousePos;

            FSMController soldadoMasCercano = null;
            float distanciaMinima = Mathf.Infinity;

            foreach (var unit in units)
            {
                // CORRECCIÓN: Accedemos al enum mediante FSMController.State
                if (unit.currentState == FSMController.State.Liderando) continue;

                float dist = Vector2.Distance(unit.transform.position, mousePos);
                if (dist < distanciaMinima)
                {
                    distanciaMinima = dist;
                    soldadoMasCercano = unit;
                }
            }

            if (soldadoMasCercano != null)
            {
                // PASAMOS LA POSICIÓN (Vector3)
                soldadoMasCercano.SetOrder(mousePos);
            }
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            foreach (var unit in units)
            {
                unit.RegresarAFormacion();
            }
        }
    }
}