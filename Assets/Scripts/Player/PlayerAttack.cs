using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private AttackHitbox hitbox;
    [SerializeField] private float attackDuration = 0.2f;
    [SerializeField] private float attackCooldown = 0.4f;
    [SerializeField] private float comboWindow = 0.5f;  // TODO: 애니메이션 붙일 때 조정
    [SerializeField] private float attackBuffer = 0.1f;

    private enum AttackPhase { Ready, Attacking, Cooldown }
    private AttackPhase phase = AttackPhase.Ready;
    private float timer;

    private int comboIndex = 0;
    private float comboResetTimer;
    private bool attackBuffered;
    private float attackBufferTimer;

    private bool facingRight = true;
    private PlayerInputHandler inputHandler;

    private void Awake()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
    }

    private void OnEnable()
    {
        inputHandler.OnAttack += HandleAttack;
    }

    private void OnDisable()
    {
        inputHandler.OnAttack -= HandleAttack;
        hitbox?.Deactivate();
        phase = AttackPhase.Ready;
        attackBuffered = false;
    }

    private void Update()
    {
        if (inputHandler.MoveInput.x != 0)
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
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    hitbox.Deactivate();
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
        if (hitbox == null) return;

        if (phase == AttackPhase.Ready)
            ExecuteAttack();
        else
        {
            attackBuffered = true;
            attackBufferTimer = attackBuffer;
        }
    }

    private void ExecuteAttack()
    {
        // TODO: PlayAnimation(comboIndex) — 애니메이션 붙일 때 연결

        Vector3 pos = hitbox.transform.localPosition;
        hitbox.transform.localPosition = new Vector3(
            Mathf.Abs(pos.x) * (facingRight ? 1f : -1f),
            pos.y,
            pos.z
        );

        hitbox.Activate();
        timer = attackDuration;
        phase = AttackPhase.Attacking;
        comboIndex = (comboIndex + 1) % 3;
    }
}
