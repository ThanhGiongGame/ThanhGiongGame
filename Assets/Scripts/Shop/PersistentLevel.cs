using System;
using UnityEngine;

public static class PersistentLevel
{
    public static event Action OnLevelChanged;

    public static int Current
    {
        get => PlayerPrefs.GetInt("PersistentPlayerLevel", 1);
        private set
        {
            PlayerPrefs.SetInt("PersistentPlayerLevel", value);
            PlayerPrefs.Save();
            OnLevelChanged?.Invoke();
        }
    }

    public static void SetLevel(int level)
    {
        Current = Mathf.Max(1, level);
    }

    public static void AddLevel(int amount)
    {
        SetLevel(Current + amount);
    }

    public static void ResetLevel()
    {
        SetLevel(1);
    }
}
