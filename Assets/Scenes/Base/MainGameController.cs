using UnityEngine;
using TMPro;

public class MainGameController : MonoBehaviour
{
    public LeaderManager leaderManager;
    public TextMeshProUGUI txtLider;
    public TextMeshProUGUI txtAliados;
    public float suavizado = 5f;

    void FixedUpdate()
    {
        // 1. SEGUIR AL LÍDER CON LA CÁMARA
        if (GlobalData.liderActual != null)
        {
            Vector3 destino = GlobalData.liderActual.transform.position + new Vector3(0, 0, -10);
            transform.position = Vector3.Lerp(transform.position, destino, suavizado * Time.deltaTime);

            // 2. INFO DETALLADA DEL LÍDER
            Destruible v = GlobalData.liderActual.GetComponent<Destruible>();
            Municion m = GlobalData.liderActual.GetComponent<Municion>();

            if (v != null && m != null)
            {
                txtLider.text = "<color=yellow>LIDER ACTUAL:</color> " + GlobalData.liderActual.name.ToUpper() + "\n" +
                                "HP: " + (int)v.vida + " / " + v.maxVida + "\n" +
                                "AMMO: " + m.balasActuales;
            }
        }

        // 3. LISTA DEL PELOTÓN
        string lista = "<color=yellow>PELOTON:</color>\n";

        for (int i = 0; i < leaderManager.unidades.Count; i++)
        {
            var u = leaderManager.unidades[i];
            int numeroReal = i + 1;

            if (u == null)
            {
                lista += "[" + numeroReal + "] <color=red>MUERTO</color>\n";
            }
            else
            {
                Destruible v = u.GetComponent<Destruible>();

                // --- CAMBIO DE COLOR AQUÍ ---
                // Si es el líder, el estado sale en AMARILLO. Si no, en BLANCO.
                string colorEstado = (u.currentState == FSMController.State.Liderando) ? "yellow" : "white";

                lista += "[" + numeroReal + "] " + u.name + " HP: " + (int)v.vida +
                         " (<color=" + colorEstado + ">" + u.currentState + "</color>)\n";
            }
        }

        txtAliados.text = lista;
    }
}