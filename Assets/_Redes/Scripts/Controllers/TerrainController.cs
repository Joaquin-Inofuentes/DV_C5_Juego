using UnityEngine;
using Redes.Models;
using Redes.Views;

namespace Redes.Controllers
{
    [RequireComponent(typeof(TerrainModel))]
    [RequireComponent(typeof(TerrainView))]
    public class TerrainController : MonoBehaviour
    {
        private TerrainModel _model;
        private TerrainView _view;
        
        [SerializeField] private Texture2D _terrainTexture;

        private void Awake()
        {
            _model = GetComponent<TerrainModel>();
            _view = GetComponent<TerrainView>();
        }

        private void Start()
        {
            _view.Initialize(_model.Size, _model.TerrainColor, _terrainTexture);
        }
    }
}
