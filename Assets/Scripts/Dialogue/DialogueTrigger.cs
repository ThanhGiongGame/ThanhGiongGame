using UnityEngine;

/// <summary>
/// Gắn vào NPC/trigger zone để kích hoạt đoạn thoại khi player lại gần.
/// </summary>
public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private DialogueData dialogueData;

    [Header("Trigger Settings")]
    [SerializeField] private float triggerRadius = 2.5f;
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private bool showRangeGizmo = true;

    private bool _hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (triggerOnce && _hasTriggered) return;

        _hasTriggered = true;
        DialogueUI.Instance?.StartDialogue(dialogueData);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showRangeGizmo) return;
        Gizmos.color = new Color(1f, 0.85f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, triggerRadius);
    }
}
