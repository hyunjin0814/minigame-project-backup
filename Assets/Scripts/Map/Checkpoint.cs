using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Hollow Knight 방식 체크포인트.
/// 플레이어가 범위 안에서 윗방향키(Interact)를 누를 때마다 발동:
///   1. HP 전체 회복
///   2. GameState에 위치 저장
///   3. JSON 세이브 파일 갱신
///
/// 시각 상태:
///   - 미등록(inactiveColor) → 처음 상호작용 시 활성(activeColor)으로 변경
///   - 이후 재방문해도 activeColor 유지
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Header("식별자")]
    [SerializeField] private string checkpointID;

    [Header("시각 피드백")]
    [SerializeField] private SpriteRenderer indicatorSprite;
    [SerializeField] private Color activeColor   = Color.yellow;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private GameObject activateEffect;

    [Header("상호작용 힌트")]
    [SerializeField] private GameObject interactHint;  // InteractHint 오브젝트 연결

    // 한 번이라도 등록됐는지 (시각 표현용 — 색상 유지)
    private bool _isRegistered;

    // 현재 범위 안에 있는 플레이어 참조
    private PlayerInputHandler _playerInput;
    private Health             _playerHealth;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Start()
    {
        // 이 체크포인트가 마지막으로 저장된 경우 → 활성 색상으로 표시
        _isRegistered = GameState.Instance != null
                     && GameState.Instance.lastCheckpointID == checkpointID;

        if (indicatorSprite != null)
            indicatorSprite.color = _isRegistered ? activeColor : inactiveColor;
    }

    private void OnDestroy()
    {
        UnsubscribeInput();
    }

    // ── 트리거 ────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        _playerHealth = other.GetComponent<Health>();
        _playerInput  = other.GetComponent<PlayerInputHandler>();

        if (_playerInput != null)
            _playerInput.OnInteract += TryActivate;

        if (interactHint != null)
            interactHint.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        UnsubscribeInput();

        if (interactHint != null)
            interactHint.SetActive(false);
    }

    // ── 상호작용 ──────────────────────────────────────────────────────────

    private void TryActivate()
    {
        // HP 전체 회복
        if (_playerHealth != null)
        {
            int missing = _playerHealth.MaxHp - _playerHealth.CurrentHp;
            if (missing > 0) _playerHealth.Heal(missing);
        }

        // 체크포인트 & 세이브 파일 저장
        if (GameState.Instance != null)
        {
            GameState.Instance.SaveCheckpoint(
                checkpointID,
                SceneManager.GetActiveScene().name,
                transform.position
            );

            if (GameState.Instance.currentSaveSlot >= 0)
                SaveManager.Save(GameState.Instance.currentSaveSlot);
        }

        // 처음 등록 시에만 시각 피드백 갱신
        if (!_isRegistered)
        {
            _isRegistered = true;

            if (indicatorSprite != null)
                indicatorSprite.color = activeColor;

            if (activateEffect != null)
                activateEffect.SetActive(true);
        }

        Debug.Log($"[Checkpoint] 상호작용: {checkpointID}");
    }

    // ── 내부 유틸 ─────────────────────────────────────────────────────────

    private void UnsubscribeInput()
    {
        if (_playerInput != null)
            _playerInput.OnInteract -= TryActivate;

        _playerInput  = null;
        _playerHealth = null;
    }
}
