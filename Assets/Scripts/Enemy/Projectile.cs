using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField]
    private int damage = 1;

    [SerializeField]
    private float lifetime = 3f;

    private Vector2 velocity;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Init(Vector2 dir, float spd)
    {
        velocity = dir * spd;
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = velocity;
    }

    private void Update()
    {
        Destroy(gameObject, lifetime); // TODO: 풀링으로 교체 예정 (#11)
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<IDamageable>(out var target))
            target.TakeDamage(damage);

        Destroy(gameObject); // TODO: 풀링으로 교체 예정 (#11)
    }
}
