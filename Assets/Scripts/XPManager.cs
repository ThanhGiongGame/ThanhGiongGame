using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Singleton that tracks XP and player level.
/// Fires OnLevelUp for each level gained; queues multiple level-ups.
/// </summary>
public class XPManager : MonoBehaviour
{
    public static XPManager Instance { get; private set; }

    [Header("XP Settings")]
    public float baseXPPerLevel = 100f;
    public float xpScaling      = 1.3f;

    // ---- Events ----
    /// <summary>Fired once per level gained. LevelUpUI handles freezing time.</summary>
    public static event Action OnLevelUp;
    /// <summary>Fired whenever XP changes. Args: currentXP, xpNeeded, level.</summary>
    public static event Action<float, float, int> OnXPChanged;

    private float _currentXP;
    private int   _currentLevel      = 1;
    private int   _pendingLevelUps   = 0;
    private bool  _processingLevelUp = false;

    public float CurrentXP      => _currentXP;
    public int   CurrentLevel   => _currentLevel;
    public float XPForNextLevel => Mathf.Floor(baseXPPerLevel * Mathf.Pow(_currentLevel, xpScaling));

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>Call to award XP to the player.</summary>
    public void AddXP(float amount)
    {
        _currentXP += amount;

        // Accumulate all levels first, then raise events one at a time
        while (_currentXP >= XPForNextLevel)
        {
            _currentXP -= XPForNextLevel;
            _currentLevel++;
            _pendingLevelUps++;
        }

        OnXPChanged?.Invoke(_currentXP, XPForNextLevel, _currentLevel);

        if (_pendingLevelUps > 0 && !_processingLevelUp)
            ProcessNextLevelUp();
    }

    private void ProcessNextLevelUp()
    {
        if (_pendingLevelUps <= 0) { _processingLevelUp = false; return; }
        _processingLevelUp = true;
        _pendingLevelUps--;
        OnLevelUp?.Invoke();
        // LevelUpUI calls ResumeFromLevelUp() after the player picks an upgrade
    }

    /// <summary>Called by LevelUpUI after the player picks an upgrade card.</summary>
    public void ResumeFromLevelUp()
    {
        if (_pendingLevelUps > 0)
            StartCoroutine(DelayedNextLevelUp());
        else
            _processingLevelUp = false;
    }

    private IEnumerator DelayedNextLevelUp()
    {
        yield return new WaitForSecondsRealtime(0.3f);
        ProcessNextLevelUp();
    }
}
