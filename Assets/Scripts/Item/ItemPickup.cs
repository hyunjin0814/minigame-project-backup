using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour
{
    public static event Action<ItemData> OnItemPickedUp;

    [SerializeField] private ItemData itemData;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[ItemPickup] 트리거 감지: {other.gameObject.name} / tag: {other.tag}");
        if (!other.CompareTag("Player")) return;

        if (itemData is InstantItemData instant)
            instant.ApplyEffect(other.gameObject);
        else if (itemData is InventoryItemData)
            InventoryManager.Instance.Add(itemData);

        OnItemPickedUp?.Invoke(itemData);
        Destroy(gameObject);
    }
}
