using System;
using UnityEngine;

[RequireComponent(typeof(PlayerMotor), typeof(PlayerInputHandler))]
public class PlayerDash : MonoBehaviour
{
    [Header("Dash")]
    [SerializeField]
    private float dashSpeed = 20f;

    [SerializeField]
    private float dashDuration = 0.18f;

    [SerializeField]
    private float dashCooldown = 0.4f;

    public bool IsDashing { get; private set; }

    public event Action OnDashStart;
    public event Action OnDashEnd;

    private PlayerMotor motor;
    private PlayerInputHandler inputHandler;

    private float dashTimer;
    private float cooldownTimer;
    private float dashDirection = 1f; // +1 right, -1 left
    private float facingRight = 1f;

    private float? savedGravityScale;
    private Rigidbody2D rb;

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        inputHandler = GetComponent<PlayerInputHandler>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        inputHandler.OnDash += HandleDash;
    }

    private void OnDisable()
    {
        inputHandler.OnDash -= HandleDash;

        if (IsDashing)
            EndDash();
    }

    private void Update()
    {
        if (inputHandler.MoveInput.x > 0.1f)
            facingRight = 1f;
        else if (inputHandler.MoveInput.x < -0.1f)
            facingRight = -1f;

        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (IsDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
                EndDash();
        }
    }

    private void FixedUpdate()
    {
        if (IsDashing)
        {
            motor.SetVelocityX(dashDirection * dashSpeed);
            motor.SetVelocityY(0f);
        }
    }

    private void HandleDash()
    {
        if (IsDashing || cooldownTimer > 0f)
            return;

        StartDash();
    }

    private void StartDash()
    {
        IsDashing = true;
        dashTimer = dashDuration;
        dashDirection = facingRight;

        savedGravityScale = rb.gravityScale;
        motor.SetGravityScale(0f);

        Debug.Log($"[PlayerDash] Dash 시작 (dir={dashDirection})");
        OnDashStart?.Invoke();
    }

    private void EndDash()
    {
        IsDashing = false;
        cooldownTimer = dashCooldown;

        if (savedGravityScale.HasValue)
        {
            motor.SetGravityScale(savedGravityScale.Value);
            savedGravityScale = null;
        }

        Debug.Log("[PlayerDash] Dash 종료");
        OnDashEnd?.Invoke();
    }
}
