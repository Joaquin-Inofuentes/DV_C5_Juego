using UnityEngine;

public class Municion : MonoBehaviour
{
    public int balasActuales = 300;
    public int maxBalas = 300;

    public bool PuedeDisparar()
    {
        return balasActuales > 0;
    }

    public void GastarBala()
    {
        if (balasActuales > 0) balasActuales--;
    }
}