using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Cần dùng UI để nhận diện Slider
using UnityEngine.Video; // Thêm thư viện Video

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject settingsPanel;
    
    [Header("Intro Video")]
    public GameObject introVideoPanel;
    public VideoPlayer introVideoPlayer;
    public GameObject mainMenuContainer; // Kéo cụm UI chứa nút Play, Settings... vào đây để ẩn đi

    [Header("Audio Settings")]
    public AudioSource backgroundMusic; // Kéo Audio Source phát nhạc nền vào đây để tắt khi xem video
    public Image muteButtonImage;
    public Sprite soundOnSprite;
    public Sprite soundOffSprite;
    
    [Tooltip("Kéo Slider âm lượng vào đây")]
    public Slider volumeSlider; // <--- KHAI BÁO THÊM THANH TRƯỢT
    
    private bool isMuted = false;
    private float currentVolume = 1f;

    private void Start()
    {
        // 1. Tắt bảng setting khi mới vào game
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        // 2. Lấy dữ liệu đã lưu (Volume). Mặc định luôn bật âm thanh (isMuted = false)
        isMuted = false;
        PlayerPrefs.SetInt("IsMuted", 0); // Xóa trạng thái tắt âm thanh cũ nếu có
        currentVolume = PlayerPrefs.GetFloat("Volume", 1f); // Mặc định là 1 (Max)

        // 3. Cài đặt thanh trượt (nếu có)
        if (volumeSlider != null)
        {
            volumeSlider.value = currentVolume;
            // Lắng nghe sự kiện kéo thanh trượt -> gọi hàm SetVolume
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        // Tắt panel video ban đầu (nếu có)
        if (introVideoPanel != null)
        {
            introVideoPanel.SetActive(false);
        }
        
        // Lắng nghe sự kiện video chạy xong
        if (introVideoPlayer != null)
        {
            introVideoPlayer.loopPointReached += OnIntroVideoFinished;
        }

        ApplyMuteState();
    }

    public void PlayGame()
    {
        // Thay vì LoadScene ngay, ta hiển thị và bật video trước
        if (introVideoPanel != null && introVideoPlayer != null)
        {
            introVideoPanel.SetActive(true);
            introVideoPlayer.Play();
            
            // Ẩn Menu đi khi video bắt đầu phát
            if (mainMenuContainer != null)
            {
                mainMenuContainer.SetActive(false);
            }
            
            // Tắt nhạc nền
            if (backgroundMusic != null)
            {
                backgroundMusic.Stop();
            }
        }
        else
        {
            // Nếu không có thiết lập video, thì vào thẳng game
            LoadNextScene();
        }
    }

    // Hàm dành cho nút Bỏ qua (Skip)
    public void SkipVideo()
    {
        LoadNextScene();
    }

    // Hàm tự động gọi khi video chạy xong
    private void OnIntroVideoFinished(VideoPlayer vp)
    {
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        SceneManager.LoadScene("SampleScene"); 
    }

    // Nút ấn bật/tắt Setting
    public void ToggleSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
    }

    // Nút tắt bật hẳn âm thanh (Mute)
    public void ToggleMute()
    {
        isMuted = !isMuted;
        ApplyMuteState();
        
        PlayerPrefs.SetInt("IsMuted", isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    // ==========================================
    // HÀM NÀY MỚI THÊM ĐỂ CHỈNH ÂM LƯỢNG BẰNG THANH TRƯỢT
    // ==========================================
    public void SetVolume(float volume)
    {
        currentVolume = volume;
        
        // Nếu đang KHÔNG BỊ MUTE thì mới đổi âm thanh thực tế
        if (!isMuted)
        {
            AudioListener.volume = currentVolume;
        }

        // Lưu lại mức âm lượng mới
        PlayerPrefs.SetFloat("Volume", currentVolume);
        PlayerPrefs.Save();
    }

    private void ApplyMuteState()
    {
        // Nếu bị mute thì volume = 0, nếu không thì lấy giá trị của thanh trượt
        AudioListener.volume = isMuted ? 0f : currentVolume;

        // Thay đổi icon của nút
        if (muteButtonImage != null)
        {
            if (isMuted && soundOffSprite != null)
            {
                muteButtonImage.sprite = soundOffSprite;
            }
            else if (!isMuted && soundOnSprite != null)
            {
                muteButtonImage.sprite = soundOnSprite;
            }
        }
    }
}
