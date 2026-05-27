using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerForm { Human, Dog, Cat }

[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(PlayerHorizontalMovement))]
[RequireComponent(typeof(PlayerJump))]
[RequireComponent(typeof(PlayerAttack))]
[RequireComponent(typeof(PlayerDash))]
[RequireComponent(typeof(PlayerAnimator))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerTransformController : MonoBehaviour
{
    // 고양이 → 인간 변신이 발생한 Time.time 기록.
    // 각 적이 자신의 _sneakWindowDuration과 비교해 감지 무효 여부를 판단.
    public float SneakWindowActivatedAt { get; private set; } = float.NegativeInfinity;

    /// <summary>해금되지 않은 능력 변신 시도 시 발행 (UI 피드백용).</summary>
    public static event System.Action<AbilityType> OnLockedAbilityAttempted;

    [Header("Transformation Data")]
    [SerializeField]
    private TransformationData humanData;

    [SerializeField]
    private TransformationData dogData;

    [SerializeField]
    private TransformationData catData;

    // 다른 컴포넌트에 대한 접근 (State가 사용)
    public PlayerHorizontalMovement HorizontalMovement { get; private set; }
    public PlayerJump Jump { get; private set; }
    public PlayerAttack Attack { get; private set; }
    public PlayerDash Dash { get; private set; }
    public PlayerAnimator PlayerAnimator { get; private set; }

    // 히트스톱 상태는 HitStopManager(전역 timeScale 소유자)에 위임.
    public bool IsHitStopped => HitStopManager.Instance != null && HitStopManager.Instance.IsActive;

    public bool IsCatForm => currentState == catState;

    private Rigidbody2D rb;
    private ITransformState currentState;

    public CatStealth CatStealth { get; private set; }
    public DogScanner DogScanner { get; private set; }
    public DogDashAttack DogDashAttack { get; private set; }

    // 각 State의 인스턴스를 미리 생성해서 캐싱
    private HumanState humanState;
    private DogState dogState;
    private CatState catState;

    // 입력
    private PlayerInputHandler inputHandler;

    public BoxCollider2D Collider { get; private set; }

    private void Awake()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        HorizontalMovement = GetComponent<PlayerHorizontalMovement>();
        Jump = GetComponent<PlayerJump>();
        Attack = GetComponent<PlayerAttack>();
        Dash = GetComponent<PlayerDash>();
        PlayerAnimator = GetComponent<PlayerAnimator>();
        Collider = GetComponent<BoxCollider2D>();
        CatStealth = GetComponent<CatStealth>();
        DogScanner = GetComponent<DogScanner>();
        DogDashAttack = GetComponent<DogDashAttack>();
        rb = GetComponent<Rigidbody2D>();

        // State 인스턴스 생성
        humanState = new HumanState(this, humanData);
        dogState = new DogState(this, dogData);
        catState = new CatState(this, catData);
    }

    private void Start()
    {
        // 마지막 변신 형태 복원 (없으면 인간). 씬 전환 사이 형태 유지.
        PlayerForm form = GameState.Instance != null ? GameState.Instance.savedForm : PlayerForm.Human;

        // 해금 안 된 형태로 복원되는 경우 Human으로 폴백
        if (form == PlayerForm.Dog && (GameState.Instance == null || !GameState.Instance.dogUnlocked))
            form = PlayerForm.Human;
        if (form == PlayerForm.Cat && (GameState.Instance == null || !GameState.Instance.catUnlocked))
            form = PlayerForm.Human;

        ChangeState(StateFor(form));
    }

    private void OnEnable()
    {
        inputHandler.OnTransformHuman += HandleTransformHuman;
        inputHandler.OnTransformDog += HandleTransformDog;
        inputHandler.OnTransformCat += HandleTransformCat;
    }

    private void OnDisable()
    {
        inputHandler.OnTransformHuman -= HandleTransformHuman;
        inputHandler.OnTransformDog -= HandleTransformDog;
        inputHandler.OnTransformCat -= HandleTransformCat;
    }

    private void HandleTransformHuman() => ChangeState(humanState);

    private void HandleTransformDog()
    {
        if (GameState.Instance != null && !GameState.Instance.dogUnlocked)
        {
            Debug.Log("[PlayerTransformController] 강아지 변신 미해금");
            OnLockedAbilityAttempted?.Invoke(AbilityType.Dog);
            return;
        }
        ChangeState(dogState);
    }

    private void HandleTransformCat()
    {
        if (GameState.Instance != null && !GameState.Instance.catUnlocked)
        {
            Debug.Log("[PlayerTransformController] 고양이 변신 미해금");
            OnLockedAbilityAttempted?.Invoke(AbilityType.Cat);
            return;
        }
        ChangeState(catState);
    }

    /// <summary>현재 변신 형태를 PlayerForm 열거값으로 반환.</summary>
    public PlayerForm CurrentForm =>
        currentState == dogState ? PlayerForm.Dog :
        currentState == catState ? PlayerForm.Cat :
        PlayerForm.Human;

    private ITransformState StateFor(PlayerForm form) => form switch
    {
        PlayerForm.Dog => dogState,
        PlayerForm.Cat => catState,
        _              => humanState,
    };

    private void ChangeState(ITransformState newState)
    {
        if (currentState == newState)
            return;

        // 히트스톱 중에는 변신 FSM 전환 잠금
        if (IsHitStopped)
        {
            Debug.Log("[PlayerTransformController] 히트스톱 중 변신 차단");
            return;
        }

        // 고양이 → 인간 변신 시만 스니크 윈도우 활성화
        bool comingFromCat = currentState == catState;

        currentState?.Exit();
        currentState = newState;
        currentState.Enter();

        // 현재 형태를 영속화 → 씬 전환 후 복원에 사용
        if (GameState.Instance != null)
            GameState.Instance.savedForm = CurrentForm;

        if (comingFromCat && newState == humanState)
        {
            SneakWindowActivatedAt = Time.time;
            Debug.Log("[PlayerTransformController] 스니크 윈도우 활성화");
        }

    }
}
