using UnityEngine;
using System.Collections.Generic;

public class BalaPool : MonoBehaviour
{
    public static BalaPool Instance;
    public Bala prefabBala;

    private Queue<Bala> pool = new Queue<Bala>();

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        Instance = this;
        ReconstruirPoolDesdeHijos();

        if (pool.Count == 0)
        {
            ExpandirPool(500);
        }
    }

    void ReconstruirPoolDesdeHijos()
    {
        pool.Clear();

        foreach (Transform child in transform)
        {
            Bala b = child.GetComponent<Bala>();
            if (b != null)
            {
                b.gameObject.SetActive(false);
                pool.Enqueue(b);
            }
        }

        Debug.Log("Pool reconstruido con hijos: " + pool.Count);
    }

    void ExpandirPool(int cantidad)
    {
        for (int i = 0; i < cantidad; i++)
        {
            Bala nueva = Instantiate(prefabBala, transform);
            nueva.gameObject.SetActive(false);
            pool.Enqueue(nueva);
        }

        Debug.Log("Pool expandido: +" + cantidad);
    }

    public Bala GetBala()
    {
        if (pool.Count == 0)
        {
            ExpandirPool(50);
        }

        Bala b = pool.Dequeue();
        b.gameObject.SetActive(true);
        return b;
    }

    public void ReturnBala(Bala b)
    {
        b.gameObject.SetActive(false);
        pool.Enqueue(b);
    }
}
