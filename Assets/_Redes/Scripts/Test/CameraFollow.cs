using UnityEngine;

namespace Redes.Test
{
    /// <summary>
    /// Simple script to make the camera follow a target horizontally
    /// without copying its rotation (crucial for top-down mouse look).
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _height = 15f;

        private void LateUpdate()
        {
            if (_target != null)
            {
                transform.position = new Vector3(_target.position.x, _height, _target.position.z);
            }
        }
    }
}
