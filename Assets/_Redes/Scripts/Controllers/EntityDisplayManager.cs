using System.Collections.Generic;
using UnityEngine;
using Redes.Models;
using Redes.Views;
using Redes.Player;
using Redes.Core;

namespace Redes.Controllers
{
    /// <summary>
    /// MVC - CONTROLLER that manages a pool of 20 health sliders on the Canvas,
    /// converting the world positions of active entities (players/enemies) to screen space.
    /// Scalable and decoupled from concrete player implementations via IEntityDisplayModel.
    /// </summary>
    public class EntityDisplayManager : MonoBehaviour
    {
        [Header("Prefab & Parent")]
        [SerializeField] private EntityDisplayView _viewPrefab;
        [SerializeField] private RectTransform _canvasParent;

        [Header("Tuning")]
        [SerializeField] private int _poolSize = 20;
        [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 2.2f, 0f); // Offset above entity's head

        private List<EntityDisplayView> _pool = new List<EntityDisplayView>();
        private Camera _mainCamera;

        private void Awake()
        {
            _mainCamera = Camera.main;
            InitializePool();
        }

        private void InitializePool()
        {
            if (_viewPrefab == null || _canvasParent == null) return;

            for (int i = 0; i < _poolSize; i++)
            {
                var view = Instantiate(_viewPrefab, _canvasParent);
                view.SetVisible(false);
                _pool.Add(view);
            }
        }

        private void LateUpdate()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }

            // Find all active IEntityDisplayModel entities in the scene
            // In our current project, NetworkPlayer implements IEntityDisplayModel.
            var players = FindObjectsByType<Redes.Player.NetworkPlayer>(FindObjectsSortMode.None);
            List<IEntityDisplayModel> activeEntities = new List<IEntityDisplayModel>();
            foreach (var p in players)
            {
                if (p.IsActive)
                {
                    activeEntities.Add(p);
                }
            }

            int activeCount = 0;

            for (int i = 0; i < _pool.Count; i++)
            {
                var view = _pool[i];

                if (activeCount < activeEntities.Count)
                {
                    var entity = activeEntities[activeCount];
                    
                    try
                    {
                        // Convert world position (with offset) to screen coordinates
                        Vector3 worldPos = entity.WorldPosition + _worldOffset;
                        Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);

                        var playerObj = entity as Redes.Player.NetworkPlayer;
                        string debugInfo = playerObj != null && playerObj.Object != null
                            ? $"ObjId={playerObj.Object.Id}, PlayerRef={playerObj.Object.InputAuthority}, HasStateAuth={playerObj.Object.HasStateAuthority}"
                            : "N/A";

                        // Check if the entity is in front of the camera
                        if (screenPos.z > 0)
                        {
                            view.SetVisible(true);
                            view.SetPosition(screenPos);
                            view.SetHealth(entity.HealthProgress);
                            view.SetNickname(entity.Nickname);
                            view.SetReloadProgress(entity.ReloadProgress, entity.IsReloading);

                            if (Time.frameCount % 30 == 0)
                            {
                                RedesLog.Info(RedesLog.PLAYER, $"[UI slider update] entity='{entity.Nickname}', health={entity.HealthProgress} pos={screenPos} ({debugInfo})");
                            }
                        }
                        else
                        {
                            view.SetVisible(false);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        RedesLog.Error(RedesLog.PLAYER, $"[EntityDisplayManager ERROR] Exception updating slider for activeEntity index={activeCount}: {ex.Message}\n{ex.StackTrace}");
                    }

                    activeCount++;
                }
                else
                {
                    // Deactivate unused sliders in the pool
                    try
                    {
                        view.SetVisible(false);
                    }
                    catch (System.Exception ex)
                    {
                        RedesLog.Error(RedesLog.PLAYER, $"[EntityDisplayManager ERROR] Exception hiding pool view index={i}: {ex.Message}");
                    }
                }
            }
        }
    }
}
