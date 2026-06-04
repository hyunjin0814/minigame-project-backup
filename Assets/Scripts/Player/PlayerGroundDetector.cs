using UnityEngine;

public class PlayerGroundDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Transform groundCheck;

    [SerializeField]
    private LayerMask groundLayer;

    [Header("Settings")]
    [SerializeField]
    private float groundCheckRadius = 0.15f;

    public bool IsGrounded { get; private set; }

    /// <summary>공중 → 착지 전환 시 1회 발생.</summary>
    public event System.Action OnLanded;

    private BoxCollider2D col;
    private bool groundedThisStep;
    private bool _prevGrounded;

    private void Awake()
    {
        col = GetComponent<BoxCollider2D>();
    }

    private void FixedUpdate()
    {
        bool newGrounded = groundedThisStep;

        // 공중 → 지면 전환 감지
        if (!_prevGrounded && newGrounded)
            OnLanded?.Invoke();

        IsGrounded    = newGrounded;
        _prevGrounded = newGrounded;
        groundedThisStep = false;
    }

    private void OnCollisionEnter2D(Collision2D other) => EvaluateContact(other);

    private void OnCollisionStay2D(Collision2D other) => EvaluateContact(other);

    private void EvaluateContact(Collision2D other)
    {
        if (!other.enabled) return;
        if (((1 << other.gameObject.layer) & groundLayer) == 0)
            return;
        foreach (var contact in other.contacts)
        {
            if (contact.normal.y > 0.7f)
            {
                groundedThisStep = true;
                return;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;
        Gizmos.color = Color.green;
        if (col != null)
            Gizmos.DrawWireCube(
                groundCheck.position,
                new Vector2(col.size.x, groundCheckRadius * 2f)
            );
        else
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
