using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class MainMenuManager : MonoBehaviour
{
    private const string ShopSceneName = "GameShopScene";

    [Header("UI Panels")]
    public GameObject settingsPanel;

    [Header("Intro Video")]
    public GameObject introVideoPanel;
    public VideoPlayer introVideoPlayer;
    public GameObject mainMenuContainer;

    [Header("Audio Settings")]
    public AudioSource backgroundMusic;
    public Image muteButtonImage;
    public Sprite soundOnSprite;
    public Sprite soundOffSprite;
    public Slider volumeSlider;

    private bool isMuted;
    private float currentVolume = 1f;
    private bool isLoadingScene;

    private void Awake()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        ResolveReferences();
        NormalizeMenuLayout();
    }

    private void Start()
    {
        SetSettingsVisible(false);
        SetIntroVideoVisible(false);

        isMuted = PlayerPrefs.GetInt("IsMuted", 0) == 1;
        currentVolume = PlayerPrefs.GetFloat("Volume", 1f);

        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(currentVolume);
            volumeSlider.onValueChanged.RemoveListener(SetVolume);
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        if (introVideoPlayer != null)
        {
            introVideoPlayer.loopPointReached -= OnIntroVideoFinished;
            introVideoPlayer.loopPointReached += OnIntroVideoFinished;
        }

        ApplyMuteState();
    }

    private void OnDestroy()
    {
        if (introVideoPlayer != null)
        {
            introVideoPlayer.loopPointReached -= OnIntroVideoFinished;
        }
    }

    public void PlayGame()
    {
        if (isLoadingScene)
        {
            return;
        }

        if (introVideoPanel == null || introVideoPlayer == null)
        {
            LoadNextScene();
            return;
        }

        if (mainMenuContainer != null)
        {
            mainMenuContainer.SetActive(false);
        }

        SetSettingsVisible(false);
        SetIntroVideoVisible(true);

        if (backgroundMusic != null)
        {
            backgroundMusic.Stop();
        }

        introVideoPlayer.Stop();
        introVideoPlayer.Play();
    }

    public void SkipVideo()
    {
        if (introVideoPlayer != null)
        {
            introVideoPlayer.Stop();
        }

        LoadNextScene();
    }

    public void ToggleSettings()
    {
        SetSettingsVisible(settingsPanel != null && !settingsPanel.activeSelf);
    }

    public void ToggleMute()
    {
        isMuted = !isMuted;
        PlayerPrefs.SetInt("IsMuted", isMuted ? 1 : 0);
        PlayerPrefs.Save();
        ApplyMuteState();
    }

    public void SetVolume(float volume)
    {
        currentVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("Volume", currentVolume);
        PlayerPrefs.Save();
        ApplyMuteState();
    }

    private void OnIntroVideoFinished(VideoPlayer videoPlayer)
    {
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        if (isLoadingScene)
        {
            return;
        }

        isLoadingScene = true;

        if (!PlayerPrefs.HasKey("VinhDanhTotal"))
        {
            PlayerPrefs.SetInt("VinhDanhTotal", 0);
            PlayerPrefs.Save();
        }

        SceneManager.LoadScene(ShopSceneName);
    }

    private void ResolveReferences()
    {
        if (settingsPanel == null)
        {
            settingsPanel = FindByName("SettingsPanel");
        }

        if (introVideoPanel == null)
        {
            introVideoPanel = FindByName("IntroVideoPanel");
        }

        if (introVideoPlayer == null)
        {
            introVideoPlayer = FindFirstObjectByType<VideoPlayer>(FindObjectsInactive.Include);
        }

        if (volumeSlider == null)
        {
            volumeSlider = FindFirstObjectByType<Slider>(FindObjectsInactive.Include);
        }

        if (backgroundMusic == null)
        {
            backgroundMusic = FindFirstObjectByType<AudioSource>(FindObjectsInactive.Include);
        }
    }

    private void NormalizeMenuLayout()
    {
        UiSceneNormalizer.NormalizeScene("MainMenuCanvas");
        ConfigureBackground();
        ConfigureTitle();
        ConfigureMenuButton("PLAY", new Vector2(0f, 70f), "PLAY");
        ConfigureMenuButton("SETTINGS", new Vector2(0f, -85f), "SETTING");
        NormalizeSettingsPanel();
    }

    private void ConfigureBackground()
    {
        GameObject background = FindByName("Image");
        if (background == null)
        {
            return;
        }

        RectTransform rect = background.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        Image image = background.GetComponent<Image>();
        if (image != null)
        {
            image.preserveAspect = false;
            image.raycastTarget = false;
        }
    }

    private void ConfigureTitle()
    {
        GameObject titleObject = FindByName("TITLE");
        if (titleObject == null)
        {
            return;
        }

        RectTransform rect = titleObject.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -175f);
            rect.sizeDelta = new Vector2(1480f, 220f);
            rect.localScale = Vector3.one;
        }

        TMP_Text label = titleObject.GetComponent<TMP_Text>();
        if (label != null)
        {
            label.text = "THANH GIONG";
            label.fontSize = 142f;
            label.enableAutoSizing = false;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
        }
    }

    private void ConfigureMenuButton(string objectName, Vector2 position, string labelText)
    {
        GameObject buttonObject = FindByName(objectName);
        if (buttonObject == null)
        {
            return;
        }

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(560f, 96f);
            rect.localScale = Vector3.one;
        }

        Image image = buttonObject.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0.43f, 0.19f, 0.08f, 0.42f);
            image.raycastTarget = true;
        }

        TMP_Text label = buttonObject.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = labelText;
            label.fontSize = 54f;
            label.enableAutoSizing = false;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;

            RectTransform labelRect = label.GetComponent<RectTransform>();
            if (labelRect != null)
            {
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                labelRect.localScale = Vector3.one;
            }
        }
    }

    private void NormalizeSettingsPanel()
    {
        if (settingsPanel == null)
        {
            return;
        }

        RectTransform rect = settingsPanel.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(900f, 620f);
            rect.localScale = Vector3.one;
        }
    }

    private void SetSettingsVisible(bool visible)
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(visible);
        }
    }

    private void SetIntroVideoVisible(bool visible)
    {
        if (introVideoPanel != null)
        {
            introVideoPanel.SetActive(visible);
        }
    }

    private void ApplyMuteState()
    {
        AudioListener.volume = isMuted ? 0f : currentVolume;

        if (muteButtonImage == null)
        {
            return;
        }

        if (isMuted && soundOffSprite != null)
        {
            muteButtonImage.sprite = soundOffSprite;
        }
        else if (!isMuted && soundOnSprite != null)
        {
            muteButtonImage.sprite = soundOnSprite;
        }
    }

    private static GameObject FindByName(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Transform transform in transforms)
        {
            if (transform.name == objectName)
            {
                return transform.gameObject;
            }
        }

        return null;
    }
}
