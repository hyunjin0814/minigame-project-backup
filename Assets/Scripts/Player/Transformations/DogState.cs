public class DogState : BaseTransformState
{
    private DogSense _dogSense;

    public DogState(PlayerTransformController controller, TransformationData data)
        : base(controller, data)
    {
        _dogSense = controller.DogSense;
    }

    public override void Enter()
    {
        base.Enter();
        if (_dogSense != null) _dogSense.enabled = true;
    }

    public override void Exit()
    {
        base.Exit();
        if (_dogSense != null) _dogSense.enabled = false;
    }
}
