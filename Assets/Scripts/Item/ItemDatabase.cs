using UnityEngine;

/// <summary>
/// 인벤토리 아이템 전체 목록을 보관하는 ScriptableObject.
/// SaveManager가 ID로 InventoryItemData를 복원할 때 사용한다.
///
/// [설정 방법]
///   1. Project 창 우클릭 → Create → Game/ItemDatabase
///   2. Assets/Resources/ 폴더에 "ItemDatabase" 이름으로 저장
///   3. 인스펙터에서 모든 InventoryItemData SO를 items 배열에 등록
/// </summary>
[CreateAssetMenu(menuName = "Game/ItemDatabase", fileName = "ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private InventoryItemData[] items;

    /// <summary>
    /// ID로 InventoryItemData를 찾아 반환한다.
    /// 없으면 null 반환 + 경고 로그.
    /// </summary>
    public InventoryItemData GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        foreach (var item in items)
        {
            if (item != null && item.id == id)
                return item;
        }

        Debug.LogWarning($"[ItemDatabase] ID '{id}'인 아이템을 찾을 수 없음 — 인스펙터 등록 확인");
        return null;
    }
}
