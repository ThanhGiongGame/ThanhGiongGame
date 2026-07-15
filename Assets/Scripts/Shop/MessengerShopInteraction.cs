using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MessengerShopInteraction : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource dialogueAudioSource;
    public AudioClip voiceMap1;
    public AudioClip voiceMap2;
    public AudioClip voiceDefault;

    private int pendingMap;
    private GameObject dialogueCanvas;
    private Coroutine typewriterCoroutine;
    private TextMeshProUGUI bodyTextTMP;
    private GameObject continueIndicator;
    private bool typewriterDone;
    private string fullDialogueText;

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
            ShowDialogue(); // Automatically trigger dialogue
        }
    }

    private void Update()
    {
        // Dev cheats to test SuGia dialogue
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.f9Key.wasPressedThisFrame)
            {
                PlayerPrefs.SetInt("PendingMapUnlock", 1);
                PlayerPrefs.Save();
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
            if (UnityEngine.InputSystem.Keyboard.current.f10Key.wasPressedThisFrame)
            {
                PlayerPrefs.SetInt("PendingMapUnlock", 2);
                PlayerPrefs.Save();
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
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

    private void ShowDialogue()
    {
        CameraManager camManager = FindFirstObjectByType<CameraManager>();
        if (camManager != null)
        {
            camManager.ShowSuGiaCamera();
        }

        string dialogueText = "";
        if (pendingMap == 1)
        {
            dialogueText = "Đây là bản đồ dẫn đến khu rừng... Hãy nhanh chóng đến đó và dọn dẹp bọn chúng đi";
        }
        else if (pendingMap == 2)
        {
            dialogueText = "Đây là bản đồ đến rừng tre, tên trùm sẽ ở đó nên hãy cẩn thận...";
        }
        else
        {
            dialogueText = "Cảm ơn bạn đã tìm thấy bản đồ bí ẩn này...";
        }

        dialogueCanvas = new GameObject("MessengerDialogueCanvas");
        Canvas canvas = dialogueCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        
        CanvasScaler scaler = dialogueCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        dialogueCanvas.AddComponent<GraphicRaycaster>();

        // Invisible full-screen button to capture clicks (replaces ContinueButton)
        GameObject clickCatcher = new GameObject("ClickCatcher");
        clickCatcher.transform.SetParent(dialogueCanvas.transform, false);
        RectTransform clickRT = clickCatcher.AddComponent<RectTransform>();
        clickRT.anchorMin = Vector2.zero;
        clickRT.anchorMax = Vector2.one;
        clickRT.sizeDelta = Vector2.zero;
        Image clickImg = clickCatcher.AddComponent<Image>();
        clickImg.color = Color.clear; // Invisible
        Button clickBtn = clickCatcher.AddComponent<Button>();
        clickBtn.onClick.AddListener(OnContinueClicked);

        // Dialogue Panel (bottom screen, full width)
        GameObject panelGO = new GameObject("DialoguePanel");
        panelGO.transform.SetParent(dialogueCanvas.transform, false);
        RectTransform panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0f, 0f);
        panelRT.anchorMax = new Vector2(1f, 0f);
        panelRT.pivot = new Vector2(0.5f, 0f);
        panelRT.anchoredPosition = Vector2.zero;
        panelRT.sizeDelta = new Vector2(0f, 210f); // Height 210
        Image panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0.03f, 0.02f, 0.01f, 0.88f); // Dark background

        // Gold Line on top
        GameObject lineGO = new GameObject("GoldLine");
        lineGO.transform.SetParent(panelGO.transform, false);
        RectTransform lineRT = lineGO.AddComponent<RectTransform>();
        lineRT.anchorMin = new Vector2(0.04f, 1f);
        lineRT.anchorMax = new Vector2(0.96f, 1f);
        lineRT.pivot = new Vector2(0.5f, 1f);
        lineRT.anchoredPosition = Vector2.zero;
        lineRT.sizeDelta = new Vector2(0f, 1.5f);
        Image lineImg = lineGO.AddComponent<Image>();
        lineImg.color = new Color(1f, 0.78f, 0.1f, 0.95f);

        // Speaker Name
        GameObject nameGO = new GameObject("SpeakerText");
        nameGO.transform.SetParent(panelGO.transform, false);
        RectTransform nameRT = nameGO.AddComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0.5f, 1f);
        nameRT.anchorMax = new Vector2(0.5f, 1f);
        nameRT.pivot = new Vector2(0.5f, 1f);
        nameRT.anchoredPosition = new Vector2(0f, -14f);
        nameRT.sizeDelta = new Vector2(900f, 40f);
        TextMeshProUGUI nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
        nameTMP.text = "SỨ GIẢ";
        nameTMP.fontSize = 26;
        nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.color = new Color(1f, 0.82f, 0.18f); // Gold
        nameTMP.alignment = TextAlignmentOptions.Center;

        // Body Text
        GameObject bodyGO = new GameObject("BodyText");
        bodyGO.transform.SetParent(panelGO.transform, false);
        RectTransform bodyRT = bodyGO.AddComponent<RectTransform>();
        bodyRT.anchorMin = new Vector2(0.5f, 0.5f);
        bodyRT.anchorMax = new Vector2(0.5f, 0.5f);
        bodyRT.pivot = new Vector2(0.5f, 0.5f);
        bodyRT.anchoredPosition = new Vector2(0f, 18f);
        bodyRT.sizeDelta = new Vector2(1400f, 110f);
        TextMeshProUGUI bodyTMP = bodyGO.AddComponent<TextMeshProUGUI>();
        bodyTMP.text = "";
        bodyTMP.fontSize = 26;
        bodyTMP.color = new Color(0.96f, 0.93f, 0.85f); // Off-white
        bodyTMP.alignment = TextAlignmentOptions.Center;
        bodyTMP.enableWordWrapping = true;
        bodyTextTMP = bodyTMP;

        // Continue Indicator
        continueIndicator = new GameObject("ContinueIndicator");
        continueIndicator.transform.SetParent(panelGO.transform, false);
        RectTransform indRT = continueIndicator.AddComponent<RectTransform>();
        indRT.anchorMin = new Vector2(0.5f, 0f);
        indRT.anchorMax = new Vector2(0.5f, 0f);
        indRT.pivot = new Vector2(0.5f, 0f);
        indRT.anchoredPosition = new Vector2(0f, 12f);
        indRT.sizeDelta = new Vector2(40f, 32f);
        TextMeshProUGUI indTMP = continueIndicator.AddComponent<TextMeshProUGUI>();
        indTMP.text = "...";
        indTMP.fontSize = 20f;
        indTMP.color = new Color(1f, 0.82f, 0.18f, 1f);
        indTMP.alignment = TextAlignmentOptions.Center;
        continueIndicator.SetActive(false);

        // Play audio
        AudioClip clipToPlay = voiceDefault;
        if (pendingMap == 1) clipToPlay = voiceMap1;
        else if (pendingMap == 2) clipToPlay = voiceMap2;

        if (dialogueAudioSource != null && clipToPlay != null)
        {
            dialogueAudioSource.Stop();
            dialogueAudioSource.clip = clipToPlay;
            dialogueAudioSource.Play();
        }

        fullDialogueText = dialogueText;
        typewriterDone = false;
        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        typewriterCoroutine = StartCoroutine(TypewriterEffect(dialogueText));
    }

    private System.Collections.IEnumerator TypewriterEffect(string text)
    {
        if (bodyTextTMP == null) yield break;
        bodyTextTMP.text = string.Empty;
        for (int i = 0; i < text.Length; i++)
        {
            bodyTextTMP.text += text[i];
            yield return new WaitForSecondsRealtime(0.038f);
        }
        typewriterDone = true;
        if (continueIndicator != null)
        {
            continueIndicator.SetActive(true);
            StartCoroutine(BlinkIndicator());
        }
    }

    private System.Collections.IEnumerator BlinkIndicator()
    {
        while (continueIndicator != null && continueIndicator.activeSelf)
        {
            continueIndicator.SetActive(false);
            yield return new WaitForSecondsRealtime(0.38f);
            if (continueIndicator != null) continueIndicator.SetActive(true);
            yield return new WaitForSecondsRealtime(0.38f);
        }
    }

    private void OnContinueClicked()
    {
        if (!typewriterDone)
        {
            // Bỏ qua hiệu ứng gõ phím
            if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
            if (dialogueAudioSource != null) dialogueAudioSource.Stop();
            if (bodyTextTMP != null) bodyTextTMP.text = fullDialogueText;
            typewriterDone = true;
            if (continueIndicator != null)
            {
                continueIndicator.SetActive(true);
                StartCoroutine(BlinkIndicator());
            }
        }
        else
        {
            OnDialogueComplete();
        }
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

        // Open Map Selection & restore camera
        CameraManager camManager = FindFirstObjectByType<CameraManager>();
        if (camManager != null)
        {
            camManager.ShowEquipment(); // Return to Equipment Camera
            camManager.OpenMapSelection();
        }
    }
}
