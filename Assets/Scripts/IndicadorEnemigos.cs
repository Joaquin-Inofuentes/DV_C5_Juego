using USP.Services;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IndicadorEnemigos : MonoBehaviour
{
    public GameManager gameManager; // Referencia al GameManager para acceder a la lista de enemigos
    public Texture2D texturaIndicador; // Textura que se usar� como indicador
    public float desface = 10f; // Desfase para la posici�n del indicador
    public Camera camara; // Referencia a la c�mara que se est� usando

    // Pool reutilizable de indicadores (evita Destroy/Instantiate cada frame)
    private readonly List<Image> _pool = new List<Image>();
    private Sprite _spriteIndicador;

    private void Update()
    {
        if (gameManager == null || camara == null) return;

        transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, transform.position.z);

        int usados = 0;

        // Itera sobre la lista de enemigos generados en el GameManager
        foreach (GameObject enemigo in gameManager.enemigosGenerados)
        {
            if (enemigo == null) continue;

            // Calcula la posici�n del enemigo en la pantalla
            Vector3 posicionEnPantalla = camara.WorldToScreenPoint(enemigo.transform.position);
            posicionEnPantalla.x = Mathf.Clamp(posicionEnPantalla.x, 0 + desface, Screen.width - desface);
            posicionEnPantalla.y = Mathf.Clamp(posicionEnPantalla.y, 0 + desface, Screen.height - desface);

            Image indicador = ObtenerIndicador(usados);
            indicador.rectTransform.anchoredPosition = posicionEnPantalla;
            indicador.gameObject.SetActive(true);
            usados++;
        }

        // Desactivar indicadores sobrantes del pool (sin destruirlos)
        for (int i = usados; i < _pool.Count; i++)
        {
            if (_pool[i].gameObject.activeSelf) _pool[i].gameObject.SetActive(false);
        }
    }

    private Image ObtenerIndicador(int index)
    {
        if (index < _pool.Count) return _pool[index];

        // Crear uno nuevo y agregarlo al pool
        GameObject indicador = new GameObject("IndicadorEnemigo");
        indicador.transform.SetParent(transform);

        Image imagen = indicador.AddComponent<Image>();
        if (_spriteIndicador == null && texturaIndicador != null)
        {
            _spriteIndicador = Sprite.Create(texturaIndicador, new Rect(0, 0, texturaIndicador.width, texturaIndicador.height), new Vector2(0.5f, 2.0f));
        }
        imagen.sprite = _spriteIndicador;
        imagen.rectTransform.sizeDelta = new Vector2(50, 50);

        _pool.Add(imagen);
        return imagen;
    }
}

