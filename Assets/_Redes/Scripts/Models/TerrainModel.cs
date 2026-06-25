using UnityEngine;

namespace Redes.Models
{
    public class TerrainModel : MonoBehaviour
    {
        [SerializeField] private Vector3 _size = new Vector3(20, 1, 20);
        public Vector3 Size => _size;

        [SerializeField] private Color _terrainColor = new Color(0.18f, 0.48f, 0.18f, 1f); // Gameplay Green
        public Color TerrainColor => _terrainColor;
    }
}
