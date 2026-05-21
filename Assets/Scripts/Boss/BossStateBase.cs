public abstract class BossStateBase
{
    protected BossBase Boss { get; }

    protected BossStateBase(BossBase boss) => Boss = boss;

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }
}
