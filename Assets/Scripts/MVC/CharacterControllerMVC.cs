using UnityEngine;

namespace Game.MVC
{
    public class CharacterControllerMVC
    {
        private readonly CharacterModel model;
        private readonly CharacterView view;
        private readonly ICharacterInput input;
        private readonly IMovementHandler movement;

        public CharacterControllerMVC(CharacterModel model, CharacterView view, ICharacterInput input, IMovementHandler movement)
        {
            this.model = model;
            this.view = view;
            this.input = input;
            this.movement = movement;
        }

        public void HandleUpdate(Transform transform)
        {
            if (model == null || view == null || input == null || movement == null) return;

            // Update Cursor Pos & Rotation
            Vector3 mouseWorldPos = input.GetMouseWorldPosition(null);
            view.UpdateCursorPosition(mouseWorldPos);
            movement.RotateTowards(transform, mouseWorldPos);

            // Actions
            if (input.GetHealInput())
            {
                model.UsarKitMedico();
            }

            if (input.GetFullHealInput())
            {
                model.CurarAlMaximo();
            }

            if (input.GetThrowGrenadeInput() && model.HasGrenades())
            {
                view.SpawnGrenade();
                model.ConsumeGrenade();
            }
        }

        public void HandleFixedUpdate(Transform transform, Rigidbody2D rb2D)
        {
            if (model == null || input == null || movement == null) return;

            // Keyboard Movement
            Vector2 moveInput = input.GetMovementInput();
            if (moveInput.sqrMagnitude > 0.001f)
            {
                movement.Move(transform, rb2D, moveInput, model.velocidadMovimiento);
            }

            // Jump
            if (input.GetJumpInput())
            {
                movement.Jump(transform, rb2D, model.desplazamientoSalto);
            }
        }

        public void HandleLateUpdate(Transform transform, Rigidbody2D rb2D)
        {
            if (view == null) return;

            // Sync Rigidbody physics rotation with transform rotation
            if (rb2D != null)
            {
                rb2D.position = transform.position;
                rb2D.rotation = transform.eulerAngles.z;
            }

            // Camera follow
            view.SyncCameraPosition();
        }

        public void ForceTranslate(Transform transform, string direction, float speed)
        {
            movement?.TranslateDirectional(transform, direction, speed);
        }
    }
}
