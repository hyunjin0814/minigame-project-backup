using UnityEngine;

// 본체에 닿으면 피해를 주는 접촉 히트박스.
// 적(또는 보스)의 자식 오브젝트에 부착하고, 콜라이더를 본체 크기에 맞춘다.
//   - Layer = EnemyAttack (Player와 충돌 ON인 레이어여야 닿음. Enemy 레이어는 Player와 OFF라 안 됨)
//   - Is Trigger = ON (물리로 밀지 않고 겹침만 감지)
//
// OnTriggerStay2D로 매 프레임 시도하지만, 플레이어 무적프레임(PlayerHurtEffect → Health.IsInvincible)이
// 게이트하므로 i-frame 간격마다 한 번씩만 적용된다 ("붙어있으면 i-frame마다 1대").
[RequireComponent(typeof(Collider2D))]
public class ContactDamage : MonoBehaviour
{
    [SerializeField]
    [Tooltip("접촉 1회당 데미지")]
    private int damage = 1;

    [SerializeField]
    [Tooltip("켜면 'Player' 태그인 대상에게만 피해 (적끼리 겹칠 때 오발 방지)")]
    private bool playerTagOnly = true;

    private void Reset()
    {
        // 컴포넌트 추가 시 자동으로 트리거로 설정 (물리 밀림 방지)
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerStay2D(Collider2D other) => TryDamage(other);

    private void TryDamage(Collider2D other)
    {
        if (playerTagOnly && !other.CompareTag("Player"))
            return;

        // 강아지 돌진·고양이 은신 등 접촉 피해 면역 상태면 무시
        foreach (var immune in other.GetComponentsInParent<IContactDamageImmune>())
            if (immune.IsContactDamageImmune)
                return;

        if (other.TryGetComponent<IDamageable>(out var target))
            target.TakeDamage(damage, transform.position);
    }
}
