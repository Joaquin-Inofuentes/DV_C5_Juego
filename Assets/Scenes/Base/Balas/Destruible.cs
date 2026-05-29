using UnityEngine;
using Game.Squad;

/// <summary>
/// Componente genérico para objetos destruibles (como enemigos o elementos del escenario).
/// Implementa IDaniable para recibir impactos del sistema de proyectiles.
/// </summary>
public class Destruible : MonoBehaviour, IDaniable
{
    [Header("Configuración de Salud")]
    public float vida = 100f;
    public float maxVida = 100f;

    [Header("Regeneración de Salud")]
    public float healRate = 5f;
    public float healDelay = 2f;

    private float lastDamageTime;

    private void Start()
    {
        vida = Mathf.Min(vida, maxVida);
    }

    private void Update()
    {
        // Regeneración de vida pasiva (Desactivada)
        /*
        if (Time.time - lastDamageTime >= healDelay && vida < maxVida)
        {
            vida = Mathf.MoveTowards(vida, maxVida, healRate * Time.deltaTime);
        }
        */
    }

    public void RecibirDano(int cantidad, GameObject atacante)
    {
        vida -= cantidad;
        lastDamageTime = Time.time;

        // Si es un soldado controlado por SoldierController, derivar el daño a su propio MVC
        SoldierController soldier = GetComponent<SoldierController>();
        if (soldier != null)
        {
            soldier.RecibirDano(cantidad, atacante);
            return;
        }

        // Si es un enemigo controlado por EnemyController (MVC nuevo)
        Game.Enemy.EnemyController newEnemy = GetComponent<Game.Enemy.EnemyController>();
        if (newEnemy != null)
        {
            newEnemy.RecibirDano(cantidad, atacante);
            return;
        }

        // Si es un Enemigo estándar que tiene el script Enemigo
        Enemigo enemigo = GetComponent<Enemigo>();
        if (enemigo != null)
        {
            enemigo.vidaActual = vida; // Sincronizar vida con script Enemigo
            if (atacante != null)
            {
                enemigo.objetivo = atacante.transform;
            }
        }

        if (vida <= 0)
        {
            Morir();
        }
    }

    private void Morir()
    {
        // Sincronizar con el líder actual si era el líder
        if (GlobalData.liderActual != null && GlobalData.liderActual.gameObject == gameObject)
        {
            GlobalData.liderActual = null;
        }

        // Si tiene el script Enemigo, notificar al GameManager
        Enemigo enemigo = GetComponent<Enemigo>();
        if (enemigo != null && GameManager.instance != null)
        {
            GameManager.instance.EliminarEnemigo(gameObject);
        }

        Destroy(gameObject);
    }
}
