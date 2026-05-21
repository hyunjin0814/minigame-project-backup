using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(EnemySight))]
[RequireComponent(typeof(EnemyMovement))]
public class EnemyController : MonoBehaviour
{
    public enum EnemyState
    {
        Patrol,
        Alert,
        Chase,
        Search,
        Attack,
    }

    // SightConeRenderer가 콘 색상 결정에 사용
    public EnemyState CurrentState => state;

    [SerializeField]
    private float attackCooldown = 1.5f;

    [SerializeField]
    private float attackWindup = 0.5f;

    [SerializeField]
    private float alertDuration = 1f;

    [SerializeField]
    private float alertRadiusMultiplier = 1.4f;

    public float AlertRadiusMultiplier => alertRadiusMultiplier;

    [SerializeField]
    private float searchDuration = 3f;

    [SerializeField]
    private float chaseCircleRadius = 7f;

    [SerializeField]
    private float attackCircleRadius = 9f;

    [SerializeField]
    private float attackOutOfRangeGrace = 1.5f;

    [SerializeField]
    private float verticalChaseThreshold = 2f;

    // SightConeRenderer가 원형 메시 크기 계산에 사용
    public float ChaseCircleRadius => chaseCircleRadius;
    public float AttackCircleRadius => attackCircleRadius;

    private EnemyState state = EnemyState.Patrol;
    private EnemySight sight;
    private Health health;
    private IEnemyAttack attackBehavior;
    private EnemyMovement movement;
    private float attackTimer;
    private float alertTimer;
    private float searchTimer;
    private float attackOutOfRangeTimer;
    private bool chaseIsLunging;
    private Vector2 lastSeenPosition;

    private void Awake()
    {
        sight = GetComponent<EnemySight>();
        health = GetComponent<Health>();
        attackBehavior = GetComponent<IEnemyAttack>();
        movement = GetComponent<EnemyMovement>();
    }

    private void OnEnable() => health.OnDeath += HandleDeath;

    private void OnDisable() => health.OnDeath -= HandleDeath;

    private void Update()
    {
        switch (state)
        {
            case EnemyState.Patrol:
                movement.PatrolTick();
                if (sight.CanSeePlayer())
                    ChangeState(EnemyState.Alert);
                break;

            case EnemyState.Alert:
                alertTimer -= Time.deltaTime;
                if (alertTimer > 0f)
                    break;
                // 1초 풀 대기 후 한 번만 판정 — 확장 시야(1.4배)로 재확인
                ChangeState(
                    sight.CanSeePlayer(alertRadiusMultiplier)
                        ? EnemyState.Chase
                        : EnemyState.Patrol
                );
                break;

            case EnemyState.Chase:
                // 수직 추격 차단 — 원형 감지 범위 밖이면서 너무 높을 때만 포기
                if (IsPlayerTooHigh() && !sight.IsPlayerInCircle(chaseCircleRadius))
                {
                    ChangeState(EnemyState.Search);
                    break;
                }

                // 하이브리드: 원형 안이면 동적 추적, 밖이면 lastSeenPosition 돌진
                bool inChaseCircle = sight.IsPlayerInCircle(chaseCircleRadius);
                chaseIsLunging = !inChaseCircle;
                if (inChaseCircle)
                {
                    movement.ChaseTick();
                    lastSeenPosition = sight.Player.position;
                }
                else
                {
                    movement.ChaseTowardTarget(lastSeenPosition);
                    int dirToTarget =
                        lastSeenPosition.x > transform.position.x ? 1 : -1;
                    // 도착하거나 벽/절벽에 막히면 Search로 (벽 너머 추격 포기)
                    if (
                        movement.ArrivedAtChaseTarget(lastSeenPosition)
                        || movement.IsBlockedToward(dirToTarget)
                    )
                    {
                        ChangeState(EnemyState.Search);
                        break;
                    }
                }
                // 동적 모드(원형 안)에서만 Attack 전환 — lunge 중엔 LoS 차단된 상태일 수 있음
                if (
                    inChaseCircle
                    && attackBehavior != null
                    && attackBehavior.IsInRange(sight.Player)
                )
                    ChangeState(EnemyState.Attack);
                break;

            case EnemyState.Search:
                movement.SearchTick(lastSeenPosition);
                // 두리번 도중 시야각에 재감지 → Alert (Chase/Attack 직행 아님)
                if (sight.CanSeePlayer())
                {
                    ChangeState(EnemyState.Alert);
                    break;
                }
                // 두리번 타이머는 도착한 후에만 카운트 — 이동 시간 제외
                if (movement.ArrivedAtSearch)
                {
                    searchTimer -= Time.deltaTime;
                    if (searchTimer <= 0f)
                        ChangeState(EnemyState.Patrol);
                }
                break;

            case EnemyState.Attack:
                // 수직 추격 차단 — 원형 감지 범위 밖이면서 너무 높을 때만 포기
                if (IsPlayerTooHigh() && !sight.IsPlayerInCircle(attackCircleRadius))
                {
                    ChangeState(EnemyState.Search);
                    break;
                }

                // Attack 원형 감지 (Chase보다 넓음, 벽 차단)
                bool inAttackCircle = sight.IsPlayerInCircle(attackCircleRadius);
                if (inAttackCircle)
                {
                    lastSeenPosition = sight.Player.position;
                    attackOutOfRangeTimer = 0f;
                }
                else
                {
                    // 원형 밖에 머문 시간 누적 — grace 끝나면 Search
                    attackOutOfRangeTimer += Time.deltaTime;
                    if (attackOutOfRangeTimer >= attackOutOfRangeGrace)
                    {
                        ChangeState(EnemyState.Search);
                        break;
                    }
                }

                // 사거리 안: 정지 + 공격 모션 / 사거리 밖 + 원형 안: 추격 이동
                if (sight.Player != null && attackBehavior.IsInRange(sight.Player))
                {
                    movement.ChaseTick(); // 방향만 갱신
                    attackTimer -= Time.deltaTime;
                    if (attackTimer <= 0f)
                    {
                        attackBehavior.DoAttack(sight.Player);
                        attackTimer = attackCooldown;
                    }
                }
                else if (sight.Player != null)
                {
                    movement.ChaseTick();
                }
                // sight.Player == null (grace 중): 이동 없이 대기
                break;
        }
    }

    private void FixedUpdate()
    {
        switch (state)
        {
            case EnemyState.Patrol:
                movement.ApplyPatrolVelocity();
                break;
            case EnemyState.Alert:
                movement.ApplyAlertVelocity();
                break;
            case EnemyState.Chase:
                movement.ApplyChaseVelocity(chaseIsLunging);
                break;
            case EnemyState.Search:
                movement.ApplySearchVelocity();
                break;
            case EnemyState.Attack:
                // 사거리 안: 정지(공격 중) / 사거리 밖 + 원형 안: 추격 / grace 중: 대기
                if (sight.Player != null && attackBehavior.IsInRange(sight.Player))
                    movement.ApplyStopVelocity();
                else if (sight.Player != null)
                    movement.ApplyChaseVelocity();
                else
                    movement.ApplyStopVelocity();
                break;
        }
    }

    private void ChangeState(EnemyState newState)
    {
        if (newState == EnemyState.Alert)
            alertTimer = alertDuration;

        if (newState == EnemyState.Search)
        {
            searchTimer = searchDuration;
            movement.ResetSearch();
        }

        if (newState == EnemyState.Attack)
        {
            attackOutOfRangeTimer = 0f;
            attackTimer = attackWindup;   // 첫 공격까지 windup 대기 (회피 타이밍 부여)
        }
        else
        {
            attackTimer = 0f;
        }

        if (newState == EnemyState.Chase)
            chaseIsLunging = false;   // 진입 시점에는 원형 안 (Alert 재확인 통과)

        state = newState;
    }

    public void ReactToHit(Vector2 sourcePosition)
    {
        movement.Flip(sourcePosition.x > transform.position.x ? 1 : -1);
        if (state == EnemyState.Patrol)
            ChangeState(EnemyState.Chase);
    }

    // 플레이어가 적보다 임계값 이상 위에 있는지 (수직 추격 차단용)
    private bool IsPlayerTooHigh()
    {
        if (sight.Player == null)
            return false;
        return sight.Player.position.y - transform.position.y > verticalChaseThreshold;
    }

    private void HandleDeath() => gameObject.SetActive(false);
}
