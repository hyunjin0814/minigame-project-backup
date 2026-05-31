using UnityEngine;

// Phase1: cleave, smash 만 사용
public class SecondBossPhase1State : SecondBossCombatState
{
    private static readonly Pattern[] Patterns = { Pattern.Cleave, Pattern.Smash };

    public SecondBossPhase1State(SecondBossController boss, Settings settings)
        : base(boss, settings, Patterns) { }

    public override void Enter()
    {
        base.Enter();
        Debug.Log("[SecondBoss] Phase1State Enter");
    }
}
