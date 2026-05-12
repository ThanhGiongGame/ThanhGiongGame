using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Offset")]
    public Vector3 offset = new Vector3(0f, 15f, -10f); // Default bird's-eye view offset
    public float pitchAngle = 60f; // Downward angle of the camera

    [Header("Follow Settings")]
    public float smoothSpeed = 5f;

    void Start()
    {
        // Set the initial camera rotation
        transform.rotation = Quaternion.Euler(pitchAngle, 0f, 0f);
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
    }
}
