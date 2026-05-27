using UnityEngine;

/// <summary>
/// 강아지 형태 전용 액티브 스캔 컴포넌트.
/// 입력 시 범위 내 IWeaknessTarget(CanBeSensedExternally=true)에 약점 마킹.
/// 잡몹: 항상 마킹 가능. 보스: 그로기 상태에서만 마킹 가능.
/// DogState.Enter/Exit에서 enabled 토글. 쿨타임은 Time.time 기반이라 폼 전환과 무관하게 흐름.
/// </summary>
public class DogScanner : MonoBehaviour
{
    [Header("Scan")]
    [SerializeField]
    private float _scanRadius = 8f;

    [SerializeField]
    private float _scanCooldown = 5f;

    [SerializeField]
    private float _weaknessDuration = 4f;

    [SerializeField]
    private LayerMask _targetLayer;

    private PlayerInputHandler _input;
    private float _cooldownEndTime = 0f;

    public float TotalCooldown => _scanCooldown;
    public float CooldownRemaining => Mathf.Max(0f, _cooldownEndTime - Time.time);
    public bool IsReady => Time.time >= _cooldownEndTime;

    private void Awake()
    {
        _input = GetComponentInParent<PlayerInputHandler>();
    }

    private void OnEnable()
    {
        // 쿨타임은 Time.time 기반이라 폼 전환과 무관하게 계속 흐름.
        if (_input != null)
            _input.OnScan += HandleScan;
    }

    private void OnDisable()
    {
        if (_input != null)
            _input.OnScan -= HandleScan;
    }

    private void HandleScan()
    {
        if (Time.time < _cooldownEndTime)
        {
            Debug.Log($"[DogScanner] 스캔 쿨타임 {CooldownRemaining:F1}s 남음");
            return;
        }
        DoScan();
        _cooldownEndTime = Time.time + _scanCooldown;
    }

    private void DoScan()
    {
        int marked = 0;
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            _scanRadius,
            _targetLayer
        );
        foreach (var col in hits)
        {
            var target = col.GetComponentInParent<IWeaknessTarget>();
            if (target == null)
                continue;
            if (!target.CanBeSensedExternally)
                continue;
            target.ExposeWeakness(_weaknessDuration);
            marked++;
        }
        Debug.Log($"[DogScanner] 스캔 실행 — 대상 {marked}개 마킹");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.9f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, _scanRadius);
    }
}
