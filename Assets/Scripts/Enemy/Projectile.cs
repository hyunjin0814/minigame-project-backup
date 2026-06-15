using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifetime = 3f;

    private IObjectPool<Projectile> objectPool;
    private Rigidbody2D rb;
    private float timer;
    private bool isReturning;

    private void Awake() => rb = GetComponent<Rigidbody2D>();

    public void Init(Vector2 dir, float spd, IObjectPool<Projectile> ownerPool)
    {
        rb.linearVelocity = dir * spd;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        objectPool = ownerPool;
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
        if (other.TryGetComponent<IDamageable>(out var target))
            target.TakeDamage(damage);

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (isReturning) return;
        isReturning = true;
        rb.linearVelocity = Vector2.zero;
        objectPool.Release(this);
    }
}
