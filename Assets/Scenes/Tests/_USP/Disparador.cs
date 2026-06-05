using UnityEngine;
using Game.Squad;

public class Disparador : MonoBehaviour
{
    public float dañoBala = 10f;
    public float velocidadBala = 25f;

    [Header("Efectos y Sonidos (Por Nombre)")]
    public string vfxName;
    public string disparoSoundName;
    public string impactSoundName;

    private void Start()
    {
        // Validar si existe el VFX
        if (!string.IsNullOrEmpty(vfxName) && Manager_VFX.Instance != null)
        {
            bool exists = Manager_VFX.Instance.vfxPrefabs.Exists(p => p != null && p.name.Equals(vfxName, System.StringComparison.OrdinalIgnoreCase));
            if (!exists)
            {
                Debug.LogError($"[Disparador - {name}] ¡Error! El efecto VFX '{vfxName}' asignado no existe en Manager_VFX.");
            }
        }

        // Validar si existe el Sonido de Disparo
        if (!string.IsNullOrEmpty(disparoSoundName))
        {
            var audioSource = BD_Audios.ObtenerSonidoPorNombre(disparoSoundName);
            if (audioSource == null)
            {
                Debug.LogError($"[Disparador - {name}] ¡Error! El sonido de disparo '{disparoSoundName}' no se encuentra cargado en BD_Audios.");
            }
        }

        // Validar si existe el Sonido de Impacto
        if (!string.IsNullOrEmpty(impactSoundName))
        {
            var audioSource = BD_Audios.ObtenerSonidoPorNombre(impactSoundName);
            if (audioSource == null)
            {
                Debug.LogError($"[Disparador - {name}] ¡Error! El sonido de impacto '{impactSoundName}' no se encuentra cargado en BD_Audios.");
            }
        }
    }

    public void Disparar()
    {
        if (BalaPool.Instance == null)
        {
            BalaPool.Instance = FindFirstObjectByType<BalaPool>();
        }

        if (BalaPool.Instance == null)
        {
            Debug.LogError("[Disparador] ¡Falta el prefab de Bala en BalaPool o BalaPool no está instanciado!");
            return;
        }

        // Reproducir sonido de disparo si está configurado
        if (!string.IsNullOrEmpty(disparoSoundName))
        {
            BD_Audios.ReproducirConSolapamiento(disparoSoundName);
        }

        Bala b = BalaPool.Instance.GetBala();
        if (b == null)
        {
            Debug.LogError("[Disparador] ¡BalaPool.Instance.GetBala() retornó null!");
            return;
        }

        b.transform.position = transform.position;
        b.transform.rotation = transform.rotation;

        b.damage = dañoBala;
        b.velocidad = velocidadBala;
        b.dueno = transform.root.gameObject;
        
        // Asignar nombres de VFX e Impacto a la bala
        b.vfxName = vfxName;
        b.impactSoundName = impactSoundName;
    }
}
