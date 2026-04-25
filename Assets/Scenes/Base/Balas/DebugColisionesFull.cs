using UnityEngine;

public class DebugColisionesFull : MonoBehaviour
{
    // =========================
    // 🔴 COLLISION 2D
    // =========================

    public GameObject Objetivo;
    
    private void OnCollisionEnter2D(Collision2D c)
    {
        DebugCollision2D("ENTER", c);
    }

    private void OnCollisionStay2D(Collision2D c)
    {
        DebugCollision2D("STAY", c);
    }

    private void OnCollisionExit2D(Collision2D c)
    {
        DebugCollision2D("EXIT", c);
    }

    void DebugCollision2D(string fase, Collision2D c)
    {
        Debug.Log(
            $"[2D COLLISION {fase}] YO: {name} | CON: {c.gameObject.name} | TAG: {c.gameObject.tag} | LAYER: {LayerMask.LayerToName(c.gameObject.layer)} | CONTACTOS: {c.contactCount}",
            gameObject
        );
    }

    // =========================
    // 🟢 TRIGGER 2D
    // =========================

    private void OnTriggerEnter2D(Collider2D other)
    {
        DebugTrigger2D("ENTER", other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        DebugTrigger2D("STAY", other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        DebugTrigger2D("EXIT", other);
    }

    void DebugTrigger2D(string fase, Collider2D other)
    {
        Debug.Log(
            $"[2D TRIGGER {fase}] YO: {name} | CON: {other.gameObject.name} | TAG: {other.tag} | LAYER: {LayerMask.LayerToName(other.gameObject.layer)}",
            gameObject
        );
    }

    // =========================
    // 🔴 COLLISION 3D
    // =========================

    private void OnCollisionEnter(Collision c)
    {
        DebugCollision3D("ENTER", c);
    }

    private void OnCollisionStay(Collision c)
    {
        DebugCollision3D("STAY", c);
    }

    private void OnCollisionExit(Collision c)
    {
        DebugCollision3D("EXIT", c);
    }

    void DebugCollision3D(string fase, Collision c)
    {
        Debug.Log(
            $"[3D COLLISION {fase}] YO: {name} | CON: {c.gameObject.name} | TAG: {c.gameObject.tag} | LAYER: {LayerMask.LayerToName(c.gameObject.layer)} | CONTACTOS: {c.contactCount}",
            gameObject
        );
    }

    // =========================
    // 🟢 TRIGGER 3D
    // =========================

    private void OnTriggerEnter(Collider other)
    {
        DebugTrigger3D("ENTER", other);
    }

    private void OnTriggerStay(Collider other)
    {
        DebugTrigger3D("STAY", other);
    }

    private void OnTriggerExit(Collider other)
    {
        DebugTrigger3D("EXIT", other);
    }

    void DebugTrigger3D(string fase, Collider other)
    {
        Debug.Log(
            $"[3D TRIGGER {fase}] YO: {name} | CON: {other.gameObject.name} | TAG: {other.tag} | LAYER: {LayerMask.LayerToName(other.gameObject.layer)}",
            gameObject
        );
    }
    
}