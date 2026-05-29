using UnityEngine;

namespace Game.MVC
{
    public class UnityCharacterInput : ICharacterInput
    {
        public Vector2 GetMovementInput()
        {
            return new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        }

        public Vector3 GetMouseWorldPosition(Camera mainCamera)
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return Vector3.zero;

            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            return new Vector3(mousePos.x, mousePos.y, 0f);
        }

        public bool GetJumpInput()
        {
            return Input.GetKey(KeyCode.Escape);
        }

        public bool GetHealInput()
        {
            return Input.GetKeyDown(KeyCode.Q);
        }

        public bool GetThrowGrenadeInput()
        {
            return Input.GetKeyDown(KeyCode.G) || Input.GetKeyDown(KeyCode.Mouse1);
        }

        public bool GetFullHealInput()
        {
            return Input.GetKeyDown(KeyCode.T);
        }
    }
}
