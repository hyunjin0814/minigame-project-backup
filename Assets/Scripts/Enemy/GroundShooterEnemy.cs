using UnityEngine;

/// <summary>
/// 지상 원거리 적. Walk로 순찰하다 플레이어 감지 시 제자리에서 수평 투사체를 반복 발사한다.
/// 플레이어가 _losePlayerRange 밖으로 나가면 Patrol 복귀 (EnemyBase.TickPlayerLost 공통 처리).
///
/// AnimatorController 파라미터:
///   float  SpeedX  — 0이면 Idle, 0 초과이면 Walk (EnemyAnimator 자동 설정)
///   trigger Attack  — 발사 시 1회 재생 후 Idle 복귀
///   trigger Hit     — 피격 시
///   trigger Death   — 사망 시
///
/// [프리팹 설정]
///   - EnemyAnimator 컴포넌트 + AnimatorController 할당
///   - RangedAttack 컴포넌트 + projectilePrefab 할당
///   - _groundLayer, _playerLayer, _obstacleLayer 레이어 설정
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class GroundShooterEnemy : EnemyBase
{
    [Header("Detection")]
    [SerializeField] private float _detectRadius        = 10f;
    [SerializeField] private float _sneakWindowDuration = 1.5f;

    [Header("Patrol Movement")]
    [SerializeField] private float     _patrolSpeed       = 2f;
    [SerializeField] private float     _wallCheckDistance = 0.3f;
    [SerializeField] private float     _edgeCheckDistance = 0.5f;
    [SerializeField] private LayerMask _groundLayer;

    [Header("Attack")]
    [SerializeField] private float _attackCooldown    = 2f;
    [SerializeField] private float _attackWindup      = 0.5f;   // Detect → Attack 직후 첫 발사까지 대기
    [Tooltip("true: Animation Event(AnimFireProjectile)로 발사 타이밍 제어. false: 타이머 만료 즉시 발사.")]
    [SerializeField] private bool  _useAnimationEvent = false;

    [Header("State Timers")]
    [SerializeField] private float _detectDelay = 0.3f;

    private Rigidbody2D  _rb;
    private IEnemyAttack _attackBehavior;
    private int   _facingDirection = 1;
    private float _detectTimer;
    private float _attackTimer;

    // ── 라이프사이클 ──────────────────────────────────────────────
    protected override void Awake()
    {
        base.Awake();
        _rb              = GetComponent<Rigidbody2D>();
        _attackBehavior  = GetComponent<IEnemyAttack>();
        _losePlayerRange = _detectRadius + 2f;
    }

    protected override void Update()
    {
        base.Update(); // TickDebuff + TickWeakness + TickKnockback + TickPlayerLost
        if (IsDead) return;

        switch (_currentState)
        {
            case EnemyState.Patrol:
                CheckPatrolFlip();
                if (DetectPlayer()) ChangeState(EnemyState.Detect);
                break;

            case EnemyState.Detect:
                _detectTimer -= Time.deltaTime;
                if (_detectTimer <= 0f) ChangeState(EnemyState.Attack);
                break;

            case EnemyState.Attack:
                // 플레이어 방향 트래킹 (TickPlayerLost가 범위 이탈 시 Patrol 복귀 처리)
                if (_player != null)
                {
                    float dx = _player.position.x - transform.position.x;
                    UpdateFacing(dx >= 0f ? 1 : -1);
                    _lastKnownPlayerPosition = _player.position;
                }

                _attackTimer -= Time.deltaTime;
                if (_attackTimer <= 0f)
                {
                    RaiseAttackPerformed();
                    if (!_useAnimationEvent) FireProjectile();
                    _attackTimer = _attackCooldown;
                }
                break;
        }
    }

    private void FixedUpdate()
    {
        if (IsDead) return;

        if (_isKnockedBack)
        {
            _rb.linearVelocity = new Vector2(_knockbackVelocity.x, _rb.linearVelocity.y);
            return;
        }

        switch (_currentState)
        {
            case EnemyState.Patrol:
                _rb.linearVelocity = new Vector2(_facingDirection * _patrolSpeed, _rb.linearVelocity.y);
                break;
            case EnemyState.Detect:
            case EnemyState.Attack:
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                break;
        }
    }

    // ── Animation Event 핸들러 ───────────────────────────────────
    // Attack 클립의 원하는 프레임에 Function=AnimFireProjectile로 등록
    public void AnimFireProjectile() => FireProjectile();

    // ── 투사체 발사 ───────────────────────────────────────────────
    private void FireProjectile()
    {
        if (_player == null) return;
        _attackBehavior?.DoAttack(_player);
    }

    // ── 플레이어 이탈 — 수색 없이 Patrol 복귀 ────────────────────
    protected override void OnPlayerLost()
    {
        ChangeState(EnemyState.Patrol);
    }

    // ── 감지 ─────────────────────────────────────────────────────
    protected override bool DetectPlayer()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, _detectRadius, _playerLayer);
        if (hit == null) { _player = null; return false; }

        Transform pt = hit.transform.root;

        Vector2 dir  = ((Vector2)pt.position - (Vector2)transform.position).normalized;
        float   dist = Vector2.Distance(transform.position, pt.position);
        RaycastHit2D obs = Physics2D.Raycast(
            (Vector2)transform.position + Vector2.up * 0.5f, dir, dist, _obstacleLayer);
        if (obs.collider != null) { _player = null; return false; }

        CatStealth stealth = pt.GetComponent<CatStealth>();
        if (stealth != null && !stealth.IsDetectable) { _player = null; return false; }

        PlayerTransformController ptc = pt.GetComponent<PlayerTransformController>();
        if (ptc != null && Time.time - ptc.SneakWindowActivatedAt < _sneakWindowDuration)
        { _player = null; return false; }

        _player = pt;
        _lastKnownPlayerPosition = _player.position;
        return true;
    }

    // ── 상태 전환 ─────────────────────────────────────────────────
    protected override void ChangeState(EnemyState newState)
    {
        switch (newState)
        {
            case EnemyState.Detect:
                _detectTimer = _detectDelay;
                Debug.Log("[GroundShooterEnemy] 플레이어 감지 — Detect 진입");
                break;
            case EnemyState.Attack:
                _attackTimer = _attackWindup;
                Debug.Log("[GroundShooterEnemy] Attack 진입");
                break;
            case EnemyState.Patrol:
                Debug.Log("[GroundShooterEnemy] Patrol 복귀");
                break;
        }
        base.ChangeState(newState);
    }

    // ── 이동 헬퍼 ─────────────────────────────────────────────────
    private void UpdateFacing(int direction)
    {
        _facingDirection = direction;
        transform.localScale = new Vector3(direction, 1f, 1f);
    }

    private void CheckPatrolFlip()
    {
        if (IsBlockedToward(_facingDirection))
            UpdateFacing(-_facingDirection);
    }

    private bool IsBlockedToward(int direction)
    {
        bool wall = Physics2D.Raycast(
            transform.position, Vector2.right * direction, _wallCheckDistance, _groundLayer);
        Vector2 edge = (Vector2)transform.position + Vector2.right * direction * 0.5f;
        bool noGround = !Physics2D.Raycast(edge, Vector2.down, _edgeCheckDistance, _groundLayer);
        return wall || noGround;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _losePlayerRange);
    }
#endif
}
