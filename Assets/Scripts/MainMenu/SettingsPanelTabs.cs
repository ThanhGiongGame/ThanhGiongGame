using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelTabs : MonoBehaviour
{
    [SerializeField] private GameObject settingsPage;
    [SerializeField] private GameObject controlsPage;
    [SerializeField] private Image settingsTabImage;
    [SerializeField] private Image controlsTabImage;

    private Button settingsTabButton;
    private Button controlsTabButton;

    private readonly Color selectedColor = new Color(0.35f, 0.55f, 0.78f, 0.42f);
    private readonly Color normalColor = new Color(0.12f, 0.13f, 0.16f, 0.3f);

    private void Awake()
    {
        ResolveReferences();
        WireButtons();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        WireButtons();
        ShowSettings();
    }

    public void ShowSettings()
    {
        if (settingsPage != null)
        {
            settingsPage.SetActive(true);
        }

        if (controlsPage != null)
        {
            controlsPage.SetActive(false);
        }

        SetTabColors(true);
    }

    public void ShowControls()
    {
        if (settingsPage != null)
        {
            settingsPage.SetActive(false);
        }

        if (controlsPage != null)
        {
            controlsPage.SetActive(true);
        }

        SetTabColors(false);
    }

    private void SetTabColors(bool settingsSelected)
    {
        if (settingsTabImage != null)
        {
            settingsTabImage.color = settingsSelected ? selectedColor : normalColor;
        }

        if (controlsTabImage != null)
        {
            controlsTabImage.color = settingsSelected ? normalColor : selectedColor;
        }
    }

    private void ResolveReferences()
    {
        if (settingsPage == null)
        {
            Transform target = transform.Find("SettingsPage");
            if (target != null)
            {
                settingsPage = target.gameObject;
            }
        }

        if (controlsPage == null)
        {
            Transform target = transform.Find("ControlsPage");
            if (target != null)
            {
                controlsPage = target.gameObject;
            }
        }

        if (settingsTabButton == null || settingsTabImage == null)
        {
            Transform target = transform.Find("SettingsTabButton");
            if (target != null)
            {
                settingsTabButton = target.GetComponent<Button>();
                settingsTabImage = target.GetComponent<Image>();
            }
        }

        if (controlsTabButton == null || controlsTabImage == null)
        {
            Transform target = transform.Find("ControlsTabButton");
            if (target != null)
            {
                controlsTabButton = target.GetComponent<Button>();
                controlsTabImage = target.GetComponent<Image>();
            }
        }
    }

    private void WireButtons()
    {
        if (settingsTabButton != null)
        {
            settingsTabButton.onClick.RemoveListener(ShowSettings);
            settingsTabButton.onClick.AddListener(ShowSettings);
            settingsTabButton.interactable = true;
        }

        if (controlsTabButton != null)
        {
            controlsTabButton.onClick.RemoveListener(ShowControls);
            controlsTabButton.onClick.AddListener(ShowControls);
            controlsTabButton.interactable = true;
        }
    }
}
