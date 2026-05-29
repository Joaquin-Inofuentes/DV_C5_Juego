using UnityEngine;

namespace Game.MVC
{
    public interface ICharacterInput
    {
        Vector2 GetMovementInput();
        Vector3 GetMouseWorldPosition(Camera mainCamera);
        bool GetJumpInput();
        bool GetHealInput();
        bool GetThrowGrenadeInput();
        bool GetFullHealInput();
    }
}
