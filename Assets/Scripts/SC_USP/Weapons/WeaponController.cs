using UnityEngine;
using Game.MVC;
using USP.Core;

namespace USP.Weapons
{
    /// <summary>
    /// Controlador principal de armas (MVC - Controller).
    /// Coordina la entrada de disparo/recarga, la selección en el WeaponModel y la respuesta visual en WeaponView.
    /// Reemplaza completamente a CambioDeArma.
    /// Preparado para integración futura con Photon mediante la bandera 'hasInputAuthority'.
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
    [Header("Componentes MVC")]
    [Tooltip("Modelo de armas que gestiona estadísticas y munición.")]
    public WeaponModel model;

    [Tooltip("Vista de armas que gestiona efectos de disparo, sonidos y modelos activos.")]
    public WeaponView view;

    [Header("UI (Lectura)")]
    [Tooltip("Indicador de balas actual en formato 'balas / reserva'.")]
    public string IndicadorDeBalas;

    [Header("Preparación para Multijugador (Photon)")]
    [Tooltip("Define si este cliente controla a este jugador localmente. En multijugador, esto debe sincronizarse con Object.HasInputAuthority o photonView.IsMine.")]
    public bool hasInputAuthority = true;

    // Componentes de Entrada
    private IWeaponInput input;
    private WeaponControllerMVC controllerMVC;

    private void Start()
    {
        input = new UnityWeaponInput();

        // Autodetectar si no se asignaron en el Inspector
        if (model == null) model = GetComponent<WeaponModel>();
        if (view == null) view = GetComponent<WeaponView>();

        if (model != null)
        {
            ValidarListasModelo();
        }

        if (model != null && view != null)
        {
            controllerMVC = new WeaponControllerMVC(model, view, input);
        }
    }

    private void Update()
    {
        // Si no tiene autoridad de entrada (es un proxy remoto en red), no procesar inputs de disparo locales
        if (!hasInputAuthority) return;

        if (controllerMVC != null && model != null)
        {
            controllerMVC.HandleUpdate(Time.deltaTime);

            // Sincronizar el string del indicador para otros componentes
            IndicadorDeBalas = controllerMVC.IndicadorDeBalas;
        }
    }

    private void ValidarListasModelo()
    {
        int cantidadArmas = model.tiposDeArmas.Length;

        if (model.balasEnCargador == null || model.balasEnCargador.Length != cantidadArmas)
        {
            model.balasEnCargador = new int[cantidadArmas];
            for (int i = 0; i < cantidadArmas; i++)
            {
                model.balasEnCargador[i] = model.cargadorMaximo[i];
            }
        }

        if (model.reservaTotal == null || model.reservaTotal.Length != cantidadArmas)
        {
            model.reservaTotal = new int[] { 45, 90, 18 };
        }
    }

    private void OnDestroy()
    {
        controllerMVC?.Unsubscribe();
    }
}
}
