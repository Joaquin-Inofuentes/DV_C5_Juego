using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SenalizacionDeEnemigos : MonoBehaviour
{
    [Header("Configuración")]
    public GameObject prefabFlecha; // Prefab de la flecha
    public float distanciaVisible = 20f; // Distancia máxima para señalar enemigos
    public float tiempoEntreActualizaciones = 0.5f; // Intervalo para actualizar la lista de enemigos
    public Transform objetoFlechas; // Objeto vacío en el canvas donde se almacenarán las flechas

    [Header("Listas Públicas")]
    public List<Transform> enemigosTransform = new List<Transform>(); // Lista pública de los Transform de los enemigos
    public List<GameObject> flechasActivas = new List<GameObject>(); // Lista pública para almacenar flechas activas

    private void Start()
    {
        // Comienza la Coroutine para actualizar enemigos
        StartCoroutine(ActualizarEnemigos());
    }

    private IEnumerator ActualizarEnemigos()
    {
        while (true)
        {
            // Encuentra todos los enemigos con el nombre que contiene "Soldado_Enemigo"
            GameObject[] enemigos = GameObject.FindGameObjectsWithTag("Soldado_Enemigo");
            List<Transform> enemigosDetectados = new List<Transform>(); // Lista temporal para los enemigos detectados

            // Verifica todos los enemigos encontrados
            foreach (GameObject enemigo in enemigos)
            {
                float distancia = Vector3.Distance(transform.position, enemigo.transform.position);
                if (distancia <= distanciaVisible)
                {
                    enemigosDetectados.Add(enemigo.transform); // Agrega el enemigo detectado a la lista temporal

                    // Si el enemigo no está en la lista, crea una flecha
                    if (!enemigosTransform.Contains(enemigo.transform))
                    {
                        // Genera la flecha
                        GameObject nuevaFlecha = Instantiate(prefabFlecha, Vector3.zero, Quaternion.identity);
                        nuevaFlecha.transform.SetParent(objetoFlechas); // Establece el objeto vacío como padre

                        // Inicializa la escala de la flecha
                        nuevaFlecha.transform.localScale = new Vector3(1,-2,1); // Escala inicial en (1,1,1)
                        nuevaFlecha.transform.localPosition = new Vector3(0,1,0); // Posición inicial en (0,0,0)

                        // Añade la flecha a la lista activa
                        flechasActivas.Add(nuevaFlecha);
                        enemigosTransform.Add(enemigo.transform); // Agrega el transform a la lista de enemigos
                    }
                }
            }

            // Revisa si hay enemigos que se alejan y elimina flechas correspondientes
            for (int i = flechasActivas.Count - 1; i >= 0; i--)
            {
                GameObject flecha = flechasActivas[i];
                Transform enemigoAsociado = enemigosTransform[i];
                float distancia = Vector3.Distance(transform.position, enemigoAsociado.position);

                if (distancia > distanciaVisible)
                {
                    // Destruye la flecha y elimina el transform de la lista
                    Destroy(flecha);
                    flechasActivas.RemoveAt(i);
                    enemigosTransform.RemoveAt(i);
                }
            }

            // Muestra en el debug cuántos enemigos fueron detectados
            //Debug.Log("Enemigos detectados: " + enemigosTransform.Count);

            yield return new WaitForSeconds(tiempoEntreActualizaciones); // Espera antes de la próxima actualización
        }
    }

    private void Update()
    {
        // Actualiza la rotación de las flechas en referencia a los enemigos
        for (int i = flechasActivas.Count - 1; i >= 0; i--) // Itera hacia atrás para poder eliminar elementos sin afectar el índice
        {
            GameObject flecha = flechasActivas[i];
            Transform enemigoAsociado = enemigosTransform[i];

            // Verifica si el enemigo asociado existe
            if (enemigoAsociado == null)
            {
                // Si el enemigo ha sido eliminado, destruye la flecha y elimina de las listas
                Destroy(flecha);
                flechasActivas.RemoveAt(i);
                enemigosTransform.RemoveAt(i);
            }
            else
            {
                // Solo actualiza la rotación si el enemigo asociado aún existe
                Vector3 direccion = enemigoAsociado.position - transform.position;
                float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg - 90f; // Ajusta el ángulo
                flecha.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angulo));
            }
        }
    }
}
