using UnityEngine;

/// <summary>
/// 강아지 전용 돌진 공격 (원키 액티브 스킬).
/// 발동: PlayerTransformController.HandleTransformDog → BeginSkill(facing).
/// 흐름: 수평 돌진 → 첫 적 충돌 → 데미지 + 약점 부여(CanBeSensedExternally일 때) → 강타 정지 → 후퇴.
/// 종료 시 OnSkillCompleted 발행 → PlayerTransformController가 인간 복귀.
/// 접촉 피해(ContactDamage)에는 돌진 중 면역(IContactDamageImmune).
/// 쿨타임은 Time.time 기반이라 폼 전환과 무관. AbortDash는 쿨타임만 적용 (이벤트 미발행).
/// </summary>
[RequireComponent(typeof(PlayerMotor))]
public class DogDashAttack : MonoBehaviour, IContactDamageImmune
{
    [Header("Dash")]
    [SerializeField] private float _dashSpeed = 18f;
    [SerializeField] private float _maxDashDuration = 0.7f;
    [SerializeField] private float _maxDashDistance = 8f;
    [SerializeField] private float _retreatDistance = 2f;
    [SerializeField] private float _retreatSpeed = 12f;
    [SerializeField] private float _retreatMaxDuration = 0.5f;
    [SerializeField] private float _strikePauseDuration = 0.15f;
    [SerializeField] private float _dashAttackCooldown = 7f;
    [SerializeField] private int _damage = 20;
    [SerializeField] private float _weaknessExposeOnHitDuration = 5f;

    [Header("Hitbox")]
    [SerializeField] private Vector2 _hitboxOffset = new Vector2(0.7f, 0f);
    [SerializeField] private Vector2 _hitboxSize = new Vector2(0.8f, 0.6f);
    [SerializeField] private LayerMask _targetLayer;

    public float TotalCooldown => _dashAttackCooldown;
    public float CooldownRemaining => Mathf.Max(0f, _cooldownEndTime - Time.time);
    public bool IsReady => _phase == DashPhase.Idle && Time.time >= _cooldownEndTime;
    public bool IsExecuting => _phase != DashPhase.Idle;

    /// <summary>돌진 시퀀스가 자연 종료됐을 때 발행. AbortDash(강제 중단)는 발행하지 않음.</summary>
    public event System.Action OnSkillCompleted;

    // 돌진 시퀀스(돌진·강타·후퇴) 중 접촉 피해 면역
    public bool IsContactDamageImmune => isActiveAndEnabled && IsExecuting;

    private enum DashPhase { Idle, Dashing, Striking, Retreating }
    private DashPhase _phase = DashPhase.Idle;

    private PlayerGroundDetector _ground;
    private PlayerMotor _motor;
    private Rigidbody2D _rb;

    private float _cooldownEndTime = 0f;
    private float _dashTimeoutTimer;
    private float _strikePauseTimer;
    private float _retreatTimeoutTimer;
    private int _facing = 1;
    private Vector2 _dashStartPos;
    private Vector2 _retreatTargetPos;
    private float _savedGravity;

    private void Awake()
    {
        _ground = GetComponent<PlayerGroundDetector>();
        _motor = GetComponent<PlayerMotor>();
        _rb = GetComponent<Rigidbody2D>();
    }

    private void OnDisable()
    {
        if (IsExecuting) AbortDash();
    }

    private void Update()
    {
        switch (_phase)
        {
            case DashPhase.Dashing:    TickDashing(); break;
            case DashPhase.Striking:   TickStriking(); break;
            case DashPhase.Retreating: TickRetreating(); break;
        }
    }

    private void FixedUpdate()
    {
        switch (_phase)
        {
            case DashPhase.Dashing:
                _motor.SetVelocityX(_facing * _dashSpeed);
                _motor.SetVelocityY(0f);
                break;
            case DashPhase.Striking:
                _motor.SetVelocityX(0f);
                _motor.SetVelocityY(0f);
                break;
            case DashPhase.Retreating:
                _motor.SetVelocityX(-_facing * _retreatSpeed);
                _motor.SetVelocityY(0f);
                break;
        }
    }

    /// <summary>
    /// PlayerTransformController에서 강아지 변신 직후 호출. facing = 1(오른쪽) or -1(왼쪽).
    /// </summary>
    public void BeginSkill(int facing)
    {
        _facing = facing;
        StartDash();
    }

    private void StartDash()
    {
        _phase = DashPhase.Dashing;
        _dashStartPos = transform.position;
        _dashTimeoutTimer = _maxDashDuration;
        _savedGravity = _rb.gravityScale;
        _motor.SetGravityScale(0f);
        Debug.Log($"[DogDashAttack] 돌진 시작 (dir={_facing})");
    }

    private void TickDashing()
    {
        _dashTimeoutTimer -= Time.deltaTime;
        float traveled = Vector2.Distance(transform.position, _dashStartPos);

        // 타임아웃 또는 최대 거리 — 적 못 만남, 종료
        if (_dashTimeoutTimer <= 0f || traveled >= _maxDashDistance)
        {
            Debug.Log("[DogDashAttack] 적 못 만남, 종료");
            EndDash();
            return;
        }

        // 적 충돌 감지 (첫 번째 IDamageable 명중)
        Vector2 hitboxCenter = (Vector2)transform.position + new Vector2(_facing * _hitboxOffset.x, _hitboxOffset.y);
        Collider2D[] hits = Physics2D.OverlapBoxAll(hitboxCenter, _hitboxSize, 0f, _targetLayer);
        foreach (var col in hits)
        {
            var damageable = col.GetComponentInParent<IDamageable>();
            if (damageable == null) continue;

            damageable.TakeDamage(_damage, transform.position);

            // 약점 부여: 잡몹=항상, 보스=그로기 상태일 때만 (CanBeSensedExternally로 판별)
            var weaknessTarget = col.GetComponentInParent<IWeaknessTarget>();
            if (weaknessTarget != null && weaknessTarget.CanBeSensedExternally)
                weaknessTarget.ExposeWeakness(_weaknessExposeOnHitDuration);

            Debug.Log("[DogDashAttack] 돌진 명중");
            EnterStriking();
            return;
        }
    }

    private void EnterStriking()
    {
        _phase = DashPhase.Striking;
        _strikePauseTimer = _strikePauseDuration;
    }

    private void TickStriking()
    {
        _strikePauseTimer -= Time.deltaTime;
        if (_strikePauseTimer <= 0f) EnterRetreating();
    }

    private void EnterRetreating()
    {
        _phase = DashPhase.Retreating;
        _retreatTargetPos = (Vector2)transform.position + new Vector2(-_facing * _retreatDistance, 0f);
        _retreatTimeoutTimer = _retreatMaxDuration;
        Debug.Log("[DogDashAttack] 후퇴");
    }

    private void TickRetreating()
    {
        _retreatTimeoutTimer -= Time.deltaTime;
        float dx = transform.position.x - _retreatTargetPos.x;
        bool reached = (_facing > 0 && dx <= 0.05f) || (_facing < 0 && dx >= -0.05f);
        if (reached || _retreatTimeoutTimer <= 0f) EndDash();
    }

    private void EndDash()
    {
        _motor.SetGravityScale(_savedGravity);
        _phase = DashPhase.Idle;
        _cooldownEndTime = Time.time + _dashAttackCooldown;
        Debug.Log("[DogDashAttack] 종료");
        OnSkillCompleted?.Invoke();
    }

    private void AbortDash()
    {
        _motor.SetGravityScale(_savedGravity);
        _phase = DashPhase.Idle;
        _cooldownEndTime = Time.time + _dashAttackCooldown; // 폼 전환 우회 방지
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.35f);
        Vector2 center = (Vector2)transform.position + new Vector2(_facing * _hitboxOffset.x, _hitboxOffset.y);
        Gizmos.DrawWireCube(center, _hitboxSize);
    }
}
