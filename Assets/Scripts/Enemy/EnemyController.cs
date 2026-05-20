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
    private float searchDuration = 3f;

    private EnemyState state = EnemyState.Patrol;
    private EnemySight sight;
    private Health health;
    private IEnemyAttack attackBehavior;
    private EnemyMovement movement;
    private float attackTimer;
    private float alertTimer;
    private float searchTimer;
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
                // 1초 풀 대기 후 한 번만 판정
                ChangeState(sight.CanSeePlayer() ? EnemyState.Chase : EnemyState.Patrol);
                break;

            case EnemyState.Chase:
                movement.ChaseTick();
                // IsPlayerWithinRadius()가 false 반환 시 Player가 null이 되므로 그 전에 저장
                if (sight.Player != null)
                    lastSeenPosition = sight.Player.position;
                if (!sight.IsPlayerWithinRadius())
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
                searchTimer -= Time.deltaTime;
                if (searchTimer <= 0f)
                    ChangeState(EnemyState.Patrol);
                break;

            case EnemyState.Attack:
                movement.ChaseTick();
                if (sight.Player != null)
                    lastSeenPosition = sight.Player.position;
                if (!sight.IsPlayerWithinRadius())
                {
                    ChangeState(EnemyState.Search);
                    break;
                }
                if (!attackBehavior.IsInRange(sight.Player))
                {
                    ChangeState(EnemyState.Chase);
                    break;
                }
                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0f)
                {
                    attackBehavior.DoAttack(sight.Player);
                    attackTimer = attackCooldown;
                }
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
