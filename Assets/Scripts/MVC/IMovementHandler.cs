using UnityEngine;

namespace Game.MVC
{
    public interface IMovementHandler
    {
        void Move(Transform transform, Rigidbody2D rb2D, Vector2 direction, float speed);
        void TranslateDirectional(Transform transform, string direction, float speed);
        void RotateTowards(Transform transform, Vector3 targetPosition);
        void Jump(Transform transform, Rigidbody2D rb2D, float jumpForce);
    }
}
