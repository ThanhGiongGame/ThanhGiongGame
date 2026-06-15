using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private Camera shopCamera;
    [SerializeField] private Camera equipmentCamera;

    private void Start()
    {
        ShowShop();
    }

    public void ShowShop()
    {
        shopCamera.gameObject.SetActive(true);
        equipmentCamera.gameObject.SetActive(false);
    }

    public void ShowEquipment()
    {
        shopCamera.gameObject.SetActive(false);
        equipmentCamera.gameObject.SetActive(true);
    }

    public void ChangeGameplayScene()
    {
        SceneManager.LoadScene("SampleScene");
    }
}