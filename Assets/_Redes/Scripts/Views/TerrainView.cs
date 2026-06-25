using UnityEngine;

namespace Redes.Views
{
    public class TerrainView : MonoBehaviour
    {
        [SerializeField] private Renderer _terrainRenderer;

        public void Initialize(Vector3 size, Color color, Texture2D texture)
        {
            transform.localScale = size;
            
            if (_terrainRenderer != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                if (texture != null)
                {
                    mat.mainTexture = texture;
                    mat.mainTextureScale = new Vector2(size.x, size.z);
                }
                _terrainRenderer.sharedMaterial = mat;
            }
        }
    }
}
