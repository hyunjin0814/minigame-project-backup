using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [SerializeField] private Image[] itemSlots;

    private readonly List<InventoryItemData> _slotItems = new();

    private void OnEnable()
    {
        InventoryManager.OnItemAdded   += HandleItemAdded;
        InventoryManager.OnItemRemoved += HandleItemRemoved;
    }

    private void OnDisable()
    {
        InventoryManager.OnItemAdded   -= HandleItemAdded;
        InventoryManager.OnItemRemoved -= HandleItemRemoved;
    }

    private void HandleItemAdded(InventoryItemData item)
    {
        if (_slotItems.Count >= itemSlots.Length) return;
        _slotItems.Add(item);
        RefreshSlots();
    }

    private void HandleItemRemoved(InventoryItemData item)
    {
        if (!_slotItems.Remove(item)) return;
        RefreshSlots();
    }

    private void RefreshSlots()
    {
        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (i < _slotItems.Count)
            {
                itemSlots[i].sprite = _slotItems[i].icon;
                itemSlots[i].enabled = true;
            }
            else
            {
                itemSlots[i].sprite = null;
                itemSlots[i].enabled = false;
            }
        }
    }
}
