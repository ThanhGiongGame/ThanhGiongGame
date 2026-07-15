using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Skill 2 – Flame Dash (Press [2] to aim, Left-Click to confirm).
/// Player dashes through enemies, leaves a burning flame trail, ends with a 360° spin attack.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class SkillFlameDash : MonoBehaviour
{
    // ---- Per-level stats ----
    private static readonly float[] DashDamages    = { 0f, 20f,  28f,  28f,  28f  };
    private static readonly float[] TrailDamages   = { 0f,  5f,   7f,   7f,   7f  };
    private static readonly float[] SpinDamages    = { 0f, 30f,  30f,  45f,  45f  };
    private static readonly float[] DashRanges     = { 0f,  8f,   8f,   8f,   8f  };
    private static readonly float[] Cooldowns      = { 0f, 12f,  12f,  12f,   9f  };
    private static readonly float[] TrailDurations = { 0f,  2f,   2f,   2f,   3f  };
    private static readonly float[] TrailWidths    = { 0f,  1f, 1.5f, 1.5f, 1.5f  };
    private static readonly float[] SpinRadii      = { 0f,  3f,   3f,   3f,   3f  };
    private const float DashSpeed      = 30f;   // units per second
    private const float TrailInterval  = 0.05f; // spawn trail every N seconds
    private const float SpinDuration   = 0.25f;
    private const float InvulnDuration = 1f;  // brief iframes after spin

    // ---- Colors ----
    private static readonly Color LineColor   = new Color(1.00f, 0.55f, 0.10f, 1f);
    private static readonly Color CircleColor = new Color(1.00f, 0.30f, 0.00f, 0.85f);

    // ---- Runtime ----
    private int   _level = 0;
    private float _dashDamage, _trailDamage, _spinDamage;
    private float _dashRange, _cooldown, _trailDuration, _trailWidth, _spinRadius;
    private float _cooldownTimer = 0f;

    private enum State { Idle, Aiming, Active }
    private State _state = State.Idle;

    // ---- References ----
    private PlayerController _pc;
    private CharacterController _cc;
    private CameraController _cam;
    private Camera           _mainCamera;
    private SkillIndicator   _indicator;
    private Vector3          _dashDirection;

    // ---- HUD ----
    public int   Level             => _level;
    public bool  IsOnCooldown      => _cooldownTimer > 0f;
    public float CooldownRemaining => _cooldownTimer;
    public float CooldownMax       => _cooldown;

    // ================================================================
    private void Start()
    {
        _pc         = GetComponent<PlayerController>();
        _cc         = GetComponent<CharacterController>();
        _cam        = FindObjectOfType<CameraController>();
        _mainCamera = Camera.main;
    }

    private void Update()
    {
        if (_level == 0) return;

        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;

        switch (_state)
        {
            case State.Idle:   HandleIdle();   break;
            case State.Aiming: HandleAiming(); break;
        }
    }
    public void SetLevel(int level)
    {
        _level = Mathf.Clamp(level, 0, 4);
        if (_level == 0) return;
        _dashDamage    = DashDamages[_level];
        _trailDamage   = TrailDamages[_level];
        _spinDamage    = SpinDamages[_level];
        _dashRange     = DashRanges[_level] * 1.5f;
        _cooldown      = Cooldowns[_level];
        _trailDuration = TrailDurations[_level];
        _trailWidth    = TrailWidths[_level];
        _spinRadius    = SpinRadii[_level] * 2;
    }

    // ================================================================
    // States
    // ================================================================

    private void HandleIdle()
    {
        if (_pc.IsPerformingSkill) return;
        if (IsOnCooldown)          return;

        if (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame)
            EnterAiming();
    }

    private void EnterAiming()
    {
        _state = State.Aiming;
        _pc.IsPerformingSkill = true;

        if (_indicator == null)
            _indicator = SkillIndicator.CreateLineAndCircle(LineColor, CircleColor);
        _indicator.SetVisible(true);
    }

    private void HandleAiming()
    {
        Vector3 dir = transform.forward;
        dir.y = 0f;

        Vector3 endPt = transform.position + dir.normalized * _dashRange;
        endPt.y = 0f;

        _dashDirection = (endPt - transform.position);
        _dashDirection.y = 0f;

        _indicator.UpdateLineAndCircle(transform.position, endPt, 0f); // No spin radius needed

        // Confirm
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            StartCoroutine(DashRoutine(_dashDirection.normalized));
            return;
        }

        // Cancel
        bool cancel =
            (Mouse.current    != null && Mouse.current.rightButton.wasPressedThisFrame) ||
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame);
        if (cancel)
        {
            _indicator.SetVisible(false);
            _state = State.Idle;
            _pc.IsPerformingSkill = false;
        }
    }

    // ================================================================
    // Dash Coroutine
    // ================================================================

    // ================================================================
    // Dash Coroutine
    // ================================================================

    private IEnumerator DashRoutine(Vector3 direction)
    {
        _state = State.Active;
        _pc.IsInvulnerable = true;

        float dashDuration = _dashRange / DashSpeed;
        float elapsed = 0f;
        float trailTimer = 0f;
        var hitEnemies = new HashSet<Enemy>();

        // --- BƯỚC 1: TẮT COLLIDER ĐỂ KHÔNG BỊ KHỰNG KHI LƯỚT QUA QUÁI ---
        if (_cc != null) _cc.enabled = false;

        // ---- Dash ----
        while (elapsed < dashDuration)
        {
            // Ép trạng thái bất tử liên tục mỗi frame để không bị script khác đè cấu trúc
            _pc.IsInvulnerable = true;

            float dt = Time.deltaTime;
            elapsed += dt;
            trailTimer += dt;

            // Di chuyển trực tiếp transform trong lúc tắt Collider
            transform.position += direction * DashSpeed * dt;

            // Pin Y
            Vector3 p = transform.position; p.y = 0f; transform.position = p;

            // Face dash direction
            if (direction != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(direction);

            // Gây sát thương dọc đường lướt (Tuyệt đối không tự vả vào chính mình)
            Collider[] cols = Physics.OverlapSphere(transform.position, 1.2f);
            foreach (Collider col in cols)
            {
                if (col.gameObject == gameObject || !col.CompareTag("Enemy")) continue;
                Enemy e = col.GetComponent<Enemy>();
                if (e != null && hitEnemies.Add(e))
                {
                    e.TakeDamage(_dashDamage);
                    Vector3 kb = direction;
                    kb.y = 0f;
                    e.ApplyKnockbackStun(kb.normalized, 10f, 0.5f);
                }
            }

            // Flame trail
            if (trailTimer >= TrailInterval)
            {
                trailTimer = 0f;
                SpawnFlameTrail(transform.position);
            }

            yield return null;
        }

        // --- BƯỚC 2: BẬT LẠI COLLIDER KHI KẾT THÚC ĐOẠN LƯỚT ---
        if (_cc != null)
        {
            _cc.enabled = true;
            _cc.Move(Vector3.zero); // Giải phóng vận tốc thừa
        }

        // ---- Spin attack removed based on feedback ----

        // ---- Brief invulnerability after dash ----
        // Tiếp tục dùng vòng lặp duy trì bất tử cho hết thời gian InvulnDuration
        float elapsedInvuln = 0f;
        while (elapsedInvuln < InvulnDuration)
        {
            _pc.IsInvulnerable = true;
            elapsedInvuln += Time.deltaTime;
            yield return null;
        }

        // --- KẾT THÚC SKILL ---
        _indicator.SetVisible(false);
        _pc.IsInvulnerable = false;
        _pc.IsPerformingSkill = false;
        _state = State.Idle;
        _cooldownTimer = _cooldown;
    }
    // ================================================================
    // Flame Trail
    // ================================================================

    private void SpawnFlameTrail(Vector3 pos)
    {
        GameObject go = new GameObject("FlameTrail");
        go.transform.position = pos;
        FlameTrailZone zone = go.AddComponent<FlameTrailZone>();
        zone.Initialize(_trailDamage, _trailDuration, _trailWidth);
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

// ============================================================
// Flame Trail Zone — burns lingering enemies
// ============================================================
public class FlameTrailZone : MonoBehaviour
{
    private float _damage;
    private float _duration;
    private float _width;
    private float _tickTimer;
    private const float TickInterval = 0.25f;

    public void Initialize(float damage, float duration, float width)
    {
        _damage   = damage;
        _duration = duration;
        _width    = width;

        // Trigger collider
        SphereCollider col = gameObject.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius    = width * 0.6f;

        // Particles
        BuildFireParticles();

        Destroy(gameObject, duration);
    }

    private void Update()
    {
        _tickTimer += Time.deltaTime;
        if (_tickTimer >= TickInterval)
        {
            _tickTimer = 0f;
            // Overlap tick for enemies already inside
            Collider[] cols = Physics.OverlapSphere(transform.position, _width * 0.6f);
            foreach (Collider col in cols)
            {
                if (col.CompareTag("Enemy"))
                {
                    Enemy e = col.GetComponent<Enemy>();
                    if (e != null) e.TakeDamage(_damage * TickInterval);
                }
            }
        }
    }

    private void BuildFireParticles()
    {
        GameObject psGO = new GameObject("Fire");
        psGO.transform.SetParent(transform, false);
        ParticleSystem ps = psGO.AddComponent<ParticleSystem>();

        Color fireOrange = new Color(1f, 0.45f, 0.05f);
        Color fireYellow = new Color(1f, 0.85f, 0.10f);

        var main = ps.main;
        main.loop            = true;
        main.playOnAwake     = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(1f, 3f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.15f * _width, 0.4f * _width);
        main.startColor      = new ParticleSystem.MinMaxGradient(fireOrange, fireYellow);
        main.gravityModifier = -0.4f;
        main.maxParticles    = 60;

        var emission = ps.emission;
        emission.rateOverTime = 40f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = _width * 0.4f;

        var col2 = ps.colorOverLifetime;
        col2.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(fireOrange, 0f), new GradientColorKey(new Color(0.3f, 0.1f, 0f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col2.color = new ParticleSystem.MinMaxGradient(g);

        // Shader
        string[] candidates = {
            "Universal Render Pipeline/Particles/Unlit",
            "Particles/Standard Unlit",
            "Legacy Shaders/Particles/Additive",
            "Sprites/Default"
        };
        Shader shader = null;
        foreach (string s in candidates) { shader = Shader.Find(s); if (shader != null) break; }
        Material mat = new Material(shader ?? Shader.Find("Standard"));
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color",     fireOrange);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", fireOrange);
        ps.GetComponent<ParticleSystemRenderer>().material = mat;

        ps.Play();
    }
}
