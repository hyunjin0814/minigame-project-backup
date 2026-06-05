using System;
using UnityEngine;

[RequireComponent(typeof(Health))]
public abstract class EnemyBase : MonoBehaviour, IWeaknessTarget
{
    public enum EnemyState
    {
        Idle,
        Patrol,
        Detect, // 짧은 인지 딜레이 (Alert 상당)
        Chase,
        Combat, // 감지 후 전투 지속 상태
        Attack,
    }

    [Header("Detection")]
    [SerializeField]
    protected LayerMask _playerLayer;

    [SerializeField]
    protected LayerMask _obstacleLayer;

    [Tooltip("Chase/Attack 중 플레이어가 이 거리 밖으로 나가면 추적 포기. 서브클래스에서 Awake에 감지 반경+α로 설정 권장.")]
    [SerializeField]
    protected float _losePlayerRange = 14f;

    [Header("Weakness")]
    [SerializeField]
    private float _weaknessDamageMultiplier = 2f;

    [Header("Knockback")]
    [SerializeField] private bool  _canBeKnockedBack  = false;
    [SerializeField] private float _knockbackForce    = 5f;
    [SerializeField] private float _knockbackDuration = 0.15f;

    /// <summary>FixedUpdate에서 서브클래스가 velocity 적용에 사용.</summary>
    protected bool   _isKnockedBack;
    protected Vector2 _knockbackVelocity;
    private   float  _knockbackTimer;

    [Header("Death")]
    [SerializeField]
    private float _deathAnimDuration = 2f;

    public bool IsDead { get; private set; }

    protected Health _health;
    protected int _hp => _health.CurrentHp;

    protected EnemyState _currentState = EnemyState.Patrol;
    public EnemyState CurrentState => _currentState;

    protected Vector2 _lastKnownPlayerPosition;
    protected Transform _player;

    protected WeaknessDebuff _currentDebuff;

    // ── 약점 시스템 ───────────────────────────────────────────
    // IWeaknessTarget.Transform — MonoBehaviour.transform과 이름 충돌 회피 위해 명시적 구현
    Transform IWeaknessTarget.Transform => transform;

    public bool IsWeaknessExposed { get; private set; }
    public event Action<bool> OnWeaknessChanged;
    private float _weaknessTimer;

    // ── 시각 표현용 이벤트 (EnemyAnimator가 구독) ─────────────
    public event Action AttackPerformed;
    public event Action Hurt;
    public event Action Died;

    protected void RaiseAttackPerformed() => AttackPerformed?.Invoke();

    // ── 라이프사이클 ─────────────────────────────────────────
    protected virtual void Awake()
    {
        _health = GetComponent<Health>();
        _health.DamageModifier = ComputeFinalDamage;
    }

    protected virtual void OnEnable()
    {
        _health.OnHit   += HandleHit;
        _health.OnDeath += HandleDeath;
        Health.OnPlayerDied += HandlePlayerDied;
    }

    protected virtual void OnDisable()
    {
        _health.OnHit   -= HandleHit;
        _health.OnDeath -= HandleDeath;
        Health.OnPlayerDied -= HandlePlayerDied;
        if (IsWeaknessExposed) ClearWeakness();
    }

    private void HandlePlayerDied()
    {
        if (IsDead) return;
        _player = null;
        ChangeState(EnemyState.Patrol);
    }

    protected virtual void Update()
    {
        if (IsDead) return;
        TickDebuff();
        TickWeakness();
        TickKnockback();
        TickPlayerLost();
    }

    // ── 피격/사망 ─────────────────────────────────────────────
    private void HandleHit(int damage, Vector2 source) => OnHit(source);

    private void HandleDeath() => Die();

    protected virtual void OnHit(Vector2 attackerPosition)
    {
        if (_health.CurrentHp <= 0)
            return;
        Hurt?.Invoke();

        // 공격 중이 아닐 때만 hitbox 비활성화 — 공격 취소 없음 원칙 적용
        if (_currentState != EnemyState.Attack)
            DeactivateAllHitboxes();

        // 넉백 — _canBeKnockedBack이 true인 적만 위치가 밀려남
        if (_canBeKnockedBack)
        {
            float dirX = transform.position.x >= attackerPosition.x ? 1f : -1f;
            _knockbackVelocity = new Vector2(dirX * _knockbackForce, 0f);
            _isKnockedBack     = true;
            _knockbackTimer    = _knockbackDuration;
        }

        if (_currentState == EnemyState.Chase || _currentState == EnemyState.Attack)
            return;
        _lastKnownPlayerPosition = attackerPosition;
        ChangeState(EnemyState.Chase);
    }

    private void DeactivateAllHitboxes()
    {
        foreach (var hitbox in GetComponentsInChildren<AttackHitbox>(true))
            hitbox.Deactivate();
    }

    // 사망 시 본체·접촉·공격 등 모든 콜라이더를 즉시 비활성화 (플레이어와의 상호작용 차단)
    private void DisableAllColliders()
    {
        foreach (var col in GetComponentsInChildren<Collider2D>(true))
            col.enabled = false;
    }

    protected virtual void Die()
    {
        if (IsDead) return;
        IsDead = true;

        Debug.Log($"[{GetType().Name}] 사망");
        Died?.Invoke();

        // 약점 즉시 해제 — 강아지가 시체를 마킹하지 못하게
        if (IsWeaknessExposed) ClearWeakness();

        // 사망 즉시 플레이어와의 모든 상호작용 차단:
        //  - 콜라이더 전부 비활성화 → 접촉 데미지(ContactDamage)·공격 히트박스 정지
        //  - 본체 콜라이더도 꺼서 플레이어의 내려찍기(포고)가 시체에 반응하지 않게
        DisableAllColliders();

        // 물리 정지 — 시체가 낙하·충돌 없이 죽은 자리에 멈춤 (FixedUpdate도 IsDead 가드로 막힘)
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        // Death 애니메이션 재생 시간 확보 후 비활성화
        Invoke(nameof(DisableAfterDeath), _deathAnimDuration);
    }

    private void DisableAfterDeath()
    {
        gameObject.SetActive(false);
    }

    // ── 넉백 타이머 ───────────────────────────────────────────
    private void TickKnockback()
    {
        if (!_isKnockedBack) return;
        _knockbackTimer -= Time.deltaTime;
        if (_knockbackTimer <= 0f)
            _isKnockedBack = false;
    }

    // ── 디버프 ────────────────────────────────────────────────
    public void ApplyDebuff(WeaknessDebuff debuff)
    {
        _currentDebuff = new WeaknessDebuff(debuff.Duration, debuff.DamageMultiplier);
    }

    public void ExposeWeakness(float duration)
    {
        _weaknessTimer = duration;
        if (!IsWeaknessExposed)
        {
            IsWeaknessExposed = true;
            OnWeaknessChanged?.Invoke(true);
            WeaknessRegistry.NotifyExposed(this);
        }
    }

    public void ClearWeakness()
    {
        if (!IsWeaknessExposed)
            return;
        IsWeaknessExposed = false;
        _weaknessTimer = 0f;
        OnWeaknessChanged?.Invoke(false);
        WeaknessRegistry.NotifyCleared(this);
    }

    // 강아지 외부 스캔 허용 여부. 잡몹은 기본 항상 허용.
    // 특수 적(EliteEnemy 등)이 다른 규칙 원하면 override.
    public virtual bool CanBeSensedExternally => true;

    // ── 데미지 계산 체인 ──────────────────────────────────────
    // Health.DamageModifier에 항상 이 메서드만 등록.
    // 서브클래스는 DamageModifier를 교체하지 말고 ApplySpecialModifier만 오버라이드.
    // 가장 최근 피격이 백스탭이었는지. 히트스톱 치명타 판정용(PlayerAttack이 조회).
    // ComputeFinalDamage 진입 시 false로 리셋하고, 서브클래스가 ApplySpecialModifier에서 설정.
    public bool LastHitWasBackstab { get; protected set; }

    private int ComputeFinalDamage(int baseDamage, Vector2 source)
    {
        LastHitWasBackstab = false;

        // ⓪ 완전 차단 (방향성 가드 등) — 최소 1 보정보다 먼저 처리해 0 데미지 보장
        if (BlocksDamageFrom(source))
            return 0;

        // ① 서브클래스 전용 배율 (백스탭, 가드 감소 등)
        int damage = ApplySpecialModifier(baseDamage, source);

        // ② 약점 배율 — 강아지 감지로 마킹된 경우
        if (IsWeaknessExposed)
            damage = Mathf.RoundToInt(damage * _weaknessDamageMultiplier);

        // ③ 외부 디버프 배율 (아이템, 스킬 등)
        if (_currentDebuff != null)
            damage = Mathf.RoundToInt(damage * _currentDebuff.DamageMultiplier);

        return Mathf.Max(1, damage);
    }

    // 특정 방향/조건의 피해를 완전 차단(0 데미지). 기본은 차단 안 함.
    // 방향성 가드 등 source 기반 판정이 필요한 서브클래스가 override.
    protected virtual bool BlocksDamageFrom(Vector2 source) => false;

    // 서브클래스 전용 데미지 배율. 기본 구현은 그대로 반환.
    // 위치 기반 판정(백스탭 등)이 필요한 경우 source 사용.
    protected virtual int ApplySpecialModifier(int damage, Vector2 source) => damage;

    private void TickDebuff()
    {
        if (_currentDebuff == null)
            return;
        _currentDebuff.RemainingTime -= Time.deltaTime;
        if (_currentDebuff.RemainingTime <= 0f)
            _currentDebuff = null;
    }

    private void TickWeakness()
    {
        if (!IsWeaknessExposed)
            return;
        _weaknessTimer -= Time.deltaTime;
        if (_weaknessTimer <= 0f)
            ClearWeakness(); // Registry 정리도 포함
    }

    // ── 플레이어 이탈 감지 ────────────────────────────────────
    private void TickPlayerLost()
    {
        if (_player == null) return;
        if (_currentState != EnemyState.Chase && _currentState != EnemyState.Attack) return;
        if (Vector2.Distance(transform.position, _player.position) > _losePlayerRange)
        {
            _player = null;
            OnPlayerLost();
        }
    }

    /// <summary>
    /// 플레이어가 _losePlayerRange 밖으로 나가 추적 포기 시 호출.
    /// 기본: Combat(마지막 위치 수색) → Patrol.
    /// 공중 적 등 수색이 불필요한 경우 override해서 Patrol로 직행.
    /// </summary>
    protected virtual void OnPlayerLost()
    {
        ChangeState(EnemyState.Combat);
    }

    // ── 추상 메서드 ───────────────────────────────────────────
    protected abstract bool DetectPlayer();

    // ── 상태 전환 ─────────────────────────────────────────────
    protected virtual void ChangeState(EnemyState newState)
    {
        _currentState = newState;
    }
}
