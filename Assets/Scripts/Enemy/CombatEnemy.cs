using UnityEngine;

/// <summary>
/// 원형 감지 기반 지상 전투형 적.
/// 후방 기습(Idle/Patrol/Detect 상태에서 등 뒤 공격) 시 데미지 배율 적용.
/// 공중 이동·점프 없음 — 지상 전용.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class CombatEnemy : EnemyBase
{
    [Header("Detection")]
    [SerializeField]
    private float _detectRadius = 10f;

    // 카멜레온 → 인간 변신 후 이 시간(초) 동안 감지 무효. 적마다 다르게 설정해 난이도 조절.
    // TODO: 카멜레온 → 고양이 리네임 후 주석 업데이트
    [SerializeField]
    private float _sneakWindowDuration = 1.5f;

    [Header("Movement")]
    [SerializeField]
    private float _patrolSpeed = 3f;

    [SerializeField]
    private float _chaseSpeed = 5f;

    [SerializeField]
    private float _wallCheckDistance = 0.3f;

    [SerializeField]
    private float _edgeCheckDistance = 0.5f;

    [SerializeField]
    private LayerMask _groundLayer;

    [Header("State Timers")]
    [SerializeField]
    private float _detectDelay = 0.3f;

    [SerializeField]
    private float _verticalChaseThreshold = 2f;

    [SerializeField]
    private float _searchDuration = 3f;

    [Header("Combat")]
    [SerializeField]
    private float _attackCooldown = 1.5f;

    [SerializeField]
    private float _attackWindup = 0.5f;

    [SerializeField]
    private int _backstabMultiplier = 3;

    private Rigidbody2D _rb;
    private IEnemyAttack _attackBehavior;
    private int _facingDirection = 1;
    private float _detectTimer;
    private float _searchTimer;
    private float _attackTimer;
    private bool _arrivedAtSearch;

    // ── 라이프사이클 ─────────────────────────────────────────
    protected override void Awake()
    {
        base.Awake();
        _rb = GetComponent<Rigidbody2D>();
        _attackBehavior = GetComponent<IEnemyAttack>();
    }

    protected override void Update()
    {
        base.Update(); // TickDebuff

        switch (_currentState)
        {
            case EnemyState.Patrol:
                CheckPatrolFlip();
                if (DetectPlayer())
                    ChangeState(EnemyState.Detect);
                break;

            case EnemyState.Detect:
                _detectTimer -= Time.deltaTime;
                if (_detectTimer <= 0f)
                    ChangeState(EnemyState.Chase);
                break;

            case EnemyState.Chase:
                if (_player != null)
                {
                    _lastKnownPlayerPosition = _player.position;
                    UpdateFacing(_player.position.x > transform.position.x ? 1 : -1);

                    // 수직 추격 차단 — 발판 없는 맵에선 거의 발동 안 함 (안전장치)
                    if (IsPlayerTooHigh() && !IsPlayerInDetectCircle())
                    {
                        ChangeState(EnemyState.Combat);
                        break;
                    }

                    if (_attackBehavior != null && _attackBehavior.IsInRange(_player))
                        ChangeState(EnemyState.Attack);
                }
                else
                {
                    ChangeState(EnemyState.Combat);
                }
                break;

            case EnemyState.Combat:
                // lastKnownPosition까지 이동 후 대기 → Patrol 복귀
                float diffX = _lastKnownPlayerPosition.x - transform.position.x;
                if (
                    !_arrivedAtSearch
                    && (Mathf.Abs(diffX) > 0.3f)
                    && !IsBlockedToward(_facingDirection)
                )
                {
                    UpdateFacing(diffX > 0 ? 1 : -1);
                }
                else
                {
                    _arrivedAtSearch = true;
                    _searchTimer -= Time.deltaTime;
                    if (_searchTimer <= 0f)
                        ChangeState(EnemyState.Patrol);
                }

                // 수색 중 재감지 → Detect 재진입
                if (DetectPlayer())
                    ChangeState(EnemyState.Detect);
                break;

            case EnemyState.Attack:
                if (_player == null)
                {
                    ChangeState(EnemyState.Combat);
                    break;
                }

                UpdateFacing(_player.position.x > transform.position.x ? 1 : -1);

                if (_attackBehavior != null)
                {
                    if (!_attackBehavior.IsInRange(_player))
                    {
                        ChangeState(EnemyState.Chase);
                        break;
                    }
                    _attackTimer -= Time.deltaTime;
                    if (_attackTimer <= 0f)
                    {
                        _attackBehavior.DoAttack(_player);
                        _attackTimer = _attackCooldown;
                    }
                }
                break;
        }
    }

    private void FixedUpdate()
    {
        switch (_currentState)
        {
            case EnemyState.Patrol:
                _rb.linearVelocity = new Vector2(
                    _facingDirection * _patrolSpeed,
                    _rb.linearVelocity.y
                );
                break;
            case EnemyState.Detect:
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                break;
            case EnemyState.Chase:
                _rb.linearVelocity = new Vector2(
                    _facingDirection * _chaseSpeed,
                    _rb.linearVelocity.y
                );
                break;
            case EnemyState.Combat:
                float xVel = _arrivedAtSearch ? 0f : _facingDirection * _patrolSpeed;
                _rb.linearVelocity = new Vector2(xVel, _rb.linearVelocity.y);
                break;
            case EnemyState.Attack:
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                break;
        }
    }

    // ── DetectPlayer ─────────────────────────────────────────
    protected override bool DetectPlayer()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, _detectRadius, _playerLayer);
        if (hit == null)
        {
            _player = null;
            return false;
        }

        Transform playerTransform = hit.transform.root;

        // LoS 체크 — 벽에 가려지면 감지 실패
        Vector2 dir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        float dist = Vector2.Distance(transform.position, playerTransform.position);
        RaycastHit2D obstacle = Physics2D.Raycast(
            (Vector2)transform.position + Vector2.up * 0.5f,
            dir,
            dist,
            _obstacleLayer
        );
        if (obstacle.collider != null)
        {
            _player = null;
            return false;
        }

        // 은신 체크 — 고양이 정지 시 감지 무효
        CatStealth stealth = playerTransform.GetComponent<CatStealth>();
        if (stealth != null && !stealth.IsDetectable)
        {
            _player = null;
            return false;
        }

        // 스니크 윈도우 체크 — 고양이 → 인간 변신 후 _sneakWindowDuration 동안 감지 무효
        PlayerTransformController ptc = playerTransform.GetComponent<PlayerTransformController>();
        if (ptc != null && Time.time - ptc.SneakWindowActivatedAt < _sneakWindowDuration)
        {
            Debug.Log("[CombatEnemy] 스니크 윈도우 활성 — 감지 무효");
            _player = null;
            return false;
        }

        _player = playerTransform;
        _lastKnownPlayerPosition = _player.position;
        return true;
    }

    // ── 상태 전환 ─────────────────────────────────────────────
    protected override void ChangeState(EnemyState newState)
    {
        switch (newState)
        {
            case EnemyState.Detect:
                _detectTimer = _detectDelay;
                Debug.Log("[CombatEnemy] 플레이어 감지 — Detect 진입");
                break;
            case EnemyState.Combat:
                _searchTimer = _searchDuration;
                _arrivedAtSearch = false;
                break;
            case EnemyState.Attack:
                _attackTimer = _attackWindup;
                break;
        }
        base.ChangeState(newState);
    }

    // ── 데미지 모디파이어 ─────────────────────────────────────
    // 약점·디버프 공통 로직은 EnemyBase.ComputeFinalDamage에서 처리.
    // 여기서는 백스탭 배율만 계산.
    protected override int ApplySpecialModifier(int damage, Vector2 source)
    {
        return IsBackstabCondition(source) ? damage * _backstabMultiplier : damage;
    }

    private bool IsBackstabCondition(Vector2 attackerPos)
    {
        // 전투 중(Chase/Combat/Attack)에는 백스탭 없음
        if (
            _currentState == EnemyState.Chase
            || _currentState == EnemyState.Combat
            || _currentState == EnemyState.Attack
        )
            return false;

        // 공격자가 facingDirection과 같은 방향 = 등 뒤에서 공격
        int dirToAttacker = attackerPos.x > transform.position.x ? 1 : -1;
        return dirToAttacker == _facingDirection;
    }

    // ── 이동 헬퍼 ─────────────────────────────────────────────
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
        bool wallAhead = Physics2D.Raycast(
            transform.position,
            Vector2.right * direction,
            _wallCheckDistance,
            _groundLayer
        );

        Vector2 edgeOrigin = (Vector2)transform.position + Vector2.right * direction * 0.5f;
        bool noGround = !Physics2D.Raycast(
            edgeOrigin,
            Vector2.down,
            _edgeCheckDistance,
            _groundLayer
        );

        return wallAhead || noGround;
    }

    private bool IsPlayerTooHigh()
    {
        if (_player == null)
            return false;
        return _player.position.y - transform.position.y > _verticalChaseThreshold;
    }

    private bool IsPlayerInDetectCircle()
    {
        return Physics2D.OverlapCircle(transform.position, _detectRadius, _playerLayer) != null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectRadius);
    }
}
