using UnityEngine;

public class EnemySight : MonoBehaviour
{
    [SerializeField]
    private float detectionRadius = 5f;

    [SerializeField]
    private float detectionAngle = 60f;

    [SerializeField]
    private LayerMask playerLayer;

    [SerializeField]
    private LayerMask obstacleLayer; // 벽/바닥을 판정할 레이어 추가

    public Transform Player { get; private set; }

    public int FacingDirection { get; set; } = 1;

    // SightConeRenderer가 콘 메시 크기 계산에 사용
    public float DetectionRadius => detectionRadius;
    public float DetectionAngle => detectionAngle;

    public bool CanSeePlayer()
    {
        if (!IsPlayerWithinRadius())
            return false;

        Vector2 dirToPlayer = (Player.position - transform.position).normalized;
        float angle = Vector2.Angle(Vector2.right * FacingDirection, dirToPlayer);

        if (angle > detectionAngle * 0.5f)
            return false;

        // 장애물(벽)에 가려졌는지 레이캐스트로 3차 확인
        float distanceToPlayer = Vector2.Distance(transform.position, Player.position);

        // 적의 위치에서 플레이어 방향으로, 플레이어와의 거리만큼만 레이를 쏩니다.
        RaycastHit2D obstacleHit = Physics2D.Raycast(
            (Vector2)transform.position + Vector2.up * 0.5f,
            dirToPlayer,
            distanceToPlayer,
            obstacleLayer
        );

        // 중간에 장애물 레이어에 무언가 맞았다면, 벽 너머에 있는 것이므로 false 반환
        if (obstacleHit.collider != null)
        {
            return false;
        }

        ChameleonStealth stealth = Player.GetComponent<ChameleonStealth>();
        if (stealth != null && !stealth.IsDetectable)
            return false;

        return true;
    }

    public bool IsPlayerWithinRadius()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRadius, playerLayer);
        if (hit == null)
        {
            Player = null;
            return false;
        }
        Player = hit.transform.root;
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Vector3 facing = Vector2.right * FacingDirection;
        Vector3 left = Quaternion.Euler(0, 0, detectionAngle * 0.5f) * facing;
        Vector3 right = Quaternion.Euler(0, 0, -detectionAngle * 0.5f) * facing;
        Gizmos.DrawRay(transform.position, left * detectionRadius);
        Gizmos.DrawRay(transform.position, right * detectionRadius);
    }
}
