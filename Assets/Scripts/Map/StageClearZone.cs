using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class StageClearZone : MonoBehaviour
{
    [SerializeField] private KeyItemData requiredKey;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (requiredKey != null && !InventoryManager.Instance.Has(requiredKey)) return;

        StageClearManager.Instance.TriggerClear();
    }
}
