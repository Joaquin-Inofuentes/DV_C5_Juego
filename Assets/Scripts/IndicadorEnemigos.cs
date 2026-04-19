using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IndicadorEnemigos : MonoBehaviour
{
    public GameManager gameManager; // Referencia al GameManager para acceder a la lista de enemigos
    public Texture2D texturaIndicador; // Textura que se usará como indicador
    public float desface = 10f; // Desfase para la posición del indicador
    public Camera camara; // Referencia a la cámara que se está usando

    private void Update()
    {
        transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, transform.position.z);

        // Limpia los hijos del objeto actual
        foreach (Transform hijo in transform)
        {
            Destroy(hijo.gameObject);
        }

        // Itera sobre la lista de enemigos generados en el GameManager
        foreach (GameObject enemigo in gameManager.enemigosGenerados)
        {
            if (enemigo != null)
            {
                // Calcula la posición del enemigo en la pantalla
                Vector3 posicionEnPantalla = camara.WorldToScreenPoint(enemigo.transform.position);
                CrearIndicador(posicionEnPantalla);
            }
        }
    }

    private void CrearIndicador(Vector3 posicion)
    {
        // Crea un nuevo GameObject para el indicador
        GameObject indicador = new GameObject("IndicadorEnemigo");
        indicador.transform.SetParent(transform); // Establece como hijo del objeto actual

        // Agrega un componente de imagen
        Image imagen = indicador.AddComponent<Image>();
        imagen.sprite = Sprite.Create(texturaIndicador, new Rect(0, 0, texturaIndicador.width, texturaIndicador.height), new Vector2(0.5f, 2.0f));
        imagen.rectTransform.sizeDelta = new Vector2(50, 50); // Tamaño del indicador

        // Ajusta la posición del indicador
        posicion.x = Mathf.Clamp(posicion.x, 0 + desface, Screen.width - desface);
        posicion.y = Mathf.Clamp(posicion.y, 0 + desface, Screen.height - desface);
        imagen.rectTransform.anchoredPosition = posicion;
    }
}
