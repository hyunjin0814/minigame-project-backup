using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(PlayerInputHandler), typeof(PlayerGroundDetector))]
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

    private Rigidbody2D rb;
    private PlayerInputHandler inputHandler;
    private PlayerGroundDetector groundDetector;

    private float coyoteCounter;
    private float jumpBufferCounter;

    public event System.Action OnJumped;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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
            if (rb.linearVelocity.y > 0f)
            {
                rb.linearVelocity = new Vector2(
                    rb.linearVelocity.x,
                    rb.linearVelocity.y * jumpCutMultiplier
                );
            }
        }

        // 점프 실행
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpSpeed);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
            OnJumped?.Invoke();
        }

        // 비대칭 중력 적용
        rb.gravityScale = rb.linearVelocity.y > 0f ? riseGravity : fallGravity;
    }
}
