using UnityEngine;

public class BossIntroState : BossStateBase
{
    private readonly BossStateBase phase1State;
    private readonly float waitDuration;
    private float timer;

    public BossIntroState(BossBase boss, BossStateBase phase1, float waitDuration = 1f)
        : base(boss)
    {
        phase1State = phase1;
        this.waitDuration = waitDuration;
    }

    public override void Enter()
    {
        timer = waitDuration;
        Boss.Rb.linearVelocity = Vector2.zero;
        Debug.Log("[Boss] IntroState Enter");
    }

    public override void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
            Boss.TransitionTo(phase1State);
    }
}
