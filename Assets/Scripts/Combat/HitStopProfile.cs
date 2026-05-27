using UnityEngine;

/// <summary>
/// 적/보스 프리팹에 부착. 이 대상을 때렸을 때의 기본 히트스톱 등급.
/// PlayerAttack이 GetComponentInParent로 조회한다. 컴포넌트가 없으면 Light로 간주.
///
/// 예) 잡몹 = Light(미부착 가능), 보스/대형 적 = Heavy.
///     백스탭·마무리 일격은 공격자(PlayerAttack)가 Critical로 승격하므로 여기서 지정하지 않는다.
/// </summary>
public class HitStopProfile : MonoBehaviour
{
    [SerializeField] private HitStopType baseType = HitStopType.Light;

    public HitStopType BaseType => baseType;
}
