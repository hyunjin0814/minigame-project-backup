using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossProjectile : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifetime = 3f;

    private Rigidbody2D rb;
    private ProjectilePool pool;
    private float timer;

    private void Awake() => rb = GetComponent<Rigidbody2D>();

    public void Init(Vector2 dir, float speed, ProjectilePool ownerPool)
    {
        rb.linearVelocity = dir * speed;
        pool = ownerPool;
        timer = lifetime;
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
        rb.linearVelocity = Vector2.zero;
        pool.Return(this);
    }
}
