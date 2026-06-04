using UnityEngine;

[RequireComponent(typeof(EnemyBase))]
public class EnemyAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator _animator;
    [SerializeField] private Rigidbody2D _rb;

    private static readonly int SpeedXHash = Animator.StringToHash("SpeedX");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DeathHash = Animator.StringToHash("Death");
    private static readonly int GuardHash = Animator.StringToHash("Guard");
    private static readonly int CounterHash = Animator.StringToHash("Counter");

    private EnemyBase _enemy;
    private bool _hasSpeedX;
    private bool _hasGuard;   // Guard/Counter 파라미터를 가진 컨트롤러(EliteEnemy/Skeleton)에서만 동작
    private bool _hasCounter;

    private void Awake()
    {
        _enemy = GetComponent<EnemyBase>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        if (_rb == null)       _rb       = GetComponent<Rigidbody2D>();
        if (_animator != null)
        {
            _hasSpeedX  = HasParameter(SpeedXHash);
            _hasGuard   = HasParameter(GuardHash);
            _hasCounter = HasParameter(CounterHash);
        }
    }

    private void OnEnable()
    {
        if (_enemy == null) return;
        _enemy.AttackPerformed += HandleAttack;
        _enemy.Hurt += HandleHurt;
        _enemy.Died += HandleDied;
    }

    private void OnDisable()
    {
        if (_enemy == null) return;
        _enemy.AttackPerformed -= HandleAttack;
        _enemy.Hurt -= HandleHurt;
        _enemy.Died -= HandleDied;
    }

    private void Update()
    {
        if (_animator != null && _rb != null && _hasSpeedX)
            _animator.SetFloat(SpeedXHash, Mathf.Abs(_rb.linearVelocity.x));
    }

    private void HandleAttack()
    {
        if (_animator != null) _animator.SetTrigger(AttackHash);
    }

    private void HandleHurt()
    {
        if (_animator != null) _animator.SetTrigger(HitHash);
    }

    private void HandleDied()
    {
        if (_animator != null) _animator.SetTrigger(DeathHash);
    }

    // EliteEnemy 등 방패형 적이 직접 호출 — Guard/Counter는 전용 동작이라 이벤트 대신 메서드로 노출
    public void PlayGuard()
    {
        if (_animator != null && _hasGuard) _animator.SetTrigger(GuardHash);
    }

    public void PlayCounter()
    {
        if (_animator != null && _hasCounter) _animator.SetTrigger(CounterHash);
    }

    public void SetAnimatorController(RuntimeAnimatorController controller)
    {
        if (_animator != null)
        {
            _animator.runtimeAnimatorController = controller;
            _hasSpeedX  = HasParameter(SpeedXHash);
            _hasGuard   = HasParameter(GuardHash);
            _hasCounter = HasParameter(CounterHash);
        }
    }

    private bool HasParameter(int hash)
    {
        foreach (var p in _animator.parameters)
            if (p.nameHash == hash) return true;
        return false;
    }
}
