using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(PlayerInputHandler), typeof(PlayerGroundDetector))]
public class PlayerHorizontalMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField]
    private float maxSpeed = 9f;

    [SerializeField]
    private float accelTime = 0.08f;

    [SerializeField]
    private float decelTime = 0.05f;

    [SerializeField]
    private float airControl = 0.9f;

    private Rigidbody2D rb;
    private PlayerInputHandler inputHandler;
    private PlayerGroundDetector groundDetector;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        inputHandler = GetComponent<PlayerInputHandler>();
        groundDetector = GetComponent<PlayerGroundDetector>();
    }

    private void FixedUpdate()
    {
        float targetSpeed = inputHandler.MoveInput.x * maxSpeed;

        float accelRate =
            (Mathf.Abs(targetSpeed) > 0.1f) ? maxSpeed / accelTime : maxSpeed / decelTime;

        if (!groundDetector.IsGrounded)
            accelRate *= airControl;

        float speedDiff = targetSpeed - rb.linearVelocity.x;
        float movement =
            Mathf.Sign(speedDiff)
            * Mathf.Min(Mathf.Abs(speedDiff), accelRate * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(rb.linearVelocity.x + movement, rb.linearVelocity.y);
    }
}
