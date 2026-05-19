using System.Collections;
using UnityEngine;

public class MeleeAttack : MonoBehaviour, IEnemyAttack
{
    [SerializeField]
    private float meleeRange = 2.5f;

    [SerializeField]
    private float hitboxDuration = 0.2f;

    private AttackHitbox hitbox;

    private void Awake()
    {
        hitbox = GetComponentInChildren<AttackHitbox>();
    }

    public bool IsInRange(Transform target) =>
        Vector2.Distance(transform.position, target.position) <= meleeRange;

    public void DoAttack(Transform target) => StartCoroutine(HitboxRoutine());

    private IEnumerator HitboxRoutine()
    {
        hitbox.Activate();
        yield return new WaitForSeconds(hitboxDuration);
        hitbox.Deactivate();
    }
}
