using UnityEngine;

/// <summary>
/// 구역 경계 트리거.
/// 플레이어가 접촉하면 SceneTransitionManager를 통해 씬을 전환한다.
/// 씬 전환 전 현재 HP를 GameState.savedHP에 보관해 다음 씬에서 복원한다.
///
/// [인스펙터 설정]
///  - targetScene    : 이동할 씬 이름 (Build Settings에 등록 필요)
///  - targetEntryID  : 대상 씬에 배치된 SpawnPoint의 entryID
///                     (좌표가 아니라 이름표를 넘긴다 → 대상 씬에서 위치 결정)
///
/// [씬 배치]
///  - IsTrigger = true인 Collider2D를 갖는 오브젝트에 붙인다.
///  - 양방향 이동이 필요하면 반대쪽 경계에도 ZoneTransition을 배치한다.
///  - 대상 씬에는 targetEntryID와 같은 ID의 SpawnPoint가 있어야 한다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ZoneTransition : MonoBehaviour
{
    [Header("목적지")]
    [SerializeField] private string targetScene;
    [SerializeField] private string targetEntryID;

    [Header("지도")]
    [Tooltip("지도에서 이 출구의 방향. Auto면 문 위치로 자동 판별. 2층 분기 등 애매한 문만 수동 지정.")]
    [SerializeField] private MapDirOverride mapDirection = MapDirOverride.Auto;

    /// <summary>지도 자동 배치용. 이 출구가 향하는 목적지 씬 이름.</summary>
    public string TargetScene => targetScene;

    /// <summary>지도 방향 수동 지정값(Auto면 자동 판별).</summary>
    public MapDirOverride MapDirection => mapDirection;

    // ── 트리거 ────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogWarning("[ZoneTransition] SceneTransitionManager를 찾을 수 없습니다.");
            return;
        }
        if (SceneTransitionManager.Instance.IsTransitioning) return;

        // 현재 HP / 최대 HP 보관 (다음 씬 PlayerSpawner가 복원)
        if (GameState.Instance != null && other.TryGetComponent<Health>(out var health))
        {
            GameState.Instance.savedHP    = health.CurrentHp;
            GameState.Instance.savedMaxHP = health.MaxHp;
        }

        // 스프라이트 방향 보관 (다음 씬 PlayerAnimator가 복원)
        if (GameState.Instance != null && other.TryGetComponent<PlayerAnimator>(out var anim))
            GameState.Instance.savedFacingLeft = anim.IsFacingLeft;

        SceneTransitionManager.Instance.TransitionTo(targetScene, targetEntryID);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // 에디터에서 목적지 방향 시각화
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * 0.6f);
    }
#endif
}
