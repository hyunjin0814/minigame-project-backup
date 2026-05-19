// using UnityEngine;

// [RequireComponent(typeof(Rigidbody2D))]
// [RequireComponent(typeof(PlayerInputHandler))]
// [RequireComponent(typeof(PlayerGroundDetector))]
// [RequireComponent(typeof(PlayerJump))]
// public class PlayerAnimator : MonoBehaviour
// {
//     [Header("References")]
//     [SerializeField]
//     private Animator animator;

//     [SerializeField]
//     private SpriteRenderer spriteRenderer;

//     // Animator 파라미터 해시
//     private static readonly int SpeedXHash = Animator.StringToHash("SpeedX");
//     private static readonly int VerticalVelocityHash = Animator.StringToHash("VerticalVelocity");
//     private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
//     private static readonly int JumpHash = Animator.StringToHash("Jump");

//     private Rigidbody2D rb;
//     private PlayerInputHandler inputHandler;
//     private PlayerGroundDetector groundDetector;
//     private PlayerJump jumpComponent;

//     private void Awake()
//     {
//         rb = GetComponent<Rigidbody2D>();
//         inputHandler = GetComponent<PlayerInputHandler>();
//         groundDetector = GetComponent<PlayerGroundDetector>();
//         jumpComponent = GetComponent<PlayerJump>();

//         // 자식에서 Animator / SpriteRenderer 자동 탐색
//         if (animator == null)
//             animator = GetComponentInChildren<Animator>();
//         if (spriteRenderer == null)
//             spriteRenderer = GetComponentInChildren<SpriteRenderer>();
//     }

//     private void OnEnable()
//     {
//         jumpComponent.OnJumped += HandleJumped;
//     }

//     private void OnDisable()
//     {
//         jumpComponent.OnJumped -= HandleJumped;
//     }

//     private void HandleJumped()
//     {
//         animator.SetTrigger(JumpHash);
//     }

//     private void Update()
//     {
//         // 상태 파라미터 — 매 프레임 갱신
//         animator.SetFloat(SpeedXHash, Mathf.Abs(rb.linearVelocity.x));
//         animator.SetFloat(VerticalVelocityHash, rb.linearVelocity.y);
//         animator.SetBool(IsGroundedHash, groundDetector.IsGrounded);

//         // 좌우 flip — 입력 기준 (관성 미끄러짐 중에도 바라보는 방향 유지)
//         if (inputHandler.MoveInput.x > 0.1f)
//             spriteRenderer.flipX = false;
//         else if (inputHandler.MoveInput.x < -0.1f)
//             spriteRenderer.flipX = true;
//     }

//     public void SetAnimatorController(RuntimeAnimatorController controller)
//     {
//         animator.runtimeAnimatorController = controller;
//     }
// }
