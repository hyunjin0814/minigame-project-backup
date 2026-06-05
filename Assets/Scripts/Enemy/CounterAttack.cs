using UnityEngine;

/// <summary>
/// 반격 전용 히트박스 릴레이. Counter 애니메이션 클립의 Animation Event가 호출해 hitbox를 켜고 끈다.
/// Animation Event는 Animator와 같은 GameObject의 컴포넌트만 호출하므로, 이 컴포넌트는 Animator와 같은
/// 오브젝트(EliteEnemy 루트)에 둔다. 반격이 있는 적(EliteEnemy 등)에서만 사용.
/// 위치·크기·데미지는 counterHitbox의 BoxCollider2D / AttackHitbox.damage 로 기본 공격과 다르게 설정.
/// </summary>
public class CounterAttack : MonoBehaviour
{
    [Tooltip("반격 전용 히트박스. 기본 공격 히트박스와 별개로 위치·크기·데미지를 설정.")]
    [SerializeField]
    private AttackHitbox counterHitbox;

    public void AnimEvent_EnableCounterHitbox() => counterHitbox?.Activate();
    public void AnimEvent_DisableCounterHitbox() => counterHitbox?.Deactivate();
}
