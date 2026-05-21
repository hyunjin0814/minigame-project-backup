using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    public static event Action<InventoryItemData> OnItemAdded;

    private readonly List<InventoryItemData> _items = new();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Add(ItemData data)
    {
        if (data is not InventoryItemData item)
            return;
        _items.Add(item);
        OnItemAdded?.Invoke(item);
    }

    public bool Has(InventoryItemData item) => _items.Contains(item);
}
