using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Keeps the tutorial camera cinematic while gameplay maps use a third-person orbit camera.
/// </summary>
public class CameraController : MonoBehaviour
{
    public enum CameraMode
    {
        TopDown,
        Cinematic,
        ThirdPerson
    }

    [Header("Target")]
    public Transform target;

    [Header("Top-down / Cinematic")]
    public Vector3 offset = new Vector3(0f, 15f, -10f);
    public float pitchAngle = 60f;

    [Header("Third-person Gameplay")]
    public float followHeight = 1.8f;
    public float thirdPersonDistance = 7.5f;
    [Range(10f, 45f)] public float thirdPersonPitch = 20f;
    public float mouseYawSensitivity = 0.16f;
    public float keyboardYawSpeed = 150f;

    [Header("Third-person Soft Follow")]
    [Min(0f)] public float lockedFollowYawSpeed = 240f;

    [Header("Follow")]
    public float smoothSpeed = 10f;

    public bool IsThirdPerson => currentMode == CameraMode.ThirdPerson;

    private Vector3 defaultOffset;
    private float defaultPitch;
    private CameraMode defaultMode;
    private CameraMode currentMode;
    private Vector3 focusPoint;
    private float yawAngle;

    private bool frozen;
    private float freezeTimer;
    private bool shaking;
    private float shakeIntensity;
    private float shakeTimer;
    private bool inspectorTuningPaused;
    private float timeScaleBeforeInspectorTuning = 1f;

    private void OnValidate()
    {
        // Inspector values should remain previewable while a running game is paused.
        if (!Application.isPlaying || target == null || Time.timeScale > 0f)
        {
            return;
        }

        focusPoint = target.position;
        SnapToTarget();
    }

    private void Start()
    {
        bool isGameplayMap = IsGameplayMap(SceneManager.GetActiveScene().name);
        if (target == null && isGameplayMap)
        {
            PlayerController playerController = FindFirstObjectByType<PlayerController>();
            if (playerController != null)
            {
                target = playerController.transform;
            }
        }

        if (isGameplayMap)
        {
            ApplySharedGameplayCameraSettings();
            EnsureGameplayPlayerControls();
            EnsureSharedGameplaySystems();
        }

        defaultOffset = offset;
        defaultPitch = pitchAngle;
        defaultMode = IsGameplayMap(SceneManager.GetActiveScene().name)
            ? CameraMode.ThirdPerson
            : CameraMode.TopDown;
        currentMode = defaultMode;
        yawAngle = target != null ? target.eulerAngles.y : transform.eulerAngles.y;

        if (target != null)
        {
            focusPoint = target.position;
            SnapToTarget();
        }

        UpdateCursorState();
    }

    private void EnsureGameplayPlayerControls()
    {
        if (!IsGameplayMap(SceneManager.GetActiveScene().name))
        {
            return;
        }

        PlayerController playerController = target != null
            ? target.GetComponent<PlayerController>()
            : FindFirstObjectByType<PlayerController>();

        if (playerController != null && playerController.GetComponent<AttackArcIndicator>() == null)
        {
            playerController.gameObject.AddComponent<AttackArcIndicator>();
        }
    }

    private void EnsureSharedGameplaySystems()
    {
        if (!IsGameplayMap(SceneManager.GetActiveScene().name))
        {
            return;
        }

        GameObject systems = GameObject.Find("GameplayRuntimeSystems");
        if (systems == null)
        {
            systems = new GameObject("GameplayRuntimeSystems");
        }

        if (FindFirstObjectByType<XPManager>() == null)
        {
            systems.AddComponent<XPManager>();
        }

        if (FindFirstObjectByType<UpgradeManager>() == null)
        {
            systems.AddComponent<UpgradeManager>();
        }

        if (FindFirstObjectByType<LevelUpUI>() == null)
        {
            systems.AddComponent<LevelUpUI>();
        }

        if (FindFirstObjectByType<PlayerLevelUI>() == null)
        {
            systems.AddComponent<PlayerLevelUI>();
        }

        if (FindFirstObjectByType<SkillCooldownUI>() == null)
        {
            systems.AddComponent<SkillCooldownUI>();
        }

        if (FindFirstObjectByType<GameOverManager>() == null)
        {
            systems.AddComponent<GameOverManager>();
        }

        PlayerHealth playerHealth = target != null
            ? target.GetComponent<PlayerHealth>()
            : FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null && FindFirstObjectByType<PlayerHealthUI>() == null)
        {
            PlayerHealthUI healthUi = systems.AddComponent<PlayerHealthUI>();
            healthUi.playerHealth = playerHealth;
        }

        if (FindFirstObjectByType<GameplayPauseMenu>(FindObjectsInactive.Include) == null)
        {
            systems.AddComponent<GameplayPauseMenu>();
        }
    }

    private void ApplySharedGameplayCameraSettings()
    {
        // Maps own their terrain and spawn points; camera feel is global gameplay.
        followHeight = 3.83f;
        thirdPersonDistance = 8.7f;
        thirdPersonPitch = 16.4f;
        lockedFollowYawSpeed = 240f;
        smoothSpeed = 10f;
    }

    private void Update()
    {
        if (WasCameraTuningPressed())
        {
            ToggleCameraTuningPause();
            return;
        }

        if (currentMode != CameraMode.ThirdPerson || frozen || Time.timeScale <= 0f)
        {
            return;
        }

        if (target != null)
        {
            yawAngle = Mathf.MoveTowardsAngle(
                yawAngle,
                target.eulerAngles.y,
                lockedFollowYawSpeed * Time.unscaledDeltaTime);
        }

        UpdateCursorState();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        if (!frozen)
        {
            focusPoint = target.position;
        }

        Quaternion desiredRotation;
        Vector3 desiredPosition;

        if (currentMode == CameraMode.ThirdPerson)
        {
            Vector3 thirdPersonFocus = focusPoint + Vector3.up * followHeight;
            desiredRotation = Quaternion.Euler(thirdPersonPitch, yawAngle, 0f);
            desiredPosition = thirdPersonFocus - desiredRotation * Vector3.forward * thirdPersonDistance;
        }
        else
        {
            desiredRotation = Quaternion.Euler(pitchAngle, 0f, 0f);
            desiredPosition = focusPoint + offset;
        }

        // Keep the gameplay paused, but allow Inspector camera tuning to be visible.
        if (Time.timeScale <= 0f)
        {
            transform.SetPositionAndRotation(desiredPosition, desiredRotation);
            return;
        }

        if (frozen)
        {
            freezeTimer -= Time.deltaTime;
            if (freezeTimer <= 0f)
            {
                frozen = false;
            }
        }
        else
        {
            float followT = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followT);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, followT);
        }

        ApplyShake();
    }

    public Vector3 GetPlanarForward()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        return forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
    }

    public void PreviewFromInspector()
    {
        if (target == null)
        {
            return;
        }

        focusPoint = target.position;
        SnapToTarget();
    }

    public void PauseForCameraTuning()
    {
        if (inspectorTuningPaused || Time.timeScale <= 0f)
        {
            return;
        }

        inspectorTuningPaused = true;
        timeScaleBeforeInspectorTuning = Time.timeScale;
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        PreviewFromInspector();
    }

    public void ResumeFromCameraTuning()
    {
        if (!inspectorTuningPaused)
        {
            return;
        }

        inspectorTuningPaused = false;
        Time.timeScale = timeScaleBeforeInspectorTuning <= 0f ? 1f : timeScaleBeforeInspectorTuning;
        UpdateCursorState();
    }

    private void ToggleCameraTuningPause()
    {
        if (inspectorTuningPaused)
        {
            ResumeFromCameraTuning();
            return;
        }

        PauseForCameraTuning();
    }

    public void SetCinematicView(Vector3 newOffset, float newPitch)
    {
        offset = newOffset;
        pitchAngle = newPitch;
        currentMode = CameraMode.Cinematic;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ResetView()
    {
        offset = defaultOffset;
        pitchAngle = defaultPitch;
        currentMode = defaultMode;
        UpdateCursorState();
    }

    public void FreezeFor(float seconds)
    {
        frozen = true;
        freezeTimer = seconds;
    }

    public void Shake(float intensity, float duration)
    {
        shaking = true;
        shakeIntensity = intensity;
        shakeTimer = Mathf.Max(shakeTimer, duration);
    }

    private void SnapToTarget()
    {
        if (currentMode == CameraMode.ThirdPerson)
        {
            Quaternion rotation = Quaternion.Euler(thirdPersonPitch, yawAngle, 0f);
            transform.rotation = rotation;
            transform.position = focusPoint + Vector3.up * followHeight - rotation * Vector3.forward * thirdPersonDistance;
            return;
        }

        transform.rotation = Quaternion.Euler(pitchAngle, 0f, 0f);
        transform.position = focusPoint + offset;
    }

    private void ApplyShake()
    {
        if (!shaking)
        {
            return;
        }

        shakeTimer -= Time.deltaTime;
        if (shakeTimer <= 0f)
        {
            shaking = false;
            return;
        }

        float decay = Mathf.Clamp01(shakeTimer / 0.5f);
        Vector3 shake = Random.insideUnitSphere * shakeIntensity * decay;
        shake.y = 0f;
        transform.position += shake;
    }

    private void UpdateCursorState()
    {
        if (currentMode != CameraMode.ThirdPerson || Time.timeScale <= 0f)
        {
            return;
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private static bool WasCameraTuningPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F9);
#endif
    }

    private static bool IsGameplayMap(string sceneName)
    {
        return sceneName == "map 1"
            || sceneName == "map2"
            || sceneName == "map3"
            || sceneName == "SampleScene";
    }
}
