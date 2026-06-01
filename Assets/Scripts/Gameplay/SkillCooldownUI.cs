using UnityEngine;
using UnityEngine.UI;

public class SkillCooldownUI : MonoBehaviour
{
    // ---- Cấu hình giao diện ----
    private static readonly Color SlotBgColor = new Color(0.05f, 0.05f, 0.08f, 0.85f);
    private static readonly Color SlotBorderColor = new Color(0.35f, 0.35f, 0.45f, 1.00f);
    private static readonly Color ReadyColor = new Color(0.20f, 0.80f, 0.30f, 1.00f);
    private static readonly Color RadialFillColor = new Color(0.00f, 0.00f, 0.00f, 0.65f);
    private static readonly Color TextColor = new Color(0.9f, 0.9f, 0.95f, 1f);

    private Canvas _canvas;

    // Skill 1 UI Elements
    private Image _maskS1;
    private Text _txtCdS1;
    private Text _txtKeyS1;
    private SkillSkyPlunge _skill1;

    // Skill 2 UI Elements
    private Image _maskS2;
    private Text _txtCdS2;
    private Text _txtKeyS2;
    private SkillFlameDash _skill2;

    private void Start()
    {
        // Tìm tham chiếu tới 2 skill trên Player
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _skill1 = player.GetComponent<SkillSkyPlunge>();
            _skill2 = player.GetComponent<SkillFlameDash>();
        }

        BuildUI();
    }

    private void Update()
    {
        UpdateSkillSlot(_skill1, _maskS1, _txtCdS1, _txtKeyS1, "1");
        UpdateSkillSlot(_skill2, _maskS2, _txtCdS2, _txtKeyS2, "2");
    }

    private void UpdateSkillSlot(MonoBehaviour skill, Image mask, Text txtCd, Text txtKey, string keyBind)
    {
        if (skill == null || mask == null) return;

        // Ép kiểu dynamic để đọc dữ liệu từ cả 2 script skill khác nhau
        int level = 0;
        bool isOnCooldown = false;
        float cdRemaining = 0f;
        float cdMax = 1f;

        if (skill is SkillSkyPlunge s1)
        {
            level = s1.Level;
            isOnCooldown = s1.IsOnCooldown;
            cdRemaining = s1.CooldownRemaining;
            cdMax = s1.CooldownMax;
        }
        else if (skill is SkillFlameDash s2)
        {
            level = s2.Level;
            isOnCooldown = s2.IsOnCooldown;
            cdRemaining = s2.CooldownRemaining;
            cdMax = s2.CooldownMax;
        }

        // Nếu chưa học skill -> Ẩn ô skill đó đi
        if (level <= 0)
        {
            mask.transform.parent.gameObject.SetActive(false);
            return;
        }

        // Nếu đã học -> Hiện ô skill
        mask.transform.parent.gameObject.SetActive(true);

        if (isOnCooldown)
        {
            mask.fillAmount = cdRemaining / (cdMax > 0 ? cdMax : 1f);
            txtCd.text = cdRemaining > 1f ? cdRemaining.ToString("F0") : cdRemaining.ToString("F1");
            txtKey.color = Color.gray;
        }
        else
        {
            mask.fillAmount = 0f;
            txtCd.text = ""; // Sẵn sàng thì không hiện số
            txtKey.color = ReadyColor;
        }
    }

    // ---- Khởi tạo cấu trúc UI hoàn toàn bằng Code ----
    private void BuildUI()
    {
        // 1. Tạo Canvas riêng cho Hotbar
        GameObject cgo = new GameObject("SkillHotbarCanvas");
        _canvas = cgo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 50;

        CanvasScaler cs = cgo.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;

        // 2. Tạo vùng chứa (Container) ở chính giữa cạnh dưới màn hình
        GameObject container = new GameObject("HotbarContainer");
        container.transform.SetParent(_canvas.transform, false);
        RectTransform rectContainer = container.AddComponent<RectTransform>();
        rectContainer.anchorMin = new Vector2(0.5f, 0f);
        rectContainer.anchorMax = new Vector2(0.5f, 0f);
        rectContainer.pivot = new Vector2(0.5f, 0f);
        rectContainer.anchoredPosition = new Vector2(0f, 40f); // Cách đáy màn hình 40px
        rectContainer.sizeDelta = new Vector2(300f, 100f);

        // 3. Tạo ô Skill 1 (nằm bên trái) và Ô Skill 2 (nằm bên phải)
        CreateSlot(container.transform, new Vector2(-60f, 0f), "S1", "Sky P.", "[1]", out _maskS1, out _txtCdS1, out _txtKeyS1);
        CreateSlot(container.transform, new Vector2(60f, 0f), "S2", "Dash", "[2]", out _maskS2, out _txtCdS2, out _txtKeyS2);
    }

    private void CreateSlot(Transform parent, Vector2 pos, string id, string skillName, string keyBind,
                            out Image maskImage, out Text txtCd, out Text txtKey)
    {
        // Viền ô vuông bên ngoài
        GameObject borderGO = new GameObject($"SlotBorder_{id}");
        borderGO.transform.SetParent(parent, false);
        RectTransform borderRt = borderGO.AddComponent<RectTransform>();
        borderRt.sizeDelta = new Vector2(90f, 90f);
        borderRt.anchoredPosition = pos;
        borderGO.AddComponent<Image>().color = SlotBorderColor;

        // Nền ô vuông bên trong
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(borderGO.transform, false);
        RectTransform bgRt = bgGO.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = new Vector2(3f, 3f); bgRt.offsetMax = new Vector2(-3f, -3f);
        bgGO.AddComponent<Image>().color = SlotBgColor;

        // Chữ hiển thị tên viết tắt của Skill
        GameObject nameGO = new GameObject("NameText");
        nameGO.transform.SetParent(bgGO.transform, false);
        Text tName = nameGO.AddComponent<Text>();
        tName.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tName.text = skillName;
        tName.fontSize = 20;
        tName.alignment = TextAnchor.MiddleCenter;
        tName.color = TextColor;
        RectTransform nameRt = nameGO.GetComponent<RectTransform>();
        nameRt.anchorMin = Vector2.zero; nameRt.anchorMax = Vector2.one;
        nameRt.sizeDelta = Vector2.zero;

        // Lớp Phủ Cooldown (Radial Fill Mask)
        GameObject maskGO = new GameObject("CooldownMask");
        maskGO.transform.SetParent(bgGO.transform, false);
        RectTransform maskRt = maskGO.AddComponent<RectTransform>();
        maskRt.anchorMin = Vector2.zero; maskRt.anchorMax = Vector2.one;
        maskRt.offsetMin = maskRt.offsetMax = Vector2.zero;

        maskImage = maskGO.AddComponent<Image>();
        maskImage.color = RadialFillColor;
        maskImage.type = Image.Type.Filled;
        maskImage.fillMethod = Image.FillMethod.Radial360;
        maskImage.fillOrigin = (int)Image.Origin360.Top; // Quay từ đỉnh vòng tròn xuống
        maskImage.fillClockwise = false; // Quay ngược chiều kim đồng hồ

        // Chữ số đếm ngược thời gian hồi (Giây)
        GameObject cdTextGO = new GameObject("CDText");
        cdTextGO.transform.SetParent(maskGO.transform, false);
        txtCd = cdTextGO.AddComponent<Text>();
        txtCd.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txtCd.fontSize = 28;
        txtCd.fontStyle = FontStyle.Bold;
        txtCd.alignment = TextAnchor.MiddleCenter;
        txtCd.color = new Color(1f, 0.35f, 0.1f);
        RectTransform cdTextRt = cdTextGO.GetComponent<RectTransform>();
        cdTextRt.anchorMin = Vector2.zero; cdTextRt.anchorMax = Vector2.one;
        cdTextRt.sizeDelta = Vector2.zero;

        // Chữ hiển thị phím tắt [1], [2] ở góc dưới ô skill
        GameObject keyGO = new GameObject("KeyText");
        keyGO.transform.SetParent(bgGO.transform, false);
        txtKey = keyGO.AddComponent<Text>();
        txtKey.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txtKey.text = keyBind;
        txtKey.fontSize = 18;
        txtKey.fontStyle = FontStyle.Bold;
        txtKey.alignment = TextAnchor.LowerRight;
        RectTransform keyRt = keyGO.GetComponent<RectTransform>();
        keyRt.anchorMin = Vector2.zero; keyRt.anchorMax = Vector2.one;
        keyRt.offsetMin = new Vector2(0, 4f); keyRt.offsetMax = new Vector2(-6f, 0);
    }
}