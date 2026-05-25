using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class MouseParallaxUI : MonoBehaviour
{
    [Header("Parallax Settings")]
    [Tooltip("Mức độ di chuyển theo chiều ngang (trái/phải)")]
    public float movementX = 20f;
    
    [Tooltip("Mức độ di chuyển theo chiều dọc (lên/xuống)")]
    public float movementY = 20f;
    
    [Tooltip("Độ mượt của chuyển động. Số càng nhỏ càng trễ, càng lớn càng nhanh.")]
    public float smoothing = 5f;

    [Tooltip("Bật nếu muốn hướng di chuyển ngược lại với hướng chuột")]
    public bool invertDirection = true;

    private RectTransform rectTransform;
    private Vector2 startPosition;

    private void Start()
    {
        // Lấy component RectTransform (UI)
        rectTransform = GetComponent<RectTransform>();
        
        // Lưu lại vị trí ban đầu
        startPosition = rectTransform.anchoredPosition;
    }

    private void Update()
    {
        // Lấy tọa độ chuột hiện tại
        Vector2 mousePos = Input.mousePosition;

        // Tính toán độ lệch của chuột so với tâm màn hình (Chuẩn hóa về khoảng -1 đến 1)
        float normalizedX = (mousePos.x / Screen.width) * 2f - 1f;
        float normalizedY = (mousePos.y / Screen.height) * 2f - 1f;

        // Đảo ngược hướng nếu cần
        int dir = invertDirection ? -1 : 1;

        // Tính toán vị trí mục tiêu dựa trên độ lệch
        Vector2 targetPosition = new Vector2(
            startPosition.x + (normalizedX * movementX * dir),
            startPosition.y + (normalizedY * movementY * dir)
        );

        // Nội suy mượt mà (Lerp) từ vị trí hiện tại đến vị trí mục tiêu
        rectTransform.anchoredPosition = Vector2.Lerp(
            rectTransform.anchoredPosition, 
            targetPosition, 
            Time.deltaTime * smoothing
        );
    }
}
