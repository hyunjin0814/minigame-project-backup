using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

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

    public bool IsHitStopped { get; private set; }

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
        // 기본 상태: 인간
        ChangeState(humanState);
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

    private void HandleTransformDog() => ChangeState(dogState);

    private void HandleTransformCat() => ChangeState(catState);

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

        if (comingFromCat && newState == humanState)
        {
            SneakWindowActivatedAt = Time.time;
            Debug.Log("[PlayerTransformController] 스니크 윈도우 활성화");
        }

    }

    public void HitStop(Rigidbody2D enemyRb, float duration, Action onEnd = null)
    {
        StartCoroutine(HitStopCoroutine(enemyRb, duration, onEnd));
    }

    private IEnumerator HitStopCoroutine(Rigidbody2D enemyRb, float duration, Action onEnd)
    {
        IsHitStopped = true;

        Time.timeScale = 0f;

        Debug.Log($"[PlayerTransformController] 히트스톱 시작 ({duration}s)");

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = 1f;

        IsHitStopped = false;
        Debug.Log("[PlayerTransformController] 히트스톱 종료");

        onEnd?.Invoke();
    }
}
