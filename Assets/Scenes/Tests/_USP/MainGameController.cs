using Game.Squad;
using UnityEngine;
using TMPro;

public class MainGameController : MonoBehaviour
{
    public LeaderManager leaderManager;
    public TextMeshProUGUI txtLider;
    public TextMeshProUGUI txtAliados;
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

    void FixedUpdate()
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
                txtLider.text = $"<color=yellow>LIDER:</color> {GlobalData.liderActual.name.ToUpper()}\n" +
                                $"HP: {(int)m.healthActual}/{m.healthMax}\n" +
                                $"AMMO: {m.ammoActual}";
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
                lista += $"[{i + 1}] {u.name} HP: {(int)u.model.healthActual} ({estado})\n";
            }
        }
        txtAliados.text = lista;
    }
}