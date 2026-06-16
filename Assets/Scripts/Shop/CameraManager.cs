using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CameraManager : MonoBehaviour
{
    private const string GameplaySceneName = "SampleScene";

    [SerializeField] private Camera shopCamera;
    [SerializeField] private Camera equipmentCamera;

    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        NormalizeShopLayout();
        ShowShop();
    }

    public void ShowShop()
    {
        SetCameraActive(shopCamera, true);
        SetCameraActive(equipmentCamera, false);
    }

    public void ShowEquipment()
    {
        SetCameraActive(shopCamera, false);
        SetCameraActive(equipmentCamera, true);
    }

    public void ChangeGameplayScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameplaySceneName);
    }

    private void NormalizeShopLayout()
    {
        UiSceneNormalizer.NormalizeScene("ShopCanvas");

        ConfigureCanvasRoot();
        ConfigurePanel("LeftPanel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(56f, -92f), new Vector2(520f, 410f), new Vector2(0f, 1f));
        ConfigurePanel("RightPannel", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-56f, -92f), new Vector2(540f, 410f), new Vector2(1f, 1f));
        ConfigureRightPanelText();
        ConfigureBuyButton();
        ConfigureBottomButton("BtnShop", new Vector2(-330f, 58f), "Shop");
        ConfigureBottomButton("BtnEquipment", new Vector2(0f, 58f), "Equip");
        ConfigureBottomButton("BtnGame", new Vector2(330f, 58f), "Play");
        StretchScrollView();
    }

    private void ConfigureCanvasRoot()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
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
        ConfigureText("ItemName", new Vector2(0f, -48f), new Vector2(470f, 58f), 38f, Color.white, FontStyles.Bold);
        ConfigureText("ItemCategory", new Vector2(-150f, -94f), new Vector2(220f, 34f), 22f, Color.white, FontStyles.Bold);
        ConfigureText("ItemPrice", new Vector2(150f, -94f), new Vector2(220f, 34f), 20f, new Color(1f, 0.92f, 0.12f, 1f), FontStyles.Bold);
        ConfigureText("ItemDescription", new Vector2(0f, -135f), new Vector2(470f, 46f), 20f, Color.white, FontStyles.Normal);
        ConfigureText("ItemStatus", new Vector2(0f, -210f), new Vector2(460f, 58f), 34f, new Color(0.2f, 1f, 0.25f, 1f), FontStyles.Bold);
    }

    private void ConfigureBuyButton()
    {
        GameObject buttonObject = FindByName("Buy");
        if (buttonObject == null)
        {
            return;
        }

        ConfigureRect(buttonObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -310f), new Vector2(380f, 66f), new Vector2(0.5f, 0.5f));

        Image image = buttonObject.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0.35f, 0.68f, 0.28f, 0.88f);
            image.raycastTarget = true;
        }

        TMP_Text label = buttonObject.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.fontSize = 30f;
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

        ConfigureRect(buttonObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), position, new Vector2(270f, 58f), new Vector2(0.5f, 0f));

        Image image = buttonObject.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0.58f, 0.92f, 0.95f, 0.9f);
            image.raycastTarget = true;
        }

        TMP_Text label = buttonObject.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = labelText;
            label.fontSize = 28f;
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

        ConfigureRect(panel, anchorMin, anchorMax, position, size, pivot);

        Image image = panel.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0.08f, 0.08f, 0.08f, 0.42f);
            image.raycastTarget = false;
        }
    }

    private void ConfigureText(string objectName, Vector2 position, Vector2 size, float fontSize, Color color, FontStyles style)
    {
        GameObject textObject = FindByName(objectName);
        if (textObject == null)
        {
            return;
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

    private void StretchScrollView()
    {
        GameObject scrollView = FindByName("Scroll View");
        if (scrollView != null)
        {
            ConfigureRect(scrollView, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            Stretch(scrollView.GetComponent<RectTransform>(), 14f, 14f);
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
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = new Vector2(horizontalPadding, verticalPadding);
        rect.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);
        rect.localScale = Vector3.one;
    }

    private static void SetCameraActive(Camera targetCamera, bool active)
    {
        if (targetCamera != null)
        {
            targetCamera.gameObject.SetActive(active);
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
