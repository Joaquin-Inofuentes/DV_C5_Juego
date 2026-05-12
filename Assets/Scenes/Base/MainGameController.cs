using UnityEngine;
using TMPro;

public class MainGameController : MonoBehaviour
{
    public LeaderManager leaderManager;
    public TextMeshProUGUI txtLider;
    public TextMeshProUGUI txtAliados;
    public float suavizado = 5f;

    void FixedUpdate ()
    {
        // 1. SEGUIR AL LÍDER
        if (GlobalData.liderActual != null)
        {
            Vector3 destino = GlobalData.liderActual.transform.position + new Vector3(0, 0, -10);
            transform.position = Vector3.Lerp(transform.position, destino, suavizado * Time.deltaTime);

            // 2. INFO DEL LÍDER
            Destruible v = GlobalData.liderActual.GetComponent<Destruible>();
            Municion m = GlobalData.liderActual.GetComponent<Municion>();
            txtLider.text = "LIDER: " + GlobalData.liderActual.name + "\n" +
                            "HP: " + (int)v.vida + " / " + v.maxVida + "\n" +
                            "AMMO: " + m.balasActuales;
        }

        // 3. LISTA DE ALIADOS
        string lista = "PELOTON:\n";
        for (int i = 0; i < leaderManager.unidades.Count; i++)
        {
            var u = leaderManager.unidades[i];
            int tecla = i + 4;

            if (u == null)
            {
                lista += "[" + tecla + "] MUERTO\n";
            }
            else
            {
                Destruible v = u.GetComponent<Destruible>();
                lista += "[" + tecla + "] " + u.name + " HP: " + (int)v.vida + " (" + u.currentState + ")\n";
            }
        }
        txtAliados.text = lista;
    }
}