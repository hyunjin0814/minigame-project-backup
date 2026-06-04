using UnityEngine;

/// <summary>
/// SentryEnemy 기반 방패형 적. 스탯 강화 변종.
/// 피격 시 공격자 방향으로 방패를 들어 _guardDuration 동안 "정면 한정" 완전 방어 → _counterDuration 동안 반격.
/// 쉴드 → 반격 시퀀스는 도중에 끊기지 않고 끝까지 진행된다(IsActionLocked로 상태머신 정지).
///   - 가드 중: 바라보는 방향(정면)에서 온 피해만 무효, 등 뒤 공격은 정상 피해.
///   - 반격 중: 무적 아님(모든 방향 피해), 단 시퀀스는 안 끊김.
/// 반격 데미지는 Counter 클립의 Animation Event가 켜는 counterHitbox(CounterAttack)가 담당.
/// </summary>
public class EliteEnemy : SentryEnemy
{
    [Header("Guard Pattern")]
    [Tooltip("방패를 들고 반격까지 버티는 시간(초). 이 동안 정면 한정 무적.")]
    [SerializeField] private float _guardDuration = 1f;
    [Tooltip("반격 모션 동안 정지·잠금되는 시간(초). Counter 클립 길이에 맞추세요.")]
    [SerializeField] private float _counterDuration = 0.6f;
    [Tooltip("반격 후 다시 가드 가능해지기까지의 쿨다운. 이 동안은 일반 피격.")]
    [SerializeField] private float _guardCooldown = 5f;

    private EnemyAnimator _animator;
    private bool _isGuarding;
    private bool _isCountering;
    private float _guardTimer;
    private float _counterTimer;
    private float _guardCooldownTimer;

    // 공격 잠금(base) + 가드/반격 시퀀스 중에는 SentryEnemy 상태머신·이동을 정지
    protected override bool IsActionLocked => base.IsActionLocked || _isGuarding || _isCountering;

    protected override void Awake()
    {
        base.Awake();
        _animator = GetComponent<EnemyAnimator>();
    }

    protected override void Update()
    {
        base.Update();
        if (IsDead) return;
        TickGuard();
    }

    private void TickGuard()
    {
        if (_guardCooldownTimer > 0f) _guardCooldownTimer -= Time.deltaTime;

        if (_isGuarding)
        {
            _guardTimer -= Time.deltaTime;
            if (_guardTimer <= 0f) StartCounter();
        }
        else if (_isCountering)
        {
            _counterTimer -= Time.deltaTime;
            if (_counterTimer <= 0f) EndCounter();
        }
    }

    protected override void OnHit(Vector2 attackerPosition)
    {
        // 사망 타격이면 가드 진입하지 않고 일반 처리(Die)에 맡김
        if (_health.CurrentHp <= 0) return;

        // 가드/반격 시퀀스 중 피격은 반응 무시 (등 뒤 피해는 들어가되 시퀀스는 끝까지 진행)
        if (_isGuarding || _isCountering) return;

        // 쿨다운이 끝났으면: 이번 피격은 정상으로 받되, 공격자 쪽으로 방패를 들고 _guardDuration 후 반격
        if (_guardCooldownTimer <= 0f)
        {
            _lastKnownPlayerPosition = attackerPosition;
            EnterGuard(attackerPosition);
            return;
        }

        // 쿨다운 중 → 일반 피격(Hurt 애니, 넉백, Chase 전환 등)
        base.OnHit(attackerPosition);
    }

    private void EnterGuard(Vector2 attackerPosition)
    {
        // 공격자(플레이어) 쪽으로 방패를 듦 → 그 정면에서만 막힌다
        UpdateFacing(attackerPosition.x > transform.position.x ? 1 : -1);

        _isGuarding = true;
        _guardTimer = _guardDuration;
        _guardCooldownTimer = _guardCooldown;
        _animator?.PlayGuard(); // 방패 들기 애니메이션
        Debug.Log("[EliteEnemy] 방어 자세 진입 — 정면 한정 방어, 반격 대기");
    }

    // 쉴드 종료 → 반격 시작 (무조건 진행). 반격 중엔 무적 아님 — 시퀀스만 안 끊김.
    private void StartCounter()
    {
        _isGuarding = false;
        _isCountering = true;
        _counterTimer = _counterDuration;
        _animator?.PlayCounter(); // Counter 클립이 Animation Event로 counterHitbox를 켬
        Debug.Log("[EliteEnemy] 반격 시작");
    }

    private void EndCounter()
    {
        _isCountering = false;
        ChangeState(EnemyState.Chase);
        Debug.Log("[EliteEnemy] 반격 종료 → 추격 재개");
    }

    // 가드 중 바라보는 방향(정면)에서 온 피해만 완전 차단. 등 뒤·반격 중에는 정상 피해.
    protected override bool BlocksDamageFrom(Vector2 source)
    {
        if (!_isGuarding) return false;
        int dirToAttacker = source.x > transform.position.x ? 1 : -1;
        return dirToAttacker == FacingDirection;
    }
}
