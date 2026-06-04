using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TutorialTrigger : MonoBehaviour
{
    [SerializeField] private string keyText    = "A / D";
    [SerializeField] private string actionText = "이동";

    private bool _triggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || _triggered) return;
        _triggered = true;
        TutorialManager.Instance?.ShowSubtitle(keyText, actionText);
    }
}
