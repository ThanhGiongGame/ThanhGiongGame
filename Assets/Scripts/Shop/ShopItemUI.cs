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
}