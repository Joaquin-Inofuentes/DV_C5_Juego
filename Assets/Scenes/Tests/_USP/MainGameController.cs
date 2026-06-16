using Game.Squad;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MainGameController : MonoBehaviour
{
    public LeaderManager leaderManager;
    public TextMeshProUGUI txtLider;
    public TextMeshProUGUI txtAliados;
    public Image imgHP;
    public Image imgAmmo;
    public float suavizado = 5f;

    private float transicionDuracion = 0f;
    private float transicionTimer = 0f;
    private Vector3 transicionInicioPos;

    public void IniciarTransicionSuave(float duracion)
    {
        transicionDuracion = duracion;
        transicionTimer = duracion;
        transicionInicioPos = transform.position;
    }

    private float lastLogTime;

    void Update()
    {
        if (leaderManager == null) return;

        // 1. SEGUIR AL LÍDER CON LA CÁMARA
        if (GlobalData.liderActual != null)
        {
            Vector3 destino = GlobalData.liderActual.transform.position + new Vector3(0, 0, -10);
            
            if (transicionTimer > 0f)
            {
                transicionTimer -= Time.deltaTime;
                float t = Mathf.Clamp01(1f - (transicionTimer / transicionDuracion));
                transform.position = Vector3.Lerp(transicionInicioPos, destino, Mathf.SmoothStep(0f, 1f, t));
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, destino, suavizado * Time.deltaTime);
            }

            UnitModel m = GlobalData.liderActual.model;
            if (m != null)
            {
                string specName = m.specialization.ToString().ToUpper();
                if (m.specialization == UnitSpecialization.Flancotirador)
                {
                    specName = "SNIPER";
                }

                txtLider.text = specName;

                if (imgHP != null)
                {
                    imgHP.fillAmount = Mathf.Clamp01(m.healthActual / m.healthMax);
                }
                if (imgAmmo != null)
                {
                    imgAmmo.fillAmount = Mathf.Clamp01((float)m.ammoActual / m.ammoMax);
                }

                if (Time.time - lastLogTime > 0.5f)
                {
                    lastLogTime = Time.time;
                    Debug.Log($"[UI_DEBUG] Lider: {specName} | HP Fill: {(imgHP != null ? imgHP.fillAmount : 0):F2} | Ammo Fill: {(imgAmmo != null ? imgAmmo.fillAmount : 0):F2}");
                }
            }
        }

        // 2. LISTA DEL PELOTÓN
        string lista = "<color=yellow>PELOTON:</color>\n";
        for (int i = 0; i < leaderManager.unidades.Count; i++)
        {
            var u = leaderManager.unidades[i];
            if (u == null || u.model.IsDead)
            {
                lista += $"[{i + 1}] <color=red>MUERTO</color>\n";
            }
            else
            {
                UnitFSM fsm = u.GetComponent<UnitFSM>();
                string estado = fsm != null ? fsm.currentState.ToString() : "---";
                string specName = u.model.specialization.ToString();
                if (u.model.specialization == UnitSpecialization.Flancotirador)
                {
                    specName = "Sniper";
                }
                lista += $"[{i + 1}] {specName} ({estado})\n";
            }
        }
        txtAliados.text = lista;
    }
}