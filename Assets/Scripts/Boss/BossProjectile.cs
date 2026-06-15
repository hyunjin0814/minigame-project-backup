using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(Rigidbody2D))]
public class BossProjectile : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifetime = 3f;

    private IObjectPool<BossProjectile> objectPool;


    private Rigidbody2D rb;
    public IObjectPool<BossProjectile> ObjectPool { set => objectPool = value; }
    private float timer;
    private bool isReturning; // 이중 반환 방지

    private void Awake() => rb = GetComponent<Rigidbody2D>();

    public void Init(Vector2 dir, float speed, IObjectPool<BossProjectile> ownerPool)
    {
        rb.linearVelocity = dir * speed;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        ObjectPool = ownerPool;
        timer = lifetime;
        isReturning = false;
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f) ReturnToPool();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Boss")) return;

        if (other.TryGetComponent<IDamageable>(out var target))
            target.TakeDamage(damage);

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (isReturning) return;            // Update + OnTriggerEnter2D 동시 호출 방어
        isReturning = true;
        rb.linearVelocity = Vector2.zero;
        objectPool.Release(this);
    }
}
