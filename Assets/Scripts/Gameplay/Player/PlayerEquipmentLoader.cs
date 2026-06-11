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
                "Character_Tier2"
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
        string equipped =
            PlayerPrefs.GetString(
                "EquippedHorse",
                "Horse_Tier1"
            );

        GameObject prefab = horseTier1;

        switch (equipped)
        {
            case "Horse_Tier1":
                prefab = horseTier1;
                break;
            case "Horse_Tier2":
                prefab = horseTier2;
                break;
        }
        Debug.Log(
    "EquippedHorse = " +
    PlayerPrefs.GetString(
        "EquippedHorse"
    )
);
        Instantiate(
            prefab,
            horseRoot
        );
        if (equipped == "Horse_Tier2")
        {
            Vector3 pos = characterRoot.localPosition;
            pos.y = 1f;
            characterRoot.localPosition = pos;
            Vector3 posChild = characterRoot.GetChild(0).position;
            Debug.Log("posChild = " + posChild);
        }

    }
}