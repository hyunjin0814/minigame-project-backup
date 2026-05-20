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

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        inputHandler = GetComponent<PlayerInputHandler>();
        groundDetector = GetComponent<PlayerGroundDetector>();
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
