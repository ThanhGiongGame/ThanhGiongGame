using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class HorseLoader : MonoBehaviour
{
    [SerializeField]
    public GameObject horseTier1;
    public GameObject horseTier2;
    public GameObject horseTier3;
    public GameObject horseTier4;
    public float bonusHealth;
    public float bonusDamage;
    public float bonusSpeed;
    public Transform riderAnchor;
    private GameObject currentHorse;

    public void LoadHorse()
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
                bonusHealth += 0;
                bonusSpeed += 0;
                break;
            case "Horse_Tier2":
                prefab = horseTier2;
                bonusHealth -= 30;
                bonusSpeed += 10;
                break;
            case "Horse_Tier3":
                prefab = horseTier3;
                bonusHealth += 50;
                bonusSpeed += 5;

                break;
            case "Horse_Tier4":
                prefab = horseTier4;
                bonusHealth += 300;
                bonusSpeed += 8;
                break;
        }
        currentHorse =
        Instantiate(
            prefab,
            transform
        );
        Animator anim =
currentHorse.GetComponentInChildren<Animator>();

        PlayerController player =
            GetComponentInParent<PlayerController>();

        player.horseAnimator = anim;
        SkillSkyPlunge skillSkyPlunge =
            GetComponentInParent<SkillSkyPlunge>();
        skillSkyPlunge._horseAnimator = anim;

    }
}
