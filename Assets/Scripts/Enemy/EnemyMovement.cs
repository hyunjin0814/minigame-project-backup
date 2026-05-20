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

    private const float LookAroundInterval = 0.8f;

    public int FacingDirection { get; private set; } = 1;

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

    public void ApplyChaseVelocity() =>
        rb.linearVelocity = new Vector2(FacingDirection * chaseSpeed, rb.linearVelocity.y);

    public void ResetSearch()
    {
        arrivedAtSearch = false;
        lookAroundTimer = 0f;
    }

    public void SearchTick(Vector2 target)
    {
        if (Mathf.Abs(transform.position.x - target.x) < 0.3f)
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
            Flip(target.x > transform.position.x ? 1 : -1);
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
