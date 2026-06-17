using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemUI : MonoBehaviour
{
    [SerializeField]
    private TMP_Text itemName;
    [SerializeField]
    private TMP_Text itemPrice;
    [SerializeField]
    private Button button;

    private GameItemData item;

    public void Setup(
        GameItemData data,
        Action<GameItemData> onClick
    )
    {
        NormalizeLayout();

        item = data;

        itemName.text =
            data.displayName;
        itemPrice.text =
            data.cost + " VD";

        button.onClick.RemoveAllListeners();

        button.onClick.AddListener(() =>
        {
            onClick?.Invoke(item);
        });
    }

    private void NormalizeLayout()
    {
        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(560f, 72f);
            rect.localScale = Vector3.one;
        }

        ConfigureLabel(itemName, 25f, TextAlignmentOptions.MidlineLeft);
                ConfigureLabel(itemPrice, 22f, TextAlignmentOptions.MidlineRight);
        ConfigureChildRect(itemName, new Vector2(0f, 0f), new Vector2(0.68f, 1f), new Vector2(18f, 0f), Vector2.zero);
        ConfigureChildRect(itemPrice, new Vector2(0.68f, 0f), Vector2.one, new Vector2(-18f, 0f), Vector2.zero);
        if (button != null)
        {
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0.88f, 0.78f, 0.52f, 0.98f);
                image.raycastTarget = true;
            }

            RectTransform buttonRect = button.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                buttonRect.anchorMin = Vector2.zero;
                buttonRect.anchorMax = Vector2.one;
                buttonRect.offsetMin = Vector2.zero;
                buttonRect.offsetMax = Vector2.zero;
                buttonRect.localScale = Vector3.one;
            }

            Outline outline = button.GetComponent<Outline>();
            if (outline == null)
            {
                outline = button.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = new Color(0f, 0f, 0f, 0.36f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }
    }

    private static void ConfigureLabel(TMP_Text label, float fontSize, TextAlignmentOptions alignment)
    {
        if (label == null)
        {
            return;
        }

        label.fontSize = fontSize;
        label.color = new Color(0.08f, 0.08f, 0.08f, 1f);
        label.fontStyle = FontStyles.Bold;
        label.enableAutoSizing = false;
        label.enableWordWrapping = false;
        label.alignment = alignment;
        label.margin = Vector4.zero;
        label.raycastTarget = false;
        label.rectTransform.localScale = Vector3.one;
    }

    private static void ConfigureChildRect(TMP_Text label, Vector2 anchorMin, Vector2 anchorMax, Vector2 offset, Vector2 size)
    {
        if (label == null)
        {
            return;
        }

        RectTransform rect = label.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = offset;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }
}
