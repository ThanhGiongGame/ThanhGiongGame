using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class CutsceneStep
{
    [Tooltip("Tên gợi nhớ cho bước này (ví dụ: Gióng chạy lại gần cha mẹ)")]
    public string stepName;

    [Header("Animation Settings")]
    [Tooltip("Animator của nhân vật cần chạy Animation trong bước này")]
    public Animator targetAnimator;
    [Tooltip("Tên Trigger để kích hoạt Animation")]
    public string animationTriggerName;
    [Tooltip("Thời gian chờ trước khi chạy Animation/Thoại")]
    public float waitBeforeStep = 0f;

    [Header("Voice & Dialogue Settings")]
    [Tooltip("AudioSource để phát âm thanh lồng tiếng")]
    public AudioSource voiceAudioSource;
    [Tooltip("File âm thanh lồng tiếng cho câu thoại này")]
    public AudioClip voiceClip;
    [Tooltip("Dữ liệu câu thoại (Sẽ hiển thị lên UI)")]
    public DialogueData dialogueData;

    [Header("End Step Settings")]
    [Tooltip("Có chờ người chơi bấm hết thoại rồi mới sang bước tiếp theo không?")]
    public bool waitForDialogueFinish = true;
    [Tooltip("Chờ thêm bao nhiêu giây sau khi thoại và animation xong mới sang bước tiếp theo")]
    public float waitAfterStep = 0.5f;
    
    [Tooltip("Sự kiện gọi ngay khi bước này bắt đầu (VD: Bật tắt Object, v.v.)")]
    public UnityEvent OnStepStart;
}

/// <summary>
/// Quản lý một đoạn chuyển cảnh (Cutscene) gồm nhiều bước liên tiếp.
/// Hỗ trợ chạy Animation, phát Voice Over và hiển thị Dialogue UI cùng lúc.
/// </summary>
public class StoryCutsceneManager : MonoBehaviour
{
    [Header("Cutscene Configuration")]
    [Tooltip("Tự động chạy cutscene này khi Scene bắt đầu?")]
    public bool playOnStart = false;
    
    [Tooltip("Nhạc nền riêng cho đoạn chuyển cảnh này (Tùy chọn)")]
    public AudioClip cutsceneMusic;
    [Tooltip("AudioSource để phát nhạc nền")]
    public AudioSource musicSource;
    
    [Tooltip("Danh sách các bước trong đoạn chuyển cảnh này")]
    public CutsceneStep[] cutsceneSteps;

    [Header("Events")]
    public UnityEvent OnCutsceneStart;
    public UnityEvent OnCutsceneComplete;

    private Coroutine _cutsceneRoutine;

    private void Start()
    {
        if (playOnStart)
        {
            PlayCutscene();
        }
    }

    /// <summary>
    /// Bắt đầu chạy Cutscene
    /// </summary>
    public void PlayCutscene()
    {
        if (_cutsceneRoutine != null)
        {
            StopCoroutine(_cutsceneRoutine);
        }
        _cutsceneRoutine = StartCoroutine(CutsceneRoutine());
    }

    /// <summary>
    /// Dừng Cutscene giữa chừng (Nếu cần)
    /// </summary>
    public void StopCutscene()
    {
        if (_cutsceneRoutine != null)
        {
            StopCoroutine(_cutsceneRoutine);
            _cutsceneRoutine = null;
        }
    }

    private IEnumerator CutsceneRoutine()
    {
        OnCutsceneStart?.Invoke();

        if (cutsceneMusic != null && musicSource != null)
        {
            musicSource.clip = cutsceneMusic;
            musicSource.loop = true;
            musicSource.Play();
        }

        foreach (var step in cutsceneSteps)
        {
            // 1. Chờ trước khi bắt đầu step
            if (step.waitBeforeStep > 0f)
            {
                yield return new WaitForSecondsRealtime(step.waitBeforeStep);
            }

            step.OnStepStart?.Invoke();

            // 2. Kích hoạt Animation
            if (step.targetAnimator != null && !string.IsNullOrEmpty(step.animationTriggerName))
            {
                // Set UnscaledTime để animation vẫn chạy khi Time.timeScale = 0 (do DialogueUI gây ra)
                step.targetAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
                step.targetAnimator.SetTrigger(step.animationTriggerName);
            }

            // 3. Phát Lồng Tiếng (Voice Over)
            if (step.voiceAudioSource != null && step.voiceClip != null)
            {
                step.voiceAudioSource.clip = step.voiceClip;
                step.voiceAudioSource.Play();
            }

            // 4. Hiển thị Dialogue
            if (step.dialogueData != null && DialogueUI.Instance != null)
            {
                DialogueUI.Instance.StartDialogue(step.dialogueData);

                if (step.waitForDialogueFinish)
                {
                    // DialogueUI.Instance.StartDialogue sẽ set Time.timeScale = 0f
                    // Khi người chơi bấm qua hết thoại, DialogueUI sẽ EndDialogue và set lại Time.timeScale = 1f
                    while (Time.timeScale == 0f || DialogueUI.Instance.gameObject.transform.GetChild(0).gameObject.activeInHierarchy)
                    {
                        yield return null;
                    }
                }
            }

            // 5. Chờ Voice Over chạy xong (nếu cần thiết và chưa xong)
            if (step.voiceAudioSource != null && step.voiceAudioSource.isPlaying)
            {
                // Đợi cho đến khi audio ngừng phát
                while (step.voiceAudioSource.isPlaying)
                {
                    yield return null;
                }
            }

            // 6. Chờ sau khi step kết thúc
            if (step.waitAfterStep > 0f)
            {
                yield return new WaitForSecondsRealtime(step.waitAfterStep);
            }
        }

        if (musicSource != null && musicSource.clip == cutsceneMusic)
        {
            musicSource.Stop();
        }

        OnCutsceneComplete?.Invoke();
        _cutsceneRoutine = null;
    }
}
