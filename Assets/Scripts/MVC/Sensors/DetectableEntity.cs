using UnityEngine;

namespace Game.Sensors
{
    public class DetectableEntity : MonoBehaviour, IDetectable
    {
        [SerializeField] private string targetName = "Desconocido";
        [SerializeField] private DetectableType type;

        public void Initialize(string name, DetectableType detectableType)
        {
            targetName = name;
            type = detectableType;
        }

        public string GetName() => targetName;
        public DetectableType GetDetectableType() => type;
        public Transform GetTransform() => transform;
    }
}
