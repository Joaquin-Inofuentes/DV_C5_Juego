using UnityEngine;

public class TopDownCameraFollow : MonoBehaviour
{
    public static TopDownCameraFollow Instance { get; private set; }

    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
    [SerializeField] private float smooth = 12f;

    private Transform _target;

    private void Awake()
    {
        Instance = this;
    }

    public void SetTarget(Transform target)
    {
        _target = target;
        Debug.Log("[TopDownCameraFollow] Camera sigui al jugador principal");
    }

    private void LateUpdate()
    {
        if (_target == null) return;

        Vector3 desired = _target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * smooth);
    }
}
