using UnityEngine;
using Game.Squad;

public class Disparador : MonoBehaviour
{
    public float dañoBala = 10f;
    public float velocidadBala = 25f;

    [Header("Dispersión")]
    [Tooltip("Ángulo total de dispersión en grados (0=preciso, 30=cono amplio)")]
    public float dispersión = 0f;

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
        Debug.Log($"[FLAG:SHOOT_START] {name} inició secuencia de Disparar()");

        if (BalaPool.Instance == null)
        {
            BalaPool.Instance = FindFirstObjectByType<BalaPool>();
            Debug.Log($"[FLAG:SHOOT_POOL_FIND] Buscando BalaPool en escena...");
        }

        if (BalaPool.Instance == null)
        {
            Debug.LogError("[FLAG:SHOOT_ERROR] ¡Falta el prefab de Bala en BalaPool o BalaPool no está instanciado!");
            return;
        }

        // Reproducir sonido de disparo si está configurado
        if (!string.IsNullOrEmpty(disparoSoundName))
        {
            BD_Audios.ReproducirConSolapamiento(disparoSoundName);
            Debug.Log($"[FLAG:SHOOT_AUDIO] Sonido '{disparoSoundName}' reproducido.");
        }

        Bala b = BalaPool.Instance.GetBala();
        if (b == null)
        {
            Debug.LogError("[FLAG:SHOOT_ERROR] ¡BalaPool.Instance.GetBala() retornó null!");
            return;
        }

        Debug.Log($"[FLAG:SHOOT_SPAWN] Bala instanciada/obtenida del pool exitosamente.");

        b.transform.position = transform.position;

        // Aplicar dispersión: rotación base + variación aleatoria dentro del cono
        if (dispersión > 0.01f)
        {
            float angulo = UnityEngine.Random.Range(-dispersión * 0.5f, dispersión * 0.5f);
            b.transform.rotation = transform.rotation * Quaternion.Euler(0, 0, angulo);
            Debug.Log($"[FLAG:SHOOT_SPREAD] Dispersión aplicada: {angulo} grados.");
        }
        else
        {
            b.transform.rotation = transform.rotation;
        }

        b.damage = dañoBala;
        b.velocidad = velocidadBala;
        b.dueno = transform.root.gameObject;

        // Asignar nombres de VFX e Impacto a la bala
        b.vfxName = vfxName;
        b.impactSoundName = impactSoundName;

        Debug.Log($"[FLAG:SHOOT_EXPELLED] Bala expulsada. Dueño: {b.dueno.name}, Daño: {b.damage}, Vel: {b.velocidad}");
    }
}
