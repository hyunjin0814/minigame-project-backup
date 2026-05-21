using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemySight))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField]
    private float patrolSpeed = 3f;

    [SerializeField]
    private float wallCheckDistance = 0.3f;

    [SerializeField]
    private float edgeCheckDistance = 0.5f;

    [SerializeField]
    private LayerMask groundLayer;

    [Header("Chase")]
    [SerializeField]
    private float chaseSpeed = 5f;

    [SerializeField]
    private float chaseLungeSpeed = 7f;

    private const float LookAroundInterval = 0.8f;

    public int FacingDirection { get; private set; } = 1;

    // EnemyController가 두리번 타이머 시작 시점 판단에 사용
    public bool ArrivedAtSearch => arrivedAtSearch;

    private Rigidbody2D rb;
    private EnemySight sight;
    private bool arrivedAtSearch;
    private float lookAroundTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sight = GetComponent<EnemySight>();
    }

    public void PatrolTick() => CheckPatrolFlip();

    public void ChaseTick() => UpdateChaseDirection();

    public void ApplyPatrolVelocity() =>
        rb.linearVelocity = new Vector2(FacingDirection * patrolSpeed, rb.linearVelocity.y);

    public void ApplyAlertVelocity() =>
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

    // isLunging: lastSeenPosition으로 돌진 중일 때 true → chaseLungeSpeed 사용
    public void ApplyChaseVelocity(bool isLunging = false)
    {
        float speed = isLunging ? chaseLungeSpeed : chaseSpeed;
        rb.linearVelocity = new Vector2(FacingDirection * speed, rb.linearVelocity.y);
    }

    // 고정 좌표(lastSeenPosition)로 향하는 facing 갱신
    public void ChaseTowardTarget(Vector2 target)
    {
        float diffX = target.x - transform.position.x;
        if (Mathf.Abs(diffX) < 0.3f)
            return;
        Flip(diffX > 0 ? 1 : -1);
    }

    public bool ArrivedAtChaseTarget(Vector2 target) =>
        Mathf.Abs(transform.position.x - target.x) < 0.3f;

    // 벽 또는 절벽으로 막혔는지 검사 (CheckPatrolFlip과 동일한 패턴, 외부 노출)
    // Chase lunge / Search 이동 중 도달 불가 판정에 사용
    public bool IsBlockedToward(int direction)
    {
        bool wallAhead = Physics2D.Raycast(
            transform.position,
            Vector2.right * direction,
            wallCheckDistance,
            groundLayer
        );

        Vector2 edgeOrigin = (Vector2)transform.position + Vector2.right * direction * 0.5f;
        bool noGround = !Physics2D.Raycast(
            edgeOrigin,
            Vector2.down,
            edgeCheckDistance,
            groundLayer
        );

        return wallAhead || noGround;
    }

    public void ResetSearch()
    {
        arrivedAtSearch = false;
        lookAroundTimer = 0f;
    }

    public void SearchTick(Vector2 target)
    {
        int dir = target.x > transform.position.x ? 1 : -1;
        bool atTarget = Mathf.Abs(transform.position.x - target.x) < 0.3f;
        // 도착 못해도 벽/절벽에 막히면 그 자리에서 두리번
        bool blocked = !atTarget && IsBlockedToward(dir);

        if (atTarget || blocked)
        {
            arrivedAtSearch = true;
            lookAroundTimer += Time.deltaTime;
            if (lookAroundTimer >= LookAroundInterval)
            {
                Flip();
                lookAroundTimer = 0f;
            }
        }
        else
        {
            arrivedAtSearch = false;
            Flip(dir);
        }
    }

    public void ApplySearchVelocity()
    {
        float xVel = arrivedAtSearch ? 0f : FacingDirection * patrolSpeed;
        rb.linearVelocity = new Vector2(xVel, rb.linearVelocity.y);
    }

    public void ApplyStopVelocity() => rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

    public void Flip(int direction)
    {
        FacingDirection = direction;
        transform.localScale = new Vector3(direction, 1, 1);
        sight.FacingDirection = direction;
    }

    private void Flip() => Flip(FacingDirection * -1);

    private void CheckPatrolFlip()
    {
        bool wallAhead = Physics2D.Raycast(
            transform.position,
            Vector2.right * FacingDirection,
            wallCheckDistance,
            groundLayer
        );

        Vector2 edgeOrigin = (Vector2)transform.position + Vector2.right * FacingDirection * 0.5f;
        bool noGround = !Physics2D.Raycast(
            edgeOrigin,
            Vector2.down,
            edgeCheckDistance,
            groundLayer
        );

        if (wallAhead || noGround)
            Flip();
    }

    private void UpdateChaseDirection()
    {
        if (sight.Player == null)
            return;
        Vector2 toPlayer = (
            (Vector2)sight.Player.position - (Vector2)transform.position
        ).normalized;
        if (Mathf.Abs(toPlayer.x) < 0.3f)
            return;
        Flip(toPlayer.x > 0 ? 1 : -1);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Vector2.right * FacingDirection * wallCheckDistance);
        Vector2 edgeOrigin = (Vector2)transform.position + Vector2.right * FacingDirection * 0.5f;
        Gizmos.DrawRay(edgeOrigin, Vector2.down * edgeCheckDistance);
    }
}
