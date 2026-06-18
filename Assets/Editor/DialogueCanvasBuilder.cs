// Editor-only builder. Xóa sau khi dùng xong.
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class DialogueCanvasBuilder
{
    [MenuItem("ThanhGiong/Build Dialogue Canvas")]
    public static void Build()
    {
        // ── 1. Root Canvas ──────────────────────────────────────────────
        GameObject canvasGO = new GameObject("DialogueCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── 2. DialoguePanel ────────────────────────────────────────────
        GameObject panelGO = new GameObject("DialoguePanel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        Image panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.72f);
        SetRect(panelGO, 0f, 0f, 1f, 0f, 0.5f, 0f, 0f, 0f, 0f, 200f);

        // ── 3. Gold line ────────────────────────────────────────────────
        GameObject lineGO = new GameObject("GoldLine");
        lineGO.transform.SetParent(panelGO.transform, false);
        lineGO.AddComponent<Image>().color = new Color(1f, 0.78f, 0.1f, 0.9f);
        SetRect(lineGO, 0.05f, 1f, 0.95f, 1f, 0.5f, 1f, 0f, 0f, 0f, 1.5f);

        // ── 4. Character Name ────────────────────────────────────────────
        GameObject nameGO = new GameObject("CharacterNameText");
        nameGO.transform.SetParent(panelGO.transform, false);
        TextMeshProUGUI nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
        nameTMP.text = "Thánh Gióng";
        nameTMP.fontSize = 26f;
        nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.color = new Color(1f, 0.82f, 0.18f, 1f);
        nameTMP.alignment = TextAlignmentOptions.Center;
        SetRect(nameGO, 0.1f, 1f, 0.9f, 1f, 0.5f, 1f, 0f, -18f, 0f, 40f);

        // ── 5. Body Text ─────────────────────────────────────────────────
        GameObject bodyGO = new GameObject("DialogueBodyText");
        bodyGO.transform.SetParent(panelGO.transform, false);
        TextMeshProUGUI bodyTMP = bodyGO.AddComponent<TextMeshProUGUI>();
        bodyTMP.text = "Ta là Thánh Gióng, đứa con của đất Phù Đổng...";
        bodyTMP.fontSize = 22f;
        bodyTMP.color = new Color(0.96f, 0.96f, 0.92f, 1f);
        bodyTMP.alignment = TextAlignmentOptions.Center;
        bodyTMP.lineSpacing = 6f;
        SetRect(bodyGO, 0.08f, 0.15f, 0.92f, 0.85f, 0.5f, 0.5f, 0f, 0f, 0f, 0f);

        // ── 6. Continue Indicator ◆ ──────────────────────────────────────
        GameObject indGO = new GameObject("ContinueIndicator");
        indGO.transform.SetParent(panelGO.transform, false);
        TextMeshProUGUI indTMP = indGO.AddComponent<TextMeshProUGUI>();
        indTMP.text = "◆";
        indTMP.fontSize = 20f;
        indTMP.color = new Color(1f, 0.82f, 0.18f, 1f);
        indTMP.alignment = TextAlignmentOptions.Center;
        SetRect(indGO, 0.5f, 0f, 0.5f, 0f, 0.5f, 0f, 0f, 10f, 40f, 30f);

        // ── 7. Gắn DialogueUI component ──────────────────────────────────
        DialogueUI ui = canvasGO.AddComponent<DialogueUI>();

        // Dùng SerializedObject để assign private serialized fields
        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("dialoguePanel").objectReferenceValue     = panelGO;
        so.FindProperty("characterNameText").objectReferenceValue = nameTMP;
        so.FindProperty("dialogueBodyText").objectReferenceValue  = bodyTMP;
        so.FindProperty("continueIndicator").objectReferenceValue = indGO;
        so.ApplyModifiedProperties();

        // Mark scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Selection.activeGameObject = canvasGO;
        Debug.Log("[DialogueCanvasBuilder] DialogueCanvas đã được tạo và assign xong!");
    }

    // ancMin(x,y), ancMax(x,y), pivot(x,y), pos(x,y), size(x,y)
    static void SetRect(GameObject go,
        float ancMinX, float ancMinY, float ancMaxX, float ancMaxY,
        float pivX, float pivY, float posX, float posY, float szX, float szY)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(ancMinX, ancMinY);
        rt.anchorMax = new Vector2(ancMaxX, ancMaxY);
        rt.pivot     = new Vector2(pivX, pivY);
        rt.anchoredPosition = new Vector2(posX, posY);
        rt.sizeDelta = new Vector2(szX, szY);
    }
}
#endif
