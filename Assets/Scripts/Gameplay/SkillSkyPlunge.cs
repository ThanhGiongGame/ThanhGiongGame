using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Skill 1 – Sky Plunge (Press [1] to aim, Left-Click to confirm, Right-Click/Escape to cancel).
/// Player flies up, camera holds, then slams down dealing AOE damage + knockback stun.
/// Levels 1-4 buffed stats. Level 0 = locked.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class SkillSkyPlunge : MonoBehaviour
{
    // ---- Per-level stats (index = level) ----
    private static readonly float[] Damages   = { 0f, 40f,  60f,  60f,   60f  };
    private static readonly float[] Radii     = { 0f,  4f,   4f,   5.2f,  5.2f };
    private static readonly float[] Cooldowns = { 0f, 20f,  20f,  20f,   15f  };

    // ---- Timing constants ----
    private const float AscendTime     = 0.50f;
    private const float CameraHoldTime = 1.00f;
    private const float PlungeTime     = 0.30f;
    private const float InvulnDuration = 1.00f;
    private const float AscendHeight   = 10f;

    // ---- Colors ----
    private static readonly Color RingColor   = new Color(1.00f, 0.10f, 0.10f, 1.00f);

    // ---- Runtime state ----
    private int   _level = 0;
    private float _damage;
    private float _impactRadius;
    private float _cooldown;
    private float _cooldownTimer = 0f;
    private enum State { Idle, Aiming, Active }
    private State _state = State.Idle;

    // ---- References ----
    private PlayerController _pc;
    private CameraController _cam;
    private Camera           _mainCamera;
    private SkillIndicator   _indicator;
    private Vector3          _originalScale;
    private Vector3          _targetPos;

    // ---- Public accessors for HUD ----
    public int Level => _level; // Hoặc biến lưu level của bạn
    public bool IsOnCooldown => _cooldownTimer > 0f;
    public float CooldownRemaining => _cooldownTimer;
    public float CooldownMax => _cooldown;

    // ================================================================
    private void Start()
    {
        _pc            = GetComponent<PlayerController>();
        _cam           = FindObjectOfType<CameraController>();
        _mainCamera    = Camera.main;
        _originalScale = transform.localScale;
    }

    private void Update()
    {
        if (_level == 0) return;

        // Tick cooldown with real time to survive any timeScale changes
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;

        switch (_state)
        {
            case State.Idle:   HandleIdle();   break;
            case State.Aiming: HandleAiming(); break;
            // State.Active is handled by coroutine
        }
    }

    // ---- Set level (called by UpgradeManager) ----
    public void SetLevel(int level)
    {
        _level        = Mathf.Clamp(level, 0, 4);
        if (_level == 0) return;
        _damage       = Damages[_level];
        _impactRadius = Radii[_level]*2;
        _cooldown     = Cooldowns[_level];
    }

    // ================================================================
    // States
    // ================================================================

    private void HandleIdle()
    {
        if (_pc.IsPerformingSkill) return;
        if (IsOnCooldown)          return;

        if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
            EnterAiming();
    }

    private void EnterAiming()
    {
        _state = State.Aiming;
        _pc.IsPerformingSkill = true;

        if (_indicator == null)
            _indicator = SkillIndicator.CreateRing(RingColor);
        _indicator.SetVisible(true);
    }

    private void HandleAiming()
    {
        Vector3 mouseWorld = GetMouseWorld();
        _targetPos = mouseWorld;
        _indicator.UpdateRing(mouseWorld, _impactRadius);

        // Confirm
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            _indicator.SetVisible(false);
            StartCoroutine(PlungeRoutine());
            return;
        }

        // Cancel
        bool cancel =
            (Mouse.current  != null && Mouse.current.rightButton.wasPressedThisFrame) ||
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame);
        if (cancel)
        {
            _indicator.SetVisible(false);
            _state = State.Idle;
            _pc.IsPerformingSkill = false;
        }
    }

    // ================================================================
    // Coroutine
    // ================================================================

    private IEnumerator PlungeRoutine()
    {
        _state = State.Active;
        _pc.IsInvulnerable = true;

        // lấy CharacterController gắn trên Player
        CharacterController cc = GetComponent<CharacterController>();

        // --- CHẶN ĐỨNG XUNG ĐỘT VẬT LÝ KHI BAY ---
        if (cc != null) cc.enabled = false;

        // Freeze camera for ascend + hold time
        if (_cam != null) _cam.FreezeFor(AscendTime + CameraHoldTime);

        // ---- Phase 1: Ascend ----
        float t0 = 0f;
        Vector3 startP = transform.position;
        Vector3 highP = startP + Vector3.up * AscendHeight;
        Vector3 startS = _originalScale;
        Vector3 tinyS = _originalScale * 0.05f;

        while (t0 < AscendTime)
        {
            t0 += Time.deltaTime;
            float frac = Mathf.Clamp01(t0 / AscendTime);
            float ease = frac * frac;                       // ease-in
            transform.position = Vector3.Lerp(startP, highP, ease);
            transform.localScale = Vector3.Lerp(startS, tinyS, frac);
            yield return null;
        }
        transform.localScale = tinyS;

        // ---- Phase 2: Camera hold (player is tiny / invisible) ----
        yield return new WaitForSeconds(CameraHoldTime);

        // ---- Phase 3: Plunge ----
        // Teleport above target
        transform.position = _targetPos + Vector3.up * AscendHeight;
        transform.localScale = _originalScale;

        float t1 = 0f;
        Vector3 top = transform.position;
        while (t1 < PlungeTime)
        {
            t1 += Time.deltaTime;
            float frac = Mathf.Clamp01(t1 / PlungeTime);
            float ease = frac * frac * frac;                // cubic ease-in — fast landing
            transform.position = Vector3.Lerp(top, _targetPos, ease);
            yield return null;
        }

        // Đảm bảo tọa độ chuẩn xác tuyệt đối trước khi bật lại Collider
        transform.position = _targetPos;
        transform.localScale = _originalScale;

        // --- BẬT LẠI VẬT LÝ KHI ĐÃ ĐÁP ĐẤT AN TOÀN ---
        if (cc != null)
        {
            cc.enabled = true;
            // Xóa toàn bộ vận tốc tích tụ cũ (nếu có) để tránh bị khựng/giật hình
            cc.Move(Vector3.zero);
        }

        // ---- Landing ----
        DoLandingImpact();

        // ---- Invulnerability frames (Giữ trạng thái bất tử) ----
        // Ép liên tục trong vòng lặp để tránh bị các script khác đè cấu trúc tắt bất tử
        float elapsedInvuln = 0f;
        while (elapsedInvuln < InvulnDuration)
        {
            _pc.IsInvulnerable = true;
            elapsedInvuln += Time.deltaTime;
            yield return null;
        }

        // --- KẾT THÚC SKILL ---
        _pc.IsInvulnerable = false;
        _pc.IsPerformingSkill = false;
        _state = State.Idle;
        _cooldownTimer = _cooldown;
    }
    // ================================================================
    // Impact
    // ================================================================

    private void DoLandingImpact()
    {
        // Camera shake
        if (_cam != null) _cam.Shake(0.6f, 0.35f);

        // Visual burst
        HitEffect.Spawn(_targetPos + Vector3.up * 0.5f, new Color(1f, 0.50f, 0.10f), 3.5f);
        HitEffect.Spawn(_targetPos + Vector3.up * 0.5f, new Color(1f, 0.20f, 0.00f), 2.5f);

        // AOE
        Collider[] hits = Physics.OverlapSphere(_targetPos, _impactRadius);
        foreach (Collider col in hits)
        {
            if (!col.CompareTag("Enemy")) continue;
            Enemy e = col.GetComponent<Enemy>();
            if (e == null) continue;

            e.TakeDamage(_damage);

            Vector3 dir = (col.transform.position - _targetPos);
            dir.y = 0f;
            if (dir == Vector3.zero) dir = Random.insideUnitSphere;
            dir.Normalize();
            e.ApplyKnockbackStun(dir, 18f, 1.8f);
        }
    }

    // ================================================================
    // Utility
    // ================================================================

    private Vector3 GetMouseWorld()
    {
        if (_mainCamera == null || Mouse.current == null) return transform.position;
        Ray   ray   = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        if (plane.Raycast(ray, out float d)) return ray.GetPoint(d);
        return transform.position;
    }
}
