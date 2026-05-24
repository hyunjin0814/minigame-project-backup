using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(PlayerHorizontalMovement))]
[RequireComponent(typeof(PlayerJump))]
[RequireComponent(typeof(PlayerAttack))]
// [RequireComponent(typeof(PlayerAnimator))]
public class PlayerTransformController : MonoBehaviour
{
    // 카멜레온 → 인간 변신이 발생한 Time.time 기록.
    // 각 적이 자신의 _sneakWindowDuration과 비교해 감지 무효 여부를 판단.
    // TODO: 카멜레온 → 고양이 리네임 후 주석 업데이트
    public float SneakWindowActivatedAt { get; private set; } = float.NegativeInfinity;

    [Header("Transformation Data")]
    [SerializeField]
    private TransformationData humanData;

    [SerializeField]
    private TransformationData dogData;

    [SerializeField]
    private TransformationData chameleonData;

    // 다른 컴포넌트에 대한 접근 (State가 사용)
    public PlayerHorizontalMovement HorizontalMovement { get; private set; }
    public PlayerJump Jump { get; private set; }
    public PlayerAttack Attack { get; private set; }

    // public PlayerAnimator PlayerAnimator { get; private set; }

    private ITransformState currentState;

    public ChameleonStealth ChameleonStealth { get; private set; }

    // 각 State의 인스턴스를 미리 생성해서 캐싱
    private HumanState humanState;
    private DogState dogState;
    private ChameleonState chameleonState;

    // 입력
    private PlayerInputHandler inputHandler;

    public BoxCollider2D Collider { get; private set; }

    private void Awake()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        HorizontalMovement = GetComponent<PlayerHorizontalMovement>();
        Jump = GetComponent<PlayerJump>();
        Attack = GetComponent<PlayerAttack>();
        // PlayerAnimator = GetComponent<PlayerAnimator>();
        Collider = GetComponent<BoxCollider2D>();
        ChameleonStealth = GetComponent<ChameleonStealth>();

        // State 인스턴스 생성
        humanState = new HumanState(this, humanData);
        dogState = new DogState(this, dogData);
        chameleonState = new ChameleonState(this, chameleonData);
    }

    private void Start()
    {
        // 기본 상태: 인간
        ChangeState(humanState);
    }

    private void OnEnable()
    {
        inputHandler.OnTransformHuman += HandleTransformHuman;
        inputHandler.OnTransformDog += HandleTransformDog;
        inputHandler.OnTransformChameleon += HandleTransformChameleon;
    }

    private void OnDisable()
    {
        inputHandler.OnTransformHuman -= HandleTransformHuman;
        inputHandler.OnTransformDog -= HandleTransformDog;
        inputHandler.OnTransformChameleon -= HandleTransformChameleon;
    }

    private void HandleTransformHuman() => ChangeState(humanState);

    private void HandleTransformDog() => ChangeState(dogState);

    private void HandleTransformChameleon() => ChangeState(chameleonState);

    private void ChangeState(ITransformState newState)
    {
        if (currentState == newState)
            return;

        // 카멜레온 → 인간 변신 시만 스니크 윈도우 활성화
        // TODO: 카멜레온 → 고양이 리네임 후 주석 업데이트
        bool comingFromChameleon = currentState == chameleonState;

        currentState?.Exit();
        currentState = newState;
        currentState.Enter();

        if (comingFromChameleon && newState == humanState)
        {
            SneakWindowActivatedAt = Time.time;
            Debug.Log("[PlayerTransformController] 스니크 윈도우 활성화");
        }

    }
}
