using System.Collections;
using UnityEngine;

public class BGMManager : MonoBehaviour
{
    private static BGMManager instance;
    public static BGMManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<BGMManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("BGMManager");
                    instance = go.AddComponent<BGMManager>();
                }
            }
            return instance;
        }
    }

    [SerializeField] private AudioSource audioSource;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        InitializeAudioSource();
    }

    private void InitializeAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        if (audioSource == null)
        {
            GameObject bgMusicGO = GameObject.Find("BackgroundMusic");
            if (bgMusicGO != null)
            {
                audioSource = bgMusicGO.GetComponent<AudioSource>();
            }
        }
        if (audioSource == null)
        {
            audioSource = FindFirstObjectByType<AudioSource>();
        }
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.playOnAwake = false;
        }
    }

    public void FadeTo(string clipPath, float duration)
    {
        AudioClip newClip = Resources.Load<AudioClip>(clipPath);
        if (newClip == null)
        {
            Debug.LogError($"[BGMManager] Could not load AudioClip at Resources/{clipPath}");
            return;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeToCoroutine(newClip, duration));
    }

    private IEnumerator FadeToCoroutine(AudioClip newClip, float duration)
    {
        if (audioSource == null)
        {
            InitializeAudioSource();
        }

        if (audioSource == null)
        {
            yield break;
        }

        float originalVolume = audioSource.volume;
        // If volume is extremely low or 0, default to 0.5f for target
        float targetVolume = originalVolume > 0.05f ? originalVolume : 0.5f;

        // Fade Out current music
        if (audioSource.isPlaying && audioSource.clip != null)
        {
            float t = 0f;
            float startVol = audioSource.volume;
            while (t < duration * 0.5f)
            {
                t += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVol, 0f, t / (duration * 0.5f));
                yield return null;
            }
        }

        // Swap the clip
        audioSource.Stop();
        audioSource.clip = newClip;
        audioSource.Play();

        // Fade In new music
        float tIn = 0f;
        while (tIn < duration * 0.5f)
        {
            tIn += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, targetVolume, tIn / (duration * 0.5f));
            yield return null;
        }

        audioSource.volume = targetVolume;
        fadeCoroutine = null;
    }
}
