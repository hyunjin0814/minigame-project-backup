using System;
using UnityEngine;

[RequireComponent(typeof(Health))]
public abstract class EnemyBase : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Patrol,
        Detect,  // 짧은 인지 딜레이 (Alert 상당)
        Chase,
        Combat,  // 감지 후 전투 지속 상태
        Attack,
    }

    [Header("Detection")]
    [SerializeField] protected LayerMask _playerLayer;
    [SerializeField] protected LayerMask _obstacleLayer;

    [Header("Weakness")]
    [SerializeField] private float _weaknessDamageMultiplier = 2f;

    protected Health _health;
    protected int _hp => _health.CurrentHp;

    protected EnemyState _currentState = EnemyState.Patrol;
    public EnemyState CurrentState => _currentState;

    protected Vector2 _lastKnownPlayerPosition;
    protected Transform _player;

    protected WeaknessDebuff _currentDebuff;

    // ── 약점 시스템 ───────────────────────────────────────────
    public bool IsWeaknessExposed { get; private set; }
    public event Action<bool> OnWeaknessChanged;
    private float _weaknessTimer;

    // ── 라이프사이클 ─────────────────────────────────────────
    protected virtual void Awake()
    {
        _health = GetComponent<Health>();
        _health.DamageModifier = ComputeFinalDamage;
    }

    protected virtual void OnEnable()
    {
        _health.OnHit += HandleHit;
        _health.OnDeath += HandleDeath;
    }

    protected virtual void OnDisable()
    {
        _health.OnHit -= HandleHit;
        _health.OnDeath -= HandleDeath;
    }

    protected virtual void Update()
    {
        TickDebuff();
        TickWeakness();
    }

    // ── 피격/사망 ─────────────────────────────────────────────
    private void HandleHit(int damage, Vector2 source) => OnHit(source);
    private void HandleDeath() => Die();

    protected virtual void OnHit(Vector2 attackerPosition)
    {
        if (_health.CurrentHp <= 0) return;
        if (_currentState == EnemyState.Chase || _currentState == EnemyState.Attack) return;
        _lastKnownPlayerPosition = attackerPosition;
        ChangeState(EnemyState.Chase);
    }

    protected virtual void Die()
    {
        Debug.Log($"[{GetType().Name}] 사망");
        gameObject.SetActive(false);
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
        }
    }

    public void ClearWeakness()
    {
        if (!IsWeaknessExposed) return;
        IsWeaknessExposed = false;
        _weaknessTimer = 0f;
        OnWeaknessChanged?.Invoke(false);
    }

    // ── 데미지 계산 체인 ──────────────────────────────────────
    // Health.DamageModifier에 항상 이 메서드만 등록.
    // 서브클래스는 DamageModifier를 교체하지 말고 ApplySpecialModifier만 오버라이드.
    private int ComputeFinalDamage(int baseDamage, Vector2 source)
    {
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

    // 서브클래스 전용 데미지 배율. 기본 구현은 그대로 반환.
    // 위치 기반 판정(백스탭 등)이 필요한 경우 source 사용.
    protected virtual int ApplySpecialModifier(int damage, Vector2 source) => damage;

    private void TickDebuff()
    {
        if (_currentDebuff == null) return;
        _currentDebuff.RemainingTime -= Time.deltaTime;
        if (_currentDebuff.RemainingTime <= 0f)
            _currentDebuff = null;
    }

    private void TickWeakness()
    {
        if (!IsWeaknessExposed) return;
        _weaknessTimer -= Time.deltaTime;
        if (_weaknessTimer <= 0f)
        {
            IsWeaknessExposed = false;
            OnWeaknessChanged?.Invoke(false);
        }
    }

    // ── 추상 메서드 ───────────────────────────────────────────
    protected abstract bool DetectPlayer();

    // ── 상태 전환 ─────────────────────────────────────────────
    protected virtual void ChangeState(EnemyState newState)
    {
        _currentState = newState;
    }
}
