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
            rect.sizeDelta = new Vector2(480f, 64f);
            rect.localScale = Vector3.one;
        }

        ConfigureLabel(itemName, 24f, TextAlignmentOptions.MidlineLeft);
        ConfigureLabel(itemPrice, 22f, TextAlignmentOptions.MidlineRight);

        if (button != null)
        {
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0.9f, 0.9f, 0.86f, 0.72f);
                image.raycastTarget = true;
            }
        }
    }

    private static void ConfigureLabel(TMP_Text label, float fontSize, TextAlignmentOptions alignment)
    {
        if (label == null)
        {
            return;
        }

        label.fontSize = fontSize;
        label.enableAutoSizing = false;
        label.enableWordWrapping = false;
        label.alignment = alignment;
        label.margin = Vector4.zero;
        label.raycastTarget = false;
        label.rectTransform.localScale = Vector3.one;
    }
}
