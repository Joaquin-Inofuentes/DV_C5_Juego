using UnityEngine;

public static class IA_P2_LineOfSight3D
{
    public static bool Check(Vector3 from, Vector3 to, LayerMask obstacleLayer, float radius = 0f)
    {
        Vector2 start = from;
        Vector2 end = to;
        
        if (radius <= 0f)
        {
            RaycastHit2D hit = Physics2D.Linecast(start, end, obstacleLayer);
            return hit.collider == null;
        }
        else
        {
            Vector2 dir = end - start;
            float dist = dir.magnitude;
            if (dist < 0.01f) return true;
            dir.Normalize();
            
            RaycastHit2D hit = Physics2D.CircleCast(start, radius, dir, dist, obstacleLayer);
            return hit.collider == null;
        }
    }
}
