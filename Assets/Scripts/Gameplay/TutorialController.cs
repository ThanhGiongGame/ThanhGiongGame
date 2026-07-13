using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TutorialController : MonoBehaviour
{
    private const string PlayerName = "thanhgiongembe";
    private const string MotherName = "bacumomv2";
    private const string MessengerName = "sugia";
    private const string ChickenName = "TutorialChicken";
    private const string ShopSceneName = "GameShopScene";

    private Transform player;
    private Transform mother;
    private Transform messenger;
    private Enemy firstChicken;
    private CameraController cameraController;
    private PlayerController playerController;
    private SkillSkyPlunge skill1;
    private SkillFlameDash skill2;

    [Header("Audio")]
    public AudioSource dialogueAudioSource;
    public AudioClip[] tutorialVoices;

    private Canvas tutorialCanvas;
    private CanvasGroup dialogueGroup;
    private TMP_Text speakerText;
    private TMP_Text bodyText;
    private TMP_Text objectiveText;
    private Button skipButton;
    private Image fadeImage;
    private GameObject continueIndicator;
    private Coroutine typewriterCoroutine;
    private bool typewriterDone;
    private string fullDialogueText;

    private static TutorialController instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private Transform moveMarker;
    private Transform fightMarker;
    private Transform gateMarker;
    private GameObject chickenTemplate;
    private readonly List<Enemy> activeWave = new List<Enemy>();
    private readonly List<GameObject> hudObjects = new List<GameObject>();

    private int phase;
    private bool waitingForDialogue;
    private bool playerControlEnabled = true;
    private bool routineRunning;
    private bool skillOneWaveActive;
    private bool wave2Active;
    private bool wave3Active;
    private Vector3 playerStart;
    private float phaseStartedAt;

    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        ResolveSceneReferences();
        ConfigureActors();
        EnsureGameplayHud();
        BuildTutorialCanvas();

        XPManager.OnLevelUp += HandleTutorialLevelUp;

        StartCoroutine(OpeningSequence());
    }

    private void OnDestroy()
    {
        XPManager.OnLevelUp -= HandleTutorialLevelUp;
    }

    private void Update()
    {
        if (!playerControlEnabled)
        {
            SetGameplayHudVisible(false);
        }

        PulseMarker(moveMarker, new Vector3(1.2f, 0.04f, 1.2f));
        PulseMarker(fightMarker, new Vector3(1.25f, 0.04f, 1.25f));
        PulseMarker(gateMarker, new Vector3(1.4f, 0.04f, 1.4f));

        if (waitingForDialogue && WasConfirmPressed())
        {
            ContinueDialogue();
            return;
        }

        if (!playerControlEnabled || waitingForDialogue || routineRunning)
        {
            return;
        }

        switch (phase)
        {
            case 1:
                if (player != null && Vector3.Distance(player.position, GetMarkerPosition(moveMarker)) < 1.8f)
                {
                    StartCoroutine(MessengerCloseupPrompt());
                }
                break;
            case 3:
                if (firstChicken == null || firstChicken.currentHealth <= 0f)
                {
                    StartCoroutine(ChickenWavePrompt());
                }
                break;
            case 4:
                CleanupDeadWaveEnemies();
                if (skillOneWaveActive && activeWave.Count == 0)
                {
                    StartCoroutine(Wave1ClearSequence());
                }
                break;
            case 5:
                CleanupDeadWaveEnemies();
                if (wave2Active && activeWave.Count == 0)
                {
                    StartCoroutine(Wave2ClearSequence());
                }
                break;
            case 6:
                CleanupDeadWaveEnemies();
                if (wave3Active && activeWave.Count == 0)
                {
                    StartCoroutine(EndingSequence());
                }
                break;
        }
    }

    private IEnumerator OpeningSequence()
    {
        yield return null;
        SetPlayerControl(false);
        SetGameplayHudVisible(false);
        HideObjective();
        SetMarkerVisible(moveMarker, false);
        SetMarkerVisible(fightMarker, false);
        SetMarkerVisible(gateMarker, false);

        if (player != null)
        {
            player.position = new Vector3(-7.2f, 0f, -6f);
            LookAtFlat(player, new Vector3(-2.2f, 0f, -2f));
        }

        if (mother != null)
        {
            mother.position = new Vector3(-8.4f, 0f, -6.3f);
            LookAtFlat(mother, player != null ? player.position : Vector3.zero);
        }

        if (cameraController != null && player != null)
        {
            cameraController.target = player;
            cameraController.SetCinematicView(new Vector3(0f, 6.2f, -7.2f), 42f);
        }

        yield return AutoWalk(player, new Vector3(-2.2f, 0f, -2f), 2.4f);
        if (mother != null) yield return AutoWalk(mother, new Vector3(-5.4f, 0f, -4.6f), 1.8f);

        yield return ShowDialogue("Mẹ Gióng", "Con còn nhỏ, sao lại bước ra sân đình lúc trống trận vang như vậy?", GetVoiceClip(0));
        yield return ShowDialogue("Gióng", "Mẹ ra mời sứ giả vào đây. Giặc đến cõi bờ, con xin đi phá giặc, cứu nước.", GetVoiceClip(1));
        yield return ShowDialogue("Xứ giả", "Nếu lời ấy là thật, hãy bước đến sân tập. Ta sẽ xem sức con.", GetVoiceClip(2));

        if (cameraController != null)
        {
            cameraController.ResetView();
        }

        SetPlayerControl(true);
        SetGameplayHudVisible(true);
        SetMarkerVisible(moveMarker, true);
        SetObjective("Di chuyển bằng W A S D hoặc phím mũi tên đến vòng sáng.");
        playerStart = player != null ? player.position : Vector3.zero;
        phase = 1;
        phaseStartedAt = Time.time;
    }

    private IEnumerator MessengerCloseupPrompt()
    {
        routineRunning = true;
        phase = 2;
        SetMarkerVisible(moveMarker, false);
        SetPlayerControl(false);
        SetGameplayHudVisible(false);
        HideObjective();

        // Camera zoom gần vào sứ giả + Gióng
        if (cameraController != null && messenger != null)
        {
            // target tạm thời chuyển sang messenger để zoom
            cameraController.target = messenger;
            cameraController.SetCinematicView(new Vector3(0f, 3.8f, -4.5f), 38f);
        }

        // Sứ giả quay mặt nhìn Gióng
        if (messenger != null && player != null)
            LookAtFlat(messenger, player.position);
        if (player != null && messenger != null)
            LookAtFlat(player, messenger.position);

        yield return new WaitForSecondsRealtime(0.8f);

        yield return ShowDialogue("Xứ giả", "Ngươi nói muốn ra trận? Thế thì hãy chứng minh đi! Hạ con gà trống kia trước đã.", GetVoiceClip(3));

        // Reset camera về player
        if (cameraController != null && player != null)
        {
            cameraController.target = player;
            cameraController.ResetView();
        }

        SetPlayerControl(true);
        SetGameplayHudVisible(true);

        // Bật gà
        if (firstChicken != null)
            firstChicken.enabled = true;

        SetObjective("Hạ con gà trống. Đòn thường tự vung theo hướng chuột.");
        phase = 3;
        phaseStartedAt = Time.time;
        routineRunning = false;
    }

    private IEnumerator ChickenWavePrompt()
    {
        routineRunning = true;
        phase = 4;
        skillOneWaveActive = false;
        SetMarkerVisible(fightMarker, false);
        SetObjective("Chọn một nâng cấp để mở khóa Thiên Giáng.");

        if (XPManager.Instance != null)
        {
            XPManager.Instance.AddXP(10f);
        }

        yield return new WaitUntil(() => Time.timeScale > 0f);

        if (skill2 != null)
        {
            skill2.SetLevel(0);
        }

        int spawned = SpawnChickenWave(2);
        skillOneWaveActive = spawned > 0;
        SetObjective(spawned > 0
            ? "Đàn gà đã ùa vào! Bấm 1, chọn tâm đàn gà bằng chuột trái để dùng Thiên Giáng."
            : "Không tìm thấy mẫu gà để tạo wave. Kiểm tra TutorialChicken trong scene.");
        routineRunning = false;
    }

    private IEnumerator Wave1ClearSequence()
    {
        routineRunning = true;
        phase = 5;
        SetPlayerControl(false);
        SetGameplayHudVisible(false);
        HideObjective();

        // Camera zoom vào Gióng + Sứ giả
        if (cameraController != null && player != null)
        {
            cameraController.target = player;
            cameraController.SetCinematicView(new Vector3(0f, 4.2f, -5f), 38f);
        }
        if (player != null && messenger != null)
            LookAtFlat(player, messenger.position);
        if (messenger != null && player != null)
            LookAtFlat(messenger, player.position);

        yield return new WaitForSecondsRealtime(0.6f);

        yield return ShowDialogue("Gióng", "Con làm được rồi đó!", GetVoiceClip(4));
        yield return ShowDialogue("Xứ giả", "Tốt! Nhưng chưa đủ. Hãy thử với đàn này xem sao!", GetVoiceClip(5));

        if (cameraController != null && player != null)
        {
            cameraController.target = player;
            cameraController.ResetView();
        }

        SetPlayerControl(true);
        SetGameplayHudVisible(true);

        int spawned = SpawnChickenWave(3);
        wave2Active = spawned > 0;
        SetObjective(spawned > 0
            ? "3 con gà tràn vào! Hạ tất cả chúng!"
            : "Không tìm thấy mẫu gà.");
        routineRunning = false;
    }

    private IEnumerator Wave2ClearSequence()
    {
        routineRunning = true;
        phase = 6;

        int spawned = SpawnChickenWave(5);
        wave3Active = spawned > 0;
        SetObjective(spawned > 0
            ? "5 con gà! Đây là thử thách cuối — hạ tất cả!"
            : "Không tìm thấy mẫu gà.");
        routineRunning = false;
        yield break;
    }

    private IEnumerator EndingSequence()
    {
        routineRunning = true;
        phase = 9;
        SetPlayerControl(false);
        SetGameplayHudVisible(false);
        SetMarkerVisible(fightMarker, false);
        SetMarkerVisible(gateMarker, false);
        HideObjective();

        // Camera zoom vào Sứ giả đang think
        if (cameraController != null && messenger != null)
        {
            cameraController.target = messenger;
            cameraController.SetCinematicView(new Vector3(0f, 3.2f, -4f), 35f);
        }
        if (messenger != null && player != null) LookAtFlat(messenger, player.position);
        if (player != null && messenger != null) LookAtFlat(player, messenger.position);
        if (mother != null && player != null) LookAtFlat(mother, player.position);

        yield return new WaitForSecondsRealtime(2.2f);

        yield return ShowDialogue("Xứ giả", "...", GetVoiceClip(6));
        yield return ShowDialogue("Xứ giả", "Ta đã thấy đủ rồi. Sức mạnh ấy... không phải của người thường.", GetVoiceClip(7));
        yield return ShowDialogue("Gióng", "Thưa sứ giả, con có thể ra trận không?", GetVoiceClip(8));
        yield return ShowDialogue("Xứ giả", "Không chỉ ra trận — ngươi sẽ dẫn đầu. Hãy theo ta vào yết kiến Đức Vua!", GetVoiceClip(9));
        yield return ShowDialogue("Mẹ Gióng", "Con... hãy đi đi. Mẹ tin con sẽ trở về.", GetVoiceClip(10));

        if (cameraController != null && player != null)
        {
            cameraController.target = player;
            cameraController.SetCinematicView(new Vector3(0f, 5.8f, -6.8f), 40f);
        }

        if (messenger != null) LookAtFlat(messenger, new Vector3(0f, 0f, 14f));
        yield return new WaitForSecondsRealtime(0.5f);

        if (messenger != null) StartCoroutine(AutoWalk(messenger, new Vector3(1.7f, 0f, 13f), 2.0f));
        yield return new WaitForSecondsRealtime(0.4f);
        if (mother != null) StartCoroutine(AutoWalk(mother, new Vector3(-1.8f, 0f, 12.5f), 2.5f));
        yield return new WaitForSecondsRealtime(0.3f);

        Coroutine playerWalk = StartCoroutine(AutoWalk(player, new Vector3(0f, 0f, 14f), 2.7f));
        yield return playerWalk;

        SetMarkerVisible(gateMarker, true);
        yield return new WaitForSecondsRealtime(0.8f);

        yield return FadeToWhite();
        PlayerPrefs.SetInt("TutorialComplete", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(ShopSceneName);
    }

    private void ResolveSceneReferences()
    {
        player = FindByName(PlayerName);
        mother = FindByName(MotherName);
        messenger = FindByName(MessengerName);

        Transform chickenTransform = FindByName(ChickenName) ?? FindByName("chicken");
        if (chickenTransform != null)
        {
            firstChicken = chickenTransform.GetComponent<Enemy>();
            chickenTemplate = Instantiate(chickenTransform.gameObject);
            chickenTemplate.name = "TutorialChickenTemplate";
            chickenTemplate.SetActive(false);
            Enemy templateEnemy = chickenTemplate.GetComponent<Enemy>();
            if (templateEnemy != null)
            {
                templateEnemy.enabled = false;
            }
        }

        Transform move = FindByName("YardMarker_Move");
        Transform fight = FindByName("YardMarker_Fight");
        moveMarker = move;
        fightMarker = fight;
        gateMarker = CreateMarker("YardMarker_Gate", new Vector3(0f, 0.03f, 12.8f), new Color(1f, 0.92f, 0.35f, 0.45f));

        Camera mainCamera = Camera.main;
        cameraController = mainCamera != null ? mainCamera.GetComponent<CameraController>() : null;
        playerController = player != null ? player.GetComponent<PlayerController>() : null;
        skill1 = player != null ? player.GetComponent<SkillSkyPlunge>() : null;
        skill2 = player != null ? player.GetComponent<SkillFlameDash>() : null;
    }

    private void ConfigureActors()
    {
        if (player != null)
        {
            player.gameObject.tag = "Player";
            playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.moveSpeed = 5.5f;
            }

            // Disable Legend weapons so the player only has the basic slash in the tutorial
            var legendSystems = player.GetComponents<MonoBehaviour>();
            foreach (var comp in legendSystems)
            {
                if (comp.GetType().Name.StartsWith("LegendSystem"))
                {
                    comp.enabled = false;
                }
            }
        }

        WaveSpawner spawner = FindFirstObjectByType<WaveSpawner>();
        if (spawner != null)
        {
            spawner.enabled = false;
            Destroy(spawner.gameObject);
        }

        if (skill1 != null) skill1.SetLevel(0);
        if (skill2 != null) skill2.SetLevel(0);

        if (firstChicken != null)
        {
            ConfigureChicken(firstChicken, 45f, 1.3f);
            firstChicken.enabled = false;
        }

        if (cameraController != null && player != null)
        {
            cameraController.target = player;
            cameraController.offset = new Vector3(0f, 12f, -9f);
            cameraController.pitchAngle = 58f;
        }
    }

    private void EnsureGameplayHud()
    {
        if (FindFirstObjectByType<XPManager>() == null)
        {
            XPManager xpManager = new GameObject("XPManager").AddComponent<XPManager>();
            xpManager.baseXPPerLevel = 10f;
        }
        else
        {
            FindFirstObjectByType<XPManager>().baseXPPerLevel = 10f;
        }

        if (FindFirstObjectByType<UpgradeManager>() == null)
        {
            new GameObject("UpgradeManager").AddComponent<UpgradeManager>();
        }

        if (FindFirstObjectByType<PlayerLevelUI>() == null)
        {
            new GameObject("PlayerLevelUI").AddComponent<PlayerLevelUI>();
        }

        LevelUpUI lvlUI = FindFirstObjectByType<LevelUpUI>();
        if (lvlUI != null)
        {
            Destroy(lvlUI.gameObject);
        }

        if (FindFirstObjectByType<SkillCooldownUI>() == null)
        {
            new GameObject("SkillCooldownUI").AddComponent<SkillCooldownUI>();
        }
    }

    private void BuildTutorialCanvas()
    {
        GameObject canvasObject = new GameObject("TutorialCanvas");
        tutorialCanvas = canvasObject.AddComponent<Canvas>();
        tutorialCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        tutorialCanvas.sortingOrder = 900;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        // ── Panel đen full-width phía dưới ──────────────────────────────────
        GameObject panel = new GameObject("DialoguePanel");
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot     = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(0f, 210f);
        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.03f, 0.02f, 0.01f, 0.88f);
        dialogueGroup = panel.AddComponent<CanvasGroup>();

        // ── Line vàng trên đầu panel ─────────────────────────────────────────
        GameObject line = new GameObject("GoldLine");
        line.transform.SetParent(panel.transform, false);
        RectTransform lineRT = line.AddComponent<RectTransform>();
        lineRT.anchorMin = new Vector2(0.04f, 1f);
        lineRT.anchorMax = new Vector2(0.96f, 1f);
        lineRT.pivot     = new Vector2(0.5f, 1f);
        lineRT.anchoredPosition = Vector2.zero;
        lineRT.sizeDelta = new Vector2(0f, 1.5f);
        line.AddComponent<Image>().color = new Color(1f, 0.78f, 0.1f, 0.95f);

        // ── Tên nhân vật (vàng, căn giữa, in đậm) ───────────────────────────
        speakerText = CreateText(panel.transform, "Speaker", new Vector2(0f, -14f), new Vector2(900f, 40f), 26, FontStyle.Bold, new Color(1f, 0.82f, 0.18f));
        RectTransform spkRT = speakerText.GetComponent<RectTransform>();
        spkRT.anchorMin = new Vector2(0.5f, 1f);
        spkRT.anchorMax = new Vector2(0.5f, 1f);
        spkRT.pivot     = new Vector2(0.5f, 1f);
        spkRT.anchoredPosition = new Vector2(0f, -14f);
        spkRT.sizeDelta = new Vector2(900f, 40f);
        speakerText.alignment = TextAlignmentOptions.Center;

        // ── Body text (trắng ngà, căn giữa) ─────────────────────────────────
        bodyText = CreateText(panel.transform, "Body", Vector2.zero, new Vector2(1400f, 110f), 26, FontStyle.Normal, new Color(0.96f, 0.93f, 0.85f));
        RectTransform bodyRT = bodyText.GetComponent<RectTransform>();
        bodyRT.anchorMin = new Vector2(0.5f, 0.5f);
        bodyRT.anchorMax = new Vector2(0.5f, 0.5f);
        bodyRT.pivot     = new Vector2(0.5f, 0.5f);
        bodyRT.anchoredPosition = new Vector2(0f, 18f);
        bodyRT.sizeDelta = new Vector2(1400f, 110f);
        bodyText.alignment = TextAlignmentOptions.Center;

        // ── Icon ◆ nhấp nháy ────────────────────────────────────────────────
        continueIndicator = new GameObject("ContinueIndicator");
        continueIndicator.transform.SetParent(panel.transform, false);
        RectTransform indRT = continueIndicator.AddComponent<RectTransform>();
        indRT.anchorMin = new Vector2(0.5f, 0f);
        indRT.anchorMax = new Vector2(0.5f, 0f);
        indRT.pivot     = new Vector2(0.5f, 0f);
        indRT.anchoredPosition = new Vector2(0f, 12f);
        indRT.sizeDelta = new Vector2(40f, 32f);
        TextMeshProUGUI indTMP = continueIndicator.AddComponent<TextMeshProUGUI>();
        indTMP.text = "...";
        indTMP.fontSize = 20f;
        indTMP.color = new Color(1f, 0.82f, 0.18f, 1f);
        indTMP.alignment = TextAlignmentOptions.Center;
        continueIndicator.SetActive(false);

        // ── Objective text (trên cùng màn hình) ──────────────────────────────
        objectiveText = CreateText(canvasObject.transform, "Objective", Vector2.zero, new Vector2(1100f, 62f), 28, FontStyle.Bold, new Color(1f, 0.94f, 0.55f));
        RectTransform objectiveRect = objectiveText.GetComponent<RectTransform>();
        objectiveRect.anchorMin = new Vector2(0.5f, 1f);
        objectiveRect.anchorMax = new Vector2(0.5f, 1f);
        objectiveRect.pivot = new Vector2(0.5f, 1f);
        objectiveRect.anchoredPosition = new Vector2(0f, -34f);

        // ── Skip button ───────────────────────────────────────────────────────
        skipButton = CreateButton(canvasObject.transform, "BỎ QUA", Vector2.zero, new Vector2(170f, 50f), new Color(0.13f, 0.15f, 0.18f), FinishTutorialNow);
        RectTransform skipRect = skipButton.GetComponent<RectTransform>();
        skipRect.anchorMin = new Vector2(1f, 1f);
        skipRect.anchorMax = new Vector2(1f, 1f);
        skipRect.pivot = new Vector2(1f, 1f);
        skipRect.anchoredPosition = new Vector2(-30f, -32f);
        skipButton.gameObject.SetActive(false);

        // ── Fade overlay ──────────────────────────────────────────────────────
        GameObject fadeObject = new GameObject("WhiteFade", typeof(RectTransform));
        fadeObject.transform.SetParent(canvasObject.transform, false);
        RectTransform fadeRect = fadeObject.GetComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = Vector2.zero;
        fadeRect.offsetMax = Vector2.zero;
        fadeImage = fadeObject.AddComponent<Image>();
        fadeImage.color = new Color(1f, 0.92f, 0.68f, 0f);
        fadeImage.raycastTarget = false;

        HideDialogue();
    }

    private AudioClip GetVoiceClip(int index)
    {
        if (tutorialVoices != null && index >= 0 && index < tutorialVoices.Length)
            return tutorialVoices[index];
        return null;
    }

    private IEnumerator ShowDialogue(string speaker, string body, AudioClip voiceClip = null)
    {
        waitingForDialogue = true;
        typewriterDone = false;
        fullDialogueText = body;

        if (!playerControlEnabled) SetGameplayHudVisible(false);

        if (dialogueGroup != null)
        {
            dialogueGroup.alpha = 1f;
            dialogueGroup.interactable = true;
            dialogueGroup.blocksRaycasts = true;
        }

        if (speakerText != null) speakerText.text = speaker;
        if (bodyText != null) bodyText.text = string.Empty;
        if (continueIndicator != null) continueIndicator.SetActive(false);

        if (voiceClip != null && dialogueAudioSource != null)
        {
            dialogueAudioSource.Stop();
            dialogueAudioSource.clip = voiceClip;
            dialogueAudioSource.Play();
        }

        // Typewriter
        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        typewriterCoroutine = StartCoroutine(TypewriterEffect(body));

        // Chờ player nhấn xác nhận
        while (waitingForDialogue)
        {
            yield return null;
        }

        if (dialogueAudioSource != null) dialogueAudioSource.Stop();
        if (typewriterCoroutine != null) { StopCoroutine(typewriterCoroutine); typewriterCoroutine = null; }
        HideDialogue();
    }

    private IEnumerator TypewriterEffect(string text)
    {
        if (bodyText == null) yield break;
        bodyText.text = string.Empty;
        for (int i = 0; i < text.Length; i++)
        {
            bodyText.text += text[i];
            yield return new WaitForSecondsRealtime(0.038f);
        }
        typewriterDone = true;
        if (continueIndicator != null)
        {
            continueIndicator.SetActive(true);
            StartCoroutine(BlinkIndicator());
        }
    }

    private IEnumerator BlinkIndicator()
    {
        while (continueIndicator != null && continueIndicator.activeSelf && waitingForDialogue)
        {
            continueIndicator.SetActive(false);
            yield return new WaitForSecondsRealtime(0.38f);
            if (continueIndicator != null) continueIndicator.SetActive(true);
            yield return new WaitForSecondsRealtime(0.38f);
        }
    }

    private void ContinueDialogue()
    {
        if (!typewriterDone)
        {
            // Nhấn lần 1: hiện ngay toàn bộ text
            if (typewriterCoroutine != null) { StopCoroutine(typewriterCoroutine); typewriterCoroutine = null; }
            if (dialogueAudioSource != null) dialogueAudioSource.Stop();
            if (bodyText != null) bodyText.text = fullDialogueText;
            typewriterDone = true;
            if (continueIndicator != null)
            {
                continueIndicator.SetActive(true);
                StartCoroutine(BlinkIndicator());
            }
        }
        else
        {
            // Nhấn lần 2: sang câu tiếp
            waitingForDialogue = false;
        }
    }

    private void HideDialogue()
    {
        if (continueIndicator != null) continueIndicator.SetActive(false);
        if (dialogueGroup != null)
        {
            dialogueGroup.alpha = 0f;
            dialogueGroup.interactable = false;
            dialogueGroup.blocksRaycasts = false;
        }
    }

    private IEnumerator AutoWalk(Transform subject, Vector3 destination, float duration)
    {
        if (subject == null) yield break;

        Animator anim = subject.GetComponentInChildren<Animator>();
        if (anim != null) anim.SetBool("isWalking", true);

        Vector3 start = subject.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            subject.position = Vector3.Lerp(start, destination, t);
            LookAtFlat(subject, destination);
            yield return null;
        }
        subject.position = destination;

        if (anim != null) anim.SetBool("isWalking", false);
    }

    private int SpawnChickenWave(int count)
    {
        activeWave.Clear();
        if (chickenTemplate == null) return 0;

        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i;
            Vector3 pos = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * 5.2f, 0f, Mathf.Sin(angle * Mathf.Deg2Rad) * 4.2f) + new Vector3(1.5f, 0f, 1.2f);
            GameObject clone = Instantiate(chickenTemplate, pos, Quaternion.Euler(0f, angle + 180f, 0f));
            clone.name = "TutorialWaveChicken";
            clone.SetActive(true);
            clone.transform.localScale = Vector3.one * 0.2f;
            clone.tag = "Enemy";

            Enemy enemy = clone.GetComponent<Enemy>();
            if (enemy == null) enemy = clone.AddComponent<Enemy>();
            ConfigureChicken(enemy, 35f, 1.55f);
            enemy.enabled = true;
            activeWave.Add(enemy);
        }

        return activeWave.Count;
    }

    private void SpawnDashPracticeWave()
    {
        activeWave.Clear();
        if (chickenTemplate == null) return;

        Vector3[] positions =
        {
            new Vector3(-2.5f, 0f, -0.8f),
            new Vector3(-1.2f, 0f, -0.1f),
            new Vector3(0.2f, 0f, 0.6f),
            new Vector3(1.6f, 0f, 1.3f),
            new Vector3(3.0f, 0f, 2.0f),
            new Vector3(4.4f, 0f, 2.7f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject clone = Instantiate(chickenTemplate, positions[i], Quaternion.Euler(0f, 235f, 0f));
            clone.name = "TutorialDashChicken";
            clone.SetActive(true);
            clone.transform.localScale = Vector3.one * 0.2f;
            clone.tag = "Enemy";

            Enemy enemy = clone.GetComponent<Enemy>();
            if (enemy == null) enemy = clone.AddComponent<Enemy>();
            ConfigureChicken(enemy, 28f, 1.2f);
            enemy.enabled = true;
            activeWave.Add(enemy);
        }
    }

    private void ConfigureChicken(Enemy enemy, float health, float speed)
    {
        enemy.gameObject.tag = "Enemy";
        enemy.maxHealth = health;
        enemy.currentHealth = health;
        enemy.moveSpeed = speed;
        enemy.damage = 2f;
        enemy.attackRange = 1.1f;
        enemy.attackCooldown = 1.35f;
    }

    private void CleanupDeadWaveEnemies()
    {
        for (int i = activeWave.Count - 1; i >= 0; i--)
        {
            if (activeWave[i] == null || activeWave[i].currentHealth <= 0f)
            {
                activeWave.RemoveAt(i);
            }
        }
    }

    private void HighlightDashLane()
    {
        GameObject lane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lane.name = "DashLaneHint";
        lane.transform.position = new Vector3(1.8f, 0.05f, 1.2f);
        lane.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
        lane.transform.localScale = new Vector3(8f, 0.04f, 0.55f);
        Destroy(lane.GetComponent<BoxCollider>());

        Renderer renderer = lane.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = BuildMaterial(new Color(1f, 0.38f, 0.06f, 0.5f));
        }

        Destroy(lane, 7f);
    }

    private IEnumerator FadeToWhite()
    {
        if (fadeImage == null) yield break;

        float elapsed = 0f;
        while (elapsed < 1.6f)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / 1.6f);
            fadeImage.color = new Color(1f, 0.92f, 0.68f, alpha);
            yield return null;
        }
    }

    private void SetPlayerControl(bool enabled)
    {
        playerControlEnabled = enabled;
        if (playerController != null)
        {
            playerController.enabled = enabled;
        }
    }

    // ========================================================================
    // Custom Tutorial Level Up
    // ========================================================================
    private GameObject tutorialLevelUpPanel;

    private void HandleTutorialLevelUp()
    {
        Time.timeScale = 0f;
        
        tutorialLevelUpPanel = new GameObject("TutorialLevelUpPanel");
        tutorialLevelUpPanel.transform.SetParent(tutorialCanvas.transform, false);
        
        RectTransform pr = tutorialLevelUpPanel.AddComponent<RectTransform>();
        pr.anchorMin = Vector2.zero;
        pr.anchorMax = Vector2.one;
        pr.offsetMin = pr.offsetMax = Vector2.zero;
        
        Image bgImg = tutorialLevelUpPanel.AddComponent<Image>();
        bgImg.color = new Color(0.02f, 0.02f, 0.03f, 0.95f); // Dark overlay

        // Title
        TMP_Text titleText = CreateText(tutorialLevelUpPanel.transform, "HỆ THỐNG NÂNG CẤP", Vector2.zero, Vector2.zero, 40, FontStyle.Bold, new Color(1f, 0.85f, 0.2f));
        titleText.alignment = TextAlignmentOptions.Center;
        RectTransform titleRt = titleText.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 0.85f);
        titleRt.anchorMax = new Vector2(1f, 0.95f);
        titleRt.offsetMin = titleRt.offsetMax = Vector2.zero;
        
        // Subtitle
        TMP_Text subText = CreateText(tutorialLevelUpPanel.transform, "Trong thực chiến, bạn sẽ nhận được 3 lựa chọn mỗi khi lên cấp.\n(Nhấn vào KỸ NĂNG để tiếp tục bài hướng dẫn)", Vector2.zero, Vector2.zero, 26, FontStyle.Italic, new Color(0.8f, 0.8f, 0.8f));
        subText.alignment = TextAlignmentOptions.Center;
        RectTransform subRt = subText.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0f, 0.75f);
        subRt.anchorMax = new Vector2(1f, 0.85f);
        subRt.offsetMin = subRt.offsetMax = Vector2.zero;

        // Skill Card (Clickable)
        BuildTutorialCard("🌩 MỞ KHÓA: Thiên Đòn Sa", "Nhảy vọt lên không trung, sau đó dậm mạnh xuống mục tiêu\ngây sát thương diện rộng cực lớn, gây choáng\nvà đẩy lùi toàn bộ kẻ địch xung quanh.", "Icons/Skill1", new Vector2(0.05f, 0.2f), new Vector2(0.32f, 0.7f), new Color(0.1f, 0.15f, 0.2f), new Color(0.2f, 0.75f, 0.95f), true);

        // Legend Card (Blocked)
        BuildTutorialCard("MỚI: Tre Ngà", "Gióng nhổ những bụi tre ngà bên đường làm vũ khí. Tạo ra chướng ngại vật mọc lên từ lòng đất chặn đánh quân thù.", "Icons/ThanhGiong_W1", new Vector2(0.36f, 0.2f), new Vector2(0.64f, 0.7f), new Color(0.04f, 0.05f, 0.08f), new Color(1f, 0.85f, 0.2f), false);

        // Stat Card (Blocked)
        BuildTutorialCard("⚔ Cường Lực", "Tăng sát thương thêm +20%.\nĐòn đánh của bạn sẽ mạnh mẽ hơn.", "Icons/StatDamage", new Vector2(0.68f, 0.2f), new Vector2(0.95f, 0.7f), new Color(0.04f, 0.05f, 0.08f), new Color(1f, 0.45f, 0.1f), false);
    }

    private void BuildTutorialCard(string title, string desc, string iconPath, Vector2 anchorMin, Vector2 anchorMax, Color bgColor, Color borderColor, bool isClickable)
    {
        GameObject card = new GameObject("Card_" + title);
        card.transform.SetParent(tutorialLevelUpPanel.transform, false);
        
        RectTransform cr = card.AddComponent<RectTransform>();
        cr.anchorMin = anchorMin;
        cr.anchorMax = anchorMax;
        cr.offsetMin = cr.offsetMax = Vector2.zero;
        
        Image cImg = card.AddComponent<Image>();
        cImg.color = borderColor;

        GameObject inner = new GameObject("Inner");
        inner.transform.SetParent(card.transform, false);
        RectTransform ir = inner.AddComponent<RectTransform>();
        ir.anchorMin = Vector2.zero;
        ir.anchorMax = Vector2.one;
        ir.offsetMin = new Vector2(4f, 4f);
        ir.offsetMax = new Vector2(-4f, -4f);
        Image iImg = inner.AddComponent<Image>();
        iImg.color = bgColor;
        iImg.raycastTarget = false; // Prevent blocking clicks!

        // Title
        TMP_Text tTitle = CreateText(inner.transform, title, Vector2.zero, Vector2.zero, 24, FontStyle.Bold, borderColor);
        tTitle.alignment = TextAlignmentOptions.Center;
        tTitle.enableWordWrapping = false;
        RectTransform rtTitle = tTitle.GetComponent<RectTransform>();
        rtTitle.anchorMin = new Vector2(0f, 0.7f);
        rtTitle.anchorMax = new Vector2(1f, 0.95f);
        rtTitle.offsetMin = rtTitle.offsetMax = Vector2.zero;
        
        // Description
        TMP_Text tDesc = CreateText(inner.transform, desc, Vector2.zero, Vector2.zero, 18, FontStyle.Normal, new Color(0.85f, 0.85f, 0.9f));
        tDesc.alignment = TextAlignmentOptions.Center;
        RectTransform rtDesc = tDesc.GetComponent<RectTransform>();

        // Icon
        Sprite icon = Resources.Load<Sprite>(iconPath);
        if (icon != null)
        {
            GameObject iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(inner.transform, false);
            RectTransform iRect = iconGO.AddComponent<RectTransform>();
            iRect.anchorMin = new Vector2(0.05f, 0.1f);
            iRect.anchorMax = new Vector2(0.3f, 0.5f);
            iRect.offsetMin = iRect.offsetMax = Vector2.zero;
            Image img = iconGO.AddComponent<Image>();
            img.sprite = icon;
            img.preserveAspect = true;
            img.raycastTarget = false;

            rtDesc.anchorMin = new Vector2(0.35f, 0.05f);
            rtDesc.anchorMax = new Vector2(0.95f, 0.65f);
        }
        else
        {
            rtDesc.anchorMin = new Vector2(0.05f, 0.05f);
            rtDesc.anchorMax = new Vector2(0.95f, 0.65f);
        }
        rtDesc.offsetMin = rtDesc.offsetMax = Vector2.zero;

        if (isClickable)
        {
            Button btn = card.AddComponent<Button>();
            btn.targetGraphic = cImg;
            btn.onClick.AddListener(OnTutorialSkillClicked);
        }
        else
        {
            // Blocked overlay
            GameObject overlay = new GameObject("LockOverlay");
            overlay.transform.SetParent(card.transform, false);
            RectTransform or = overlay.AddComponent<RectTransform>();
            or.anchorMin = Vector2.zero;
            or.anchorMax = Vector2.one;
            or.offsetMin = or.offsetMax = Vector2.zero;
            Image oImg = overlay.AddComponent<Image>();
            oImg.color = new Color(0f, 0f, 0f, 0.6f);
            oImg.raycastTarget = false;
            
            TMP_Text tLock = CreateText(overlay.transform, "(BỊ KHÓA)", Vector2.zero, Vector2.zero, 28, FontStyle.Bold, new Color(1f, 0.3f, 0.3f));
            tLock.alignment = TextAlignmentOptions.Center;
            RectTransform rtLock = tLock.GetComponent<RectTransform>();
            rtLock.anchorMin = new Vector2(0f, 0.4f);
            rtLock.anchorMax = new Vector2(1f, 0.6f);
            rtLock.offsetMin = rtLock.offsetMax = Vector2.zero;
        }
    }

    private void OnTutorialSkillClicked()
    {
        if (tutorialLevelUpPanel != null)
        {
            Destroy(tutorialLevelUpPanel);
        }
        
        Time.timeScale = 1f;
        
        if (skill1 != null) skill1.SetLevel(1);

        if (XPManager.Instance != null)
        {
            XPManager.OnLevelUp -= HandleTutorialLevelUp;
            XPManager.Instance.enabled = false;
            XPManager.Instance.gameObject.SetActive(false); 
        }
    }
    // ========================================================================

    private void SetGameplayHudVisible(bool visible)
    {
        SetNamedObjectVisible("GameplayHUDCanvas", visible);
        SetNamedObjectVisible("HealthBarCanvas", visible);
        SetNamedObjectVisible("SkillHotbarCanvas", visible);
        SetNamedObjectVisible("HPBorder", visible);
        SetNamedObjectVisible("XPBorder", visible);
        SetNamedObjectVisible("HotbarContainer", visible);
    }

    private void SetNamedObjectVisible(string objectName, bool visible)
    {
        GameObject target = FindHudObject(objectName);
        if (target != null)
        {
            target.SetActive(visible);
        }
    }

    private GameObject FindHudObject(string objectName)
    {
        for (int i = hudObjects.Count - 1; i >= 0; i--)
        {
            if (hudObjects[i] == null)
            {
                hudObjects.RemoveAt(i);
                continue;
            }

            if (hudObjects[i].name == objectName)
            {
                return hudObjects[i];
            }
        }

        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform transform in transforms)
        {
            if (transform.name == objectName)
            {
                hudObjects.Add(transform.gameObject);
                return transform.gameObject;
            }
        }

        return null;
    }

    private void SetObjective(string text)
    {
        if (objectiveText != null)
        {
            objectiveText.gameObject.SetActive(true);
            objectiveText.text = text;
        }
    }

    private void HideObjective()
    {
        if (objectiveText != null) objectiveText.gameObject.SetActive(false);
    }

    private void FinishTutorialNow()
    {
        PlayerPrefs.SetInt("TutorialComplete", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(ShopSceneName);
    }

    private static Transform FindByName(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform transform in transforms)
        {
            if (transform.name == objectName) return transform;
        }
        return null;
    }

    private static Transform CreateMarker(string name, Vector3 position, Color color)
    {
        Transform existing = FindByName(name);
        if (existing != null) return existing;

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = name;
        marker.transform.position = position;
        marker.transform.localScale = new Vector3(1.4f, 0.04f, 1.4f);
        Destroy(marker.GetComponent<Collider>());

        Renderer renderer = marker.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = BuildMaterial(color);
        }

        return marker.transform;
    }

    private static Material BuildMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        return material;
    }

    private static void SetMarkerVisible(Transform marker, bool visible)
    {
        if (marker != null) marker.gameObject.SetActive(visible);
    }

    private static void PulseMarker(Transform marker, Vector3 baseScale)
    {
        if (marker == null || !marker.gameObject.activeSelf) return;
        float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.12f;
        marker.localScale = new Vector3(baseScale.x * pulse, baseScale.y, baseScale.z * pulse);
    }

    private static Vector3 GetMarkerPosition(Transform marker)
    {
        return marker != null ? marker.position : Vector3.zero;
    }

    private static void LookAtFlat(Transform subject, Vector3 target)
    {
        if (subject == null) return;
        Vector3 direction = target - subject.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
        {
            subject.rotation = Quaternion.LookRotation(direction.normalized);
        }
    }

    private static bool WasConfirmPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
            || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
#endif
    }

    private static bool WasKeyPressed(Key key)
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
#else
        return false;
#endif
    }

    private static GameObject CreatePanel(Transform parent, string objectName, Vector2 position, Vector2 size, Color color)
    {
        GameObject gameObject = new GameObject(objectName);
        gameObject.transform.SetParent(parent, false);
        RectTransform rect = gameObject.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        Image image = gameObject.AddComponent<Image>();
        image.color = color;
        return gameObject;
    }

    private static TMP_Text CreateText(Transform parent, string objectName, Vector2 position, Vector2 size, int fontSize, FontStyle style, Color color)
    {
        GameObject gameObject = new GameObject(objectName);
        gameObject.transform.SetParent(parent, false);
        RectTransform rect = gameObject.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        TextMeshProUGUI text = gameObject.AddComponent<TextMeshProUGUI>();
        text.text = objectName;
        text.fontSize = fontSize;
        text.fontStyle = style == FontStyle.Bold ? FontStyles.Bold : style == FontStyle.Italic ? FontStyles.Italic : FontStyles.Normal;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(Transform parent, string label, Vector2 position, Vector2 size, Color color, UnityEngine.Events.UnityAction action)
    {
        GameObject gameObject = CreatePanel(parent, label, position, size, color);
        Button button = gameObject.AddComponent<Button>();
        button.targetGraphic = gameObject.GetComponent<Image>();
        button.onClick.AddListener(action);
        TMP_Text text = CreateText(gameObject.transform, label + "Text", Vector2.zero, size, 22, FontStyle.Bold, Color.white);
        text.text = label;
        return button;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
        eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
    }
}
