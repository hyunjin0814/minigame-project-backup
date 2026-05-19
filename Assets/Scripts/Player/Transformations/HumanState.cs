public class HumanState : BaseTransformState
{
    public HumanState(PlayerTransformController controller, TransformationData data)
        : base(controller, data) { }

    public override void Enter()
    {
        base.Enter();
        controller.Attack.enabled = true;
    }

    public override void Exit()
    {
        base.Exit();
        controller.Attack.enabled = false;
    }
}
