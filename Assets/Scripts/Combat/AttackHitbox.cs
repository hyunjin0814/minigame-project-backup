using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class AttackHitbox : MonoBehaviour
{
    [SerializeField]
    private int damage = 1;

    private Collider2D col;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
        Deactivate();
    }

    public void Activate() => col.enabled = true;

    public void Deactivate() => col.enabled = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<IDamageable>(out var target))
            target.TakeDamage(damage);
    }
}
