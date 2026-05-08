using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para cambiar de escena

public class RehenBruto : MonoBehaviour
{
    [Header("CONFIGURACION SEGUIMIENTO (LIDER)")]
    public float distanciaParaSeguir = 4f;
    public float distanciaParaParar = 2f;
    public bool yaMeRescataron = false;

    [Header("CONFIGURACION VICTORIA (DESTINO)")]
    public Transform objetoVictoria; // Arrastra el objeto "Victoria" aquí
    public float distanciaParaGanar = 1.5f;
    public string nombreDeLaEscena = "EscenaVictoria"; // Escribe el nombre exacto de tu escena

    private IA_P2_AgentIA agente;

    void Start()
    {
        agente = GetComponent<IA_P2_AgentIA>();
    }

    void Update()
    {
        // --- LOGICA 1: DETECTAR AL LIDER ---
        if (GlobalData.liderActual != null)
        {
            float distAlLider = Vector3.Distance(transform.position, GlobalData.liderActual.transform.position);

            if (!yaMeRescataron && distAlLider < distanciaParaSeguir)
            {
                Debug.Log("EL PLAYER HARDCODEADO TIENE UNA DISTANCIA AL REHEN MENOR A " + distanciaParaSeguir + ". SEGUIRE EL LIDER");
                yaMeRescataron = true;
            }

            if (yaMeRescataron)
            {
                if (distAlLider > distanciaParaParar)
                {
                    agente.GoTo(GlobalData.liderActual.transform.position);
                }
                else
                {
                    agente.StopAgent();
                }
            }
        }

        // --- LOGICA 2: DETECTAR VICTORIA (BIEN TOSCO) ---
        if (objetoVictoria != null)
        {
            float distAVictoria = Vector3.Distance(transform.position, objetoVictoria.position);

            // Debug para ver en consola cuanto falta (opcional)
            // Debug.Log("Distancia a victoria: " + distAVictoria);

            if (distAVictoria < distanciaParaGanar)
            {
                Debug.Log("REHEN LLEGO A VICTORIA - CAMBIANDO ESCENA");
                SceneManager.LoadScene(nombreDeLaEscena);
            }
        }
        else
        {
            Debug.LogWarning("¡OJO! No asignaste el objeto Victoria en el Inspector del Rehen");
        }
    }
}