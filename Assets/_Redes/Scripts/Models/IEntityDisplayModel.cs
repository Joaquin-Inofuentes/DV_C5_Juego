using UnityEngine;

namespace Redes.Models
{
    /// <summary>
    /// Model interface representing an entity whose status/health can be displayed in the UI overlay.
    /// Scalable to support other properties in the future (e.g. ammo, name, energy).
    /// </summary>
    public interface IEntityDisplayModel
    {
        Vector3 WorldPosition { get; }
        float HealthProgress { get; } // Value between 0 and 1
        string Nickname { get; }
        bool IsActive { get; }
        float ReloadProgress { get; } // Value between 0 and 1
        bool IsReloading { get; }
    }
}
