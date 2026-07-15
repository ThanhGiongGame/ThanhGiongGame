using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Điều khiển UI hộp thoại kiểu Genshin Impact.
/// Gắn vào DialogueCanvas GameObject.
/// </summary>
public class DialogueUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI dialogueBodyText;
    [SerializeField] private GameObject continueIndicator; // icon diamond nhấp nháy

    [Header("Typewriter Settings")]
    [SerializeField] private float typewriterSpeed = 0.04f;  // giây mỗi ký tự
    [SerializeField] private float fastSpeed = 0.01f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    // --- State ---
    private DialogueLine[] _lines;
    private int _currentIndex;
    private bool _isTyping;
    private bool _skipRequested;
    private Coroutine _typewriterCoroutine;

    // Singleton đơn giản
    public static DialogueUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        dialoguePanel.SetActive(false);
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.ignoreListenerPause = true; // Important because timeScale = 0
    }

    // ------------------------------------------------------------------ //
    //  Public API
    // ------------------------------------------------------------------ //

    /// <summary>Bắt đầu một chuỗi đoạn thoại.</summary>
    public void StartDialogue(DialogueData data)
    {
        if (data == null || data.lines == null || data.lines.Length == 0) return;
        _lines = data.lines;
        _currentIndex = 0;
        dialoguePanel.SetActive(true);
        Time.timeScale = 0f;   // Tạm dừng game khi thoại
        ShowLine(_lines[_currentIndex]);
    }

    public System.Action onDialogueEnd;

    /// <summary>Đóng hộp thoại và tiếp tục game.</summary>
    public void EndDialogue()
    {
        StopAllCoroutines();
        if (audioSource != null) audioSource.Stop();
        dialoguePanel.SetActive(false);
        Time.timeScale = 1f;

        if (onDialogueEnd != null)
        {
            var callback = onDialogueEnd;
            onDialogueEnd = null;
            callback.Invoke();
        }
    }

    // ------------------------------------------------------------------ //
    //  Input (gọi từ Update hoặc New Input System)
    // ------------------------------------------------------------------ //

    private void Update()
    {
        if (!dialoguePanel.activeSelf) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E))
        {
            HandleAdvance();
        }
    }

    private void HandleAdvance()
    {
        if (_isTyping)
        {
            // Nhấn lần 1: hiện ngay toàn bộ text
            _skipRequested = true;
        }
        else
        {
            // Nhấn lần 2: sang câu tiếp
            _currentIndex++;
            if (_currentIndex < _lines.Length)
                ShowLine(_lines[_currentIndex]);
            else
                EndDialogue();
        }
    }

    // ------------------------------------------------------------------ //
    //  Internal
    // ------------------------------------------------------------------ //

    private void ShowLine(DialogueLine line)
    {
        // Tên nhân vật
        if (!string.IsNullOrEmpty(line.characterName))
        {
            characterNameText.text = line.characterName;
            characterNameText.gameObject.SetActive(true);
        }
        else
        {
            characterNameText.gameObject.SetActive(false);
        }

        continueIndicator.SetActive(false);

        if (_typewriterCoroutine != null)
            StopCoroutine(_typewriterCoroutine);
            
        if (!string.IsNullOrEmpty(line.voiceClipPath))
        {
            AudioClip clip = Resources.Load<AudioClip>(line.voiceClipPath);
            if (clip != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
            }
            else
            {
                Debug.LogWarning("Không tìm thấy Audio: " + line.voiceClipPath);
            }
        }
        else
        {
            audioSource.Stop();
        }

        _typewriterCoroutine = StartCoroutine(TypewriterEffect(line.text));
    }

    private IEnumerator TypewriterEffect(string fullText)
    {
        _isTyping = true;
        _skipRequested = false;
        dialogueBodyText.text = "";

        for (int i = 0; i < fullText.Length; i++)
        {
            if (_skipRequested)
            {
                dialogueBodyText.text = fullText;
                break;
            }
            dialogueBodyText.text += fullText[i];
            yield return new WaitForSecondsRealtime(typewriterSpeed); // dùng Realtime vì timeScale = 0
        }

        _isTyping = false;
        continueIndicator.SetActive(true);
        StartCoroutine(BlinkIndicator());
    }

    private IEnumerator BlinkIndicator()
    {
        // Nhấp nháy icon tiếp tục
        while (!_isTyping && continueIndicator.activeSelf)
        {
            continueIndicator.SetActive(false);
            yield return new WaitForSecondsRealtime(0.4f);
            continueIndicator.SetActive(true);
            yield return new WaitForSecondsRealtime(0.4f);
        }
    }
}
