using UnityEngine;

public static class EquipmentManager
{
    public static string EquippedHorse =>
        PlayerPrefs.GetString("EquippedHorse", "Horse_Default");

    public static string EquippedWeapon =>
        PlayerPrefs.GetString("EquippedWeapon", "Sword_Default");

    public static string EquippedUltimate =>
        PlayerPrefs.GetString("EquippedUltimate", "HorseCharge");
}