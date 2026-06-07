using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Hollow Knight 방식 체크포인트.
/// 플레이어가 범위 안에서 윗방향키(Interact)를 누를 때마다 발동:
///   1. HP 전체 회복
///   2. GameState에 위치 저장
///   3. JSON 세이브 파일 갱신
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Header("식별자")]
    [SerializeField] private string checkpointID;

    [Header("시각 피드백")]
    [SerializeField] private GameObject activateEffect;

    [Header("상호작용 힌트")]
    [SerializeField] private GameObject interactHint;  // InteractHint 오브젝트 연결

    // 현재 범위 안에 있는 플레이어 참조
    private PlayerInputHandler _playerInput;
    private Health             _playerHealth;

    // ── Lifecycle ──────────────────────────────────────────────────────────

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

        if (activateEffect != null)
            activateEffect.SetActive(true);

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
