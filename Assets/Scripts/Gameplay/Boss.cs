using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss : MonoBehaviour
{
    [Header("Stats")]
    public float moveSpeed = 1.5f;
    public float damage = 50f;
    
    [Header("Attack")]
    public float attackRange = 7f;
    public float attackCooldown = 2f;

    [HideInInspector]
    public WaveSpawner waveSpawner;

    private Enemy enemyScript;
    private Transform player;
    private float attackTimer;

    public bool Phase2Active { get; private set; } = false;
    public bool IsStunned { get; private set; } = false;
    private float abilityTimer = 5f;

    public static bool IsSpecialFinalScene = false;
    private bool isDead = false;

    // Health Bar UI
    private UnityEngine.UI.Image healthBarFill;
    private GameObject healthBarCanvas;

    private void Start()
    {
        enemyScript = GetComponent<Enemy>();
        if (enemyScript != null)
        {
            enemyScript.isBoss = true; // disable default AI
            enemyScript.maxHealth = 15000f;
            enemyScript.currentHealth = 15000f;
        }

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        CreateHealthBarUI();
    }

    private void Update()
    {
        if (Enemy.GlobalFreeze || IsStunned || player == null || enemyScript == null) return;

        MoveTowardPlayer();
        AttackPlayer();

        // Prevent flying
        Vector3 pos = transform.position;
        pos.y = 0f;
        transform.position = pos;

        // Phase Transition fallback (Primary check is in Enemy.cs)
        if (!Phase2Active && enemyScript.currentHealth <= enemyScript.maxHealth * 0.5f)
        {
            TriggerPhase2();
        }

        if (healthBarFill != null && enemyScript != null)
        {
            healthBarFill.fillAmount = enemyScript.currentHealth / enemyScript.maxHealth;
        }

        // Ability Timer
        abilityTimer -= Time.deltaTime;
        if (abilityTimer <= 0f)
        {
            UseRandomAbility();
            abilityTimer = Phase2Active ? Random.Range(6f, 10f) : Random.Range(10f, 15f);
        }
    }

    private void MoveTowardPlayer()
    {
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;

        if (distance > attackRange * 0.9f)
        {
            Vector3 dir = toPlayer.normalized;
            transform.position += dir * moveSpeed * Time.deltaTime;

            Quaternion targetRotation = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    private void AttackPlayer()
    {
        attackTimer -= Time.deltaTime;

        Vector3 offset = player.position - transform.position;
        offset.y = 0f;

        if (offset.sqrMagnitude <= attackRange * attackRange && attackTimer <= 0f)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage, offset.normalized, 20f);
            }
            attackTimer = attackCooldown;
        }
    }

    private void UseRandomAbility()
    {
        int r = Random.Range(0, 2);
        if (r == 0) StartCoroutine(ArrowWaveRoutine());
        else StartCoroutine(OrderAttackRoutine());
    }

    private IEnumerator ArrowWaveRoutine()
    {
        int count = Phase2Active ? 12 : 6;
        List<Vector3> targets = new List<Vector3>();

        for (int i = 0; i < count; i++)
        {
            Vector2 rand = Random.insideUnitCircle * 8f;
            Vector3 target = player.position + new Vector3(rand.x, 0, rand.y);
            targets.Add(target);

            SkillIndicator ind = SkillIndicator.CreateRing(Color.red);
            ind.UpdateRing(target, 2f);
            Destroy(ind.gameObject, 2f);
        }

        yield return new WaitForSeconds(2f);

        foreach (Vector3 t in targets)
        {
            HitEffect.Spawn(t, Color.red, 2f);
            
            // Deal damage
            if (Vector3.Distance(player.position, t) <= 2f)
            {
                PlayerHealth ph = player.GetComponent<PlayerHealth>();
                if (ph != null) ph.TakeDamage(damage, (player.position - t).normalized, 5f);
            }
        }
    }

    private IEnumerator OrderAttackRoutine()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;

        Vector3 start = transform.position;
        Vector3 end = start + dir * 30f;

        SkillIndicator ind = SkillIndicator.CreateLineAndCircle(Color.red, Color.clear);
        ind.UpdateLineAndCircle(start, end, 0f);

        yield return new WaitForSeconds(1.5f);
        if (ind != null) Destroy(ind.gameObject);

        GameObject linh2Prefab = EnemyPool.Instance != null ? EnemyPool.Instance.GetPrefab("linh-2") : Resources.Load<GameObject>("Prefabs/linh-2");
        if (linh2Prefab != null && waveSpawner != null)
        {
            int count = Phase2Active ? 6 : 3;
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = Vector3.Cross(dir, Vector3.up) * Random.Range(-4f, 4f);
                GameObject l2 = waveSpawner.SpawnEnemy(linh2Prefab, 1f, 1.5f, start + offset);
                if (l2 != null)
                {
                    Enemy e = l2.GetComponent<Enemy>();
                    if (e != null)
                    {
                        e.isStampeding = true;
                        e.stampedeTarget = end + offset;
                    }
                }
                yield return new WaitForSeconds(0.3f);
            }
        }
    }

    public void TriggerPhase2()
    {
        if (Phase2Active) return;
        StartCoroutine(PhaseTransitionRoutine());
    }

    private IEnumerator PhaseTransitionRoutine()
    {
        Phase2Active = true;
        IsStunned = true;

        // Epic Visual Transformation
        transform.localScale *= 1.5f;
        HitEffect.Spawn(transform.position, Color.black, 3f);
        CameraController cam = Camera.main.GetComponent<CameraController>();
        if (cam != null) cam.Shake(1.5f, 1f);

        // Force Player Transformation
        if (player != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null && !pc.IsPerformingSkill)
            {
                pc.IsInvulnerable = true;
                pc.StartFinalMoveCinematic();
            }
        }

        // Dialogue Canvas
        GameObject canvasObject = new GameObject("BossPhase2DialogueCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        
        GameObject textObj = new GameObject("DialogueText");
        textObj.transform.SetParent(canvasObject.transform, false);
        var text = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = "Ngươi nghĩ bấy nhiêu là đủ sao? Giờ mới là lúc bắt đầu!";
        text.fontSize = 36;
        text.color = Color.red;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        
        RectTransform rt = text.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(0, -200);

        // Drop Health Packs
        for (int i = 0; i < 5; i++)
        {
            Vector2 r = Random.insideUnitCircle * 3f;
            HealthPack.Spawn(transform.position + new Vector3(r.x, 0, r.y), 50f);
        }

        yield return new WaitForSeconds(1.5f);
        
        if (canvasObject != null) Destroy(canvasObject);

        IsStunned = false;

        IsSpecialFinalScene = true;
        StartCoroutine(SpecialPhase2Routine());
    }

    private IEnumerator SpecialPhase2Routine()
    {
        GameObject linh2Prefab = EnemyPool.Instance != null ? EnemyPool.Instance.GetPrefab("linh-2") : Resources.Load<GameObject>("Prefabs/linh-2");
        GameObject linh1Prefab = EnemyPool.Instance != null ? EnemyPool.Instance.GetPrefab("linh-1") : Resources.Load<GameObject>("Prefabs/linh-1");
        
        if (waveSpawner != null)
        {
            for (int i = 0; i < 20; i++)
            {
                GameObject prefab = (i % 2 == 0) ? linh2Prefab : linh1Prefab;
                if (prefab == null) continue;
                
                Vector2 randomCircle = Random.insideUnitCircle.normalized * waveSpawner.spawnRadius;
                Vector3 pos = player.position + new Vector3(randomCircle.x, 0, randomCircle.y);
                
                GameObject enemyObj = waveSpawner.SpawnEnemy(prefab, 99999f, 1.2f, pos);
                if (enemyObj != null)
                {
                    Enemy e = enemyObj.GetComponent<Enemy>();
                    if (e != null)
                    {
                        e.maxHealth = 99999f;
                        e.currentHealth = 99999f;
                    }
                }
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    public void TriggerBossDeathCinematic()
    {
        if (isDead) return;
        isDead = true;
        IsStunned = true;
        
        // Stop moving and attacking
        if (enemyScript != null)
        {
            enemyScript.enabled = false;
        }

        StartCoroutine(BossDeathRoutine());
    }

    private IEnumerator BossDeathRoutine()
    {
        CameraController cam = Camera.main.GetComponent<CameraController>();
        if (cam != null) cam.Shake(2f, 0.5f);

        // Simple Canvas for Dialogue
        GameObject canvasObject = new GameObject("BossDialogueCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        
        GameObject textObj = new GameObject("DialogueText");
        textObj.transform.SetParent(canvasObject.transform, false);
        var text = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = "Ta... không thể thua... Hỡi Gióng, ngươi là ai?!";
        text.fontSize = 36;
        text.color = Color.red;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        
        RectTransform rt = text.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(0, -200);

        yield return new WaitForSeconds(3f);

        Destroy(canvasObject);

        HitEffect.Spawn(transform.position, Color.red, 3f);
        
        PlayerController pc = FindObjectOfType<PlayerController>();
        if (pc != null)
        {
            pc.AscendToSky();
        }
        
        if (waveSpawner != null)
        {
            waveSpawner.OnBossKilled(transform.position);
        }
        
        Destroy(gameObject);
    }

    private void CreateHealthBarUI()
    {
        healthBarCanvas = new GameObject("BossHealthCanvas");
        Canvas canvas = healthBarCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        // Background
        GameObject bgObj = new GameObject("HealthBarBG");
        bgObj.transform.SetParent(healthBarCanvas.transform, false);
        var bgImage = bgObj.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        RectTransform bgRT = bgImage.rectTransform;
        bgRT.anchorMin = new Vector2(0.5f, 1f);
        bgRT.anchorMax = new Vector2(0.5f, 1f);
        bgRT.pivot = new Vector2(0.5f, 1f);
        bgRT.anchoredPosition = new Vector2(0, -30);
        bgRT.sizeDelta = new Vector2(800, 30);

        // Fill
        GameObject fillObj = new GameObject("HealthBarFill");
        fillObj.transform.SetParent(bgObj.transform, false);
        healthBarFill = fillObj.AddComponent<UnityEngine.UI.Image>();
        healthBarFill.color = Color.red;

        // Generate a 1x1 white sprite to allow Filled type to work properly
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.zero);
        healthBarFill.sprite = sprite;

        healthBarFill.type = UnityEngine.UI.Image.Type.Filled;
        healthBarFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        healthBarFill.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
        
        RectTransform fillRT = healthBarFill.rectTransform;
        fillRT.anchorMin = new Vector2(0, 0);
        fillRT.anchorMax = new Vector2(1, 1);
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        // Text
        GameObject textObj = new GameObject("BossNameText");
        textObj.transform.SetParent(bgObj.transform, false);
        var text = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = "BOSS";
        text.fontSize = 24;
        text.color = Color.white;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        
        RectTransform textRT = text.rectTransform;
        textRT.anchorMin = new Vector2(0, 0);
        textRT.anchorMax = new Vector2(1, 1);
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
    }

    private void OnDestroy()
    {
        if (healthBarCanvas != null) Destroy(healthBarCanvas);
    }
}
