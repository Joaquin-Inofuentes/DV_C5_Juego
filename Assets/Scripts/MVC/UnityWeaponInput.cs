using UnityEngine;

namespace Game.MVC
{
    public class UnityWeaponInput : IWeaponInput
    {
        public bool GetFireInput(bool isAutomatic)
        {
            // Cambiar de Input.GetButton a GetMouseButton directo para máxima fiabilidad en Unity sin depender del Input Manager
            if (isAutomatic)
            {
                return Input.GetMouseButton(0); // Mantener presionado click izquierdo
            }
            else
            {
                return Input.GetMouseButtonDown(0); // Un click izquierdo
            }
        }

        public bool GetReloadInput()
        {
            return Input.GetKeyDown(KeyCode.R);
        }

        public int GetWeaponSwitchInput()
        {
            // Soportar tanto las teclas numéricas superiores como el teclado numérico derecho (Keypad)
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) return 0;
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) return 1;
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) return 2;
            return -1;
        }
    }
}
