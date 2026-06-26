using Fusion;
using UnityEngine;

namespace Redes.Network
{
    public struct NetworkInputData : INetworkInput
    {
        public Vector2 Move;          // WASD / ejes
        public Vector2 AimDirection;  // hacia dónde mira/dispara
        public NetworkButtons Buttons; // fire, reload, crouch, teleport
    }

    public enum InputButton 
    { 
        Fire     = 0, 
        Reload   = 1,
        Crouch   = 2,   // CTRL — agacharse (mecánica extra)
        Teleport = 3    // SPACE — teletransporte (mecánica extra)
    }
}
