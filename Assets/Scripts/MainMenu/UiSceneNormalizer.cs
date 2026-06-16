using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class UiSceneNormalizer
{
    private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

    public static void NormalizeScene(string preferredCanvasName = null)
    {
        EnsureEventSystem();
        NormalizeCanvases(preferredCanvasName);
        NormalizeRaycasts();
        AddButtonFeedbacks();
    }

    public static void NormalizeCanvases(string preferredCanvasName = null)
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        bool renamedPreferredCanvas = false;

        foreach (Canvas canvas in canvases)
        {
            if (
                !renamedPreferredCanvas
                && !string.IsNullOrWhiteSpace(preferredCanvasName)
                && canvas.isRootCanvas
                && ShouldRenameCanvas(canvas.gameObject.name)
            )
            {
                canvas.gameObject.name = preferredCanvasName;
                renamedPreferredCanvas = true;
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
            canvas.pixelPerfect = false;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }
        }
    }

    public static void NormalizeRaycasts()
    {
        HashSet<Graphic> selectableTargets = new HashSet<Graphic>();
        Selectable[] selectables = Object.FindObjectsByType<Selectable>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Selectable selectable in selectables)
        {
            if (selectable.targetGraphic != null)
            {
                selectableTargets.Add(selectable.targetGraphic);
            }
        }

        Graphic[] graphics = Object.FindObjectsByType<Graphic>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Graphic graphic in graphics)
        {
            graphic.raycastTarget = ShouldReceiveRaycast(graphic, selectableTargets);
        }
    }

    public static void AddButtonFeedbacks()
    {
        Button[] buttons = Object.FindObjectsByType<Button>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Button button in buttons)
        {
            if (button.GetComponent<MenuButtonFeedback>() == null)
            {
                button.gameObject.AddComponent<MenuButtonFeedback>();
            }
        }
    }

    public static void EnsureEventSystem()
    {
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>(
            FindObjectsInactive.Include
        );

        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        if (eventSystem.GetComponent<BaseInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }
    }

    private static bool ShouldReceiveRaycast(Graphic graphic, HashSet<Graphic> selectableTargets)
    {
        if (selectableTargets.Contains(graphic))
        {
            return true;
        }

        if (graphic.GetComponent<Selectable>() != null)
        {
            return true;
        }

        if (graphic.GetComponentInParent<ScrollRect>(true) != null && graphic.GetComponent<Mask>() != null)
        {
            return true;
        }

        return false;
    }

    private static bool ShouldRenameCanvas(string canvasName)
    {
        return canvasName == "Canvas"
            || canvasName == "Canva"
            || canvasName == "MENUGAME";
    }
}
