using UnityEngine;

public class PreviewManager : MonoBehaviour
{
    [SerializeField]
    private Transform previewRoot;

    private GameObject currentPreview;

    public void Show(GameItemData item)
    {
        Clear();

        if (item == null)
            return;

        if (item.prefab == null)
            return;

        if (previewRoot == null)
            return;

        currentPreview =
            Instantiate(
                item.prefab,
                previewRoot.position,
                previewRoot.rotation
            );
    }
    private void Update()
    {
        if (currentPreview == null)
            return;

        currentPreview.transform.Rotate(
            0f,
            30f * Time.deltaTime,
            0f
        );
    }
    public void Clear()
    {
        if (currentPreview != null)
        {
            currentPreview.SetActive(false);
            Destroy(currentPreview);
        }

        currentPreview = null;
    }
}
