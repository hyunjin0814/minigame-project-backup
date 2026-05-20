public class ChameleonState : BaseTransformState
{
    public ChameleonState(PlayerTransformController controller, TransformationData data)
        : base(controller, data) { }

    public override void Enter()
    {
        base.Enter();
        controller.ChameleonStealth.enabled = true;
    }

    public override void Exit()
    {
        base.Exit();
        controller.ChameleonStealth.enabled = false;
    }
}
