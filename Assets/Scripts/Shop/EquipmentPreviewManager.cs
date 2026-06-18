using UnityEngine;

public class EquipmentPreviewManager : MonoBehaviour
{
    [SerializeField] private Transform playerAnchor;
    [SerializeField] private Transform weaponAnchor;
    [SerializeField] private Transform horseAnchor;
    [SerializeField]
    private GameObject[] characterPrefabs;
    [SerializeField]
    private GameObject[] horsePrefabs;
    [SerializeField]
    private GameObject[] weaponPrefabs;
    private GameObject currentWeapon;
    private GameObject currentHorse;
    private GameObject currentCharacter;
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

        SpawnCharacter();
        SpawnHorse();
        SpawnWeapon();
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

        if (currentCharacter != null)
            Destroy(currentCharacter);

        currentWeapon = null;
        currentHorse = null;
        currentCharacter = null;
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
        SetAnchorVisible(playerAnchor, visible);
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

    private void SpawnCharacter()
    {
        string equipped =
            PlayerPrefs.GetString(
                "EquippedCharacter",
                "Character_Tier1"
            );

        if (equipped == null)
            return;
        switch (equipped)
        {
            case "Character_Tier1":
                currentCharacter =
    Instantiate(
        characterPrefabs[0],
        playerAnchor
    );
                break;
            case "Character_Tier2":
                currentCharacter =
    Instantiate(
        characterPrefabs[1],
        playerAnchor
    );
                break;
            case "Character_Tier3":
                currentCharacter = Instantiate(characterPrefabs[2], playerAnchor);
                break;
            case "Character_Tier4":
                currentCharacter = Instantiate(characterPrefabs[3], playerAnchor);
                break;

        }
        currentCharacter.transform.localPosition = Vector3.zero;
    }
    private void SpawnHorse()
    {
        string equipped =
            PlayerPrefs.GetString(
                "EquippedHorse",
                "Horse_Tier1"
            );

        switch (equipped)
        {
            case "Horse_Tier1":
                currentHorse = Instantiate(horsePrefabs[0], horseAnchor);
                break;
            case "Horse_Tier2":
                currentHorse = Instantiate(horsePrefabs[1], horseAnchor);
                break;
            case "Horse_Tier3":
                currentHorse = Instantiate(horsePrefabs[2], horseAnchor);
                break;
            case "Horse_Tier4":
                currentHorse = Instantiate(horsePrefabs[3], horseAnchor);
                break;

        }

        currentHorse.transform.localPosition =
            Vector3.zero;
    }
    private void SpawnWeapon()
    {
        string equipped =
            PlayerPrefs.GetString(
                "EquippedWeapon",
                "Weapon_Tier1"
            );

        switch (equipped)
        {
            case "Weapon_Tier1":
                currentWeapon = Instantiate(weaponPrefabs[0], weaponAnchor);
                break;
            case "Weapon_Tier2":
                currentWeapon = Instantiate(weaponPrefabs[1], weaponAnchor);
                break;
            case "Weapon_Tier3":
                currentWeapon = Instantiate(weaponPrefabs[2], weaponAnchor);
                break;

        }

        currentWeapon.transform.localPosition =
            Vector3.zero;
    }
}
