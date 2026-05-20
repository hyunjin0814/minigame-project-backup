using UnityEngine;

// 모든 State가 공유하는 공통 로직: SO 데이터를 각 컴포넌트에 주입
public abstract class BaseTransformState : ITransformState
{
    protected readonly PlayerTransformController controller;
    protected readonly TransformationData data;

    protected BaseTransformState(PlayerTransformController controller, TransformationData data)
    {
        this.controller = controller;
        this.data = data;
    }

    public virtual void Enter()
    {
        // 이동·점프 컴포넌트에 스탯 주입
        controller.HorizontalMovement.ApplyData(data);
        controller.Jump.ApplyData(data);

        // Animator Controller 교체
        // if (data.animatorController != null)
        //     controller.PlayerAnimator.SetAnimatorController(data.animatorController);

        // 콜라이더 사이즈 교체
        controller.Collider.size = data.colliderSize;
        controller.Collider.offset = data.colliderOffset;
    }

    public virtual void Exit() { }
}
