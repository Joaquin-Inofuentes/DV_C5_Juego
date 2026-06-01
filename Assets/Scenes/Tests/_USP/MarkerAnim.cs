using UnityEngine;

public class MarkerAnim : MonoBehaviour
{
    public float velocidadAnim = 4f; // Controla qué tan rápido sube y baja
    public float escalaMaxima = 1.5f;
    private float animTimer = 0f; // El "float simple" que pediste

    void Update()
    {
        if (animTimer > 0)
        {
            // El timer baja hacia 0
            animTimer -= Time.deltaTime * velocidadAnim;

            // Usamos Sin para que suba y baje suavemente (0 -> 1 -> 0)
            // Mathf.Max para que no sea negativo
            float curva = Mathf.Sin(Mathf.Clamp01(animTimer) * Mathf.PI);
            transform.localScale = Vector3.one * (curva * escalaMaxima);
        }
        else
        {
            // Si es 0, se queda invisible/escala cero
            transform.localScale = Vector3.zero;
            animTimer = 0;
        }
    }

    // Este método lo llamaremos desde el UnitCommander
    public void IniciarAnimacion(Vector3 posicion)
    {
        transform.position = posicion;
        animTimer = 1.0f; // Reiniciamos el float al máximo
    }
}