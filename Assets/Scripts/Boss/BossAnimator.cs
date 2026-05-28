using UnityEngine;

[RequireComponent(typeof(BossBase))]
public class BossAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator _animator;
    [SerializeField] private Rigidbody2D _rb;

    private static readonly int SpeedXHash    = Animator.StringToHash("SpeedX");
    private static readonly int AttackHash    = Animator.StringToHash("Attack");
    private static readonly int TelegraphHash = Animator.StringToHash("Telegraph");
    private static readonly int IsGroggyHash  = Animator.StringToHash("IsGroggy");
    private static readonly int DeathHash     = Animator.StringToHash("Death");

    private BossBase _boss;

    private void Awake()
    {
        _boss = GetComponent<BossBase>();
        if (_animator == null) _animator = GetComponent<Animator>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        if (_rb == null) _rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        if (_boss == null) return;
        _boss.AttackPerformed += HandleAttack;
        _boss.TelegraphPerformed += HandleTelegraph;
        _boss.Died += HandleDied;
        _boss.OnGroggyStarted += HandleGroggyStart;
        _boss.OnGroggyEnded += HandleGroggyEnd;
    }

    private void OnDisable()
    {
        if (_boss == null) return;
        _boss.AttackPerformed -= HandleAttack;
        _boss.TelegraphPerformed -= HandleTelegraph;
        _boss.Died -= HandleDied;
        _boss.OnGroggyStarted -= HandleGroggyStart;
        _boss.OnGroggyEnded -= HandleGroggyEnd;
    }

    private void Update()
    {
        if (_animator == null) return;
        if (_rb != null)
            _animator.SetFloat(SpeedXHash, Mathf.Abs(_rb.linearVelocity.x));
    }

    private void HandleAttack()      { if (_animator != null) _animator.SetTrigger(AttackHash); }
    private void HandleTelegraph()   { if (_animator != null) _animator.SetTrigger(TelegraphHash); }
    private void HandleDied()        { if (_animator != null) _animator.SetTrigger(DeathHash); }
    private void HandleGroggyStart() { if (_animator != null) _animator.SetBool(IsGroggyHash, true); }
    private void HandleGroggyEnd()   { if (_animator != null) _animator.SetBool(IsGroggyHash, false); }
}
