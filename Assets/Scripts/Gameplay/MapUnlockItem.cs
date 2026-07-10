using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class MapUnlockItem : MonoBehaviour
{
    private bool isCollected = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!isCollected && other.CompareTag("Player"))
        {
            isCollected = true;
            StartCoroutine(UnlockSequence());
        }
    }

    private void Update()
    {
        if (!isCollected)
        {
            // Hover and rotate
            transform.Rotate(Vector3.up * 90f * Time.deltaTime, Space.World);
            float newY = transform.position.y + Mathf.Sin(Time.time * 2f) * 0.005f;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }

    private IEnumerator UnlockSequence()
    {
        // 1. Freeze gameplay
        Time.timeScale = 0f;
        
        // 2. Disable player collision
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 3. Float to camera center
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 startPos = transform.position;
            Vector3 targetPos = cam.transform.position + cam.transform.forward * 3.5f + cam.transform.up * -0.5f;
            Quaternion startRot = transform.rotation;
            Quaternion targetRot = Quaternion.LookRotation(cam.transform.forward) * Quaternion.Euler(90f, 0f, 0f);
            
            float duration = 1.0f; // real time
            float elapsed = 0f;
            while(elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // ease out
                t = 1f - Mathf.Pow(1f - t, 3f);
                
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                
                yield return null;
            }
        }
        
        // 4. Update Unlocks
        int currentMap = PlayerPrefs.GetInt("SelectedMap", 0);
        int nextMap = currentMap + 1;
        
        PlayerPrefs.SetInt("UnlockedMap_" + nextMap, 1);
        
        // Update persistent level
        int newLevel = 1;
        if (currentMap == 0) newLevel = 3;
        else if (currentMap == 1) newLevel = 5;
        else if (currentMap == 2) newLevel = 8;
        
        if (PersistentLevel.Current < newLevel)
        {
            PersistentLevel.SetLevel(newLevel);
        }
        
        PlayerPrefs.Save();

        // 5. Build UI Overlay
        BuildUnlockUI(nextMap, newLevel);
    }
    
    private void BuildUnlockUI(int nextMap, int newLevel)
    {
        GameObject canvasGO = new GameObject("MapUnlockCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        
        canvasGO.AddComponent<GraphicRaycaster>();
        
        // Panel Background
        GameObject panelGO = new GameObject("OverlayPanel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.sizeDelta = Vector2.zero;
        Image panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.7f);
        
        // Modal Background
        GameObject modalGO = new GameObject("ModalPanel");
        modalGO.transform.SetParent(canvasGO.transform, false);
        RectTransform modalRT = modalGO.AddComponent<RectTransform>();
        modalRT.sizeDelta = new Vector2(600f, 400f);
        Image modalImg = modalGO.AddComponent<Image>();
        modalImg.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);
        
        // Outline
        GameObject outlineGO = new GameObject("Outline");
        outlineGO.transform.SetParent(modalGO.transform, false);
        RectTransform outlineRT = outlineGO.AddComponent<RectTransform>();
        outlineRT.anchorMin = Vector2.zero;
        outlineRT.anchorMax = Vector2.one;
        outlineRT.sizeDelta = new Vector2(-10f, -10f);
        Image outlineImg = outlineGO.AddComponent<Image>();
        outlineImg.color = new Color(0.9f, 0.8f, 0.4f, 0.1f);
        
        // Title text
        CreateText(modalGO.transform, "BẢN ĐỒ MỚI ĐÃ MỞ KHÓA!", new Vector2(0f, 130f), 32, new Color(0.9f, 0.8f, 0.5f), FontStyles.Bold);
        
        string mapName = nextMap == 1 ? "Đồng Bằng (Chiến Trường)" : nextMap == 2 ? "Rừng Tre (Bamboo Forest)" : "Chương Cuối";
        
        CreateText(modalGO.transform, mapName, new Vector2(0f, 50f), 28, Color.white, FontStyles.Bold);
        CreateText(modalGO.transform, "Cấp độ trang bị mới: " + newLevel, new Vector2(0f, -10f), 20, new Color(0.4f, 0.8f, 0.4f), FontStyles.Normal);
        
        // Return button
        CreateButton(modalGO.transform, "TIẾP TỤC", new Vector2(0f, -110f), new Vector2(200f, 55f), () => {
            Time.timeScale = 1f;
            SceneManager.LoadScene("GameShopScene");
        });
    }
    
    private void CreateText(Transform parent, string text, Vector2 pos, int fontSize, Color color, FontStyles style)
    {
        GameObject go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(500f, 60f);
        
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = style;
    }
    
    private void CreateButton(Transform parent, string label, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        GameObject btnGO = new GameObject("Button");
        btnGO.transform.SetParent(parent, false);
        RectTransform btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchoredPosition = pos;
        btnRT.sizeDelta = size;
        
        Image img = btnGO.AddComponent<Image>();
        img.color = new Color(0.2f, 0.22f, 0.25f, 1f);
        
        Button btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(action);
        
        CreateText(btnGO.transform, label, Vector2.zero, 24, Color.white, FontStyles.Bold);
    }
}
