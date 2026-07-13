using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CameraControlHint : MonoBehaviour
{
    private Text hintText;
    private PlayerController playerController;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForGameplayScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "map 1" && scene.name != "map2" && scene.name != "map3" && scene.name != "SampleScene")
        {
            return;
        }

        if (FindFirstObjectByType<CameraControlHint>(FindObjectsInactive.Include) != null)
        {
            return;
        }

        new GameObject("CameraControlHint").AddComponent<CameraControlHint>();
    }

    private void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        BuildUi();
    }

    private void Update()
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }

        if (hintText == null || playerController == null)
        {
            return;
        }

        hintText.text = "CHÉM: " + playerController.GetMountedAttackDirectionLabel()
            + "\nCHUỘT  ĐỔI HƯỚNG CHÉM\nWASD  LÁI NGỰA";
    }

    private void BuildUi()
    {
        GameObject canvasObject = new GameObject("CameraControlHintCanvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 700;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = new GameObject("HintPanel");
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.anchoredPosition = new Vector2(-36f, 36f);
        panelRect.sizeDelta = new Vector2(300f, 112f);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.04f, 0.04f, 0.05f, 0.72f);
        panelImage.raycastTarget = false;

        GameObject textObject = new GameObject("HintText");
        textObject.transform.SetParent(panel.transform, false);
        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 12f);
        textRect.offsetMax = new Vector2(-18f, -12f);

        hintText = textObject.AddComponent<Text>();
        hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hintText.fontSize = 22;
        hintText.fontStyle = FontStyle.Bold;
        hintText.alignment = TextAnchor.MiddleLeft;
        hintText.color = new Color(0.95f, 0.85f, 0.45f, 1f);
        hintText.raycastTarget = false;
    }
}
