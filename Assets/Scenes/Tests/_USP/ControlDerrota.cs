using UnityEngine;
using UnityEngine.SceneManagement;

public class ControlDerrota : MonoBehaviour
{
    [Header("CONFIGURACION DE DERROTA")]
    public LeaderManager leaderManager; // Arrastra el objeto que tiene el LeaderManager
    public string escenaDerrota = "EscenaPerdiste"; // Nombre de tu escena de Game Over

    [Header("DEBUG (PARA VER EN EL INSPECTOR)")]
    public int soldadosVivos = 0;

    void Update()
    {
        if (leaderManager == null) return;

        // 1. REINICIAMOS EL CONTADOR EN CADA FRAME
        int contador = 0;

        // 2. REVISAMOS LA LISTA "UNIDADES" DEL LEADER MANAGER
        for (int i = 0; i < leaderManager.unidades.Count; i++)
        {
            // Si el slot no es null, significa que ese soldado sigue vivo
            if (leaderManager.unidades[i] != null)
            {
                contador++;
            }
        }

        // Guardamos para ver en el inspector
        soldadosVivos = contador;

        // 3. SI EL CONTADOR ES 0, ES QUE TODOS SON NULL (MUERTOS)
        if (contador <= 0)
        {
            Debug.Log("<color=red>TODOS HAN MUERTO. CARGANDO ESCENA DE DERROTA...</color>");
            SceneManager.LoadScene(escenaDerrota);
        }
    }
}