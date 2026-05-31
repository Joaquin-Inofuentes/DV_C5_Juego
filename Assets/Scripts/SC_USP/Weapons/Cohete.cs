namespace USP.Weapons {
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cohete : MonoBehaviour
{
    public float velocidadCohete = 10f;
    private Vector3 objetivo;
    private bool haLlegado = false;
    public int DañoQueCausaElProyectil = 20;

    public void SetObjetivo(Vector3 nuevoObjetivo)
    {
        objetivo = nuevoObjetivo;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        IDaniable objetivo = col.gameObject.GetComponent<IDaniable>();
        if (objetivo != null)
        {
            objetivo.RecibirDano(DañoQueCausaElProyectil, gameObject);
        }
        Explotar();
    }

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
        transform.position = Vector3.MoveTowards(transform.position, objetivo, velocidadCohete * Time.deltaTime);

        if (transform.position == objetivo)
        {
            haLlegado = true;
            StartCoroutine(ExpandirYDestruir());
        }
    }

    IEnumerator ExpandirYDestruir()
    {
        transform.localScale = new Vector3(4f, 4f, 4f);
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }
}
}
