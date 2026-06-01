using System;
using System.Collections.Generic;
using UnityEngine;

public enum UpgradeType
{
    StatHealth,
    StatDamage,
    StatSpeed,
    Skill1,
    Skill2
}

[Serializable]
public class UpgradeOption
{
    public UpgradeType type;
    public string      title;
    public string      description;
    public int         currentLevel; // -1 = stat (no cap display)
    public int         maxLevel;     // -1 = no cap

    public UpgradeOption(UpgradeType t, string title, string desc, int cur, int max)
    {
        this.type         = t;
        this.title        = title;
        this.description  = desc;
        this.currentLevel = cur;
        this.maxLevel     = max;
    }
}

/// <summary>
/// Singleton. Manages upgrade levels and generates random upgrade pools.
/// Applies upgrades to player components.
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    // ---- Upgrade progress ----
    public int Skill1Level { get; private set; } = 0;  // 0=locked, 1-4=tiers
    public int Skill2Level { get; private set; } = 0;
    public int HealthUpgrades { get; private set; } = 0;
    public int DamageUpgrades { get; private set; } = 0;
    public int SpeedUpgrades  { get; private set; } = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ---- Pool generation ----
    public List<UpgradeOption> GetRandomThreeUpgrades()
    {
        var pool = BuildPool();

        // Fisher-Yates shuffle
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j   = UnityEngine.Random.Range(0, i + 1);
            var tmp = pool[i]; pool[i] = pool[j]; pool[j] = tmp;
        }

        // Pick 3 with unique types
        var result    = new List<UpgradeOption>();
        var usedTypes = new HashSet<UpgradeType>();
        foreach (var opt in pool)
        {
            if (usedTypes.Add(opt.type))
            {
                result.Add(opt);
                if (result.Count == 3) break;
            }
        }

        // Pad if needed (edge case with very few options)
        while (result.Count < 3)
            result.Add(MakeHealthOption());

        return result;
    }

    private List<UpgradeOption> BuildPool()
    {
        var pool = new List<UpgradeOption>
        {
            MakeHealthOption(),
            new UpgradeOption(UpgradeType.StatDamage, "⚔  Attack Power",
                "Slash damage +20%.\nStrike harder with every blow.",
                DamageUpgrades, -1),
            new UpgradeOption(UpgradeType.StatSpeed, "💨  Swift Feet",
                "Movement speed +15%.\nLeave your enemies behind.",
                SpeedUpgrades, -1)
        };

        // Skill 1 — only if not maxed
        if (Skill1Level < 4)
        {
            bool isUnlock = Skill1Level == 0;
            pool.Add(new UpgradeOption(
                UpgradeType.Skill1,
                isUnlock ? "🌩  UNLOCK: Sky Plunge" : "⬆  Sky Plunge",
                GetSkill1Desc(Skill1Level),
                Skill1Level, 4));
        }

        // Skill 2 — only if not maxed
        if (Skill2Level < 4)
        {
            bool isUnlock = Skill2Level == 0;
            pool.Add(new UpgradeOption(
                UpgradeType.Skill2,
                isUnlock ? "🔥  UNLOCK: Flame Dash" : "⬆  Flame Dash",
                GetSkill2Desc(Skill2Level),
                Skill2Level, 4));
        }

        return pool;
    }

    private UpgradeOption MakeHealthOption()
        => new UpgradeOption(UpgradeType.StatHealth, "❤  Vitality",
            "Max HP +25.\nSurvive more punishment.",
            HealthUpgrades, -1);

    private string GetSkill1Desc(int level)
    {
        return level switch
        {
            0 => "Leap into the sky, then slam down at a target\ndealing massive AOE damage, stunning\nand knocking back all nearby enemies.",
            1 => "Sky Plunge damage +50%.\nHit harder on each descent.",
            2 => "Impact radius +30%.\nCrush even more enemies per slam.",
            3 => "Cooldown reduced by 5 seconds.\nRain down more often.",
            _ => "MAXED"
        };
    }

    private string GetSkill2Desc(int level)
    {
        return level switch
        {
            0 => "Dash forward through enemies leaving\na scorching flame trail. Finish with\na devastating 360° spin attack.",
            1 => "Dash damage +40% and trail is wider.\nBurn a broader path of destruction.",
            2 => "Spin finisher damage +50%.\nLeave nothing standing at the end.",
            3 => "Cooldown −3s. Trail lasts an extra second.\nTorment enemies longer.",
            _ => "MAXED"
        };
    }

    // ---- Apply ----
    public void ApplyUpgrade(UpgradeOption opt)
    {
        switch (opt.type)
        {
            case UpgradeType.StatHealth: HealthUpgrades++; ApplyHealth(); break;
            case UpgradeType.StatDamage: DamageUpgrades++; ApplyDamage(); break;
            case UpgradeType.StatSpeed:  SpeedUpgrades++;  ApplySpeed();  break;
            case UpgradeType.Skill1:     Skill1Level++;    ApplySkill1(); break;
            case UpgradeType.Skill2:     Skill2Level++;    ApplySkill2(); break;
        }
    }

    private void ApplyHealth()
    {
        var ph = FindObjectOfType<PlayerHealth>();
        if (ph != null) ph.AddMaxHealth(25f);
    }

    private void ApplyDamage()
    {
        var pc = FindObjectOfType<PlayerController>();
        if (pc != null) pc.slashDamageMultiplier += 0.20f;
    }

    private void ApplySpeed()
    {
        var pc = FindObjectOfType<PlayerController>();
        if (pc != null) pc.moveSpeed *= 1.15f;
    }

    private void ApplySkill1()
    {
        // Tìm trực tiếp từ đối tượng PlayerController hiện có trong game
        var pc = FindObjectOfType<PlayerController>();
        if (pc != null)
        {
            var skill = pc.GetComponent<SkillSkyPlunge>();
            if (skill != null) skill.SetLevel(Skill1Level);
        }
    }

    private void ApplySkill2()
    {
        // Tìm trực tiếp từ đối tượng PlayerController hiện có trong game
        var pc = FindObjectOfType<PlayerController>();
        if (pc != null)
        {
            var skill = pc.GetComponent<SkillFlameDash>();
            if (skill != null) skill.SetLevel(Skill2Level);
        }
    }
}
