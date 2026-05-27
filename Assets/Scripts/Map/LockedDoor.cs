using UnityEngine;

/// <summary>구역 잠금 타입 3종.</summary>
public enum DoorType
{
    DashGap,        // 대시 해금 필요 (구역A → B)
    NarrowPassage,  // 고양이 변신 필요 (구역B → C)
    BossDoor,       // 보스 클리어 필요 (구역C → D) — openedDoors에서 doorID로 확인
}

/// <summary>
/// 세 가지 잠금 타입을 지원하는 구역 차단 오브젝트.
/// Start()에서 GameState를 확인해 조건 충족 시 자동 개방.
/// 외부(보스 사망 이벤트 등)에서 Refresh()를 호출하면 재확인 가능.
///
/// [중요] 이 오브젝트의 Collider2D는 반드시 **솔리드(IsTrigger = false)** 여야 한다.
///        트리거 콜라이더는 플레이어를 물리적으로 막지 못한다.
///        잠금 피드백은 플레이어가 문에 부딪힐 때 OnCollisionEnter2D로 받는다.
///        (플레이어가 Rigidbody2D를 가져야 충돌 콜백이 발생)
///
/// [인스펙터 설정]
///  - doorType        : 잠금 종류
///  - doorID          : BossDoor 전용 식별자 (GameState.openedDoors 키)
///  - blockCollider   : 물리적으로 막는 솔리드 Collider2D
///                      (비워두면 자신의 Collider2D 사용 — IsTrigger=false인지 확인)
///  - lockedVisual    : 잠긴 상태 표시 GameObject (자물쇠 스프라이트 등)
///  - unlockedVisual  : 열린 상태 표시 GameObject (선택)
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LockedDoor : MonoBehaviour
{
    [Header("타입 / 식별자")]
    [SerializeField] private DoorType doorType;
    [SerializeField] private string   doorID;

    [Header("참조")]
    [SerializeField] private Collider2D blockCollider;
    [SerializeField] private GameObject lockedVisual;
    [SerializeField] private GameObject unlockedVisual;

    private bool isUnlocked;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        // blockCollider 미설정 시 자신의 Collider2D 사용
        if (blockCollider == null)
            blockCollider = GetComponent<Collider2D>();

        if (blockCollider != null && blockCollider.isTrigger)
            Debug.LogWarning($"[LockedDoor] '{name}'의 차단 콜라이더가 트리거입니다. " +
                             "물리 차단을 위해 IsTrigger=false로 설정하세요.", this);
    }

    private void Start() => Refresh();

    // ── 외부 호출 ──────────────────────────────────────────────────────────

    /// <summary>
    /// 조건을 다시 확인하고 상태를 갱신한다.
    /// 보스가 사망했을 때 BossRoomTrigger 등에서 호출.
    /// </summary>
    public void Refresh()
    {
        bool conditionMet = CheckCondition();
        if (conditionMet && !isUnlocked) Open();
        else if (!conditionMet)          SetLocked();
    }

    // ── 충돌 피드백 ──────────────────────────────────────────────────────

    // 솔리드 콜라이더에 플레이어가 부딪힐 때 발생 (트리거가 아니라 OnCollisionEnter2D).
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isUnlocked || !collision.collider.CompareTag("Player")) return;

        string msg = doorType switch
        {
            DoorType.DashGap       => "대시 능력이 필요합니다",
            DoorType.NarrowPassage => "고양이 변신이 필요합니다",
            DoorType.BossDoor      => "보스를 처치해야 합니다",
            _                      => "잠겨 있습니다",
        };
        Debug.Log($"[LockedDoor] {msg}");
        // TODO: UIManager 팝업 연동
    }

    // ── 내부 ──────────────────────────────────────────────────────────────

    private bool CheckCondition()
    {
        if (GameState.Instance == null) return false;
        return doorType switch
        {
            DoorType.DashGap       => GameState.Instance.dashUnlocked,
            DoorType.NarrowPassage => GameState.Instance.catUnlocked,
            DoorType.BossDoor      => GameState.Instance.IsDoorOpen(doorID),
            _                      => false,
        };
    }

    private void Open()
    {
        isUnlocked = true;
        if (blockCollider  != null) blockCollider.enabled  = false;
        if (lockedVisual   != null) lockedVisual.SetActive(false);
        if (unlockedVisual != null) unlockedVisual.SetActive(true);
        Debug.Log($"[LockedDoor] 열림: {doorType} (id={doorID})");
    }

    private void SetLocked()
    {
        isUnlocked = false;
        if (blockCollider  != null) blockCollider.enabled  = true;
        if (lockedVisual   != null) lockedVisual.SetActive(true);
        if (unlockedVisual != null) unlockedVisual.SetActive(false);
    }
}
