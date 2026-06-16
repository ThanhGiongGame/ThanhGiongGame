using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MenuButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        ConfigureButtonColors();
    }

    private void OnEnable()
    {
        transform.localScale = Vector3.one;
    }

    private void OnDisable()
    {
        transform.localScale = Vector3.one;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = Vector3.one;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = Vector3.one;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.localScale = Vector3.one;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.localScale = Vector3.one;
    }

    private void ConfigureButtonColors()
    {
        if (button == null)
        {
            return;
        }

        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.92f, 0.35f, 1f);
        colors.pressedColor = new Color(1f, 0.74f, 0.22f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.55f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }
}
