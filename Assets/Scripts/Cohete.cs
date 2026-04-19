using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cohete : MonoBehaviour
{
    // Variables
    public float velocidadCohete = 10f;
    private Vector3 objetivo;
    private bool haLlegado = false;
    public int DañoQueCausaElProyectil = 20; // Agrege la variable

    // Método para definir el objetivo del cohete
    public void SetObjetivo(Vector3 nuevoObjetivo)
    {
        objetivo = nuevoObjetivo;




    }

    // Al colisionar con algo o alguien
    void OnCollisionEnter(Collision col)
    {
        /// Agregar q quite vida al jugador. El jugador principal dentro de sus funciones tiene una funcion publica
        /// de quitar vida q pide un float o int, usala
        /// Asi le resta vida  con la variable q tienes de "public int DañoQueCausaElProyectil = 20; // Agrege la variable"
        /// Pone q si, el collider tiene el componente info de personaje o algo asi q lo use  sino q no haga nada
        /// ¨Por q la colision puede ser un muro o un soldado


        // hace q el disparo explote
        Explotar();
    }

    // Función que se ejecutará al colisionar
    void Explotar()
    {
        haLlegado = true;
        StartCoroutine(ExpandirYDestruir());
    }



    void Update()
    {
        if (objetivo != Vector3.zero && !haLlegado)
        {
            MoverHaciaObjetivo();
        }
    }

    void MoverHaciaObjetivo()
    {
        // Mover el cohete hacia el objetivo
        transform.position = Vector3.MoveTowards(transform.position, objetivo, velocidadCohete * Time.deltaTime);

        // Verificar si ha llegado al objetivo
        if (transform.position == objetivo)
        {
            haLlegado = true;
            StartCoroutine(ExpandirYDestruir());
        }
    }

    IEnumerator ExpandirYDestruir()
    {
        // Expandir por 0.5 segundos
        // Aquí puedes aumentar el tamaño del cohete si lo deseas
        transform.localScale = new Vector3(4f, 4f, 4f); // Ejemplo de expansión

        yield return new WaitForSeconds(0.5f); // Mantener expandido por 0.5 segundos

        // Destruir el cohete
        Destroy(gameObject);
    }
}