using UnityEngine;

public class Disparador : MonoBehaviour
{
    public float dañoBala = 10f;
    public float velocidadBala = 25f;

    public void Disparar()
    {
        // 1. Buscamos el componente Municion en este objeto o en el padre (Soldado)
        Municion m = GetComponentInParent<Municion>();

        // 2. Si existe el sistema de munición, validamos
        if (m != null)
        {
            if (m.balasActuales <= 0)
            {
                Debug.Log("<color=orange>Click! Sin balas en: </color>" + transform.root.name);
                return; // SALIR: No dispara nada
            }

            // 3. GASTAR LA BALA
            m.balasActuales--;
        }

        // 4. LÓGICA DE DISPARO (Solo llega aquí si hay balas o no hay script de munición)
        Bala b = BalaPool.Instance.GetBala();

        b.transform.position = transform.position;
        b.transform.rotation = transform.rotation;

        b.daño = dañoBala;
        b.velocidad = velocidadBala;

        // El dueño es el objeto raíz (el soldado completo)
        b.dueño = transform.root.gameObject;
    }
}