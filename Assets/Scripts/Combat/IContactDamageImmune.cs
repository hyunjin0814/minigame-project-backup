/// <summary>
/// 접촉 피해(ContactDamage)에 면역인 상태를 노출.
/// 플레이어의 특정 상태(강아지 돌진, 고양이 은신 등)가 구현하면 ContactDamage가 피해를 건너뛴다.
/// </summary>
public interface IContactDamageImmune
{
    bool IsContactDamageImmune { get; }
}
