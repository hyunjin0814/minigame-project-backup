using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 각 구역 입구에 배치하는 체크포인트.
/// 플레이어가 처음 접촉하면:
///   1. HP 전체 회복
///   2. GameState에 씬 이름 + 위치 저장
///   3. 활성화 이펙트 표시 (이후 재접촉 무시)
///
/// [인스펙터 설정]
///  - checkpointID  : 씬 내 유일한 문자열 (예: "ZoneA_Entry")
///  - indicatorSprite / activateEffect : 활성화 전/후 시각 표현
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Header("식별자")]
    [SerializeField]
    private string checkpointID;

    [Header("시각 피드백")]
    [SerializeField]
    private SpriteRenderer indicatorSprite;

    [SerializeField]
    private Color activeColor = Color.yellow;

    [SerializeField]
    private Color inactiveColor = Color.gray;

    [SerializeField]
    private GameObject activateEffect;

    private bool isActivated;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Start()
    {
        // 이 체크포인트가 마지막으로 저장된 경우 → 이미 활성화 상태로 표시
        bool alreadySaved =
            GameState.Instance != null && GameState.Instance.lastCheckpointID == checkpointID;

        isActivated = alreadySaved;

        if (indicatorSprite != null)
            indicatorSprite.color = alreadySaved ? activeColor : inactiveColor;
    }

    // ── 트리거 ────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isActivated)
            return;
        if (!other.CompareTag("Player"))
            return;

        Activate(other.gameObject);
    }

    // ── 활성화 ────────────────────────────────────────────────────────────

    private void Activate(GameObject player)
    {
        isActivated = true;

        // 1. HP 전체 회복
        if (player.TryGetComponent<Health>(out var health))
        {
            int missing = health.MaxHp - health.CurrentHp;
            if (missing > 0)
                health.Heal(missing);
        }

        // 2. 체크포인트 저장
        if (GameState.Instance != null)
        {
            GameState.Instance.SaveCheckpoint(
                checkpointID,
                SceneManager.GetActiveScene().name,
                transform.position
            );
        }

        // 3. 시각 피드백
        if (indicatorSprite != null)
            indicatorSprite.color = activeColor;
        if (activateEffect != null)
            activateEffect.SetActive(true);

        Debug.Log($"[Checkpoint] 활성화: {checkpointID}");
    }
}
