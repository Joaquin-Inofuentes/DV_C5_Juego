using UnityEngine;

public static class IA_P2_LineOfSight3D
{
    public static bool Check(Vector3 from, Vector3 to, LayerMask obstacleLayer)
    {
        // En 2D YX, la profundidad (Z) debería ser la misma para ambos
        // o simplemente calculamos la dirección real en 3D.
        Vector3 dir = to - from;
        float dist = dir.magnitude;

        if (dist < 0.001f) return true;

        // El Raycast funciona perfectamente en cualquier plano siempre que no forcemos ejes a 0
        return !Physics.Raycast(from, dir.normalized, dist, obstacleLayer);
    }
}