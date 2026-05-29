using UnityEngine;

[RequireComponent(typeof(PlayerMotor), typeof(PlayerInputHandler), typeof(PlayerGroundDetector))]
public class PlayerHorizontalMovement : MonoBehaviour
{
    [Header("Movement")]
    private float maxSpeed = 9f;
    private float accelTime = 0.08f;
    private float decelTime = 0.05f;
    private float airControl = 0.9f;

    private PlayerMotor motor;
    private PlayerInputHandler inputHandler;
    private PlayerGroundDetector groundDetector;
    private PlayerDash dash;
    private PlayerAttack attack;
    private DogDashAttack dogDashAttack;
    private PlayerHurtEffect hurtEffect;

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        inputHandler = GetComponent<PlayerInputHandler>();
        groundDetector = GetComponent<PlayerGroundDetector>();
        dash = GetComponent<PlayerDash>();
        attack = GetComponent<PlayerAttack>();
        dogDashAttack = GetComponent<DogDashAttack>();
        hurtEffect = GetComponent<PlayerHurtEffect>();
    }

    public void ApplyData(TransformationData data)
    {
        maxSpeed = data.maxSpeed;
        accelTime = data.accelTime;
        decelTime = data.decelTime;
        airControl = data.airControl;
    }

    private void FixedUpdate()
    {
        // 대시 중에는 PlayerDash가 velocity를 제어하므로 입력 기반 이동 스킵
        if (dash != null && dash.IsDashing)
            return;

        // 강아지 돌진 공격 중 입력 기반 이동 차단 (DogDashAttack이 velocity 제어)
        if (dogDashAttack != null && dogDashAttack.IsExecuting)
            return;

        // 피격 넉백 중 입력 이동 차단
        if (hurtEffect != null && hurtEffect.IsHurt)
            return;

        // MoveInput.x는 대각선 입력 시 정규화되어 0.707이 됨 → Sign으로 y축 영향 배제
        float inputX = inputHandler.MoveInput.x;
        float targetSpeed = (Mathf.Abs(inputX) > 0.01f ? Mathf.Sign(inputX) : 0f) * maxSpeed;

        float accelRate =
            (Mathf.Abs(targetSpeed) > 0.1f) ? maxSpeed / accelTime : maxSpeed / decelTime;

        if (!groundDetector.IsGrounded)
            accelRate *= airControl;

        float speedDiff = targetSpeed - motor.VelocityX;
        float movement =
            Mathf.Sign(speedDiff)
            * Mathf.Min(Mathf.Abs(speedDiff), accelRate * Time.fixedDeltaTime);

        motor.SetVelocityX(motor.VelocityX + movement);
    }
}
