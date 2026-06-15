using UnityEngine;
using UnityEngine.Pool;

public class RangedAttack : MonoBehaviour, IEnemyAttack
{
    [SerializeField] private float maxRange = 8f;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private int defaultCapacity = 5;
    [SerializeField] private int maxSize = 10;

    private IObjectPool<Projectile> pool;

    private void Awake()
    {
        pool = new ObjectPool<Projectile>(
            CreateProjectile,
            OnGetFromPool,
            OnReleaseToPool,
            OnDestroyPooledObject,
            collectionCheck: true,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );
    }

    public bool IsInRange(Transform target)
        => Vector2.Distance(transform.position, target.position) <= maxRange;

    public void DoAttack(Transform target)
    {
        Vector2 targetCenter = target.TryGetComponent<Collider2D>(out var col)
            ? (Vector2)col.bounds.center
            : (Vector2)target.position;
        Vector2 dir = (targetCenter - (Vector2)transform.position).normalized;

        var proj = pool.Get();
        proj.transform.position = transform.position;
        proj.Init(dir, projectileSpeed, pool);
    }

    private Projectile CreateProjectile()
    {
        var proj = Instantiate(projectilePrefab);
        proj.gameObject.SetActive(false);
        return proj;
    }

    private void OnGetFromPool(Projectile proj) => proj.gameObject.SetActive(true);
    private void OnReleaseToPool(Projectile proj) => proj.gameObject.SetActive(false);
    private void OnDestroyPooledObject(Projectile proj) => Destroy(proj.gameObject);
}
