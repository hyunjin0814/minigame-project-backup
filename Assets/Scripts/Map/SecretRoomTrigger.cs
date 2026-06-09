using UnityEngine;

/// <summary>
/// 비밀방 레버 트리거. 3단계 상태머신.
///
/// NoLever  → 상호작용 + 열쇠 있음 → LeverPlaced  : 열쇠 소모, leverPlacedSprite 교체, ItemPickup 재생
/// NoLever  → 상호작용 + 열쇠 없음              : InteractFail 재생
/// LeverPlaced → 상호작용                → Done    : 비밀방 활성화, leverPulledSprite 교체, SecretFound 재생
///
/// 스프라이트:
///   SpriteRenderer 기본값 = 레버 없는 받침대
///   _leverPlacedSprite   = crank-up   (레버 꽂힌 상태)
///   _leverPulledSprite   = crank-down (레버 당긴 상태)
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SecretRoomTrigger : MonoBehaviour
{
    [SerializeField] private KeyItemData requiredKey;
    [SerializeField] private GameObject  secretRoomArea;
    [SerializeField] private GameObject  interactHint;

    [Header("스프라이트")]
    [SerializeField] private Sprite _leverPlacedSprite; // crank-up
    [SerializeField] private Sprite _leverPulledSprite; // crank-down

    private enum State { NoLever, LeverPlaced, Done }
    private State _state = State.NoLever;

    private SpriteRenderer     _sr;
    private PlayerInputHandler _playerInput;

    private void Awake()     => _sr = GetComponent<SpriteRenderer>();
    private void OnDestroy() => UnsubscribeInput();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_state == State.Done || !other.CompareTag("Player")) return;

        _playerInput = other.GetComponent<PlayerInputHandler>();
        if (_playerInput != null)
            _playerInput.OnInteract += TryInteract;

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

    private void TryInteract()
    {
        switch (_state)
        {
            case State.NoLever:
                if (!InventoryManager.Instance.Has(requiredKey))
                {
                    AudioManager.Instance?.PlaySFX(SoundType.InteractFail);
                    Debug.Log("[SecretRoomTrigger] 열쇠 없음 → 실패음");
                    return;
                }
                InventoryManager.Instance.Remove(requiredKey);
                _state = State.LeverPlaced;
                if (_sr != null && _leverPlacedSprite != null)
                    _sr.sprite = _leverPlacedSprite;
                AudioManager.Instance?.PlaySFX(SoundType.LeverAttach);
                Debug.Log("[SecretRoomTrigger] 레버 꽂힘");
                break;

            case State.LeverPlaced:
                _state = State.Done;
                UnsubscribeInput();
                if (_sr != null && _leverPulledSprite != null)
                    _sr.sprite = _leverPulledSprite;
                secretRoomArea.SetActive(true);
                AudioManager.Instance?.PlaySFX(SoundType.LeverPull);
                if (interactHint != null)
                    interactHint.SetActive(false);
                GetComponent<Collider2D>().enabled = false;
                Debug.Log("[SecretRoomTrigger] 레버 당김 → 비밀방 활성화");
                break;
        }
    }

    private void UnsubscribeInput()
    {
        if (_playerInput != null)
            _playerInput.OnInteract -= TryInteract;
        _playerInput = null;
    }
}
