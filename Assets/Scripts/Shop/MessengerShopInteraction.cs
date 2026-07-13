using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MessengerShopInteraction : MonoBehaviour
{
    private int pendingMap;
    private GameObject dialogueCanvas;

    private void Start()
    {
        pendingMap = PlayerPrefs.GetInt("PendingMapUnlock", 0);
        
        if (pendingMap <= 0)
        {
            // Disable mesh renderers and colliders if no map is pending
            SetVisualsActive(false);
        }
        else
        {
            SetVisualsActive(true);
        }
    }

    private void SetVisualsActive(bool active)
    {
        foreach (Renderer rend in GetComponentsInChildren<Renderer>(true))
        {
            rend.enabled = active;
        }
        foreach (Collider col in GetComponentsInChildren<Collider>(true))
        {
            col.enabled = active;
        }
    }

    private void OnMouseDown()
    {
        if (pendingMap > 0 && dialogueCanvas == null)
        {
            ShowDialogue();
        }
    }

    private void ShowDialogue()
    {
        string dialogueText = "";
        if (pendingMap == 1)
        {
            dialogueText = "Đây là bản đồ dẫn đến khu rừng...";
        }
        else if (pendingMap == 2)
        {
            dialogueText = "Đây là rừng tre, con trùm sẽ ở đó nên hãy cẩn thận...";
        }
        else
        {
            dialogueText = "Cảm ơn bạn đã tìm thấy bản đồ bí ẩn này...";
        }

        dialogueCanvas = new GameObject("MessengerDialogueCanvas");
        Canvas canvas = dialogueCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        
        dialogueCanvas.AddComponent<GraphicRaycaster>();

        // Dark overlay
        GameObject panelGO = new GameObject("Overlay");
        panelGO.transform.SetParent(dialogueCanvas.transform, false);
        RectTransform panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.sizeDelta = Vector2.zero;
        Image panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.6f);

        // Dialogue Box
        GameObject boxGO = new GameObject("DialogueBox");
        boxGO.transform.SetParent(dialogueCanvas.transform, false);
        RectTransform boxRT = boxGO.AddComponent<RectTransform>();
        boxRT.anchorMin = new Vector2(0.5f, 0f);
        boxRT.anchorMax = new Vector2(0.5f, 0f);
        boxRT.pivot = new Vector2(0.5f, 0f);
        boxRT.anchoredPosition = new Vector2(0f, 50f);
        boxRT.sizeDelta = new Vector2(900f, 250f);
        Image boxImg = boxGO.AddComponent<Image>();
        boxImg.color = new Color(0.05f, 0.05f, 0.07f, 0.95f);

        // Outline
        GameObject outlineGO = new GameObject("Outline");
        outlineGO.transform.SetParent(boxGO.transform, false);
        RectTransform outlineRT = outlineGO.AddComponent<RectTransform>();
        outlineRT.anchorMin = Vector2.zero;
        outlineRT.anchorMax = Vector2.one;
        outlineRT.sizeDelta = new Vector2(-4f, -4f);
        Image outlineImg = outlineGO.AddComponent<Image>();
        outlineImg.color = new Color(0.8f, 0.7f, 0.3f, 0.5f);

        // Name text
        GameObject nameGO = new GameObject("NameText");
        nameGO.transform.SetParent(boxGO.transform, false);
        RectTransform nameRT = nameGO.AddComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0f, 1f);
        nameRT.anchorMax = new Vector2(1f, 1f);
        nameRT.pivot = new Vector2(0.5f, 1f);
        nameRT.anchoredPosition = new Vector2(0f, -20f);
        nameRT.sizeDelta = new Vector2(-40f, 40f);
        TextMeshProUGUI nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
        nameTMP.text = "SỨ GIẢ";
        nameTMP.fontSize = 28;
        nameTMP.color = new Color(1f, 0.85f, 0.3f);
        nameTMP.fontStyle = FontStyles.Bold;

        // Body text
        GameObject bodyGO = new GameObject("BodyText");
        bodyGO.transform.SetParent(boxGO.transform, false);
        RectTransform bodyRT = bodyGO.AddComponent<RectTransform>();
        bodyRT.anchorMin = new Vector2(0f, 0f);
        bodyRT.anchorMax = new Vector2(1f, 1f);
        bodyRT.pivot = new Vector2(0.5f, 0.5f);
        bodyRT.offsetMin = new Vector2(30f, 70f);
        bodyRT.offsetMax = new Vector2(-30f, -60f);
        TextMeshProUGUI bodyTMP = bodyGO.AddComponent<TextMeshProUGUI>();
        bodyTMP.text = dialogueText;
        bodyTMP.fontSize = 24;
        bodyTMP.color = Color.white;
        bodyTMP.enableWordWrapping = true;

        // Continue Button
        GameObject btnGO = new GameObject("ContinueButton");
        btnGO.transform.SetParent(boxGO.transform, false);
        RectTransform btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(1f, 0f);
        btnRT.anchorMax = new Vector2(1f, 0f);
        btnRT.pivot = new Vector2(1f, 0f);
        btnRT.anchoredPosition = new Vector2(-30f, 20f);
        btnRT.sizeDelta = new Vector2(200f, 50f);
        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        Button btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(OnDialogueComplete);

        GameObject btnTextGO = new GameObject("BtnText");
        btnTextGO.transform.SetParent(btnGO.transform, false);
        RectTransform btnTextRT = btnTextGO.AddComponent<RectTransform>();
        btnTextRT.anchorMin = Vector2.zero;
        btnTextRT.anchorMax = Vector2.one;
        btnTextRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI btnTMP = btnTextGO.AddComponent<TextMeshProUGUI>();
        btnTMP.text = "TIẾP TỤC";
        btnTMP.fontSize = 20;
        btnTMP.color = Color.white;
        btnTMP.alignment = TextAlignmentOptions.Center;
        btnTMP.fontStyle = FontStyles.Bold;
    }

    private void OnDialogueComplete()
    {
        if (dialogueCanvas != null)
        {
            Destroy(dialogueCanvas);
        }

        // Unlock map
        PlayerPrefs.SetInt("UnlockedMap_" + pendingMap, 1);
        PlayerPrefs.SetInt("PendingMapUnlock", 0);
        PlayerPrefs.Save();

        pendingMap = 0;
        SetVisualsActive(false);

        // Open Map Selection
        CameraManager camManager = FindFirstObjectByType<CameraManager>();
        if (camManager != null)
        {
            camManager.OpenMapSelection();
        }
    }
}
