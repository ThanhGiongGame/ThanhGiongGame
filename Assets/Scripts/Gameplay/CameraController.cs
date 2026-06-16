using System.Collections;
using UnityEngine;

/// <summary>
/// Top-down camera follow that keeps the player centered.
/// Also supports FreezeFor() (used by Sky Plunge) and Shake().
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Offset")]
    public Vector3 offset        = new Vector3(0f, 15f, -10f);
    public float   pitchAngle    = 60f;
    private Vector3 _defaultOffset;
    private float _defaultPitch;
    [Header("Follow")]
    public float smoothSpeed     = 6f;

    // ---- Internal ----
    private Vector3 _focusPoint;          // world-space point camera is centred on (ground level)

    private bool  _frozen;
    private float _freezeTimer;

    private bool  _shaking;
    private float _shakeIntensity;
    private float _shakeTimer;

    // -------------------------------------------------------

    private void Start()
    {
        transform.rotation = Quaternion.Euler(pitchAngle, 0f, 0f);
        _defaultOffset = offset;
        _defaultPitch = pitchAngle;
        if (target != null)
        {
            _focusPoint = target.position;
            transform.position = _focusPoint + offset;
        }
    }
    //Cinematic View
    public void SetCinematicView(
    Vector3 newOffset,
    float newPitch)
    {
        offset = newOffset;
        pitchAngle = newPitch;

        transform.rotation =
            Quaternion.Euler(
                pitchAngle,
                0,
                0
            );
    }
    public void ResetView()
    {
        offset = _defaultOffset;
        pitchAngle = _defaultPitch;

        transform.rotation =
            Quaternion.Euler(
                pitchAngle,
                0,
                0
            );
    }
    private void LateUpdate()
    {
        if (target == null) return;

        if (!_frozen)
        {
            _focusPoint = target.position;
        }

        // ---- Desired position ----
        Vector3 desired = _focusPoint + offset;

        // ---- Freeze ----
        if (_frozen)
        {
            _freezeTimer -= Time.deltaTime;
            if (_freezeTimer <= 0f) _frozen = false;
            // Camera position stays at current value — don't lerp
        }
        else
        {
            transform.position = desired;
        }

        // ---- Shake ----
        if (_shaking)
        {
            _shakeTimer -= Time.deltaTime;
            if (_shakeTimer <= 0f)
            {
                _shaking = false;
            }
            else
            {
                float decay = _shakeTimer / 0.5f; // normalise against max 0.5 s
                Vector3 shake = Random.insideUnitSphere * _shakeIntensity * Mathf.Clamp01(decay);
                shake.y = 0f;
                transform.position += shake;
            }
        }
    }

    // -------------------------------------------------------
    // Public API for skills
    // -------------------------------------------------------

    /// <summary>Freeze camera position for <paramref name="seconds"/> seconds.</summary>
    public void FreezeFor(float seconds)
    {
        _frozen      = true;
        _freezeTimer = seconds;
    }

    /// <summary>Add a screen shake effect.</summary>
    public void Shake(float intensity, float duration)
    {
        _shaking        = true;
        _shakeIntensity = intensity;
        _shakeTimer     = Mathf.Max(_shakeTimer, duration); // don't cut short an existing shake
    }
}
