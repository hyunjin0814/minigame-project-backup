using UnityEngine;

// Phase2: cleave, smash + fire_breath, cast_spell 추가
public class SecondBossPhase2State : SecondBossCombatState
{
    private static readonly Pattern[] Patterns =
        { Pattern.Cleave, Pattern.Smash, Pattern.FireBreath, Pattern.CastSpell };

    public SecondBossPhase2State(SecondBossController boss, Settings settings)
        : base(boss, settings, Patterns) { }

    public override void Enter()
    {
        base.Enter();
        boss.ApplyPhase2Speed(); // 애니메이션 + 패턴 시간값 1.5배 가속
        Debug.Log("[SecondBoss] Phase2State Enter");
    }
}
