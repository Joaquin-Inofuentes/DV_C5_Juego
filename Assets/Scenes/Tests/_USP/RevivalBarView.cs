using UnityEngine;

namespace Game.Squad
{
    // El visual de revivimiento ahora está integrado en UnitView (heartbeat circle).
    // Este componente se mantiene como stub para no romper referencias en el prefab.
    public class RevivalBarView : MonoBehaviour
    {
        public SpriteRenderer revivalBarSprite;
        public GameObject revivalBarRoot;
        public UnitController unitController;
        public float revivalDuration = 3f;
        public float revivalDecreaseSped = 2f;
        public Color revivalColor = Color.yellow;
        public Color revivalFailColor = Color.red;
    }
}
