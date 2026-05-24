public class CatState : BaseTransformState
{
    public CatState(PlayerTransformController controller, TransformationData data)
        : base(controller, data) { }

    public override void Enter()
    {
        base.Enter();
        controller.CatStealth.enabled = true;
    }

    public override void Exit()
    {
        base.Exit();
        controller.CatStealth.enabled = false;
    }
}
