using UnityEngine;

/// <summary>
/// Gestiona la vibración de la cámara cuando el jugador recibe un impacto.
/// Debe estar asociado a la cámara principal.
/// </summary>
public class VibracionCamara : MonoBehaviour
{
    private Vector3 posicionOriginal;
    private float tiempoRestante;
    private float intensidadActual;

    private void Start()
    {
        posicionOriginal = transform.localPosition;
    }

    private void Update()
    {
        if (tiempoRestante > 0)
        {
            transform.localPosition = posicionOriginal + Random.insideUnitSphere * intensidadActual;
            tiempoRestante -= Time.deltaTime;

            if (tiempoRestante <= 0f)
            {
                transform.localPosition = posicionOriginal;
            }
        }
    }

    /// <summary>
    /// Inicia la vibración de la cámara.
    /// </summary>
    /// <param name="intensidad">Intensidad de la vibración.</param>
    public void IniciarVibracion(float intensidad)
    {
        intensidadActual = intensidad;
        tiempoRestante = ConfiguracionGlobal.duracionVibracionCamara;
    }
}
