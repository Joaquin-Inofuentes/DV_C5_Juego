using UnityEngine;
using System.Collections.Generic; // Necesario para List

public class UnitCommander : MonoBehaviour
{
    // CAMBIO: Usamos List en lugar de Array para poder limpiar los muertos
    public List<FSMController> units = new List<FSMController>();
    public Transform targetMarker;

    void Update()
    {
        // 1. Limpieza preventiva: eliminamos soldados que ya no existen
        units.RemoveAll(u => u == null);

        if (Input.GetMouseButtonDown(1)) // Click derecho
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            // --- CAMBIO AQUÍ ---
            if (targetMarker != null)
            {
                // Intentamos obtener el script de animación
                MarkerAnim anim = targetMarker.GetComponent<MarkerAnim>();
                if (anim != null)
                {
                    anim.IniciarAnimacion(mousePos);
                }
                else
                {
                    // Si no tiene el script, al menos lo movemos como antes
                    targetMarker.position = mousePos;
                }
            }

            // ESTA LÍNEA SEÑALIZA:
            if (targetMarker != null) targetMarker.position = mousePos;

            FSMController soldadoMasCercano = null;
            float distanciaMinima = Mathf.Infinity;

            foreach (var unit in units)
            {
                // 2. Validación de seguridad: saltar si es null o si es el líder
                if (unit == null) continue;
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
                soldadoMasCercano.SetOrder(mousePos);
            }
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            foreach (var unit in units)
            {
                // 3. Validación de seguridad para la orden masiva
                if (unit != null)
                {
                    unit.RegresarAFormacion();
                }
            }
        }
    }

    // Método extra por si quieres añadir soldados en tiempo de ejecución
    public void AgregarUnidad(FSMController nuevaUnidad)
    {
        if (!units.Contains(nuevaUnidad))
            units.Add(nuevaUnidad);
    }
}