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

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        inputHandler = GetComponent<PlayerInputHandler>();
        groundDetector = GetComponent<PlayerGroundDetector>();
        dash = GetComponent<PlayerDash>();
        attack = GetComponent<PlayerAttack>();
        dogDashAttack = GetComponent<DogDashAttack>();
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

        // 공격 중 수평 이동 차단
        if (attack != null && attack.IsAttacking)
            return;

        // 강아지 돌진 공격 중 입력 기반 이동 차단 (DogDashAttack이 velocity 제어)
        if (dogDashAttack != null && dogDashAttack.IsExecuting)
            return;

        float targetSpeed = inputHandler.MoveInput.x * maxSpeed;

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
