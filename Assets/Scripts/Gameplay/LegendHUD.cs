using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LegendHUD : MonoBehaviour
{
    private Dictionary<LegendSystemType, GameObject> _slots = new Dictionary<LegendSystemType, GameObject>();
    private Transform _container;

    private void Start()
    {
        BuildHUD();
    }

    private void BuildHUD()
    {
        GameObject canvasGO = new GameObject("LegendHUDCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        GameObject containerGO = new GameObject("LegendContainer");
        containerGO.transform.SetParent(canvasGO.transform, false);
        RectTransform rt = containerGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(20f, -80f); // Top Left below health bar
        
        HorizontalLayoutGroup layout = containerGO.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 15f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        
        _container = containerGO.transform;
    }

    private void Update()
    {
        if (UpgradeManager.Instance == null) return;
        
        foreach (var kvp in UpgradeManager.Instance.Legends)
        {
            LegendSystemType sys = kvp.Key;
            UpgradeManager.LegendProgress prog = kvp.Value;

            if (prog.IsActive && !_slots.ContainsKey(sys))
            {
                CreateSlot(sys, prog);
            }
            else if (_slots.ContainsKey(sys))
            {
                UpdateSlot(sys, prog);
            }
        }
    }

    private void CreateSlot(LegendSystemType sys, UpgradeManager.LegendProgress prog)
    {
        GameObject slotGO = new GameObject("LegendSlot_" + sys);
        slotGO.transform.SetParent(_container, false);
        
        RectTransform rt = slotGO.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(60f, 60f);

        Image bg = slotGO.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.12f, 0.9f);
        
        GameObject outline = new GameObject("Outline");
        outline.transform.SetParent(slotGO.transform, false);
        RectTransform outRt = outline.AddComponent<RectTransform>();
        outRt.anchorMin = Vector2.zero; outRt.anchorMax = Vector2.one;
        outRt.sizeDelta = new Vector2(4f, 4f);
        Image outImg = outline.AddComponent<Image>();
        outImg.color = new Color(0.8f, 0.6f, 0.2f, 1f);
        
        // Add text for name
        GameObject txtGO = new GameObject("Name");
        txtGO.transform.SetParent(slotGO.transform, false);
        RectTransform txtRt = txtGO.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;
        Text txt = txtGO.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 16;
        txt.fontStyle = FontStyle.Bold;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        
        // Short name
        string shortName = "";
        switch(sys) {
            case LegendSystemType.CoLoa: shortName = "C.Loa"; break;
            case LegendSystemType.DongA: shortName = "Đ.A"; break;
            case LegendSystemType.SonTinh: shortName = "S.Tinh"; break;
            case LegendSystemType.ThanhGiong: shortName = "Gióng"; break;
            case LegendSystemType.LeLoi: shortName = "LêLợi"; break;
        }
        txt.text = shortName;
        
        _slots[sys] = slotGO;
    }

    private void UpdateSlot(LegendSystemType sys, UpgradeManager.LegendProgress prog)
    {
        GameObject slotGO = _slots[sys];
        Transform outline = slotGO.transform.Find("Outline");
        if (outline != null)
        {
            Image img = outline.GetComponent<Image>();
            if (prog.evoLevel > 0)
            {
                img.color = new Color(0.9f, 0.2f, 0.9f, 1f); // Evo purple
            }
        }
    }
}
