using System;
using UnityEngine;

public enum AttackDirection
{
    Side,
    Up,
    Down,
}

[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(PlayerMotor))]
[RequireComponent(typeof(PlayerGroundDetector))]
[RequireComponent(typeof(PlayerDash))]
public class PlayerAttack : MonoBehaviour
{
    [SerializeField]
    private AttackHitbox hitbox;

    [SerializeField]
    private float attackDuration = 0.2f;

    [SerializeField]
    private float attackCooldown = 0.4f;

    [SerializeField]
    private float comboWindow = 0.5f;

    [SerializeField]
    private float attackBuffer = 0.1f;

    [Header("Hitbox - Side")]
    [SerializeField]
    private Vector2 hitboxOffsetSide = new Vector2(0.6f, 0f);

    [SerializeField]
    private Vector2 hitboxSizeSide = new Vector2(0.7f, 0.4f);

    [Header("Hitbox - Up")]
    [SerializeField]
    private Vector2 hitboxOffsetUp = new Vector2(0f, 0.7f);

    [SerializeField]
    private Vector2 hitboxSizeUp = new Vector2(0.35f, 0.6f);

    [Header("Hitbox - Down")]
    [SerializeField]
    private Vector2 hitboxOffsetDown = new Vector2(0f, -0.6f);

    [SerializeField]
    private Vector2 hitboxSizeDown = new Vector2(0.35f, 0.5f);

    [Header("Hit Reaction")]
    [SerializeField]
    private float hitStopDuration = 0.06f;

    [SerializeField]
    private float knockbackForce = 3f;

    [SerializeField]
    private float pogoForce = 12f;

    [SerializeField, Tooltip("|MoveInput.y|가 이 값을 넘으면 위/아래 공격으로 분기")]
    private float verticalThreshold = 0.5f;

    public event Action<AttackDirection> OnAttackTriggered;

    public bool IsAttacking { get; private set; }
    public bool CanDash { get; private set; } = true;

    private enum AttackPhase
    {
        Ready,
        Attacking,
        Cooldown,
    }

    private AttackPhase phase = AttackPhase.Ready;
    private float timer;

    private int comboIndex = 0;
    private float comboResetTimer;
    private bool attackBuffered;
    private float attackBufferTimer;

    private bool facingRight = true;
    private bool hitResolvedThisSwing;
    private AttackDirection currentDirection = AttackDirection.Side;

    private PlayerInputHandler inputHandler;
    private PlayerMotor motor;
    private PlayerGroundDetector groundDetector;
    private PlayerDash dash;
    private PlayerTransformController transformController;
    private Rigidbody2D rb;

    private void Awake()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        motor = GetComponent<PlayerMotor>();
        groundDetector = GetComponent<PlayerGroundDetector>();
        dash = GetComponent<PlayerDash>();
        transformController = GetComponent<PlayerTransformController>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        inputHandler.OnAttack += HandleAttack;
        if (hitbox != null)
            hitbox.OnHit += HandleHit;
    }

    private void OnDisable()
    {
        inputHandler.OnAttack -= HandleAttack;
        if (hitbox != null)
            hitbox.OnHit -= HandleHit;

        hitbox?.Deactivate();
        IsAttacking = false;
        CanDash = true;
        phase = AttackPhase.Ready;
        attackBuffered = false;
    }

    private void Update()
    {
        if (!IsAttacking && inputHandler.MoveInput.x != 0)
            facingRight = inputHandler.MoveInput.x > 0;

        if (attackBuffered)
        {
            attackBufferTimer -= Time.deltaTime;
            if (attackBufferTimer <= 0f)
                attackBuffered = false;
        }

        switch (phase)
        {
            case AttackPhase.Attacking:
                // AttackDown 중 착지하면 즉시 히트박스 종료
                if (currentDirection == AttackDirection.Down && groundDetector.IsGrounded)
                    timer = 0f;

                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    hitbox.Deactivate();
                    IsAttacking = false;
                    CanDash = true;
                    timer = attackCooldown - attackDuration;
                    phase = AttackPhase.Cooldown;
                }
                break;

            case AttackPhase.Cooldown:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    phase = AttackPhase.Ready;
                    comboResetTimer = comboWindow;

                    if (attackBuffered)
                    {
                        attackBuffered = false;
                        ExecuteAttack();
                    }
                }
                break;

            case AttackPhase.Ready:
                if (comboIndex > 0)
                {
                    comboResetTimer -= Time.deltaTime;
                    if (comboResetTimer <= 0f)
                        comboIndex = 0;
                }
                break;
        }
    }

    private void HandleAttack()
    {
        if (hitbox == null)
            return;

        if (dash != null && dash.IsDashing)
            return;

        if (phase == AttackPhase.Ready)
            ExecuteAttack();
        else
        {
            attackBuffered = true;
            attackBufferTimer = attackBuffer;
        }
    }

    private AttackDirection DetermineDirection()
    {
        float y = inputHandler.MoveInput.y;

        if (y > verticalThreshold)
            return AttackDirection.Up;

        if (y < -verticalThreshold && !groundDetector.IsGrounded)
            return AttackDirection.Down;

        return AttackDirection.Side;
    }

    private void ExecuteAttack()
    {
        currentDirection = DetermineDirection();
        hitResolvedThisSwing = false;

        Vector2 offset,
            size;
        switch (currentDirection)
        {
            case AttackDirection.Up:
                offset = hitboxOffsetUp;
                size = hitboxSizeUp;
                break;
            case AttackDirection.Down:
                offset = hitboxOffsetDown;
                size = hitboxSizeDown;
                break;
            default:
                offset = new Vector2(
                    facingRight ? hitboxOffsetSide.x : -hitboxOffsetSide.x,
                    hitboxOffsetSide.y
                );
                size = hitboxSizeSide;
                break;
        }

        hitbox.SetBox(offset, size);
        IsAttacking = true;
        CanDash = false;

        if (groundDetector.IsGrounded)
            motor.SetVelocityX(0f);

        OnAttackTriggered?.Invoke(currentDirection);

        timer = attackDuration;
        phase = AttackPhase.Attacking;
        comboIndex = (comboIndex + 1) % 3;
    }

    // 애니메이션 이벤트에서 호출
    public void EnableMove()
    {
        IsAttacking = false;
        CanDash = true;
    }

    // 애니메이션 이벤트에서 호출
    public void ActivateHitbox()
    {
        if (phase != AttackPhase.Attacking && phase != AttackPhase.Cooldown)
            return;

        hitbox?.Activate();
        IsAttacking = true;
        CanDash = false;
        timer = attackDuration;
        phase = AttackPhase.Attacking;
    }

    // 애니메이션 이벤트에서 호출
    public void DeactivateHitbox()
    {
        hitbox?.Deactivate();
    }

    private void HandleHit(IDamageable target, Collider2D other)
    {
        if (hitResolvedThisSwing)
            return;
        hitResolvedThisSwing = true;

        Rigidbody2D enemyRb = other.attachedRigidbody;
        AttackDirection dir = currentDirection;

        if (transformController != null)
        {
            transformController.HitStop(enemyRb, hitStopDuration, () => ApplyKnockback(dir));
        }
        else
        {
            ApplyKnockback(dir);
        }
    }

    private void ApplyKnockback(AttackDirection dir)
    {
        switch (dir)
        {
            case AttackDirection.Down:
                // 포고: velocity.y 직접 대입 (점프 리셋 없음)
                motor.SetVelocityY(pogoForce);
                break;
            case AttackDirection.Up:
                motor.SetVelocityY(-knockbackForce);
                break;
            default:
                // Side: 공격 반대 방향으로 반발
                motor.SetVelocityX(facingRight ? -knockbackForce : knockbackForce);
                break;
        }
    }
}
