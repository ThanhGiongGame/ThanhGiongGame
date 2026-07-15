using UnityEngine;

/// <summary>
/// Utility class for validating enemy spawn positions.
/// Ensures enemies never pop in where the player can see them.
/// </summary>
public static class SpawnValidator
{
    /// <summary>
    /// Check if a world-space point is inside the camera's view frustum.
    /// Uses a small AABB around the point for the test.
    /// </summary>
    public static bool IsInCameraFrustum(Camera cam, Vector3 point, float margin = 2f)
    {
        if (cam == null) return false;

        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(cam);
        Bounds pointBounds = new Bounds(point, Vector3.one * margin);
        return GeometryUtility.TestPlanesAABB(frustumPlanes, pointBounds);
    }

    /// <summary>
    /// Check if camera can directly see a point (no obstacles blocking).
    /// Returns true if there IS a clear line of sight (bad for spawning).
    /// Returns false if something blocks the view (good for spawning).
    /// </summary>
    public static bool HasClearLineOfSight(Camera cam, Vector3 targetPoint)
    {
        if (cam == null) return false;

        Vector3 camPos = cam.transform.position;
        Vector3 direction = targetPoint - camPos;
        float distance = direction.magnitude;

        // If too close, assume visible
        if (distance < 5f) return true;

        // Cast a ray from camera to the spawn point
        if (Physics.Raycast(camPos, direction.normalized, out RaycastHit hit, distance))
        {
            // Something blocked the ray before reaching the target point
            float hitDistance = hit.distance;
            float targetDistance = distance;

            // If the hit is significantly closer than the target, it's blocked
            if (hitDistance < targetDistance - 1f)
            {
                return false; // Blocked — safe to spawn
            }
        }

        // Nothing blocked it — camera can see the point directly
        return true;
    }

    /// <summary>
    /// Check if a point is within the spawn ring around the player.
    /// </summary>
    public static bool IsInSpawnRing(Vector3 playerPos, Vector3 point, float innerRadius, float outerRadius)
    {
        float dist = Vector3.Distance(
            new Vector3(playerPos.x, 0, playerPos.z),
            new Vector3(point.x, 0, point.z)
        );
        return dist >= innerRadius && dist <= outerRadius;
    }

    /// <summary>
    /// Try to find a valid spawn point that satisfies all conditions:
    /// 1. Inside the spawn ring (innerRadius to outerRadius)
    /// 2. NOT inside camera frustum
    /// 3. NO clear line of sight from camera
    /// 4. On y=0 (flat ground game)
    /// 
    /// Returns true if a valid point was found, with the point in 'result'.
    /// Makes up to maxAttempts tries before giving up.
    /// </summary>
    public static bool FindValidSpawnPoint(
        Vector3 playerPos,
        Camera cam,
        float innerRadius,
        float outerRadius,
        out Vector3 result,
        int maxAttempts = 15)
    {
        result = Vector3.zero;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Generate a random point in the ring
            Vector3 candidate = GenerateRingPoint(playerPos, innerRadius, outerRadius);

            // Prefer points behind the camera (60% chance of biasing backward)
            if (cam != null && attempt < maxAttempts / 2)
            {
                Vector3 camForward = cam.transform.forward;
                camForward.y = 0;
                camForward.Normalize();

                // Bias: spawn behind camera
                Vector3 behindDir = -camForward;
                float angle = Random.Range(-90f, 90f); // ±90° behind camera
                behindDir = Quaternion.Euler(0, angle, 0) * behindDir;

                float dist = Random.Range(innerRadius, outerRadius);
                candidate = playerPos + behindDir * dist;
                candidate.y = 0f;
            }

            // Check 1: Must NOT be in camera frustum
            if (IsInCameraFrustum(cam, candidate))
            {
                continue;
            }

            // Check 2: Must NOT have clear line of sight from camera
            if (HasClearLineOfSight(cam, candidate))
            {
                continue;
            }

            // All checks passed
            result = candidate;
            return true;
        }

        // Fallback: if we couldn't find a perfect spot, just pick one outside frustum
        for (int fallback = 0; fallback < 5; fallback++)
        {
            Vector3 candidate = GenerateRingPoint(playerPos, innerRadius, outerRadius);
            if (!IsInCameraFrustum(cam, candidate))
            {
                result = candidate;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Generate a random point within a ring around a center position.
    /// </summary>
    private static Vector3 GenerateRingPoint(Vector3 center, float innerRadius, float outerRadius)
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float dist = Random.Range(innerRadius, outerRadius);
        Vector3 point = center + new Vector3(
            Mathf.Cos(angle) * dist,
            0f,
            Mathf.Sin(angle) * dist
        );
        point.y = 0f;
        return point;
    }
}
