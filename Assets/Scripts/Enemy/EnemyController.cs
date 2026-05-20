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
        Attack,
    }

    // SightConeRenderer가 콘 색상 결정에 사용
    public EnemyState CurrentState => state;

    [SerializeField]
    private float attackCooldown = 1.5f;

    [SerializeField]
    private float alertDuration = 1f;

    private EnemyState state = EnemyState.Patrol;
    private EnemySight sight;
    private Health health;
    private IEnemyAttack attackBehavior;
    private EnemyMovement movement;
    private float attackTimer;
    private float alertTimer;

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
                ChangeState(
                    sight.CanSeePlayer() ? EnemyState.Chase : EnemyState.Patrol
                );
                break;

            case EnemyState.Chase:
                movement.ChaseTick();
                if (!sight.IsPlayerWithinRadius())
                    ChangeState(EnemyState.Patrol);
                else if (attackBehavior != null && attackBehavior.IsInRange(sight.Player))
                    ChangeState(EnemyState.Attack);
                break;

            case EnemyState.Attack:
                movement.ChaseTick();
                if (!sight.IsPlayerWithinRadius())
                {
                    ChangeState(EnemyState.Patrol);
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
            case EnemyState.Attack:
                movement.ApplyStopVelocity();
                break;
        }
    }

    private void ChangeState(EnemyState newState)
    {
        if (newState == EnemyState.Alert)
            alertTimer = alertDuration;

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
