using UnityEngine;

public class CutsceneTrigger : MonoBehaviour
{
    [Tooltip("Kéo cục StoryCutsceneManager mà bạn muốn chạy vào đây")]
    public StoryCutsceneManager cutsceneToPlay;

    [Tooltip("Đánh dấu nếu bạn chỉ muốn Cutscene này chạy đúng 1 lần duy nhất")]
    public bool playOnlyOnce = true;

    private bool _hasPlayed = false;

    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra xem có phải Player (Thánh Gióng) bước vào vùng này không
        if (other.CompareTag("Player"))
        {
            if (playOnlyOnce && _hasPlayed)
                return;

            if (cutsceneToPlay != null)
            {
                cutsceneToPlay.PlayCutscene();
                _hasPlayed = true;
                Debug.Log("Đã kích hoạt chạy Cutscene từ Trigger!");
            }
        }
    }
}
