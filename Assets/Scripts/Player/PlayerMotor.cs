using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMotor : MonoBehaviour
{
    private Rigidbody2D rb;

    private float? requestedX;
    private float? requestedY;

    public float VelocityX => rb.linearVelocityX;
    public float VelocityY => rb.linearVelocityY;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void SetVelocityX(float vx) => requestedX = vx;

    public void SetVelocityY(float vy) => requestedY = vy;

    public void SetGravityScale(float scale) => rb.gravityScale = scale;

    private void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(
            requestedX ?? rb.linearVelocityX,
            requestedY ?? rb.linearVelocityY
        );

        requestedX = null;
        requestedY = null;
    }
}
