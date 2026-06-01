using UnityEngine;
using System.Collections.Generic;
using Game.Squad;
using Game.Core;

public class LeaderManager : MonoBehaviour
{
    public static LeaderManager Instance;
    public List<UnitController> unidades; // Lista unificada
    public int indiceInicial = 0;

    void OnEnable() => Instance = this;

    void Start()
    {
        // Inicializar el primer líder
        if (unidades.Count > indiceInicial) CambiarLider(indiceInicial);
    }

    public void CambiarLider(int index)
    {
        if (index < 0 || index >= unidades.Count) return;

        // Desactivar líder anterior
        if (GlobalData.liderActual != null)
        {
            GlobalData.liderActual.model.IsLeader = false;
            GlobalData.liderActual.CambiarEstado(new SeguirFormacionState());
        }

        // Asignar nuevo
        GlobalData.liderActual = unidades[index];
        GlobalData.liderActual.model.IsLeader = true;

        // El líder no sigue a nadie, se mueve por input
        GlobalData.liderActual.ReleaseSlot();
        GlobalData.liderActual.CambiarEstado(new LiderandoState());

        Debug.Log($"<color=yellow>Nuevo Líder: {GlobalData.liderActual.name}</color>");
    }

    void Update()
    {
        // Teclas 1, 2, 3 para cambiar de líder rápido
        if (Input.GetKeyDown(KeyCode.Alpha1)) CambiarLider(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) CambiarLider(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) CambiarLider(2);
    }
}