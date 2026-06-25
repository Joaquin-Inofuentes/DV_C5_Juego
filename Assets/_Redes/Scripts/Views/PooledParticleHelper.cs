using UnityEngine;

namespace Redes.Views
{
    public class PooledParticleHelper : MonoBehaviour
    {
        private ParticleSystem _particleSystem;

        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
        }

        private void OnEnable()
        {
            if (_particleSystem != null)
            {
                _particleSystem.Play();
            }
        }

        private void Update()
        {
            if (_particleSystem != null && !_particleSystem.isPlaying)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
