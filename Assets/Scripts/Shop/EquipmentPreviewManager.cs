using UnityEngine;

public class EquipmentPreviewManager : MonoBehaviour
{
    [SerializeField] private Transform playerAnchor;
    [SerializeField] private Transform weaponAnchor;
    [SerializeField] private Transform horseAnchor;
    [SerializeField] private Transform ultimateAnchor;
    [SerializeField]
    private GameObject[] weaponPrefabs;
    private GameObject currentWeapon;
    private GameObject currentHorse;
    private GameObject currentUltimate;
    private void OnEnable()
    {
        InventoryManager.OnEquipmentChanged += Refresh;
    }

    private void OnDisable()
    {
        InventoryManager.OnEquipmentChanged -= Refresh;
    }
    public void Refresh()
    {
        SetPreviewVisible(true);
        Clear();

        if (InventoryManager.Instance == null)
        {
            return;
        }

        SpawnWeapon();
        SpawnHorse();
        SpawnUltimate();
    }

    public void Clear()
    {
        if (currentWeapon != null)
        {
            currentWeapon.SetActive(false);
            Destroy(currentWeapon);
        }

        if (currentHorse != null)
        {
            currentHorse.SetActive(false);
            Destroy(currentHorse);
        }

        if (currentUltimate != null)
        {
            currentUltimate.SetActive(false);
            Destroy(currentUltimate);
        }

        currentWeapon = null;
        currentHorse = null;
        currentUltimate = null;
    }

    public void SetPreviewVisible(bool visible)
    {
        GameObject root = FindByName("EquipmentRoot");
        if (root != null)
        {
            root.SetActive(visible);
            return;
        }

        SetAnchorVisible(weaponAnchor, visible);
        SetAnchorVisible(horseAnchor, visible);
        SetAnchorVisible(ultimateAnchor, visible);
    }

    public static void HideAllEquipmentPreviews()
    {
        GameObject root = FindByName("EquipmentRoot");
        if (root != null)
        {
            root.SetActive(false);
        }
    }

    private static void SetAnchorVisible(Transform anchor, bool visible)
    {
        if (anchor == null)
        {
            return;
        }

        anchor.gameObject.SetActive(visible);
    }

    private static GameObject FindByName(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform transform in transforms)
        {
            if (transform.name == objectName)
            {
                return transform.gameObject;
            }
        }

        return null;
    }

    private void SpawnWeapon()
    {
        if (weaponAnchor == null)
        {
            return;
        }

        string weaponId =
            EquipmentManager.EquippedWeapon;
        
        GameItemData weapon =
            InventoryManager.Instance
                .allItems
                .Find(x => x.id == weaponId);
        if (weapon == null)
            return;

        if (weapon.id == "Character_Tier1" && weaponPrefabs.Length > 0 && weaponPrefabs[0] != null)
        {
            currentWeapon =
                Instantiate(
                    weaponPrefabs[0],
                    weaponAnchor
                );
        }

        if (weapon.id == "Character_Tier2" && weaponPrefabs.Length > 1 && weaponPrefabs[1] != null)
        {
            currentWeapon =
                Instantiate(
                    weaponPrefabs[1],
                    weaponAnchor
                );
        }

        if (currentWeapon != null)
        {
            currentWeapon.transform.localPosition =
                Vector3.zero;
        }
    }
    private void SpawnHorse()
    {
        string weaponId =
            EquipmentManager.EquippedHorse;

        GameItemData weapon =
            InventoryManager.Instance
                .allItems
                .Find(x => x.id == weaponId);

        if (weapon == null)
            return;
        if (weapon.prefab == null || horseAnchor == null)
            return;

        currentHorse =
            Instantiate(
                weapon.prefab,
                horseAnchor
            );

        currentHorse.transform.localPosition =
            Vector3.zero;
    }
    private void SpawnUltimate()
    {
        string weaponId =
            EquipmentManager.EquippedUltimate;

        GameItemData weapon =
            InventoryManager.Instance
                .allItems
                .Find(x => x.id == weaponId);

        if (weapon == null)
            return;
        if (weapon.prefab == null || ultimateAnchor == null)
            return;

        currentUltimate =
            Instantiate(
                weapon.prefab,
                ultimateAnchor
            );

        currentUltimate.transform.localPosition =
            Vector3.zero;
    }
}
