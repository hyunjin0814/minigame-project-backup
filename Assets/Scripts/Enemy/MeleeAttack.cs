using System.Collections;
using UnityEngine;

public class MeleeAttack : MonoBehaviour, IEnemyAttack
{
    [SerializeField]
    private float meleeRange = 2.5f;

    [SerializeField]
    private float hitboxDuration = 0.2f;

    [Tooltip("켜면 Animation Event(AnimEvent_EnableHitbox/DisableHitbox)가 hitbox 제어. " +
             "끄면 DoAttack 호출 시 코루틴으로 hitboxDuration만큼 활성화.")]
    [SerializeField]
    private bool useAnimationEvent = false;

    private AttackHitbox hitbox;

    private void Awake()
    {
        hitbox = GetComponentInChildren<AttackHitbox>();
    }

    public bool IsInRange(Transform target) =>
        Vector2.Distance(transform.position, target.position) <= meleeRange;

    public void DoAttack(Transform target)
    {
        if (useAnimationEvent) return; // Animation Event가 켜고 끔
        StartCoroutine(HitboxRoutine());
    }

    private IEnumerator HitboxRoutine()
    {
        hitbox.Activate();
        yield return new WaitForSeconds(hitboxDuration);
        hitbox.Deactivate();
    }

    // Animation Event 호출용 (Goblin Attack 클립에 등록)
    public void AnimEvent_EnableHitbox() => hitbox.Activate();
    public void AnimEvent_DisableHitbox() => hitbox.Deactivate();
}
