using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Transform groundCheck;

    [SerializeField]
    private LayerMask groundLayer;

    [Header("Movement")]
    [SerializeField]
    private float maxSpeed = 9f;

    [SerializeField]
    private float accelTime = 0.08f; // 가속 시간

    [SerializeField]
    private float decelTime = 0.05f; // 감속 시간

    [SerializeField]
    private float airControl = 0.9f; // 공중 조작 비율

    [Header("Jump")]
    [SerializeField]
    private float jumpSpeed = 15f;

    [SerializeField]
    private float riseGravity = 3f; // 상승 중력

    [SerializeField]
    private float fallGravity = 5f; // 하강 중력

    [SerializeField]
    private float jumpCutMultiplier = 0.5f; // 버튼 떼면 속도 곱하기

    [Header("Assist")]
    [SerializeField]
    private float coyoteTime = 0.1f;

    [SerializeField]
    private float jumpBuffer = 0.1f;

    [SerializeField]
    private float groundCheckRadius = 0.15f;

    // 내부 상태
    private Rigidbody2D rb;
    private PlayerInputActions input;
    private Vector2 moveInput;
    private bool jumpHeld;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private bool isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        input = new PlayerInputActions();
    }

    private void OnEnable()
    {
        input.Player.Enable();
        input.Player.Move.performed += OnMove;
        input.Player.Move.canceled += OnMove;
        input.Player.Jump.performed += OnJumpPressed;
        input.Player.Jump.canceled += OnJumpReleased;
    }

    private void OnDisable()
    {
        input.Player.Move.performed -= OnMove;
        input.Player.Move.canceled -= OnMove;
        input.Player.Jump.performed -= OnJumpPressed;
        input.Player.Jump.canceled -= OnJumpReleased;
        input.Player.Disable();
    }

    private void OnGUI()
    {
        // 1. 스타일 복사 및 폰트 크기 설정
        GUIStyle myStyle = new GUIStyle(GUI.skin.label);
        myStyle.fontSize = 36; // 원하는 글자 크기

        // 2. Rect의 높이를 키우고(20 -> 40), 마지막 인자에 스타일 전달
        GUI.Label(
            new Rect(10, 10, 500, 50),
            $"moveInput: {moveInput}  velocityX: {rb.linearVelocity.x:F2}  grounded: {isGrounded}",
            myStyle
        );
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
        Debug.Log($"[OnMove] phase: {ctx.phase}, value: {moveInput}, time: {Time.time:F3}");
    }

    private void OnJumpPressed(InputAction.CallbackContext ctx)
    {
        jumpHeld = true;
        jumpBufferCounter = jumpBuffer; // 점프 버퍼 시작
    }

    private void OnJumpReleased(InputAction.CallbackContext ctx)
    {
        jumpHeld = false;
        // 상승 중에 버튼 떼면 가변 점프 — 속도 깎기
        if (rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                rb.linearVelocity.y * jumpCutMultiplier
            );
        }
    }

    private void Update()
    {
        // 지면 체크
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // 코요테 타임
        if (isGrounded)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.deltaTime;

        // 점프 버퍼
        jumpBufferCounter -= Time.deltaTime;

        // 점프 실행 (버퍼와 코요테 둘 다 살아있을 때)
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpSpeed);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }

        // 중력 스케일 (상승/하강 비대칭)
        rb.gravityScale = rb.linearVelocity.y > 0f ? riseGravity : fallGravity;
    }

    private void FixedUpdate()
    {
        // 목표 속도 = 입력 방향 × 최대 속도
        float targetSpeed = moveInput.x * maxSpeed;

        // 가속 시간/감속 시간 기준으로 가속도 계산
        float accelRate =
            (Mathf.Abs(targetSpeed) > 0.1f) ? maxSpeed / accelTime : maxSpeed / decelTime;

        // 공중이면 조작 둔화
        if (!isGrounded)
            accelRate *= airControl;

        // 현재 속도 → 목표 속도로 끌어당기기
        float speedDiff = targetSpeed - rb.linearVelocity.x;
        float movement =
            Mathf.Sign(speedDiff)
            * Mathf.Min(Mathf.Abs(speedDiff), accelRate * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(rb.linearVelocity.x + movement, rb.linearVelocity.y);
    }

    // 씬 뷰에서 GroundCheck 범위 시각화
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
