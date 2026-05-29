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

    private EnemyBase _enemy;

    private void Awake()
    {
        _enemy = GetComponent<EnemyBase>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        if (_rb == null) _rb = GetComponent<Rigidbody2D>();
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
        if (_animator != null && _rb != null)
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

    public void SetAnimatorController(RuntimeAnimatorController controller)
    {
        if (_animator != null)
            _animator.runtimeAnimatorController = controller;
    }
}
