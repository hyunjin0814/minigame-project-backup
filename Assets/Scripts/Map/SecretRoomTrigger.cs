using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SecretRoomTrigger : MonoBehaviour
{
    [SerializeField] private KeyItemData requiredKey;
    [SerializeField] private GameObject secretRoomArea;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var transformCtrl = other.GetComponent<PlayerTransformController>();
        if (transformCtrl == null || !transformCtrl.IsCatForm)
        {
            Debug.Log("[SecretRoomTrigger] 고양이 형태 아님 → 무반응");
            return;
        }

        if (!InventoryManager.Instance.Has(requiredKey))
        {
            Debug.Log("[SecretRoomTrigger] 열쇠 없음 → 무반응");
            return;
        }

        Debug.Log("[SecretRoomTrigger] 조건 충족 → 비밀방 활성화");
        InventoryManager.Instance.Remove(requiredKey);
        secretRoomArea.SetActive(true);
    }
}
