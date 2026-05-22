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

    private BoxCollider2D col;

    private void Awake()
    {
        col = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        Vector2 boxSize = new Vector2(col.size.x, groundCheckRadius * 2f);
        IsGrounded = Physics2D.OverlapBox(groundCheck.position, boxSize, 0f, groundLayer);
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
