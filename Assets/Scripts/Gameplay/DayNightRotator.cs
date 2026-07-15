using UnityEngine;

public class DayNightRotator : MonoBehaviour
{
    [Tooltip("Speed at which the light rotates in degrees per second")]
    public float rotationSpeed = 1f;

    [Tooltip("Axis of rotation (usually X for day/night cycle)")]
    public Vector3 rotationAxis = Vector3.right;

    void Update()
    {
        // Rotate the light around the specified axis
        transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
    }
}
