using UnityEngine;

[RequireComponent(typeof(PlayerMotor), typeof(PlayerInputHandler), typeof(PlayerGroundDetector))]
public class PlayerJump : MonoBehaviour
{
    [Header("Jump")]
    private float jumpSpeed = 15f;
    private float riseGravity = 3f;
    private float fallGravity = 5f;
    private float jumpCutMultiplier = 0.5f;

    [Header("Assist")]
    [SerializeField]
    private float coyoteTime = 0.1f;

    [SerializeField]
    private float jumpBuffer = 0.1f;

    private PlayerMotor motor;
    private PlayerInputHandler inputHandler;
    private PlayerGroundDetector groundDetector;

    private float coyoteCounter;
    private float jumpBufferCounter;

    public event System.Action OnJumped;

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        inputHandler = GetComponent<PlayerInputHandler>();
        groundDetector = GetComponent<PlayerGroundDetector>();
    }

    public void ApplyData(TransformationData data)
    {
        jumpSpeed = data.jumpSpeed;
        riseGravity = data.riseGravity;
        fallGravity = data.fallGravity;
        jumpCutMultiplier = data.jumpCutMultiplier;
    }

    private void Update()
    {
        // 코요테 타임 계산
        if (groundDetector.IsGrounded)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.deltaTime;

        // 점프 입력 버퍼 계산
        if (inputHandler.JumpTriggered)
        {
            jumpBufferCounter = jumpBuffer;
            inputHandler.JumpTriggered = false; // 소모 완료
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // 가변 점프 (버튼을 뗐을 때 일회성 감속)
        if (inputHandler.JumpCutRequested)
        {
            inputHandler.JumpCutRequested = false;
            if (motor.VelocityY > 0f)
                motor.SetVelocityY(motor.VelocityY * jumpCutMultiplier);
        }

        // 점프 실행
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            motor.SetVelocityY(jumpSpeed);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
            OnJumped?.Invoke();
        }

        // 비대칭 중력 적용
        motor.SetGravityScale(motor.VelocityY > 0f ? riseGravity : fallGravity);
    }
}
