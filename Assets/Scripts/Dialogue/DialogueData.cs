using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    [Tooltip("Tên nhân vật hiển thị (để trống = narrator)")]
    public string characterName;

    [Tooltip("Nội dung thoại")]
    [TextArea(3, 6)]
    public string text;

    [Tooltip("File lồng tiếng cho câu thoại này (Tùy chọn)")]
    public AudioClip voiceClip;
}

[CreateAssetMenu(fileName = "New Dialogue", menuName = "ThanhGiong/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Tooltip("Danh sách các câu thoại theo thứ tự")]
    public DialogueLine[] lines;
}
