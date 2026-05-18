public class HumanState : BaseTransformState
{
    public HumanState(PlayerTransformController controller, TransformationData data)
        : base(controller, data) { }

    // 지금은 특수 능력 없음 — Base의 Enter/Exit 그대로 사용
}
