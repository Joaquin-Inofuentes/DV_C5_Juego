using UnityEngine;

namespace Game.MVC
{
    public class CharacterView : MonoBehaviour
    {
        [Header("Estructura del Personaje")]
        [Tooltip("El objeto del personaje (visual) que representa al soldado. Usado para el seguimiento de cámara.")]
        public GameObject soldadoJugador;

        [Tooltip("La cámara principal de la escena que seguirá al jugador.")]
        public Camera camaraPrincipal;

        [Tooltip("El objeto visual del Cursor en el juego que sigue la posición del puntero del mouse.")]
        public GameObject cursor;

        [Header("Granadas")]
        [Tooltip("Punto de spawn o salida (Transform) desde donde se instancian las granadas.")]
        public Transform origenGranada;

        [Tooltip("Prefab del objeto Granada que será arrojado por el jugador.")]
        public GameObject prefabGranada;

        public void UpdateCursorPosition(Vector3 position)
        {
            if (cursor != null)
            {
                cursor.transform.position = position;
            }
        }

        public void SyncCameraPosition()
        {
            if (camaraPrincipal != null && soldadoJugador != null)
            {
                Vector3 targetPos = soldadoJugador.transform.position;
                camaraPrincipal.transform.position = new Vector3(targetPos.x, targetPos.y, camaraPrincipal.transform.position.z);
            }
        }

        public void SpawnGrenade()
        {
            if (prefabGranada != null && origenGranada != null)
            {
                Instantiate(prefabGranada, origenGranada.position, origenGranada.rotation);
            }
            else
            {
                Debug.LogError("Prefab de granada o origen de granada no asignado en CharacterView.");
            }
        }
    }
}
