using UnityEngine;

/// <summary>
/// SentryEnemy 기반 고급형 적. 스탯 강화 변종.
/// 추가 패턴:
///   1. 방어 자세 (Guard): 피격 시 짧은 시간 동안 받는 데미지 대폭 감소.
///   2. 반격 (Counter): 방어 자세 중 피격 시 즉시 근접 반격.
/// </summary>
public class EliteEnemy : SentryEnemy
{
    [Header("Guard Pattern")]
    [SerializeField] private float _guardDuration = 1.5f;
    [SerializeField] private float _guardDamageReduction = 0.8f; // 받는 데미지 80% 감소
    [SerializeField] private float _guardCooldown = 5f;

    [Header("Counter Pattern")]
    [SerializeField] private float _counterRange = 2.5f;
    [SerializeField] private int _counterDamage = 4;

    private bool _isGuarding;
    private float _guardTimer;
    private float _guardCooldownTimer;

    protected override void Update()
    {
        base.Update();
        TickGuard();
    }

    private void TickGuard()
    {
        if (_guardCooldownTimer > 0f) _guardCooldownTimer -= Time.deltaTime;

        if (_isGuarding)
        {
            _guardTimer -= Time.deltaTime;
            if (_guardTimer <= 0f)
                ExitGuard();
        }
    }

    protected override void OnHit(Vector2 attackerPosition)
    {
        if (_isGuarding)
        {
            // 방어 자세 중 → 즉시 반격 후 가드 해제
            TryCounterAttack();
            ExitGuard();
            return;
        }

        base.OnHit(attackerPosition);

        if (_guardCooldownTimer <= 0f)
            EnterGuard();
    }

    private void EnterGuard()
    {
        _isGuarding = true;
        _guardTimer = _guardDuration;
        _guardCooldownTimer = _guardCooldown;
        Debug.Log("[EliteEnemy] 방어 자세 진입");
    }

    private void ExitGuard()
    {
        _isGuarding = false;
        ChangeState(EnemyState.Chase);
        Debug.Log("[EliteEnemy] 방어 자세 해제 → 추격 재개");
    }

    protected override void FixedUpdate()
    {
        if (_isGuarding)
        {
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            return;
        }
        base.FixedUpdate();
    }

    private void TryCounterAttack()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, _counterRange, _playerLayer);
        if (hit == null) return;

        var damageable = hit.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(_counterDamage, transform.position);
            Debug.Log("[EliteEnemy] 반격!");
        }
    }

    // 약점·디버프 공통 로직은 EnemyBase.ComputeFinalDamage에서 처리.
    // 여기서는 가드 데미지 감소만 계산.
    protected override int ApplySpecialModifier(int damage, Vector2 source)
    {
        if (_isGuarding)
            return Mathf.RoundToInt(damage * (1f - _guardDamageReduction));
        return damage;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, _counterRange);
    }
}
