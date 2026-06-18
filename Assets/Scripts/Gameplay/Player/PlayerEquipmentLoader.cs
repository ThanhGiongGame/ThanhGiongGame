using System;
using UnityEngine;

public class PlayerEquipmentLoader : MonoBehaviour
{
    public Transform characterRoot;
    public Transform horseRoot;

    public GameObject characterTier1;
    public GameObject characterTier2;

    public GameObject horseTier1;
    public GameObject horseTier2;

    private void Start()
    {
        LoadCharacter();
        LoadHorse();
    }

    void LoadCharacter()
    {
        string equipped =
            PlayerPrefs.GetString(
                "EquippedCharacter",
                "Character_Tier1"
            );

        GameObject prefab = characterTier1;
        Debug.Log("EquippedCharacter = " + equipped);

        switch (equipped)
        {
            case "Character_Tier1":
                prefab = characterTier1;
                break;
            case "Character_Tier2":
                prefab = characterTier2;
                break;
        }

        Instantiate(
            prefab,
            characterRoot
        );
    }

    void LoadHorse()
    {
        // "xóa con ngựa đi!" — Ngựa đã bị xóa theo yêu cầu của người dùng.
        Debug.Log("LoadHorse: Horse disabled by user request.");
    }
}