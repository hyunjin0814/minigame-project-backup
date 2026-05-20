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

    [Header("Ceiling Passthrough")]
    [SerializeField]
    private string ceilingLayerName = "Platform"; // 충돌 끌 레이어 이름

    private PlayerMotor motor;
    private PlayerInputHandler inputHandler;
    private PlayerGroundDetector groundDetector;

    private float coyoteCounter;
    private float jumpBufferCounter;

    // FixedUpdate에서 물리 적용 판단을 위한 플래그 변수들
    private bool doJump;
    private bool doJumpCut;

    private int playerLayerIndex;
    private int ceilingLayerIndex;

    public event System.Action OnJumped;

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        inputHandler = GetComponent<PlayerInputHandler>();
        groundDetector = GetComponent<PlayerGroundDetector>();

        playerLayerIndex = gameObject.layer;
        ceilingLayerIndex = LayerMask.NameToLayer(ceilingLayerName);
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
                doJumpCut = true;
        }

        // 점프 실행
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            doJump = true;

            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }
    }

    private void FixedUpdate()
    {
        // 가변 점프컷 적용
        if (doJumpCut)
        {
            // Update와 FixedUpdate 사이의 시차 때문에 그새 하강 중으로 바뀌었는지 재차 확인
            if (motor.VelocityY > 0f)
            {
                motor.SetVelocityY(motor.VelocityY * jumpCutMultiplier);
            }
            doJumpCut = false;
        }

        // 일반 점프 적용
        if (doJump)
        {
            motor.SetVelocityY(jumpSpeed);
            OnJumped?.Invoke();
            doJump = false;
        }

        // 중력 스케일 제어 로직 (기존 로직이 중력 제어를 포함하고 있었다면 여기에 유지)
        if (motor.VelocityY > 0f)
        {
            motor.SetGravityScale(riseGravity);
        }
        else if (motor.VelocityY < 0f)
        {
            motor.SetGravityScale(fallGravity);
        }
        else if (groundDetector.IsGrounded)
        {
            motor.SetGravityScale(1f);
        }

        bool isRising = motor.VelocityY > 0.1f;
        Physics2D.IgnoreLayerCollision(playerLayerIndex, ceilingLayerIndex, isRising);
    }
}
