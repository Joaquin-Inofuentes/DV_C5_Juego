using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Requerido para Text legacy

namespace Game.MVC
{
    public class WeaponView : MonoBehaviour
    {
        [Header("Modelos 3D de las Armas (Asignar 3 elementos)")]
        [Tooltip("Arreglo con los GameObjects visuales del arma en primera persona o personaje (se activará según el índice actual).")]
        public GameObject[] modelosArmas;

        [Tooltip("Arreglo secundario de GameObjects visuales de armas por si tienes visuales adicionales de armas en el personaje.")]
        public GameObject[] modelosArmas1;

        [Header("Efectos Visuales de Disparo")]
        [Tooltip("Controlador de opacidad en pantalla o en el material del cañón para el destello del disparo (Muzzle Flash).")]
        public CambiarOpacidad cambiarOpacidad;

        [Header("Instanciación de Bala")]
        [Tooltip("Transform que define el punto exacto de origen del cañón desde donde saldrán disparadas las balas.")]
        public Transform origenDisparo;

        [Tooltip("Prefab del proyectil/bala a instanciar cuando el jugador presione disparar.")]
        public GameObject prefabBala;

        [Tooltip("Script Proyectil para actualizar dinámicamente el daño de la bala actual según el arma equipada.")]
        public Proyectil proyectil;

        [Header("UI y Textos del Personaje")]
        [Tooltip("Componente InformacionPersonaje que administra el HUD del juego para actualizar la vida.")]
        public InformacionPersonaje infoPersonaje;

        [Tooltip("Texto UI Legacy (UnityEngine.UI.Text) donde se mostrará la munición actual en formato con ceros a la izquierda (ej. 06/30).")]
        public Text textoMunicionLegacy;

        public void UpdateActiveModels(int currentWeaponIndex)
        {
            if (modelosArmas != null)
            {
                for (int i = 0; i < modelosArmas.Length; i++)
                {
                    if (modelosArmas[i] != null)
                        modelosArmas[i].SetActive(i == currentWeaponIndex);
                }
            }

            if (modelosArmas1 != null)
            {
                for (int i = 0; i < modelosArmas1.Length; i++)
                {
                    if (modelosArmas1[i] != null)
                        modelosArmas1[i].SetActive(i == currentWeaponIndex);
                }
            }
        }

        public void SetProyectilDamage(float damage)
        {
            if (proyectil != null)
            {
                proyectil.dano = damage;
            }
        }

        public void SpawnBala()
        {
            if (prefabBala != null && origenDisparo != null)
            {
                Instantiate(prefabBala, origenDisparo.position, origenDisparo.rotation);
            }
        }

        public void PlaySound(string soundName)
        {
            BD_Audios.ReproducirConSolapamiento(soundName);
        }

        public void TriggerFlash()
        {
            if (cambiarOpacidad != null && gameObject.activeInHierarchy)
            {
                StopAllCoroutines();
                StartCoroutine(EfectoDisparoFlash());
            }
        }

        private IEnumerator EfectoDisparoFlash()
        {
            if (cambiarOpacidad != null)
            {
                cambiarOpacidad.esTransparente = false;
                yield return new WaitForSeconds(0.1f);
                cambiarOpacidad.esTransparente = true;
            }
        }

        public void UpdateUI()
        {
            if (infoPersonaje != null)
            {
                infoPersonaje.ActualizarUI();
            }
        }

        /// <summary>
        /// Actualiza directamente el texto UI de munición con el formato indicado.
        /// </summary>
        public void UpdateAmmoUI(string ammoText)
        {
            if (textoMunicionLegacy != null)
            {
                textoMunicionLegacy.text = ammoText;
            }

            // Sincronizar el resto de la interfaz (vida, kits, etc.)
            if (infoPersonaje != null)
            {
                infoPersonaje.ActualizarUI();
            }
        }
    }
}
