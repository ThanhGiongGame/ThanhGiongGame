using UnityEngine;
using UnityEngine.InputSystem; // Bắt buộc phải thêm dòng này để dùng Input System mới

public class MouseParallaxUI : MonoBehaviour
{
    [Header("Parallax Settings")]
    [Tooltip("Mức độ di chuyển theo chiều ngang (trái/phải)")]
    public float movementX = 50f;
    
    [Tooltip("Mức độ di chuyển theo chiều dọc (lên/xuống)")]
    public float movementY = 50f;
    
    [Tooltip("Độ mượt của chuyển động. Số càng nhỏ càng trễ, càng lớn càng nhanh.")]
    public float smoothing = 5f;

    [Tooltip("Bật nếu muốn hướng di chuyển ngược lại với hướng chuột")]
    public bool invertDirection = true;

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.localPosition;
    }

    private void Update()
    {
        // Kiểm tra xem chuột có đang kết nối/tồn tại không
        if (Mouse.current == null) return;

        // LẤY TỌA ĐỘ CHUỘT BẰNG HỆ THỐNG MỚI CỦA UNITY (Input System)
        Vector2 mousePos = Mouse.current.position.ReadValue();

        // Tính toán độ lệch của chuột so với tâm màn hình
        float normalizedX = (mousePos.x / Screen.width) * 2f - 1f;
        float normalizedY = (mousePos.y / Screen.height) * 2f - 1f;

        int dir = invertDirection ? -1 : 1;

        // Tính toán vị trí mục tiêu
        Vector3 targetPosition = new Vector3(
            startPosition.x + (normalizedX * movementX * dir),
            startPosition.y + (normalizedY * movementY * dir),
            startPosition.z
        );

        // Di chuyển thật mượt mà
        transform.localPosition = Vector3.Lerp(
            transform.localPosition, 
            targetPosition, 
            Time.deltaTime * smoothing
        );
    }
}
