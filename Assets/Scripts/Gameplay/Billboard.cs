using UnityEngine;

/// <summary>
/// Billboard component for 2D sprites in a 3D/isometric game.
///
/// Two modes:
/// - SPHERICAL (useSpherical=true, default for projectiles):
///     Sprite always faces camera directly. Then rotates in screen space
///     so the sprite's DOWN tip points along travelDirection.
///     Best for arrows, swords, rocks — directional projectiles.
///
/// - CYLINDRICAL (useSpherical=false, default for standing objects):
///     Sprite stays perfectly upright (Y up), only rotates around Y axis to face camera.
///     Best for turtles, bamboo, ribbons — objects that should look like standing things.
/// </summary>
public class Billboard : MonoBehaviour
{
    private Camera _cam;

    /// <summary>World-space travel direction. Sprite's tip (local DOWN) aims along this.</summary>
    [HideInInspector] public Vector3 travelDirection = Vector3.zero;

    /// <summary>
    /// Extra Z rotation (degrees) applied after direction. 
    /// Use 180 if your sprite tip points UP instead of DOWN.
    /// </summary>
    [HideInInspector] public float rotationOffset = 0f;

    /// <summary>
    /// True = full spherical billboard (face camera directly) — use for projectiles.
    /// False = cylindrical (stay upright, Y-axis only rotation) — use for standing objects.
    /// </summary>
    [HideInInspector] public bool useSpherical = false;

    private void Start()
    {
        _cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (_cam == null) { _cam = Camera.main; if (_cam == null) return; }

        // ALWAYS face the camera perfectly (front of sprite faces camera)
        transform.rotation = _cam.transform.rotation;

        // If there's a travel direction, rotate in screen-space so the DOWN tip points that way
        if (travelDirection.sqrMagnitude > 0.001f)
        {
            Vector3 camFwd    = _cam.transform.forward;
            Vector3 camDown   = -_cam.transform.up;
            Vector3 projected = Vector3.ProjectOnPlane(travelDirection.normalized, camFwd);

            if (projected.sqrMagnitude > 0.001f)
            {
                float angle = Vector3.SignedAngle(camDown, projected.normalized, camFwd);
                transform.Rotate(0f, 0f, angle + rotationOffset, Space.Self);
            }
        }
        else if (rotationOffset != 0f)
        {
            transform.Rotate(0f, 0f, rotationOffset, Space.Self);
        }
    }
}
