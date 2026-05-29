using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour
{
    public static event Action<ItemData> OnItemPickedUp;

    [SerializeField]
    private ItemData itemData;

    private void Start()
    {
        // 이미 수집한 아이템이면 씬 재방문 시 비활성화 (재획득 방지)
        if (itemData != null
            && !string.IsNullOrEmpty(itemData.id)
            && GameState.Instance != null
            && GameState.Instance.HasCollected(itemData.id))
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[ItemPickup] 트리거 감지: {other.gameObject.name} / tag: {other.tag}");
        if (!other.CompareTag("Player"))
            return;

        if (itemData is InstantItemData instant)
            instant.ApplyEffect(other.gameObject);
        else if (itemData is InventoryItemData)
            InventoryManager.Instance.Add(itemData);

        OnItemPickedUp?.Invoke(itemData);
        Destroy(gameObject);
    }
}
