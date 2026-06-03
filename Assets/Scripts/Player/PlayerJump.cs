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
    private PlayerDash dash;
    private PlayerAttack attack;

    private float coyoteCounter;
    private float jumpBufferCounter;

    // FixedUpdate에서 물리 적용 판단을 위한 플래그 변수들
    private bool doJump;
    private bool doJumpCut;

    public event System.Action OnJumped;

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        inputHandler = GetComponent<PlayerInputHandler>();
        groundDetector = GetComponent<PlayerGroundDetector>();
        dash = GetComponent<PlayerDash>();
        attack = GetComponent<PlayerAttack>();
    }

    private void OnEnable()
    {
        groundDetector.OnLanded += OnLanded;
    }

    private void OnDisable()
    {
        groundDetector.OnLanded -= OnLanded;
    }

    private void OnLanded()
    {
        AudioManager.Instance?.PlaySFX(SoundType.PlayerLand);
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
        // 대시 중에는 PlayerDash가 중력/velocity를 제어 — 점프 로직과 중력 스케일 변경 모두 스킵
        if (dash != null && dash.IsDashing)
        {
            doJump = false;
            doJumpCut = false;
            return;
        }

        // 공격 중인지 여부 — 점프는 허용하되 애니메이션 전환(OnJumped)만 막아
        // 공격 모션과 히트박스 이벤트가 그대로 진행되게 한다 (velocity만 변경)
        bool attacking = attack != null && attack.IsAttacking;

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
            AudioManager.Instance?.PlaySFX(SoundType.PlayerJump);
            // 공격 중에는 점프 애니메이션으로 전환하지 않음 (공격 모션 유지, velocity만 변경)
            if (!attacking)
                OnJumped?.Invoke();
            doJump = false;
        }

        // 중력 스케일 제어
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
    }
}
