using UnityEngine;

namespace Game.MVC
{
    public class Rigidbody2DMovementHandler : MonoBehaviour, IMovementHandler
    {
        public void Move(Transform targetTransform, Rigidbody2D rb2D, Vector2 direction, float speed)
        {
            Vector3 displacement = new Vector3(direction.x, direction.y, 0f) * speed * Time.deltaTime;
            targetTransform.position += displacement;

            if (rb2D != null)
            {
                rb2D.position = targetTransform.position;
            }
        }

        public void TranslateDirectional(Transform targetTransform, string direction, float speed)
        {
            BD_Audios.ReproducirBucleConVolumen("Caminar", true, 0.5f);
            
            Vector3 moveDir = Vector3.zero;
            switch (direction)
            {
                case "Adelante":
                    moveDir = new Vector3(0, 1, 0);
                    break;
                case "Derecha":
                    moveDir = new Vector3(1, 0, 0);
                    break;
                case "Izquierda":
                    moveDir = new Vector3(0, -1, 0);
                    break;
                case "Atras":
                    moveDir = new Vector3(-1, 0, 0);
                    break;
            }

            targetTransform.Translate(moveDir * speed * Time.deltaTime);
        }

        public void RotateTowards(Transform targetTransform, Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - targetTransform.position).normalized;
            if (direction != Vector3.zero)
            {
                targetTransform.up = direction;
            }
        }

        public void Jump(Transform targetTransform, Rigidbody2D rb2D, float jumpForce)
        {
            Rigidbody rb3D = targetTransform.GetComponent<Rigidbody>();
            if (rb3D != null)
            {
                rb3D.AddForce(targetTransform.forward * jumpForce, ForceMode.Impulse);
            }
            else if (rb2D != null)
            {
                rb2D.AddForce(targetTransform.up * jumpForce, ForceMode2D.Impulse);
            }
            else
            {
                targetTransform.position += targetTransform.up * (jumpForce * 0.1f);
            }
        }
    }
}
