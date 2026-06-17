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
        Clear();

        SpawnWeapon();
        SpawnHorse();
        SpawnUltimate();
    }
    private void Clear()
    {
        if (currentWeapon != null)
            Destroy(currentWeapon);

        if (currentHorse != null)
            Destroy(currentHorse);

        if (currentUltimate != null)
            Destroy(currentUltimate);

        currentWeapon = null;
        currentHorse = null;
        currentUltimate = null;
    }
    private void SpawnWeapon()
    {
        string weaponId =
            EquipmentManager.EquippedWeapon;
        
        GameItemData weapon =
            InventoryManager.Instance
                .allItems
                .Find(x => x.id == weaponId);
        Debug.Log("Item id:" +  weaponId);
        Debug.Log("weaponPrefab" + weaponPrefabs[0]);
        if (weapon == null)
            return;
        if (weapon.id == "Character_Tier1")
        currentWeapon =
            Instantiate(
                weaponPrefabs[0],
                weaponAnchor
            );
        if (weapon.id == "Character_Tier2")
            currentWeapon =
                Instantiate(
                    weaponPrefabs[1],
                    weaponAnchor
                );

        currentWeapon.transform.localPosition =
            Vector3.zero;
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

        currentUltimate =
            Instantiate(
                weapon.prefab,
                ultimateAnchor
            );

        currentUltimate.transform.localPosition =
            Vector3.zero;
    }
}