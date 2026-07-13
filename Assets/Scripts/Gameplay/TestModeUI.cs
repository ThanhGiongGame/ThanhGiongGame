using UnityEngine;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TestModeUI : MonoBehaviour
{
    private bool isVisible = false;
    private Rect windowRect = new Rect(20, 20, 350, 450);
    private Vector2 scrollPos;

    void Update()
    {
        if (WasCheatMenuPressed())
        {
            isVisible = !isVisible;
        }
    }

    void OnGUI()
    {
        if (!isVisible) return;

        windowRect = GUI.Window(999, windowRect, DrawTestWindow, "Test Mode / Cheats");
    }

    private void DrawTestWindow(int id)
    {
        if (UpgradeManager.Instance == null)
        {
            GUILayout.Label("UpgradeManager not found!");
            return;
        }

        scrollPos = GUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("Press F2 to toggle this menu.");
        GUILayout.Space(10);

        if (GUILayout.Button("Reset Player Health"))
        {
            var ph = FindObjectOfType<PlayerHealth>();
            if (ph != null) ph.Heal(9999f);
        }
        
        if (GUILayout.Button("Force Level Up (Show UI)"))
        {
            var xp = FindObjectOfType<XPManager>();
            if (xp != null) xp.AddXP(9999f);
        }

        GUILayout.Space(15);
        GUILayout.Label("--- Legend Skills ---", GUILayout.ExpandWidth(true));
        
        foreach (var kvp in UpgradeManager.Instance.Legends)
        {
            LegendSystemType sys = kvp.Key;
            var prog = kvp.Value;

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{sys}", GUILayout.Width(100));
            
            if (GUILayout.Button($"W1 (+1)", GUILayout.Width(60)))
            {
                prog.weapon1Level = Mathf.Min(4, prog.weapon1Level + 1);
                ApplyLegend(sys, prog);
            }
            if (GUILayout.Button($"W2 (+1)", GUILayout.Width(60)))
            {
                prog.weapon2Level = Mathf.Min(4, prog.weapon2Level + 1);
                ApplyLegend(sys, prog);
            }
            if (GUILayout.Button($"EVO", GUILayout.Width(60)))
            {
                prog.evoLevel = 1;
                ApplyLegend(sys, prog);
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(15);
        GUILayout.Label("--- Standard Skills ---");
        if (GUILayout.Button("Add Skill 1 (Thiên Đòn Sa)"))
        {
            UpgradeManager.Instance.Skill1Level = Mathf.Min(4, UpgradeManager.Instance.Skill1Level + 1);
            var pc = FindObjectOfType<PlayerController>();
            if (pc != null)
            {
                var skill = pc.GetComponent<SkillSkyPlunge>();
                if (skill != null) skill.SetLevel(UpgradeManager.Instance.Skill1Level);
            }
        }
        if (GUILayout.Button("Add Skill 2 (Hỏa Ảnh Bộ)"))
        {
            UpgradeManager.Instance.Skill2Level = Mathf.Min(4, UpgradeManager.Instance.Skill2Level + 1);
            var pc = FindObjectOfType<PlayerController>();
            if (pc != null)
            {
                var skill = pc.GetComponent<SkillFlameDash>();
                if (skill != null) skill.SetLevel(UpgradeManager.Instance.Skill2Level);
            }
        }

        GUILayout.Space(15);
        GUILayout.Label("--- Map 3 Final Boss Test ---");
        
        if (GUILayout.Button("Trigger Cinematic Transformation"))
        {
            var ph = FindObjectOfType<PlayerHealth>();
            if (ph != null)
            {
                Boss.IsSpecialFinalScene = true;
                ph.TakeDamage(99999f, Vector3.zero, 0f); // Triggers the <1 HP cinematic
            }
        }
        
        if (GUILayout.Button("Trigger Ascension Ending"))
        {
            var pc = FindObjectOfType<PlayerController>();
            if (pc != null) pc.AscendToSky();
        }

        GUILayout.EndScrollView();
        GUI.DragWindow();
    }

    private void ApplyLegend(LegendSystemType sys, UpgradeManager.LegendProgress prog)
    {
        var pc = FindObjectOfType<PlayerController>();
        if (pc == null) return;
        
        var legendMgr = pc.GetComponent<LegendaryUpgradeSystem>();
        if (legendMgr == null) legendMgr = pc.gameObject.AddComponent<LegendaryUpgradeSystem>();
        
        legendMgr.UpdateLegendLevels(sys, prog.weapon1Level, prog.weapon2Level, prog.evoLevel);
    }

    private static bool WasCheatMenuPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
        {
            return true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.F2);
#else
        return false;
#endif
    }
}
