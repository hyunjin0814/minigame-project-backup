using UnityEngine;

/// <summary>
/// 공중 부유형 원거리 적.
/// 플레이어를 감지하면 측면 사격 위치로 비행 후 발사체 공격.
/// 피격 시 _retreatDist만큼 후퇴 (최대 _maxRetreatDist 이내).
/// gravityScale=0 — 중력 무시, XY 2D 자유 이동.
///
/// [프리팹 설정]
///  - Rigidbody2D: gravityScale=0, Freeze Rotation Z
///  - RangedAttack 컴포넌트 + projectilePrefab 할당
///  - _playerLayer, _obstacleLayer, _groundLayer 레이어 설정
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class FlyingShooterEnemy : EnemyBase
{
    [Header("Detection")]
    [SerializeField] private float _detectRadius        = 12f;
    [SerializeField] private float _sneakWindowDuration = 1.5f;

    [Header("Flight")]
    [SerializeField] private float _flySpeed          = 4f;
    [SerializeField] private float _retreatSpeed      = 6f;
    [SerializeField] private float _hoverOffsetY      = 2.5f;  // 공격 위치의 플레이어 기준 y 오프셋
    [SerializeField] private float _patrolBobAmp      = 0.5f;  // 유영 상하 진폭
    [SerializeField] private float _patrolBobFreq     = 1.0f;  // 유영 상하 주파수(Hz)
    [SerializeField] private float _wallCheckDistance = 0.5f;  // 순찰 중 장애물 감지 거리

    [Header("Combat Range")]
    [SerializeField] private float _attackRange    = 7f;  // 공격 위치까지 거리
    [SerializeField] private float _retreatDist    = 5f;  // 피격 시 후퇴 거리
    [SerializeField] private float _maxRetreatDist = 10f; // 플레이어로부터 최대 후퇴 거리

    [Header("Attack")]
    [SerializeField] private float _attackCooldown     = 1.8f;
    [SerializeField] private float _attackWindup       = 0.5f;
    [Tooltip("true: Animation Event(AnimFireProjectile)로 발사 타이밍 제어. false: 타이머 만료 즉시 발사.")]
    [SerializeField] private bool  _useAnimationEvent  = false;

    [Header("State Timers")]
    [SerializeField] private float _detectDelay = 0.4f;

    private Rigidbody2D  _rb;
    private IEnemyAttack _attackBehavior;
    private int     _facingDirection = 1;
    private float   _detectTimer;
    private float   _attackTimer;
    private Vector2 _retreatTarget;
    private Vector2 _spawnPos;

    // ── 라이프사이클 ──────────────────────────────────────────────
    protected override void Awake()
    {
        base.Awake();
        _rb              = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _attackBehavior  = GetComponent<IEnemyAttack>();
        _losePlayerRange = _detectRadius + 2f; // 감지 반경보다 살짝 크게
    }

    private void Start()
    {
        _spawnPos = transform.position;
    }

    // ── Update ───────────────────────────────────────────────────
    protected override void Update()
    {
        base.Update(); // TickDebuff + TickWeakness + TickKnockback + TickPlayerLost
        if (IsDead) return;

        switch (_currentState)
        {
            case EnemyState.Patrol:
                CheckFlightPatrolFlip();
                if (DetectPlayer()) ChangeState(EnemyState.Detect);
                break;

            case EnemyState.Detect:
                _detectTimer -= Time.deltaTime;
                if (_detectTimer <= 0f) ChangeState(EnemyState.Chase);
                break;

            case EnemyState.Chase:
            {
                if (_player == null) { ChangeState(EnemyState.Patrol); break; }
                _lastKnownPlayerPosition = _player.position;
                UpdateFacing(_player.position.x > transform.position.x ? 1 : -1);

                if (Vector2.Distance(transform.position, GetAttackPosition()) < 0.8f)
                    ChangeState(EnemyState.Attack);
                break;
            }

            case EnemyState.Attack:
            {
                if (_player == null) { ChangeState(EnemyState.Patrol); break; }
                _lastKnownPlayerPosition = _player.position;
                UpdateFacing(_player.position.x > transform.position.x ? 1 : -1);

                _attackTimer -= Time.deltaTime;
                if (_attackTimer <= 0f)
                {
                    RaiseAttackPerformed();
                    if (!_useAnimationEvent) FireProjectile();
                    _attackTimer = _attackCooldown;
                }
                break;
            }

            case EnemyState.Combat: // 피격 후 후퇴
            {
                float distToTarget = Vector2.Distance(transform.position, _retreatTarget);
                float distToPlayer = _player != null
                    ? Vector2.Distance(transform.position, _player.position)
                    : float.MaxValue;

                if (distToTarget < 0.5f || distToPlayer >= _maxRetreatDist)
                {
                    ChangeState(EnemyState.Chase);
                    break;
                }

                if (_player != null)
                    UpdateFacing(_player.position.x > transform.position.x ? 1 : -1);
                break;
            }
        }
    }

    // ── FixedUpdate ──────────────────────────────────────────────
    private void FixedUpdate()
    {
        if (IsDead) { _rb.linearVelocity = Vector2.zero; return; }

        if (_isKnockedBack)
        {
            _rb.linearVelocity = new Vector2(_knockbackVelocity.x, _rb.linearVelocity.y);
            return;
        }

        switch (_currentState)
        {
            case EnemyState.Patrol:
            {
                float bobVel = Mathf.Sin(Time.time * _patrolBobFreq) * _patrolBobAmp;
                // 위/아래 장애물에 막히면 수직 속도 차단
                if (bobVel > 0f && Physics2D.Raycast(transform.position, Vector2.up,    _wallCheckDistance, _obstacleLayer)) bobVel = 0f;
                if (bobVel < 0f && Physics2D.Raycast(transform.position, Vector2.down,  _wallCheckDistance, _obstacleLayer)) bobVel = 0f;
                _rb.linearVelocity = new Vector2(_facingDirection * 1.5f, bobVel);
                break;
            }

            case EnemyState.Detect:
                _rb.linearVelocity = Vector2.zero;
                break;

            case EnemyState.Chase:
            {
                Vector2 dir = (GetAttackPosition() - (Vector2)transform.position).normalized;
                _rb.linearVelocity = dir * _flySpeed;
                break;
            }

            case EnemyState.Attack:
            {
                // 공격 위치에서 미세 보정 호버링
                Vector2 diff = GetAttackPosition() - (Vector2)transform.position;
                _rb.linearVelocity = diff.magnitude > 0.5f
                    ? diff.normalized * (_flySpeed * 0.4f)
                    : Vector2.zero;
                break;
            }

            case EnemyState.Combat:
            {
                Vector2 dir = (_retreatTarget - (Vector2)transform.position).normalized;
                _rb.linearVelocity = dir * _retreatSpeed;
                break;
            }
        }
    }

    // ── 피격 오버라이드 — 후퇴 목표 설정 후 Combat 전환 ───────────
    protected override void OnHit(Vector2 attackerPosition)
    {
        base.OnHit(attackerPosition); // Hurt 이벤트 + hitbox 정리 + 넉백
        if (IsDead) return;

        Vector2 selfPos   = transform.position;
        Vector2 playerPos = _player != null ? (Vector2)_player.position : attackerPosition;
        Vector2 awayDir   = (selfPos - playerPos).normalized;
        if (awayDir == Vector2.zero) awayDir = Vector2.up;

        Vector2 candidate = selfPos + awayDir * _retreatDist;
        if (Vector2.Distance(candidate, playerPos) > _maxRetreatDist)
            candidate = playerPos + awayDir * _maxRetreatDist;

        _retreatTarget = candidate;
        ChangeState(EnemyState.Combat);
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
            (Vector2)transform.position + Vector2.up * 0.3f, dir, dist, _obstacleLayer);
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
                Debug.Log("[FlyingShooterEnemy] 플레이어 감지 — Detect 진입");
                break;
            case EnemyState.Patrol:
                _facingDirection = transform.position.x < _spawnPos.x ? 1 : -1;
                break;
            case EnemyState.Attack:
                _attackTimer = _attackWindup;
                break;
        }
        base.ChangeState(newState);
    }

    // ── 투사체 발사 ───────────────────────────────────────────────
    private void FireProjectile()
    {
        if (_player == null) return;
        _attackBehavior?.DoAttack(_player);
    }

    // ── Animation Event 핸들러 ───────────────────────────────────
    // Attack 클립의 원하는 프레임에 Function=AnimFireProjectile로 등록
    public void AnimFireProjectile() => FireProjectile();

    // ── 헬퍼 ─────────────────────────────────────────────────────
    // 순찰 중 수평 장애물 감지 → 방향 반전
    private void CheckFlightPatrolFlip()
    {
        if (Physics2D.Raycast(transform.position, Vector2.right * _facingDirection, _wallCheckDistance, _obstacleLayer))
            UpdateFacing(-_facingDirection);
    }

    // 공격 위치: 플레이어 측면(_attackRange * 0.8f) + 위(_hoverOffsetY)
    private Vector2 GetAttackPosition()
    {
        if (_player == null) return transform.position;
        float side = transform.position.x >= _player.position.x ? 1f : -1f;
        return new Vector2(
            _player.position.x + side * _attackRange * 0.8f,
            _player.position.y + _hoverOffsetY
        );
    }

    private void UpdateFacing(int direction)
    {
        _facingDirection = direction;
        transform.localScale = new Vector3(direction, 1f, 1f);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _maxRetreatDist);
    }
#endif
}
