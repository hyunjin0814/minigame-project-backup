using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BossRoomTrigger : MonoBehaviour
{
    [SerializeField] private Collider2D door;
    [Tooltip("문 시각 표현(타일맵/스프라이트 등). 콜라이더와 함께 SetActive로 토글됨")]
    [SerializeField] private GameObject doorVisual;
    [SerializeField] private BossBase boss;

    [Header("보상 통로 (선택)")]
    [Tooltip("GameState.openedDoors에 등록할 키. LockedDoor(BossDoor 타입)의 doorID와 일치시킬 것")]
    [SerializeField] private string bossDoorID;
    [Tooltip("보스 사망 즉시 Refresh()할 보상 통로. 현재 씬에서 즉시 개방되도록 함")]
    [SerializeField] private LockedDoor exitDoor;

    private Collider2D entryTrigger;

    private void Awake() => entryTrigger = GetComponent<Collider2D>();

    private void Start()
    {
        if (boss != null)
            boss.Health.OnDeath += OpenDoor;

        // 시작 시 문은 비활성 (자유 진입 허용)
        SetDoorActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        SetDoorActive(true);
        entryTrigger.enabled = false;
        boss?.Activate();
    }

    private void OpenDoor()
    {
        SetDoorActive(false);

        // 영구 진행 상태 등록 (재방문/다른 씬에서 자동 개방용)
        if (!string.IsNullOrEmpty(bossDoorID))
            GameState.Instance?.OpenDoor(bossDoorID);

        // 현재 씬의 보상 통로 즉시 개방
        exitDoor?.Refresh();
    }

    // 문의 콜라이더와 시각 표현을 함께 토글
    private void SetDoorActive(bool active)
    {
        if (door != null) door.enabled = active;
        if (doorVisual != null) doorVisual.SetActive(active);
    }

    private void OnDestroy()
    {
        if (boss != null)
            boss.Health.OnDeath -= OpenDoor;
    }
}
