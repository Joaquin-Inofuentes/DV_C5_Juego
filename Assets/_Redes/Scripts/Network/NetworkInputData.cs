using Fusion;
using UnityEngine;

namespace Redes.Network
{
    public struct NetworkInputData : INetworkInput
    {
        public Vector2 Move;          // WASD / ejes
        public Vector2 AimDirection;  // hacia dónde mira/dispara
        public NetworkButtons Buttons; // fire, reload
    }

    public enum InputButton 
    { 
        Fire = 0, 
        Reload = 1 
    }
}
