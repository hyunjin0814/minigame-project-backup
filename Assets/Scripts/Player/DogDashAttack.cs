using UnityEngine;

/// <summary>
/// 강아지 전용 돌진 공격.
/// 발동 조건: 지상 + 약점 노출 적 존재(WeaknessRegistry.HasAny) + 쿨타임 OK.
/// 흐름: 수평 돌진 → 첫 약점 적 충돌 → 정지(공격 모션) → 시작 방향 반대로 2칸 후퇴 → 쿨타임.
/// 잡몹 히트 시: 약점 윈도우 즉시 종료.
/// 보스 히트 시: 보스 자체 그로기 타이머만 (Q3-2 B — 윈도우 유지).
/// 돌진 중 무적 없음 — 기존 피격 판정 유지.
/// 쿨타임은 Time.time 기반이라 폼 전환과 무관. 돌진 중 폼 전환되면 AbortDash가 쿨타임 적용.
/// </summary>
[RequireComponent(typeof(PlayerMotor))]
public class DogDashAttack : MonoBehaviour
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

    [Header("Hitbox")]
    [SerializeField] private Vector2 _hitboxOffset = new Vector2(0.7f, 0f);
    [SerializeField] private Vector2 _hitboxSize = new Vector2(0.8f, 0.6f);
    [SerializeField] private LayerMask _targetLayer;

    public float TotalCooldown => _dashAttackCooldown;
    public float CooldownRemaining => Mathf.Max(0f, _cooldownEndTime - Time.time);
    public bool IsReady => _phase == DashPhase.Idle && Time.time >= _cooldownEndTime;
    public bool IsExecuting => _phase != DashPhase.Idle;

    private enum DashPhase { Idle, Dashing, Striking, Retreating }
    private DashPhase _phase = DashPhase.Idle;

    private PlayerInputHandler _input;
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
        _input = GetComponent<PlayerInputHandler>();
        _ground = GetComponent<PlayerGroundDetector>();
        _motor = GetComponent<PlayerMotor>();
        _rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        if (_input != null) _input.OnDogDash += HandleDogDash;
    }

    private void OnDisable()
    {
        if (_input != null) _input.OnDogDash -= HandleDogDash;
        if (IsExecuting) AbortDash();
    }

    private void Update()
    {
        // facing 갱신 (Idle일 때만)
        if (_phase == DashPhase.Idle && _input != null)
        {
            if (_input.MoveInput.x > 0.1f) _facing = 1;
            else if (_input.MoveInput.x < -0.1f) _facing = -1;
        }

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

    private void HandleDogDash()
    {
        if (_phase != DashPhase.Idle) return;
        if (Time.time < _cooldownEndTime)
        {
            Debug.Log($"[DogDashAttack] 쿨타임 {CooldownRemaining:F1}s 남음");
            return;
        }
        if (!_ground.IsGrounded)
        {
            Debug.Log("[DogDashAttack] 공중에서 사용 불가");
            return;
        }
        if (!WeaknessRegistry.HasAny)
        {
            Debug.Log("[DogDashAttack] 약점 노출된 적 없음");
            return;
        }
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

        // 약점 적 충돌 감지
        Vector2 hitboxCenter = (Vector2)transform.position + new Vector2(_facing * _hitboxOffset.x, _hitboxOffset.y);
        Collider2D[] hits = Physics2D.OverlapBoxAll(hitboxCenter, _hitboxSize, 0f, _targetLayer);
        foreach (var col in hits)
        {
            var target = col.GetComponentInParent<IWeaknessTarget>();
            if (target == null || !target.IsWeaknessExposed) continue;

            // 첫 약점 적 — 데미지 + 윈도우 처리
            var damageable = col.GetComponentInParent<IDamageable>();
            damageable?.TakeDamage(_damage, transform.position);

            // 잡몹 윈도우 종료, 보스는 자체 그로기 타이머 유지 (Q3-2 B)
            if (target is EnemyBase) target.ClearWeakness();

            Debug.Log("[DogDashAttack] 약점 일격");
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
        _motor.SetVelocityX(0f);
        _motor.SetGravityScale(_savedGravity);
        _phase = DashPhase.Idle;
        _cooldownEndTime = Time.time + _dashAttackCooldown;
        Debug.Log("[DogDashAttack] 종료");
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
