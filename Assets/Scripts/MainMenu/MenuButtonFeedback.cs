using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MenuButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float hoverScale = 1.04f;
    [SerializeField] private float pressedScale = 0.98f;

    private Button button;
    private Vector3 baseScale;

    private void Awake()
    {
        button = GetComponent<Button>();
        baseScale = transform.localScale;
        ConfigureButtonColors();
    }

    private void OnEnable()
    {
        if (baseScale == Vector3.zero)
        {
            baseScale = transform.localScale;
        }

        transform.localScale = baseScale;
    }

    private void OnDisable()
    {
        transform.localScale = baseScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsInteractable())
        {
            transform.localScale = baseScale * hoverScale;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = baseScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsInteractable())
        {
            transform.localScale = baseScale * pressedScale;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.localScale = IsInteractable() ? baseScale * hoverScale : baseScale;
    }

    private bool IsInteractable()
    {
        return button != null && button.IsInteractable();
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
