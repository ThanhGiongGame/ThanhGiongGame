using UnityEngine;

public class EquipmentPreviewManager : MonoBehaviour
{
    [Header("Anchors")]
    [SerializeField] private Transform playerAnchor;
    [SerializeField] private Transform horseAnchor;

    [Header("Prefabs")]
    [SerializeField] private GameObject[] characterPrefabs;
    [SerializeField] private GameObject[] horsePrefabs;

    private GameObject currentCharacter;
    private GameObject currentHorse;

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
            return;

        SpawnCharacter();
        SpawnHorse();
    }

    public void Clear()
    {
        if (currentCharacter != null)
            Destroy(currentCharacter);

        if (currentHorse != null)
            Destroy(currentHorse);

        currentCharacter = null;
        currentHorse = null;
    }

    #region Character

    private void SpawnCharacter()
    {
        string equippedCharacter =
            PlayerPrefs.GetString("EquippedCharacter", "Character_Tier1");

        int index = equippedCharacter switch
        {
            "Character_Tier1" => 0,
            "Character_Tier2" => 1,
            "Character_Tier3" => 2,
            "Character_Tier4" => 3,
            _ => 0
        };

        currentCharacter =
            Instantiate(characterPrefabs[index], playerAnchor);

        currentCharacter.transform.localPosition = Vector3.zero;
        currentCharacter.transform.localRotation = Quaternion.identity;

        ApplyWeaponPose();
    }

    private void ApplyWeaponPose()
    {
        if (currentCharacter == null)
            return;

        Animator animator = currentCharacter.GetComponent<Animator>();

        if (animator == null)
            return;

        string weapon = PlayerPrefs.GetString("EquippedWeapon", "Weapon_Tier1");

        float normalizedTime = weapon switch
        {
            "Weapon_Tier1" => 0.666666f,
            "Weapon_Tier2" => 0.999999f,
            "Weapon_Tier3" => 1f,
            "Weapon_Tier4" => 0f,
            _ => 0f
        };

        animator.speed = 1f;
        animator.Play("PlayerEquip", 0, normalizedTime);
        animator.Update(0f);
        animator.speed = 0f;
    }
    #endregion

    #region Horse

    private void SpawnHorse()
    {
        string equippedHorse =
            PlayerPrefs.GetString("EquippedHorse", "Horse_Tier1");

        int index = equippedHorse switch
        {
            "Horse_Tier1" => 0,
            "Horse_Tier2" => 1,
            "Horse_Tier3" => 2,
            "Horse_Tier4" => 3,
            _ => 0
        };

        currentHorse =
            Instantiate(horsePrefabs[index], horseAnchor);

        currentHorse.transform.localPosition = Vector3.zero;
        currentHorse.transform.localRotation = Quaternion.identity;
    }

    #endregion

    #region Preview

    public void SetPreviewVisible(bool visible)
    {
        GameObject root = FindByName("EquipmentRoot");

        if (root != null)
        {
            root.SetActive(visible);
            return;
        }

        if (playerAnchor != null)
            playerAnchor.gameObject.SetActive(visible);

        if (horseAnchor != null)
            horseAnchor.gameObject.SetActive(visible);
    }

    public static void HideAllEquipmentPreviews()
    {
        GameObject root = FindByName("EquipmentRoot");

        if (root != null)
            root.SetActive(false);
    }

    private static GameObject FindByName(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Transform t in transforms)
        {
            if (t.name == objectName)
                return t.gameObject;
        }

        return null;
    }

    #endregion
}