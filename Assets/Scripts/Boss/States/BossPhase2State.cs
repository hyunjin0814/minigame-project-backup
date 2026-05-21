using UnityEngine;

// Phase 2 콘텐츠는 추후 작업 예정
public class BossPhase2State : BossStateBase
{
    public BossPhase2State(BossBase boss)
        : base(boss) { }

    public override void Enter()
    {
        Debug.Log("[Boss] Phase2State Enter (미구현)");
    }
}
