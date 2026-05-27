using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 씬 내 진입점. ZoneTransition이 넘긴 entryID와 일치하는 SpawnPoint 위치로
/// PlayerSpawner가 플레이어를 이동시킨다.
///
/// 예) ZoneB 왼쪽 끝에 SpawnPoint(entryID="from_A"),
///     오른쪽 끝에 SpawnPoint(entryID="from_C")를 배치하면
///     A에서 올 때는 왼쪽, C에서 올 때는 오른쪽에 등장한다.
///
/// 좌표를 출발 씬에 하드코딩하지 않으므로, 진입점을 옮길 때
/// 이 오브젝트만 끌어다 놓으면 된다(다른 씬은 수정 불필요).
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    // 씬 로드 시 Awake에서 등록 → PlayerSpawner.Start에서 조회.
    // (Unity는 한 씬의 모든 Awake를 Start보다 먼저 호출하므로 순서 보장)
    private static readonly Dictionary<string, SpawnPoint> _registry = new();

    [SerializeField] private string entryID;

    public string EntryID => entryID;

    private void Awake()
    {
        if (string.IsNullOrEmpty(entryID))
        {
            Debug.LogWarning($"[SpawnPoint] entryID가 비어 있습니다: {name}", this);
            return;
        }
        _registry[entryID] = this;
    }

    private void OnDestroy()
    {
        // 자신이 등록한 항목만 제거.
        // 씬 전환 시 신규 씬 등록(Awake) 후 구 씬 파괴(OnDestroy)가 일어나도
        // 새로 등록된 항목을 지우지 않도록 보호한다.
        if (!string.IsNullOrEmpty(entryID)
            && _registry.TryGetValue(entryID, out var sp) && sp == this)
        {
            _registry.Remove(entryID);
        }
    }

    /// <summary>등록된 진입점 위치를 반환. 없으면 null.</summary>
    public static Vector2? GetPosition(string entryID)
    {
        if (!string.IsNullOrEmpty(entryID)
            && _registry.TryGetValue(entryID, out var sp) && sp != null)
        {
            return sp.transform.position;
        }
        return null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
    }
#endif
}
