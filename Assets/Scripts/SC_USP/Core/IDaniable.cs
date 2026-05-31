using UnityEngine;

/// <summary>
/// Contrato para cualquier objeto capaz de recibir daño del sistema de proyectiles
/// (balas, cohetes, torretas, etc.).
///
/// Vive en el namespace GLOBAL a propósito: los proyectiles y sensores la resuelven
/// vía <c>GetComponent&lt;IDaniable&gt;()</c> y varios controladores la implementan
/// como <c>global::IDaniable</c> (Destruible, SoldierController, EnemyController).
/// </summary>
public interface IDaniable
{
    /// <summary>
    /// Aplica daño al objeto.
    /// </summary>
    /// <param name="cantidad">Puntos de daño a infligir.</param>
    /// <param name="atacante">GameObject que originó el daño (puede ser null).</param>
    void RecibirDano(int cantidad, GameObject atacante);
}
