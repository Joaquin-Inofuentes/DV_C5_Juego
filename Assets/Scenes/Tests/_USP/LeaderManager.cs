using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Game.Squad;
using Game.Core;
using UnityEngine.UI;

public class LeaderManager : MonoBehaviour
{
    public static LeaderManager Instance;
    public List<UnitController> unidades;
    public int indiceInicial = 0;

    [Header("UI por Especialidad")]
    public Image imagenVisual;
    public List<UnitSpecialization> tiposSoldado = new List<UnitSpecialization>()
    {
        UnitSpecialization.Flancotirador,
        UnitSpecialization.Asalto,
        UnitSpecialization.Medico
    };
    public List<Texture2D> texturasSoldado = new List<Texture2D>();

    private int indiceActual = 0;
    private Coroutine _cameraLerpCoroutine;

    void OnEnable() => Instance = this;

    /// <summary>Suaviza el cambio de orthographicSize de la cámara principal.</summary>
    public void LerpCameraSize(float targetSize, float duration = 0.5f)
    {
        if (Camera.main == null) return;
        if (_cameraLerpCoroutine != null) StopCoroutine(_cameraLerpCoroutine);
        _cameraLerpCoroutine = StartCoroutine(CameraLerpRoutine(targetSize, duration));
    }

    private IEnumerator CameraLerpRoutine(float target, float duration)
    {
        float start = Camera.main.orthographicSize;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Camera.main.orthographicSize = Mathf.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        Camera.main.orthographicSize = target;
    }

    void Start()
    {
        if (unidades == null || unidades.Count == 0)
        {
            Debug.LogError("[LeaderManager] La lista 'unidades' está vacía. Arrastrá los soldados en el Inspector.");
            return;
        }

        if (GEN_Inputs.Instance == null)
            Debug.LogError("[LeaderManager] No se encontró GEN_Inputs en la escena. Agregá un GameObject con GEN_Inputs.");

        if (unidades.Count > indiceInicial)
            CambiarLider(indiceInicial);
        else
            Debug.LogError($"[LeaderManager] indiceInicial ({indiceInicial}) fuera de rango. Hay {unidades.Count} unidades.");
    }

    void OnDestroy()
    {
        if (GEN_Inputs.Instance != null)
            GEN_Inputs.Instance.OnCycleLeader -= OnCycleLeader;
    }

    void Update()
    {
        SuscribirseAInputs();

        // Si el líder actual ha muerto o no existe, cambiar al aliado vivo más cercano
        if (GlobalData.liderActual == null || GlobalData.liderActual.model.IsDead)
        {
            AsignarLiderMasCercanoALiderMuerto();
        }

        // --- LÓGICA DE CAMPER ---
        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
        {
            UnitController targetUnit = null;

            // 1. Verificar si el mouse apunta a algún soldado de la capa "Soldado"
            if (Camera.main != null)
            {
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Collider2D hit = Physics2D.OverlapPoint(mousePos, LayerMask.GetMask("Soldado"));
                if (hit != null)
                {
                    targetUnit = hit.GetComponent<UnitController>();
                }
            }

            // 2. Si no apunta a nadie, aplicar al líder actual
            if (targetUnit == null)
            {
                targetUnit = GlobalData.liderActual;
            }

            // 3. Aplicar toggle de camper
            if (targetUnit != null && !targetUnit.model.IsDead)
            {
                targetUnit.model.isCamper = !targetUnit.model.isCamper;
                if (targetUnit.model.isCamper)
                {
                    targetUnit.view.ShowSpeech("Camper", 2.8f);
                    targetUnit.agent.StopAgent();
                }
                else
                {
                    targetUnit.view.ShowSpeech("¡Activo!", 1.5f);
                }
                Debug.Log($"[Camper Toggle] {targetUnit.name} isCamper = {targetUnit.model.isCamper}");
            }
        }
    }

    private void AsignarLiderMasCercanoALiderMuerto()
    {
        Vector3 lastLeaderPos = Vector3.zero;
        if (GlobalData.liderActual != null)
        {
            lastLeaderPos = GlobalData.liderActual.transform.position;
        }

        UnitController mejorCandidato = null;
        float minDist = Mathf.Infinity;
        int mejorIndice = -1;

        for (int i = 0; i < unidades.Count; i++)
        {
            var u = unidades[i];
            if (u != null && !u.model.IsDead && u != GlobalData.liderActual)
            {
                float d = lastLeaderPos != Vector3.zero ? Vector3.Distance(u.transform.position, lastLeaderPos) : 0f;
                if (d < minDist)
                {
                    minDist = d;
                    mejorCandidato = u;
                    mejorIndice = i;
                }
            }
        }

        if (mejorCandidato != null && mejorIndice != -1)
        {
            Debug.Log($"[LeaderManager] El líder murió. Cambiando al aliado más cercano: {mejorCandidato.name}");
            
            // Iniciar transición suave de la cámara (MainGameController)
            var cam = FindObjectOfType<MainGameController>();
            if (cam != null)
            {
                cam.IniciarTransicionSuave(1f);
            }

            CambiarLider(mejorIndice);
        }
        else
        {
            GlobalData.liderActual = null;
        }
    }

    private bool _suscrito = false;
    private void SuscribirseAInputs()
    {
        if (_suscrito || GEN_Inputs.Instance == null) return;
        GEN_Inputs.Instance.OnCycleLeader += OnCycleLeader;
        _suscrito = true;
        Debug.Log("[LeaderManager] Suscrito a GEN_Inputs.OnCycleLeader (Q/E).");
    }

    private void OnCycleLeader(bool derecha)
    {
        if (unidades.Count == 0) return;

        int nuevoIndice = indiceActual + (derecha ? 1 : -1);

        // Wrap around
        if (nuevoIndice >= unidades.Count) nuevoIndice = 0;
        if (nuevoIndice < 0) nuevoIndice = unidades.Count - 1;

        // Saltar muertos
        int intentos = 0;
        while (unidades[nuevoIndice] == null || unidades[nuevoIndice].model.IsDead)
        {
            nuevoIndice += derecha ? 1 : -1;
            if (nuevoIndice >= unidades.Count) nuevoIndice = 0;
            if (nuevoIndice < 0) nuevoIndice = unidades.Count - 1;
            intentos++;
            if (intentos >= unidades.Count)
            {
                Debug.LogWarning("[LeaderManager] No hay unidades vivas para liderar.");
                return;
            }
        }

        Debug.Log($"[LeaderManager] Ciclando líder: {(derecha ? "E (derecha)" : "Q (izquierda)")} → índice {nuevoIndice}");
        
        var cam = FindObjectOfType<MainGameController>();
        if (cam != null)
        {
            cam.IniciarTransicionSuave(0.5f);
        }

        CambiarLider(nuevoIndice);
    }

    public void CambiarLider(int index)
    {
        if (index < 0 || index >= unidades.Count)
        {
            Debug.LogWarning($"[LeaderManager] Índice {index} fuera de rango (0-{unidades.Count - 1}).");
            return;
        }

        if (unidades[index] == null || unidades[index].model.IsDead)
        {
            Debug.LogWarning($"[LeaderManager] Unidad en índice {index} es null o está muerta.");
            return;
        }

        // Desactivar líder anterior
        if (GlobalData.liderActual != null)
        {
            GlobalData.liderActual.model.IsLeader = false;
            GlobalData.liderActual.CambiarEstado(new SeguirFormacionState());
            Debug.Log($"[LeaderManager] {GlobalData.liderActual.name} deja de ser líder → SeguirFormacion.");
        }

        // Asignar nuevo
        indiceActual = index;
        GlobalData.liderActual = unidades[index];
        GlobalData.liderActual.model.IsLeader = true;
        GlobalData.liderActual.ReleaseSlot();
        GlobalData.liderActual.CambiarEstado(new LiderandoState());

        Debug.Log($"<color=yellow>[LeaderManager] Nuevo Líder: {GlobalData.liderActual.name} (índice {index})</color>");
        
        ActualizarImagenLider(GlobalData.liderActual.model.specialization);
    }

    public void ActualizarImagenLider(UnitSpecialization especialidad)
    {
        if (imagenVisual == null || tiposSoldado == null || texturasSoldado == null) return;

        int index = tiposSoldado.IndexOf(especialidad);
        if (index >= 0 && index < texturasSoldado.Count)
        {
            Texture2D tex = texturasSoldado[index];
            if (tex != null)
            {
                imagenVisual.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            else
            {
                imagenVisual.sprite = null;
            }
        }
        else
        {
            imagenVisual.sprite = null;
        }
    }
}