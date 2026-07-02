using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CameraManager : MonoBehaviour
{
    private const string GameplaySceneName = "SampleScene";
    private const string Map2SceneName = "map2";

    [SerializeField] private Camera shopCamera;
    [SerializeField] private Camera equipmentCamera;

    private ShopUI shopUI;
    private MapSelectionUI mapSelectionUI;

    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        CleanupGameplayPauseUi();

        ResolveSceneCameras();
        shopUI = FindFirstObjectByType<ShopUI>(FindObjectsInactive.Include);
        SetupMapSelection();
        ShowShop();
    }

    public void ShowShop()
    {
        ResolveSceneCameras();
        SetCameraActive(shopCamera, true);
        SetCameraActive(equipmentCamera, false);
        SetTabVisuals("BtnShop");
        
    }

    public void ShowEquipment()
    {
        ResolveSceneCameras();
        SetCameraActive(shopCamera, false);
        SetCameraActive(equipmentCamera, true);
        SetTabVisuals("BtnEquipment");
    }

    public void ChangeGameplayScene()
    {
        Time.timeScale = 1f;
        string sceneName = PlayerPrefs.GetInt("SelectedMap", 0) == 1 ? Map2SceneName : GameplaySceneName;
        SceneManager.LoadScene(sceneName);
    }

    private void NormalizeShopLayout()
    {
        UiSceneNormalizer.NormalizeScene("ShopCanvas");

        ConfigureCanvasRoot();
        ConfigurePanel("LeftPanel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -96f), new Vector2(640f, 690f), new Vector2(0f, 1f));
        ConfigurePanel("RightPannel", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -96f), new Vector2(520f, 690f), new Vector2(1f, 1f));
        ConfigureCurrencyDisplay();
        ConfigurePanelHeader();
        ConfigureCategoryFilterBar();
        ConfigureRightPanelText();
        ConfigureBuyButton();
        EnsureMapButton();
        ConfigureBottomButton("BtnShop", new Vector2(-525f, 32f), "CỬA HÀNG");
        ConfigureBottomButton("BtnEquipment", new Vector2(-175f, 32f), "TRANG BỊ");
        ConfigureBottomButton("BtnGame", new Vector2(175f, 32f), "VÀO TRẬN");
        ConfigureBottomButton("BtnMap", new Vector2(525f, 32f), "BẢN ĐỒ");
        StretchScrollView();
    }

    private void EnsureMapButton()
    {
        GameObject mapButton = FindByName("BtnMap");
        if (mapButton != null)
        {
            return;
        }

        GameObject sourceButton = FindByName("BtnShop");
        if (sourceButton == null)
        {
            return;
        }

        mapButton = Instantiate(sourceButton, sourceButton.transform.parent);
        mapButton.name = "BtnMap";

        Button button = mapButton.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OpenMapSelection);
        }
    }

    private void ConfigureCanvasRoot()
    {
        Canvas canvas = FindShopCanvas();
        if (canvas == null)
        {
            return;
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = null;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform rect = canvas.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localScale = Vector3.one;
            rect.anchoredPosition = Vector2.zero;
        }
    }

    private void ConfigureRightPanelText()
    {
        Transform rightPanel = FindByName("RightPannel")?.transform;

        ConfigureText("ItemName", rightPanel, new Vector2(0f, -70f), new Vector2(452f, 58f), 34f, Color.white, FontStyles.Bold);
        ConfigureText("ItemCategory", rightPanel, new Vector2(-120f, -126f), new Vector2(190f, 34f), 20f, new Color(0.88f, 0.92f, 1f, 1f), FontStyles.Bold);
        ConfigureText("ItemPrice", rightPanel, new Vector2(120f, -126f), new Vector2(210f, 34f), 20f, new Color(1f, 0.84f, 0.22f, 1f), FontStyles.Bold);
        ConfigureText("ItemDescription", rightPanel, new Vector2(0f, -192f), new Vector2(452f, 84f), 21f, Color.white, FontStyles.Normal);
        ConfigureText("ItemStatus", rightPanel, new Vector2(0f, -296f), new Vector2(452f, 58f), 28f, new Color(0.38f, 1f, 0.42f, 1f), FontStyles.Bold);
    }

    private void ConfigureBuyButton()
    {
        GameObject buttonObject = FindByName("Buy");
        if (buttonObject == null)
        {
            return;
        }

        Transform rightPanel = FindByName("RightPannel")?.transform;
        if (rightPanel != null)
        {
            buttonObject.transform.SetParent(rightPanel, false);
        }

        ConfigureRect(buttonObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 38f), new Vector2(420f, 66f), new Vector2(0.5f, 0f));

        Image image = buttonObject.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0.28f, 0.58f, 0.28f, 0.96f);
            image.raycastTarget = true;
        }

        TMP_Text label = buttonObject.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.fontSize = 28f;
            label.enableAutoSizing = false;
            label.enableWordWrapping = false;
            label.alignment = TextAlignmentOptions.Center;
            label.margin = Vector4.zero;
            label.raycastTarget = false;
            Stretch(label.GetComponent<RectTransform>(), 10f, 4f);
        }
    }

    private void ConfigureBottomButton(string objectName, Vector2 position, string labelText)
    {
        GameObject buttonObject = FindByName(objectName);
        if (buttonObject == null)
        {
            return;
        }

        Canvas canvas = FindShopCanvas();
        if (canvas != null)
        {
            buttonObject.transform.SetParent(canvas.transform, false);
        }

        ConfigureRect(buttonObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), position, new Vector2(310f, 62f), new Vector2(0.5f, 0f));

        Image image = buttonObject.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0.62f, 0.88f, 0.9f, 0.98f);
            image.raycastTarget = true;
        }

        TMP_Text label = buttonObject.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = labelText;
            label.fontSize = 25f;
            label.enableAutoSizing = false;
            label.enableWordWrapping = false;
            label.alignment = TextAlignmentOptions.Center;
            label.margin = Vector4.zero;
            label.raycastTarget = false;
            Stretch(label.GetComponent<RectTransform>(), 8f, 4f);
        }

        buttonObject.transform.SetAsLastSibling();
    }

    private void ConfigurePanel(string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2 pivot)
    {
        GameObject panel = FindByName(objectName);
        if (panel == null)
        {
            return;
        }

        Canvas canvas = FindShopCanvas();
        if (canvas != null)
        {
            panel.transform.SetParent(canvas.transform, false);
        }

        ConfigureRect(panel, anchorMin, anchorMax, position, size, pivot);

        Image image = panel.GetComponent<Image>();
        if (image == null)
        {
            image = panel.AddComponent<Image>();
        }

        if (image != null)
        {
            image.color = new Color(0.03f, 0.035f, 0.035f, 0.93f);
            image.raycastTarget = false;
        }

        Outline outline = panel.GetComponent<Outline>();
        if (outline == null)
        {
            outline = panel.AddComponent<Outline>();
        }

        outline.effectColor = new Color(1f, 0.86f, 0.42f, 0.45f);
        outline.effectDistance = new Vector2(2f, -2f);
        panel.transform.SetAsFirstSibling();
    }

    private void ConfigurePanelHeader()
    {
        Transform leftPanel = FindByName("LeftPanel")?.transform;
        if (leftPanel == null)
        {
            return;
        }

        TMP_Text title = EnsurePanelText(leftPanel, "ShopPanelTitle");
        ConfigureTextRect(title, new Vector2(0f, -38f), new Vector2(560f, 52f), 34f, new Color(1f, 0.95f, 0.72f, 1f), FontStyles.Bold, TextAlignmentOptions.Center);

        TMP_Text hint = EnsurePanelText(leftPanel, "ShopPanelHint");
        ConfigureTextRect(hint, new Vector2(0f, -82f), new Vector2(560f, 34f), 18f, new Color(0.84f, 0.88f, 0.88f, 1f), FontStyles.Normal, TextAlignmentOptions.Center);
    }

    private void ConfigureCurrencyDisplay()
    {
        Canvas canvas = FindShopCanvas();
        if (canvas == null)
        {
            return;
        }

        TMP_Text currencyText = EnsurePanelText(canvas.transform, "CurrencyText");
        ConfigureTextRect(currencyText, new Vector2(40f, -24f), new Vector2(480f, 52f), 30f, new Color(1f, 0.92f, 0.12f, 1f), FontStyles.Bold, TextAlignmentOptions.Left);

        RectTransform rect = currencyText.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(40f, -24f);
        }
    }

    private void ConfigureCategoryFilterBar()
    {
        Transform leftPanel = FindByName("LeftPanel")?.transform;
        if (leftPanel == null)
        {
            return;
        }

        RectTransform bar = EnsureRect(leftPanel, "CategoryFilterBar");
        ConfigureRect(bar.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -136f), new Vector2(560f, 48f), new Vector2(0.5f, 1f));

        CreateCategoryButton(bar.transform, "CategoryAll", "TẤT CẢ", -210f);
        CreateCategoryButton(bar.transform, "CategoryWeapon", "WEAPON", -70f);
        CreateCategoryButton(bar.transform, "CategoryMount", "MOUNT", 70f);
        CreateCategoryButton(bar.transform, "CategoryUltimate", "ULT", 210f);
    }

    private void CreateCategoryButton(Transform parent, string objectName, string labelText, float x)
    {
        RectTransform rect = EnsureRect(parent, objectName);
        ConfigureRect(rect.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(128f, 42f), new Vector2(0.5f, 0.5f));

        Image image = rect.GetComponent<Image>();
        if (image == null)
        {
            image = rect.gameObject.AddComponent<Image>();
        }

        image.color = new Color(0.9f, 0.82f, 0.58f, 0.98f);
        image.raycastTarget = true;

        Button button = rect.GetComponent<Button>();
        if (button == null)
        {
            button = rect.gameObject.AddComponent<Button>();
        }

        button.targetGraphic = image;

        TMP_Text label = EnsurePanelText(rect.transform, "Label");
        ConfigureTextRect(label, Vector2.zero, new Vector2(120f, 36f), 16f, new Color(0.08f, 0.08f, 0.08f, 1f), FontStyles.Bold, TextAlignmentOptions.Center);
        label.text = labelText;
    }

    private void ConfigureText(string objectName, Transform parent, Vector2 position, Vector2 size, float fontSize, Color color, FontStyles style)
    {
        GameObject textObject = FindByName(objectName);
        if (textObject == null)
        {
            return;
        }

        if (parent != null)
        {
            textObject.transform.SetParent(parent, false);
        }

        ConfigureRect(textObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), position, size, new Vector2(0.5f, 1f));

        TMP_Text label = textObject.GetComponent<TMP_Text>();
        if (label == null)
        {
            label = textObject.GetComponentInChildren<TMP_Text>(true);
        }

        if (label == null)
        {
            return;
        }

        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = color;
        label.enableAutoSizing = false;
        label.enableWordWrapping = false;
        label.alignment = TextAlignmentOptions.Center;
        label.margin = Vector4.zero;
        label.raycastTarget = false;
    }

    private TMP_Text EnsurePanelText(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null && existing.TryGetComponent(out TMP_Text existingText))
        {
            return existingText;
        }

        GameObject textObject = new GameObject(objectName, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        return textObject.AddComponent<TextMeshProUGUI>();
    }

    private RectTransform EnsureRect(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null && existing.TryGetComponent(out RectTransform existingRect))
        {
            return existingRect;
        }

        GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        return rectObject.GetComponent<RectTransform>();
    }

    private void ConfigureTextRect(TMP_Text label, Vector2 position, Vector2 size, float fontSize, Color color, FontStyles style, TextAlignmentOptions alignment)
    {
        if (label == null)
        {
            return;
        }

        ConfigureRect(label.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), position, size, new Vector2(0.5f, 1f));
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = color;
        label.alignment = alignment;
        label.enableAutoSizing = false;
        label.enableWordWrapping = false;
        label.margin = Vector4.zero;
        label.raycastTarget = false;
    }

    private void StretchScrollView()
    {
        GameObject scrollView = FindByName("Scroll View");
        if (scrollView != null)
        {
            Transform leftPanel = FindByName("LeftPanel")?.transform;
            if (leftPanel != null)
            {
                scrollView.transform.SetParent(leftPanel, false);
            }

            ConfigureRect(scrollView, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            Stretch(scrollView.GetComponent<RectTransform>(), 24f, 198f, 24f, 24f);

            Image image = scrollView.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0f, 0f, 0f, 0.22f);
                image.raycastTarget = false;
            }
        }

        GameObject content = FindByName("Content");
        if (content != null)
        {
            RectTransform rect = content.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localScale = Vector3.one;
                rect.anchoredPosition = Vector2.zero;
            }

            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = 14f;
                layout.padding.left = 8;
                layout.padding.right = 8;
                layout.padding.top = 8;
                layout.padding.bottom = 8;
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;
            }
        }
    }

    private void SetTabVisuals(string activeButtonName)
    {
        SetButtonColor("BtnShop", activeButtonName == "BtnShop");
        SetButtonColor("BtnEquipment", activeButtonName == "BtnEquipment");
        SetButtonColor("BtnGame", false);
        SetButtonColor("BtnMap", false);
    }

    private void SetButtonColor(string objectName, bool active)
    {
        GameObject buttonObject = FindByName(objectName);
        if (buttonObject == null)
        {
            return;
        }

        Image image = buttonObject.GetComponent<Image>();
        if (image != null)
        {
            image.color = active
                ? new Color(1f, 0.72f, 0.14f, 0.98f)
                : new Color(0.62f, 0.88f, 0.9f, 0.98f);
        }
    }

    private static void ConfigureRect(GameObject target, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2 pivot)
    {
        RectTransform rect = target.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void Stretch(RectTransform rect, float horizontalPadding, float verticalPadding)
    {
        Stretch(rect, horizontalPadding, verticalPadding, horizontalPadding, verticalPadding);
    }

    private static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
        rect.localScale = Vector3.one;
    }

    private void ResolveSceneCameras()
    {
        shopCamera ??= FindCameraByName("ShopCamera");
        equipmentCamera ??= FindCameraByName("EquipmentCamera");
    }

    private static Camera FindCameraByName(string objectName)
    {
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Camera camera in cameras)
        {
            if (camera.name == objectName)
            {
                return camera;
            }
        }

        return null;
    }

    private static void SetCameraActive(Camera targetCamera, bool active)
    {
        if (targetCamera != null)
        {
            targetCamera.gameObject.SetActive(active);
            targetCamera.enabled = active;

            AudioListener listener = targetCamera.GetComponent<AudioListener>();
            if (listener != null)
            {
                listener.enabled = active;
            }
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

    private static Canvas FindShopCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Canvas fallback = null;

        foreach (Canvas canvas in canvases)
        {
            if (canvas.name == "ShopCanvas")
            {
                return canvas;
            }

            if (
                fallback == null
                && canvas.isRootCanvas
                && canvas.name != "GameplayPauseCanvas"
                && canvas.name != "MainMenuCanvas"
            )
            {
                fallback = canvas;
            }
        }

        return fallback;
    }

    private static void CleanupGameplayPauseUi()
    {
        DestroyIfExists("GameplayPauseCanvas");
        DestroyIfExists("GameplayPauseMenu");
    }

    private static void DestroyIfExists(string objectName)
    {
        GameObject target = FindByName(objectName);
        if (target != null)
        {
            Destroy(target);
        }
    }

    private void SetupMapSelection()
    {
        Canvas canvas = FindShopCanvas();
        if (canvas == null)
        {
            return;
        }

        mapSelectionUI = canvas.GetComponent<MapSelectionUI>();
        if (mapSelectionUI == null)
        {
            mapSelectionUI = canvas.gameObject.AddComponent<MapSelectionUI>();
        }

        mapSelectionUI.Initialize(() => { });
        mapSelectionUI.Hide();
    }

    public void OpenMapSelection()
    {
        if (mapSelectionUI != null)
        {
            mapSelectionUI.Show();
        }
    }
}
