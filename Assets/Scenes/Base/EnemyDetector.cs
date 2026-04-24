using UnityEngine;
using System.Collections.Generic;

public class EnemyDetector : MonoBehaviour
{
    public float detectionRadius = 6f;
    public LayerMask enemyLayer;    // Capa de los enemigos
    public LayerMask obstacleLayer; // Capa de paredes/coberturas

    public List<Transform> enemiesInRange = new List<Transform>();
    public FSMController fsm; // Referencia al FSM

    void Start()
    {
        if (fsm == null) fsm = GetComponent<FSMController>();

    }

    void Update()
    {
        LimpiarLista();
        FiltrarYEnviarAlFSM();
    }

    void LimpiarLista()
    {
        // Elimina de la lista si el enemigo fue destruido
        enemiesInRange.RemoveAll(e => e == null);
    }

    void FiltrarYEnviarAlFSM()
    {
        Transform mejorObjetivo = null;
        float distanciaCercana = Mathf.Infinity;

        foreach (Transform enemigo in enemiesInRange)
        {
            // Calcular dirección y distancia
            Vector2 direccion = enemigo.position - transform.position;
            float distancia = direccion.magnitude;

            // RAYCAST: ¿Hay una pared en medio?
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direccion.normalized, detectionRadius, enemyLayer | obstacleLayer);

            // Si el Raycast golpea algo y ese algo está en la capa de enemigos...
            if (hit.collider != null && ((1 << hit.collider.gameObject.layer) & enemyLayer) != 0)
            {
                // Dibujar línea de visión en el editor (Verde = Visible)
                Debug.DrawLine(transform.position, enemigo.position, Color.green);

                if (distancia < distanciaCercana)
                {
                    distanciaCercana = distancia;
                    mejorObjetivo = enemigo;
                }
            }
            else
            {
                // Línea roja = Hay un obstáculo en medio
                Debug.DrawLine(transform.position, enemigo.position, Color.red);
            }
        }

        // Enviamos el enemigo más cercano y visible al FSM
        fsm.objetivo = mejorObjetivo;
    }

    // Detectar cuando entran en el círculo
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & enemyLayer) != 0)
        {
            if (!enemiesInRange.Contains(other.transform))
                enemiesInRange.Add(other.transform);
        }
    }

    // Detectar cuando salen del círculo
    private void OnTriggerExit2D(Collider2D other)
    {
        enemiesInRange.Remove(other.transform);
    }
}