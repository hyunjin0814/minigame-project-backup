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

    protected Health _health;
    protected int _hp => _health.CurrentHp;

    protected EnemyState _currentState = EnemyState.Patrol;
    protected Vector2 _lastKnownPlayerPosition;
    protected Transform _player;

    protected WeaknessDebuff _currentDebuff;

    // ── 라이프사이클 ─────────────────────────────────────────
    protected virtual void Awake()
    {
        _health = GetComponent<Health>();
        _health.DamageModifier = ApplyDebuffModifier;
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

    protected virtual void Update() => TickDebuff();

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

    // source: 서브클래스(CombatEnemy 등)가 백스탭 등 위치 기반 판정에 사용.
    // 디버프만 적용하는 베이스 구현에서는 source 미사용.
    private int ApplyDebuffModifier(int baseDamage, Vector2 source)
    {
        return _currentDebuff != null
            ? Mathf.RoundToInt(baseDamage * _currentDebuff.DamageMultiplier)
            : baseDamage;
    }

    private void TickDebuff()
    {
        if (_currentDebuff == null) return;
        _currentDebuff.RemainingTime -= Time.deltaTime;
        if (_currentDebuff.RemainingTime <= 0f)
            _currentDebuff = null;
    }

    // ── 추상 메서드 ───────────────────────────────────────────
    protected abstract bool DetectPlayer();

    // ── 상태 전환 ─────────────────────────────────────────────
    protected virtual void ChangeState(EnemyState newState)
    {
        _currentState = newState;
    }
}
