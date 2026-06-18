using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    [Tooltip("Tên nhân vật hiển thị (để trống = narrator)")]
    public string characterName;

    [Tooltip("Nội dung thoại")]
    [TextArea(3, 6)]
    public string text;
}

[CreateAssetMenu(fileName = "New Dialogue", menuName = "ThanhGiong/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Tooltip("Danh sách các câu thoại theo thứ tự")]
    public DialogueLine[] lines;
}
