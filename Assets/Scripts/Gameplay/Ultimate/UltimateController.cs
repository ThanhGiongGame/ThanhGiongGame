using System.Collections;
using UnityEngine;

public class UltimateController : MonoBehaviour
{
    public static UltimateController Instance;

    [Header("References")]
    public Transform player;
    public PlayerEquipmentLoader equipmentLoader;
    public CameraController cameraController;

    [Header("Ultimate Prefabs")]
    public GameObject thunderKickPrefab;

    [Header("Timing")]
    public float startupTime = 1.5f;
    public float closeupTime = 0.8f;
    public float chargeTime = 0.5f;
    public float finishDelay = 0.5f;

    [Header("Camera")]
    public Vector3 cinematicOffset =
        new Vector3(0f, 4f, -6f);

    public Vector3 closeupOffset =
        new Vector3(0f, 2f, -2f);

    public float cinematicPitch = 20f;
    public float closeupPitch = 10f;

    private bool _isCasting;
    private Vector3 _cachedDirection;

    public bool IsCasting => _isCasting;

    private void Awake()
    {
        Instance = this;
    }

    public void TryUseUltimate()
    {
        if (_isCasting)
            return;

        string equippedUltimate =
            PlayerPrefs.GetString(
                "EquippedUltimate",
                ""
            );
        Debug.Log("Attempting to use ultimate: " + equippedUltimate);
        switch (equippedUltimate)
        {
            case "Ultimate_Tier1":

                _cachedDirection =
                    player.forward;

                StartCoroutine(
                    ThunderKickRoutine()
                );
                break;
        }
    }

    private IEnumerator ThunderKickRoutine()
    {
        _isCasting = true;

        //------------------------------------------------
        // Pause world
        //------------------------------------------------

        Enemy.GlobalFreeze = true;
        WaveSpawner.PauseSpawn = true;

        PlayerController playerController =
            player.GetComponent<PlayerController>();

        if (playerController != null)
        {
            playerController.IsPerformingSkill = true;
        }

        //------------------------------------------------
        // Get animators
        //------------------------------------------------

        Animator characterAnimator =
            equipmentLoader.characterRoot
                .GetComponentInChildren<Animator>();

        Animator horseAnimator =
            equipmentLoader.horseRoot
                .GetComponentInChildren<Animator>();
        Debug.Log("Character Animator: " + (characterAnimator != null));
        //------------------------------------------------
        // Stage 1
        // Hero pose + horse run
        //------------------------------------------------

        if (cameraController != null)
        {
            cameraController.SetCinematicView(
                cinematicOffset,
                cinematicPitch
            );
        }

        characterAnimator?.SetTrigger(
            "UltimateAttack"
        );

        horseAnimator?.SetTrigger(
            "UltimateAttack"
        );

        yield return new WaitForSeconds(
            startupTime
        );

        //------------------------------------------------
        // Stage 2
        // Face zoom
        //------------------------------------------------

        if (cameraController != null)
        {
            cameraController.SetCinematicView(
                closeupOffset,
                closeupPitch
            );

            cameraController.Shake(
                0.2f,
                0.4f
            );
        }

        yield return new WaitForSeconds(
            closeupTime
        );

        //------------------------------------------------
        // Stage 3
        // Energy charge
        //------------------------------------------------

        if (cameraController != null)
        {
            cameraController.Shake(
                0.6f,
                chargeTime
            );
        }

        yield return new WaitForSeconds(
            chargeTime
        );

        //------------------------------------------------
        // Stage 4
        // Final attack
        //------------------------------------------------

        if (cameraController != null)
        {
            cameraController.ResetView();
        }

        SpawnThunderKick();

        yield return new WaitForSeconds(
            finishDelay
        );

        //------------------------------------------------
        // Resume world
        //------------------------------------------------

        Enemy.GlobalFreeze = false;
        WaveSpawner.PauseSpawn = false;

        if (playerController != null)
        {
            playerController.IsPerformingSkill = false;
        }

        _isCasting = false;
    }

    private void SpawnThunderKick()
    {
        if (thunderKickPrefab == null)
            return;

        Vector3 spawnPos =
            player.position +
            (_cachedDirection * 3f);

        spawnPos.y = 0f;

        Instantiate(
            thunderKickPrefab,
            spawnPos,
            Quaternion.LookRotation(
                _cachedDirection
            )
        );
    }
}