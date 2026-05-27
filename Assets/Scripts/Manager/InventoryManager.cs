using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    public static event Action<InventoryItemData> OnItemAdded;
    public static event Action<InventoryItemData> OnItemRemoved;

    private readonly List<InventoryItemData> _items = new();

    /// <summary>현재 보유 아이템 (씬 전환 후 HUD 재구성에 사용).</summary>
    public IReadOnlyList<InventoryItemData> Items => _items;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // 씬 전환 시 수집 아이템(열쇠 등) 유지
    }

    public void Add(ItemData data)
    {
        if (data is not InventoryItemData item)
            return;
        _items.Add(item);
        OnItemAdded?.Invoke(item);
    }

    public bool Has(InventoryItemData item) => _items.Contains(item);

    public bool Remove(InventoryItemData item)
    {
        if (!_items.Remove(item)) return false;
        Debug.Log($"[InventoryManager] 아이템 제거: {item.displayName}");
        OnItemRemoved?.Invoke(item);
        return true;
    }
}
