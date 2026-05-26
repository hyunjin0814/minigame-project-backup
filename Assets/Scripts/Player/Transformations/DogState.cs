public class DogState : BaseTransformState
{
    private DogScanner _dogScanner;
    private DogDashAttack _dogDashAttack;

    public DogState(PlayerTransformController controller, TransformationData data)
        : base(controller, data)
    {
        _dogScanner = controller.DogScanner;
        _dogDashAttack = controller.DogDashAttack;
    }

    public override void Enter()
    {
        base.Enter();
        if (_dogScanner != null) _dogScanner.enabled = true;
        if (_dogDashAttack != null) _dogDashAttack.enabled = true;
    }

    public override void Exit()
    {
        base.Exit();
        if (_dogScanner != null) _dogScanner.enabled = false;
        if (_dogDashAttack != null) _dogDashAttack.enabled = false;
    }
}
