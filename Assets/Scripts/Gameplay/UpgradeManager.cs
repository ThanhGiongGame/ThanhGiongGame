using System;
using System.Collections.Generic;
using UnityEngine;

public enum UpgradeType
{
    StatHealth, StatDamage, StatSpeed,
    StatXpChance, StatHealthChance,
    Skill1, Skill2,
    
    // Cổ Loa (An Dương Vương)
    Legend_CoLoa_W1, Legend_CoLoa_W2, Legend_CoLoa_Evo,
    // Đông A (Trần Hưng Đạo)
    Legend_DongA_W1, Legend_DongA_W2, Legend_DongA_Evo,
    // Sơn Tinh Thủy Tinh
    Legend_SonTinh_W1, Legend_SonTinh_W2, Legend_SonTinh_Evo,
    // Thánh Gióng
    Legend_ThanhGiong_W1, Legend_ThanhGiong_W2, Legend_ThanhGiong_Evo,
    // Lê Lợi
    Legend_LeLoi_W1, Legend_LeLoi_W2, Legend_LeLoi_Evo
}

public enum LegendSystemType
{
    None, CoLoa, DongA, SonTinh, ThanhGiong, LeLoi
}

[Serializable]
public class UpgradeOption
{
    public UpgradeType type;
    public string      title;
    public string      description;
    public int         currentLevel; // -1 = stat (no cap display)
    public int         maxLevel;     // -1 = no cap

    // Legend System additions
    public LegendSystemType legendSystem;
    public string           legendSubtitle;
    public bool             isEvolution;
    
    // Icon
    public Sprite           icon;

    public UpgradeOption(UpgradeType t, string title, string desc, int cur, int max, LegendSystemType sys = LegendSystemType.None, string sub = "", bool evo = false, Sprite ico = null)
    {
        this.type         = t;
        this.title        = title;
        this.description  = desc;
        this.currentLevel = cur;
        this.maxLevel     = max;
        
        this.legendSystem   = sys;
        this.legendSubtitle = sub;
        this.isEvolution    = evo;
        this.icon           = ico;
    }
}

/// <summary>
/// Singleton. Manages upgrade levels and generates random upgrade pools.
/// Applies upgrades to player components.
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get;  set; }

    // ---- Upgrade progress ----
    public int Skill1Level { get;  set; } = 0;  // 0=locked, 1-4=tiers
    public int Skill2Level { get;  set; } = 0;
    public int HealthUpgrades { get;  set; } = 0;
    public int DamageUpgrades { get;  set; } = 0;
    public int SpeedUpgrades  { get;  set; } = 0;
    public int XpDropChanceLevel { get;  set; } = 0;
    public int HealthPackDropChanceLevel { get;  set; } = 0;

    // ---- Legend Progress ----
    public class LegendProgress
    {
        public LegendSystemType systemType;
        public int weapon1Level = 0; // max 4
        public int weapon2Level = 0; // max 4
        public int evoLevel     = 0; // max 1
        public bool IsActive => weapon1Level > 0 || weapon2Level > 0 || evoLevel > 0;
        public bool IsW1Max => weapon1Level >= 4;
        public bool IsW2Max => weapon2Level >= 4;
        public bool CanEvolve => IsW1Max && IsW2Max && evoLevel == 0;
    }

    public Dictionary<LegendSystemType, LegendProgress> Legends = new Dictionary<LegendSystemType, LegendProgress>();

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }

        gameObject.AddComponent<TestModeUI>();

        Legends[LegendSystemType.CoLoa] = new LegendProgress { systemType = LegendSystemType.CoLoa };
        Legends[LegendSystemType.DongA] = new LegendProgress { systemType = LegendSystemType.DongA };
        Legends[LegendSystemType.SonTinh] = new LegendProgress { systemType = LegendSystemType.SonTinh };
        Legends[LegendSystemType.ThanhGiong] = new LegendProgress { systemType = LegendSystemType.ThanhGiong };
        Legends[LegendSystemType.LeLoi] = new LegendProgress { systemType = LegendSystemType.LeLoi };
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
        Sprite myIcon = Resources.Load<Sprite>("Icons/CoLoa_Weapon1");
        var pool = new List<UpgradeOption>
        {
            MakeHealthOption(),
            new UpgradeOption(UpgradeType.StatDamage, "⚔  Cường Lực",
                "Tăng sát thương thêm +20%.\nĐòn đánh của bạn sẽ mạnh mẽ hơn.",
                DamageUpgrades, -1,
                LegendSystemType.None, "", false,
                Resources.Load<Sprite>("Icons/StatDamage")),
            new UpgradeOption(UpgradeType.StatSpeed, "💨  Tốc Hành",
                "Tăng tốc độ di chuyển thêm +15%.\nDễ dàng né tránh kẻ thù.",
                SpeedUpgrades, -1,
                LegendSystemType.None, "", false,
                Resources.Load<Sprite>("Icons/StatSpeed")),
            new UpgradeOption(UpgradeType.StatXpChance, "💎  Vận May XP",
                "Tăng thêm +15% tỷ lệ nhận nhân đôi Ngọc XP khi tiêu diệt quái.",
                XpDropChanceLevel, -1,
                LegendSystemType.None, "", false,
                Resources.Load<Sprite>("Icons/StatXpChance")),
            new UpgradeOption(UpgradeType.StatHealthChance, "🧪  Dược Lực",
                "Tăng thêm +8% tỷ lệ rơi Bình Máu hồi phục từ quái vật.",
                HealthPackDropChanceLevel, -1,
                LegendSystemType.None, "", false,
                Resources.Load<Sprite>("Icons/StatHealthChance"))
        };

        // Skill 1 — only if not maxed
        if (Skill1Level < 4)
        {
            bool isUnlock = Skill1Level == 0;
            pool.Add(new UpgradeOption(
                UpgradeType.Skill1,
                isUnlock ? "🌩  MỞ KHÓA: Thiên Đòn Sa" : "⬆  Thiên Đòn Sa",
                GetSkill1Desc(Skill1Level),
                Skill1Level, 4,
                LegendSystemType.None, "", false,
                Resources.Load<Sprite>("Icons/Skill1")));
        }

        // Skill 2 — only if not maxed
        if (Skill2Level < 4)
        {
            bool isUnlock = Skill2Level == 0;
            pool.Add(new UpgradeOption(
                UpgradeType.Skill2,
                isUnlock ? "🔥  MỞ KHÓA: Hỏa Ảnh Bộ" : "⬆  Hỏa Ảnh Bộ",
                GetSkill2Desc(Skill2Level),
                Skill2Level, 4,
                LegendSystemType.None, "", false,
                Resources.Load<Sprite>("Icons/Skill2")));
        }

        // ---- Legends System Pool Generation ----
        int activeLegendsCount = 0;
        foreach (var kvp in Legends)
        {
            if (kvp.Value.IsActive) activeLegendsCount++;
        }

        foreach (var kvp in Legends)
        {
            LegendSystemType sys = kvp.Key;
            LegendProgress prog = kvp.Value;

            // If we hit 4 active legends, only allow upgrades for already active ones.
            if (activeLegendsCount >= 4 && !prog.IsActive)
            {
                continue; // Can't discover new legend
            }

            string subTitle = GetLegendSubtitle(sys);

            // Evolution Combo check (Highest priority if ready)
            if (prog.CanEvolve)
            {
                pool.Add(new UpgradeOption(
                    GetLegendEvoType(sys),
                    "⭐ TIẾN HÓA: " + GetLegendEvoName(sys),
                    GetLegendEvoDesc(sys),
                    0, 1, sys, subTitle, true,
                    Resources.Load<Sprite>($"Icons/{sys}_Evo")));
                continue; // Wait to evolve before offering more? Actually if evo is available, offer it.
            }

            if (prog.evoLevel > 0)
            {
                // Already evolved, base weapons are merged/gone
                continue;
            }

            // Weapon 1
            if (prog.weapon1Level < 4)
            {
                bool isUnl = prog.weapon1Level == 0;
                pool.Add(new UpgradeOption(
                    GetLegendW1Type(sys),
                    (isUnl ? "MỚI: " : "CẤP: ") + GetLegendW1Name(sys),
                    GetLegendW1Desc(sys, prog.weapon1Level),
                    prog.weapon1Level, 4, sys, subTitle, false,
                    Resources.Load<Sprite>($"Icons/{sys}_W1")));
            }

            // Weapon 2
            if (prog.weapon2Level < 4)
            {
                bool isUnl = prog.weapon2Level == 0;
                pool.Add(new UpgradeOption(
                    GetLegendW2Type(sys),
                    (isUnl ? "MỚI: " : "CẤP: ") + GetLegendW2Name(sys),
                    GetLegendW2Desc(sys, prog.weapon2Level),
                    prog.weapon2Level, 4, sys, subTitle, false,
                    Resources.Load<Sprite>($"Icons/{sys}_W2")));
            }
        }

        return pool;
    }

    private UpgradeOption MakeHealthOption()
        => new UpgradeOption(UpgradeType.StatHealth, "❤  Sinh Lực",
            "Tăng thêm +25 HP tối đa.\nSống sót lâu hơn trong trận chiến.",
            HealthUpgrades, -1,
            LegendSystemType.None, "", false,
            Resources.Load<Sprite>("Icons/StatHealth"));

    private string GetSkill1Desc(int level)
    {
        return level switch
        {
            0 => "Nhảy vọt lên không trung, sau đó dậm mạnh xuống mục tiêu\ngây sát thương diện rộng cực lớn, gây choáng\nvà đẩy lùi toàn bộ kẻ địch xung quanh.",
            1 => "Thiên Đòn Sa tăng +50% sát thương.\nMỗi cú dậm đập tan mọi hàng phòng ngự.",
            2 => "Bán kính vụ nổ tăng thêm +30%.\nĐè bẹp nhiều kẻ địch hơn trong một lần dậm.",
            3 => "Thời gian hồi chiêu giảm đi 5 giây.\nMưa thiên thạch trút xuống liên tiếp.",
            _ => "ĐÃ TỐI ĐA"
        };
    }

    private string GetSkill2Desc(int level)
    {
        return level switch
        {
            0 => "Lướt nhanh về phía trước xuyên qua kẻ thù,\nđể lại một vệt lửa thiêu đốt phía sau.\nKết thúc bằng một đòn xoay kiếm 360 độ cực mạnh.",
            1 => "Sát thương lướt tăng +40% và vệt lửa rộng hơn.\nThiêu rụi con đường cản bước bạn.",
            2 => "Sát thương của đòn xoay kết liễu tăng +50%.\nKhông chừa bất cứ sinh vật nào cản đường.",
            3 => "Thời gian hồi chiêu giảm 3 giây. Vệt lửa tồn tại lâu hơn.\nHỏa thiêu kẻ thù trong đau đớn.",
            _ => "ĐÃ TỐI ĐA"
        };
    }

    // ---- Legend String Helpers ----
    private string GetLegendSubtitle(LegendSystemType sys)
    {
        return sys switch {
            LegendSystemType.CoLoa => "Huyền Thoại Cổ Loa (An Dương Vương)",
            LegendSystemType.DongA => "Hào Khí Đông A (Trần Hưng Đạo)",
            LegendSystemType.SonTinh => "Đại Chiến Thiên Nhiên (Sơn Tinh)",
            LegendSystemType.ThanhGiong => "Phù Đổng Thiên Vương (Thánh Gióng)",
            LegendSystemType.LeLoi => "Gươm Thần (Lê Lợi)",
            _ => ""
        };
    }

    private UpgradeType GetLegendW1Type(LegendSystemType sys) {
        return sys switch {
            LegendSystemType.CoLoa => UpgradeType.Legend_CoLoa_W1,
            LegendSystemType.DongA => UpgradeType.Legend_DongA_W1,
            LegendSystemType.SonTinh => UpgradeType.Legend_SonTinh_W1,
            LegendSystemType.ThanhGiong => UpgradeType.Legend_ThanhGiong_W1,
            LegendSystemType.LeLoi => UpgradeType.Legend_LeLoi_W1,
            _ => UpgradeType.StatHealth
        };
    }

    private UpgradeType GetLegendW2Type(LegendSystemType sys) {
        return sys switch {
            LegendSystemType.CoLoa => UpgradeType.Legend_CoLoa_W2,
            LegendSystemType.DongA => UpgradeType.Legend_DongA_W2,
            LegendSystemType.SonTinh => UpgradeType.Legend_SonTinh_W2,
            LegendSystemType.ThanhGiong => UpgradeType.Legend_ThanhGiong_W2,
            LegendSystemType.LeLoi => UpgradeType.Legend_LeLoi_W2,
            _ => UpgradeType.StatHealth
        };
    }

    private UpgradeType GetLegendEvoType(LegendSystemType sys) {
        return sys switch {
            LegendSystemType.CoLoa => UpgradeType.Legend_CoLoa_Evo,
            LegendSystemType.DongA => UpgradeType.Legend_DongA_Evo,
            LegendSystemType.SonTinh => UpgradeType.Legend_SonTinh_Evo,
            LegendSystemType.ThanhGiong => UpgradeType.Legend_ThanhGiong_Evo,
            LegendSystemType.LeLoi => UpgradeType.Legend_LeLoi_Evo,
            _ => UpgradeType.StatHealth
        };
    }

    private string GetLegendW1Name(LegendSystemType sys) {
        return sys switch {
            LegendSystemType.CoLoa => "Nỏ Liên Châu",
            LegendSystemType.DongA => "Hịch Tướng Sĩ",
            LegendSystemType.SonTinh => "Núi Cao Vời Vợi",
            LegendSystemType.ThanhGiong => "Tre Ngà",
            LegendSystemType.LeLoi => "Gươm Thuận Thiên",
            _ => ""
        };
    }

    private string GetLegendW2Name(LegendSystemType sys) {
        return sys switch {
            LegendSystemType.CoLoa => "Mai Rùa Vàng",
            LegendSystemType.DongA => "Cọc Bạch Đằng",
            LegendSystemType.SonTinh => "Dâng Nước Biển",
            LegendSystemType.ThanhGiong => "Ngựa Sắt Phun Lửa",
            LegendSystemType.LeLoi => "Rùa Vàng Ngoạm Kiếm",
            _ => ""
        };
    }

    private string GetLegendEvoName(LegendSystemType sys) {
        return sys switch {
            LegendSystemType.CoLoa => "Nỏ Thần Siêu Cấp",
            LegendSystemType.DongA => "Thiên Trường Địa Nghĩa",
            LegendSystemType.SonTinh => "Đất Nước Hóa Thân",
            LegendSystemType.ThanhGiong => "Gióng Trở Về",
            LegendSystemType.LeLoi => "Gươm Thiêng Trả Hồ",
            _ => ""
        };
    }

    private string GetLegendW1Desc(LegendSystemType sys, int level) {
        if (level == 0) {
            return sys switch {
                LegendSystemType.CoLoa => "Bảo vật của An Dương Vương. Nỏ Thần Liên Châu bắn ra hàng loạt mũi tên không bao giờ cạn, tự động hướng về kẻ thù.",
                LegendSystemType.DongA => "Hào khí sát thát của quân dân nhà Trần. Những dải lụa mang lời Hịch Tướng Sĩ bay quanh bảo vệ bạn.",
                LegendSystemType.SonTinh => "Sức mạnh dời non lấp bể. Triệu hồi những tảng đá khổng lồ từ trên trời rơi xuống nghiền nát kẻ thù.",
                LegendSystemType.ThanhGiong => "Gióng nhổ những bụi tre ngà bên đường làm vũ khí. Tạo ra chướng ngại vật mọc lên từ lòng đất chặn đánh quân thù.",
                LegendSystemType.LeLoi => "Lưỡi gươm dưới đáy nước thiêng chớp sáng. Gươm Thuận Thiên phóng ra liên tục xuyên qua hàng ngũ địch.",
                _ => "Mở khóa vũ khí mới."
            };
        }
        return "Tăng thêm sức mạnh, sát thương và kích thước cho " + GetLegendW1Name(sys) + ".";
    }

    private string GetLegendW2Desc(LegendSystemType sys, int level) {
        if (level == 0) {
            return sys switch {
                LegendSystemType.CoLoa => "Móng vuốt của Thần Kim Quy hóa thành lớp khiên ánh sáng bất hoại xoay quanh cơ thể.",
                LegendSystemType.DongA => "Sức mạnh từ trận chiến trên sông lịch sử. Những cọc gỗ Bạch Đằng đâm xuyên mặt đất tiêu diệt kẻ thù dẫm phải.",
                LegendSystemType.SonTinh => "Cơn cuồng nộ của Thủy Tinh. Dâng nước biển tạo thành những đợt sóng thần quét qua mọi thứ xung quanh.",
                LegendSystemType.ThanhGiong => "Ngựa sắt phun lửa đỏ rực bầu trời. Để lại một vệt lửa thiêu rụi bất kỳ kẻ nào đuổi theo bạn.",
                LegendSystemType.LeLoi => "Rùa Vàng hiện lên từ hồ nước thiêng. Trở thành người bạn đồng hành bay quanh bảo vệ và tấn công địch.",
                _ => "Mở khóa vũ khí mới."
            };
        }
        return "Nâng cấp sức mạnh, phạm vi và hiệu ứng của " + GetLegendW2Name(sys) + ".";
    }

    private string GetLegendEvoDesc(LegendSystemType sys) {
        return sys switch {
            LegendSystemType.CoLoa => "Tiến hóa Nỏ Liên Châu và Mai Rùa Vàng.\nTruyền thuyết Cổ Loa tái hiện: Mũi tên nổ xuyên thấu phá nát đội hình, khiên vàng nổ đẩy lùi kẻ thù và hồi máu.",
            LegendSystemType.DongA => "Tiến hóa Hịch và Cọc Bạch Đằng.\nHào khí nghìn năm: Cọc gỗ hóa lỗ đen hút địch, lụa biến thành bão lửa thiêu rụi giặc ngoại xâm.",
            LegendSystemType.SonTinh => "Tiến hóa Núi và Nước.\nĐại chiến thiên nhiên: Núi đá vỡ tạo bùn lầy làm chậm, sóng thần cuồng nộ x2 sát thương lên kẻ thù sa lầy.",
            LegendSystemType.ThanhGiong => "Tiến hóa Tre Ngà và Ngựa Sắt.\nThánh Gióng trở về cõi trời: Tăng tốc độ chóng mặt, cưỡi ngựa sắt tạo cột lửa, tre ngà tự động truy đuổi địch.",
            LegendSystemType.LeLoi => "Tiến hóa Gươm và Rùa Vàng.\nThuận ý trời, hoàn Gươm thiêng: Thanh gươm khổng lồ trảm địch. Khi bạn gục ngã, Rùa Vàng hy sinh để hồi sinh bạn và xóa sổ toàn bản đồ.",
            _ => ""
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
            case UpgradeType.StatXpChance:     XpDropChanceLevel++; break;
            case UpgradeType.StatHealthChance: HealthPackDropChanceLevel++; break;
            case UpgradeType.Skill1:     Skill1Level++;    ApplySkill1(); break;
            case UpgradeType.Skill2:     Skill2Level++;    ApplySkill2(); break;
            
            // Cổ Loa
            case UpgradeType.Legend_CoLoa_W1: Legends[LegendSystemType.CoLoa].weapon1Level++; ApplyLegend(LegendSystemType.CoLoa); break;
            case UpgradeType.Legend_CoLoa_W2: Legends[LegendSystemType.CoLoa].weapon2Level++; ApplyLegend(LegendSystemType.CoLoa); break;
            case UpgradeType.Legend_CoLoa_Evo: Legends[LegendSystemType.CoLoa].evoLevel++; ApplyLegend(LegendSystemType.CoLoa); break;
            
            // Đông A
            case UpgradeType.Legend_DongA_W1: Legends[LegendSystemType.DongA].weapon1Level++; ApplyLegend(LegendSystemType.DongA); break;
            case UpgradeType.Legend_DongA_W2: Legends[LegendSystemType.DongA].weapon2Level++; ApplyLegend(LegendSystemType.DongA); break;
            case UpgradeType.Legend_DongA_Evo: Legends[LegendSystemType.DongA].evoLevel++; ApplyLegend(LegendSystemType.DongA); break;
            
            // Sơn Tinh
            case UpgradeType.Legend_SonTinh_W1: Legends[LegendSystemType.SonTinh].weapon1Level++; ApplyLegend(LegendSystemType.SonTinh); break;
            case UpgradeType.Legend_SonTinh_W2: Legends[LegendSystemType.SonTinh].weapon2Level++; ApplyLegend(LegendSystemType.SonTinh); break;
            case UpgradeType.Legend_SonTinh_Evo: Legends[LegendSystemType.SonTinh].evoLevel++; ApplyLegend(LegendSystemType.SonTinh); break;
            
            // Thánh Gióng
            case UpgradeType.Legend_ThanhGiong_W1: Legends[LegendSystemType.ThanhGiong].weapon1Level++; ApplyLegend(LegendSystemType.ThanhGiong); break;
            case UpgradeType.Legend_ThanhGiong_W2: Legends[LegendSystemType.ThanhGiong].weapon2Level++; ApplyLegend(LegendSystemType.ThanhGiong); break;
            case UpgradeType.Legend_ThanhGiong_Evo: Legends[LegendSystemType.ThanhGiong].evoLevel++; ApplyLegend(LegendSystemType.ThanhGiong); break;
            
            // Lê Lợi
            case UpgradeType.Legend_LeLoi_W1: Legends[LegendSystemType.LeLoi].weapon1Level++; ApplyLegend(LegendSystemType.LeLoi); break;
            case UpgradeType.Legend_LeLoi_W2: Legends[LegendSystemType.LeLoi].weapon2Level++; ApplyLegend(LegendSystemType.LeLoi); break;
            case UpgradeType.Legend_LeLoi_Evo: Legends[LegendSystemType.LeLoi].evoLevel++; ApplyLegend(LegendSystemType.LeLoi); break;
        }
    }

    private void ApplyLegend(LegendSystemType type)
    {
        var pc = FindObjectOfType<PlayerController>();
        if (pc == null) return;
        
        // This will be handled by the specialized scripts on the player later.
        var legendMgr = pc.GetComponent<LegendaryUpgradeSystem>();
        if (legendMgr != null)
        {
            legendMgr.UpdateLegendLevels(type, Legends[type].weapon1Level, Legends[type].weapon2Level, Legends[type].evoLevel);
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
