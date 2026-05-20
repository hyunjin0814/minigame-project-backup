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
                movement.ChaseTick();
                // Chase 원형 감지 (반경 7, 벽 차단) — 인지 후엔 시야각이 아닌 원형
                if (sight.IsPlayerInCircle(chaseCircleRadius))
                {
                    lastSeenPosition = sight.Player.position;
                }
                else
                {
                    ChangeState(EnemyState.Search);
                    break;
                }
                if (attackBehavior != null && attackBehavior.IsInRange(sight.Player))
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
                movement.ApplyChaseVelocity();
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
            attackOutOfRangeTimer = 0f;

        attackTimer = 0f;
        state = newState;
    }

    public void ReactToHit(Vector2 sourcePosition)
    {
        movement.Flip(sourcePosition.x > transform.position.x ? 1 : -1);
        if (state == EnemyState.Patrol)
            ChangeState(EnemyState.Chase);
    }

    private void HandleDeath() => gameObject.SetActive(false);
}
