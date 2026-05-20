using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(PlayerHorizontalMovement))]
[RequireComponent(typeof(PlayerJump))]
[RequireComponent(typeof(PlayerAttack))]
// [RequireComponent(typeof(PlayerAnimator))]
public class PlayerTransformController : MonoBehaviour
{
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

    public CapsuleCollider2D Collider { get; private set; }

    private void Awake()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        HorizontalMovement = GetComponent<PlayerHorizontalMovement>();
        Jump = GetComponent<PlayerJump>();
        Attack = GetComponent<PlayerAttack>();
        // PlayerAnimator = GetComponent<PlayerAnimator>();
        Collider = GetComponent<CapsuleCollider2D>();
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

        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }
}
