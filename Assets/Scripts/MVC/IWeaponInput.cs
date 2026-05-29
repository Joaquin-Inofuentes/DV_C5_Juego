using UnityEngine;

namespace Game.MVC
{
    public interface IWeaponInput
    {
        bool GetFireInput(bool isAutomatic);
        bool GetReloadInput();
        int GetWeaponSwitchInput(); // Returns 0, 1, 2, or -1 if no selection
    }
}
