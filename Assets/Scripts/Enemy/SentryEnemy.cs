using System;
using UnityEngine;

/// <summary>
/// 시야 콘 기반 순찰형 적. 비밀방 게이트키퍼 역할.
/// 카멜레온 정지 시 감지 불가, 이동 시 감지 거리 60%, 점프/낙하 시 정상 감지.
/// 감지 시 OnSentryDetected 이벤트 발행 (비밀방 연출은 Day 10에 연결 예정).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class SentryEnemy : EnemyBase
{
    [Header("View Cone")]
    [SerializeField] private float _viewAngle = 60f;
    [SerializeField] private float _viewDistance = 6f;

    // 카멜레온 점프/낙하 판정 기준 수직 속도
    // TODO: 카멜레온 → 고양이 리네임 후 주석 업데이트
    [SerializeField] private float _jumpVelocityThreshold = 0.5f;

    [Header("Patrol Waypoints")]
    [SerializeField] private Transform[] _patrolPoints;
    [SerializeField] private float _patrolSpeed = 2f;
    [SerializeField] private float _waypointReachDistance = 0.2f;

    [Header("Combat Transition")]
    [SerializeField] private float _detectTransitionDuration = 0.5f;

    [Header("Chase / Attack")]
    [SerializeField] private float _chaseSpeed = 5f;
    [SerializeField] private float _verticalChaseThreshold = 2f;
    [SerializeField] private float _searchDuration = 3f;
    [SerializeField] private float _attackCooldown = 1.5f;
    [SerializeField] private float _attackWindup = 0.5f;
    [SerializeField] private float _wallCheckDistance = 0.3f;
    [SerializeField] private float _edgeCheckDistance = 0.5f;
    [SerializeField] private LayerMask _groundLayer;

    // Day 10 비밀방 부서짐 연출과 연결 예정. 현재는 빈 이벤트.
    public event Action OnSentryDetected;

    // ViewConeVisualizer가 읽는 프로퍼티
    public float ViewAngle => _viewAngle;
    public float ViewDistance => _viewDistance;
    public int FacingDirection => _facingDirection;

    private Rigidbody2D _rb;
    private IEnemyAttack _attackBehavior;
    private int _facingDirection = 1;

    // 순찰
    private int _patrolIndex;
    private int _patrolDirection = 1;

    // 타이머
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
                UpdatePatrolMovement();
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

                    if (IsPlayerTooHigh() && !IsPlayerInViewRange())
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
                float diffX = _lastKnownPlayerPosition.x - transform.position.x;
                if (!_arrivedAtSearch && Mathf.Abs(diffX) > 0.3f && !IsBlockedToward(_facingDirection))
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
                bool hasWaypoints = _patrolPoints != null && _patrolPoints.Length > 1;
                _rb.linearVelocity = new Vector2(
                    hasWaypoints ? _facingDirection * _patrolSpeed : 0f,
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
        // 1. 최대 시야 거리 내 플레이어 존재 확인
        Collider2D hit = Physics2D.OverlapCircle(transform.position, _viewDistance, _playerLayer);
        if (hit == null)
        {
            _player = null;
            return false;
        }

        Transform playerTransform = hit.transform.root;

        // 2. 고양이 형태별 감지 거리 조정
        float effectiveDistance = _viewDistance;
        CatStealth stealth = playerTransform.GetComponent<CatStealth>();
        if (stealth != null && stealth.enabled)
        {
            if (!stealth.IsDetectable)
            {
                // 정지 → 완전 감지 무효
                _player = null;
                return false;
            }

            Rigidbody2D playerRb = playerTransform.GetComponent<Rigidbody2D>();
            bool isAirborne = playerRb != null
                && Mathf.Abs(playerRb.linearVelocity.y) > _jumpVelocityThreshold;

            if (!isAirborne)
                effectiveDistance = _viewDistance * 0.6f; // 수평 이동 → 거리 60%
            // 점프/낙하 → effectiveDistance 변경 없음 (정상 감지)
        }

        // 3. 유효 거리 체크
        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist > effectiveDistance)
        {
            _player = null;
            return false;
        }

        // 4. 시야 각도 체크
        Vector2 dirToPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        float angle = Vector2.Angle(Vector2.right * _facingDirection, dirToPlayer);
        if (angle > _viewAngle * 0.5f)
        {
            _player = null;
            return false;
        }

        // 5. LoS 체크 — 벽에 가려지면 감지 실패
        RaycastHit2D obstacle = Physics2D.Raycast(
            (Vector2)transform.position + Vector2.up * 0.5f,
            dirToPlayer, dist, _obstacleLayer
        );
        if (obstacle.collider != null)
        {
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
                _detectTimer = _detectTransitionDuration;
                Debug.Log("[SentryEnemy] 플레이어 감지 — Combat Transition 진입");
                OnSentryDetected?.Invoke(); // Day 10 비밀방 연출 연결 예정
                break;

            case EnemyState.Combat:
                _searchTimer = _searchDuration;
                _arrivedAtSearch = false;
                break;

            case EnemyState.Attack:
                _attackTimer = _attackWindup;
                break;

            case EnemyState.Patrol:
                // 순찰 재개 시 가장 가까운 웨이포인트부터 시작
                _patrolIndex = GetNearestPatrolIndex();
                break;
        }

        base.ChangeState(newState);
    }

    // ── 순찰 이동 ─────────────────────────────────────────────
    private void UpdatePatrolMovement()
    {
        if (_patrolPoints == null || _patrolPoints.Length < 2) return;

        Transform target = _patrolPoints[_patrolIndex];
        if (target == null) return;

        float diffX = target.position.x - transform.position.x;

        if (Mathf.Abs(diffX) <= _waypointReachDistance)
        {
            // 웨이포인트 도달 → 다음 포인트로
            _patrolIndex += _patrolDirection;

            if (_patrolIndex >= _patrolPoints.Length)
            {
                _patrolDirection = -1;
                _patrolIndex = _patrolPoints.Length - 2;
            }
            else if (_patrolIndex < 0)
            {
                _patrolDirection = 1;
                _patrolIndex = 1;
            }
        }
        else
        {
            UpdateFacing(diffX > 0 ? 1 : -1);
        }
    }

    private int GetNearestPatrolIndex()
    {
        if (_patrolPoints == null || _patrolPoints.Length == 0) return 0;

        int nearest = 0;
        float minDist = float.MaxValue;
        for (int i = 0; i < _patrolPoints.Length; i++)
        {
            if (_patrolPoints[i] == null) continue;
            float d = Mathf.Abs(_patrolPoints[i].position.x - transform.position.x);
            if (d < minDist)
            {
                minDist = d;
                nearest = i;
            }
        }
        return nearest;
    }

    // ── 이동 헬퍼 ─────────────────────────────────────────────
    private void UpdateFacing(int direction)
    {
        _facingDirection = direction;
        transform.localScale = new Vector3(direction, 1f, 1f);
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
        if (_player == null) return false;
        return _player.position.y - transform.position.y > _verticalChaseThreshold;
    }

    private bool IsPlayerInViewRange()
    {
        return Physics2D.OverlapCircle(transform.position, _viewDistance, _playerLayer) != null;
    }

    // ── 에디터 시각화 ─────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        // 시야 거리
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, _viewDistance);

        // 시야 콘
        Vector3 facing = Vector3.right * _facingDirection;
        Vector3 leftRay = Quaternion.Euler(0f, 0f, _viewAngle * 0.5f) * facing;
        Vector3 rightRay = Quaternion.Euler(0f, 0f, -_viewAngle * 0.5f) * facing;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, leftRay * _viewDistance);
        Gizmos.DrawRay(transform.position, rightRay * _viewDistance);

        // 이동 시 축소 거리 (60%)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, _viewDistance * 0.6f);
    }
}
