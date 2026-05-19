using UnityEngine;

public class RangedAttack : MonoBehaviour, IEnemyAttack
{
    [SerializeField]
    private float maxRange = 8f;

    [SerializeField]
    private GameObject projectilePrefab;

    [SerializeField]
    private float projectileSpeed = 8f;

    public bool IsInRange(Transform target)
    {
        float dist = Vector2.Distance(transform.position, target.position);
        return dist <= maxRange;
    }

    public void DoAttack(Transform target)
    {
        Vector2 targetCenter = target.TryGetComponent<Collider2D>(out var col)
            ? (Vector2)col.bounds.center
            : (Vector2)target.position;
        Vector2 dir = (targetCenter - (Vector2)transform.position).normalized;
        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        proj.GetComponent<Projectile>().Init(dir, projectileSpeed);
    }
}
