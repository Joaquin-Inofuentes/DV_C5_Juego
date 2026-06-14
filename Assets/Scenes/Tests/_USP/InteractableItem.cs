using UnityEngine;
using System.Linq;
using Game.Squad;
using Game.Core;

public class InteractableItem : MonoBehaviour, IInteractable
{
    public string nombreItem = "Botiquín";
    public float curacion = 50f;

    private bool leaderInside = false;
    private GameObject leaderGo;

    public string GetInteractName() => nombreItem;
    public Transform GetTransform() => this == null ? null : transform;

    private void Update()
    {
        // Si el líder está dentro del trigger y presiona F, recoge el item
        if (leaderInside && Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log($"<color=yellow>[Interactable]</color> Líder presionó 'F'. Recogiendo {nombreItem}...");
            Interact(leaderGo);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        UnitController unit = other.GetComponent<UnitController>();
        if (unit == null || unit.model.IsDead || unit.model.team != UnitTeam.BandoA) return;

        if (unit.model.IsLeader)
        {
            leaderInside = true;
            leaderGo = other.gameObject;
            Debug.Log($"<color=yellow>[Interactable]</color> Cerca de {nombreItem}. Presiona <color=orange><b>'F'</b></color> para recogerlo.");
        }
        else
        {
            // Si es un aliado controlado por la IA, lo consume automáticamente al tocarlo
            Debug.Log($"<color=green>[Interactable]</color> Aliado {unit.name} tocó el {nombreItem}. Consumiendo...");
            Interact(other.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        UnitController unit = other.GetComponent<UnitController>();
        if (unit != null && unit.model.IsLeader)
        {
            leaderInside = false;
            leaderGo = null;
        }
    }

    public void Interact(GameObject interactuante)
    {
        if (nombreItem == "Botiquín")
        {
            // Buscar la unidad del equipo Player con menos vida (incluyendo al líder si lo necesita, priorizando heridos)
            var unidadesHeridas = FindObjectsOfType<UnitController>()
                .Where(u => u.model.team == UnitTeam.BandoA && !u.model.IsDead && u.model.healthActual < u.model.healthMax)
                .OrderBy(u => u.model.healthActual)
                .Select(u => u.model)
                .FirstOrDefault();

            if (unidadesHeridas != null)
            {
                Debug.Log($"<color=green>[CURACIÓN]</color> {nombreItem} aplicado a {unidadesHeridas.name} (+{curacion} HP)");
                unidadesHeridas.AddHealth(curacion);
                var healedUnit = unidadesHeridas.GetComponent<UnitController>();
                if (healedUnit != null) healedUnit.OnHealPickup();
                
                // Efecto de audio al curarse si existe
                BD_Audios.ReproducirConSolapamiento("Recoje la municion"); 
                
                Destroy(gameObject);
            }
            else
            {
                Debug.Log("<color=yellow>[Interactable]</color> Ninguna unidad del escuadrón necesita curación actualmente.");
            }
        }
    }
}