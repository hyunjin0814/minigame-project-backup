using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [SerializeField] private Image[] itemSlots;

    private int _slotIndex;

    private void OnEnable()  => InventoryManager.OnItemAdded += HandleItemAdded;
    private void OnDisable() => InventoryManager.OnItemAdded -= HandleItemAdded;

    private void HandleItemAdded(InventoryItemData item)
    {
        if (_slotIndex >= itemSlots.Length) return;
        itemSlots[_slotIndex].sprite = item.icon;
        itemSlots[_slotIndex].enabled = true;
        _slotIndex++;
    }
}
