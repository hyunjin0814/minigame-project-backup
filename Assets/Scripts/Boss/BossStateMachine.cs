public class BossStateMachine
{
    public BossStateBase Current { get; private set; }

    public void ChangeState(BossStateBase next)
    {
        Current?.Exit();
        Current = next;
        Current.Enter();
    }

    public void Update() => Current?.Update();
}
