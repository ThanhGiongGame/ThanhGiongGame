using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class RiderLoader : MonoBehaviour
{
    [SerializeField]
    public GameObject characterTier1;
    public GameObject characterTier2;
    public GameObject characterTier3;
    public GameObject characterTier4;
    public float bonusHealth;
    public float bonusDamage;
    public float bonusSpeed;

    private GameObject currentRider;
    private void Start()
    {
        LoadRider();
    }
    public void LoadRider()
    {
        string equipped =
            PlayerPrefs.GetString(
                "EquippedCharacter",
                "Character_Tier1"
            );

        GameObject prefab = characterTier1;

        switch (equipped)
        {
            case "Character_Tier1":
                prefab = characterTier1;
                bonusHealth += 0;
                bonusDamage += 0;
                break;
            case "Character_Tier2":
                prefab = characterTier2;
                bonusHealth += 40;
                bonusDamage += 10;
                break;
            case "Character_Tier3":
                prefab = characterTier3;
                bonusHealth += 100;
                bonusDamage += 30;
                break;
            case "Character_Tier4":
                prefab = characterTier4;
                bonusHealth += 300;
                bonusDamage += 50;
                break;

        }

        GameObject rider = Instantiate(
            prefab,
            transform
        );
        Animator anim =
    rider.GetComponentInChildren<Animator>();

        PlayerController player =
            GetComponentInParent<PlayerController>();

        player.riderAnimator = anim;


    }
}
