using UnityEngine;

public static class IA_P2_LineOfSight3D
{
    public static bool Check(Vector3 from, Vector3 to, LayerMask obstacleLayer)
    {
        Vector2 start = from;
        Vector2 end = to;
        
        RaycastHit2D hit = Physics2D.Linecast(start, end, obstacleLayer);
        return hit.collider == null;
    }
}
