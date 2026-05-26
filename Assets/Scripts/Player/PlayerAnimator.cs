using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(PlayerGroundDetector))]
[RequireComponent(typeof(PlayerJump))]
[RequireComponent(typeof(PlayerAttack))]
[RequireComponent(typeof(PlayerDash))]
public class PlayerAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    // Animator 파라미터 해시
    private static readonly int SpeedXHash = Animator.StringToHash("SpeedX");
    private static readonly int VerticalVelocityHash = Animator.StringToHash("VerticalVelocity");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int AttackUpHash = Animator.StringToHash("AttackUp");
    private static readonly int AttackDownHash = Animator.StringToHash("AttackDown");
    private static readonly int AttackSideHash = Animator.StringToHash("AttackSide");
    private static readonly int IsDashingHash = Animator.StringToHash("IsDashing");

    private Rigidbody2D rb;
    private PlayerInputHandler inputHandler;
    private PlayerGroundDetector groundDetector;
    private PlayerJump jumpComponent;
    private PlayerAttack attackComponent;
    private PlayerDash dashComponent;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        inputHandler = GetComponent<PlayerInputHandler>();
        groundDetector = GetComponent<PlayerGroundDetector>();
        jumpComponent = GetComponent<PlayerJump>();
        attackComponent = GetComponent<PlayerAttack>();
        dashComponent = GetComponent<PlayerDash>();

        // 자식에서 Animator / SpriteRenderer 자동 탐색
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        jumpComponent.OnJumped += HandleJumped;
        attackComponent.OnAttackTriggered += HandleAttackTriggered;
        dashComponent.OnDashStart += HandleDashStart;
        dashComponent.OnDashEnd += HandleDashEnd;
    }

    private void OnDisable()
    {
        jumpComponent.OnJumped -= HandleJumped;
        attackComponent.OnAttackTriggered -= HandleAttackTriggered;
        dashComponent.OnDashStart -= HandleDashStart;
        dashComponent.OnDashEnd -= HandleDashEnd;
    }

    private void HandleJumped()
    {
        if (animator != null)
            animator.SetTrigger(JumpHash);
    }

    private void HandleAttackTriggered(AttackDirection dir)
    {
        if (animator == null)
            return;

        switch (dir)
        {
            case AttackDirection.Up:
                animator.SetTrigger(AttackUpHash);
                break;
            case AttackDirection.Down:
                animator.SetTrigger(AttackDownHash);
                break;
            default:
                animator.SetTrigger(AttackSideHash);
                break;
        }
    }

    private void HandleDashStart()
    {
        if (animator != null)
            animator.SetBool(IsDashingHash, true);
    }

    private void HandleDashEnd()
    {
        if (animator != null)
            animator.SetBool(IsDashingHash, false);
    }

    private void Update()
    {
        if (animator != null)
        {
            animator.SetFloat(SpeedXHash, Mathf.Abs(rb.linearVelocity.x));
            animator.SetFloat(VerticalVelocityHash, rb.linearVelocity.y);
            animator.SetBool(IsGroundedHash, groundDetector.IsGrounded);
        }

        // 좌우 flip — 입력 기준 (관성 미끄러짐 중에도 바라보는 방향 유지)
        if (spriteRenderer != null)
        {
            if (inputHandler.MoveInput.x > 0.1f)
                spriteRenderer.flipX = false;
            else if (inputHandler.MoveInput.x < -0.1f)
                spriteRenderer.flipX = true;
        }
    }

    public void SetAnimatorController(RuntimeAnimatorController controller)
    {
        if (animator != null)
            animator.runtimeAnimatorController = controller;
    }
}
