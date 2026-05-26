using UnityEngine;

/// <summary>
/// 강아지 형태 전용 감지 컴포넌트.
/// 후각: 근거리 원형 범위 내 Idle/Patrol 적 → 약점 마킹.
/// 청각: 원거리 범위 내 Chase/Attack 적 → 약점 마킹 (소리로 위치 파악).
/// DogState.Enter/Exit 에서 enabled 토글.
/// </summary>
public class DogSense : MonoBehaviour
{
    [Header("Sense Radii")]
    [SerializeField]
    private float _smellRadius = 5f;

    [SerializeField]
    private float _hearingRadius = 10f;

    [Header("Weakness")]
    [SerializeField]
    private float _weaknessDuration = 4f;

    [SerializeField]
    private LayerMask _enemyLayer;

    private const float ScanInterval = 0.2f;
    private float _scanTimer;

    private void OnEnable()
    {
        _scanTimer = 0f;
    }

    private void OnDisable()
    {
        // 폼 해제 후에도 약점은 _weaknessDuration 타이머까지 유지.
        // 플레이어가 강아지로 마킹 → 인간으로 전환 → 공격하는 콤보 허용.
    }

    private void Update()
    {
        _scanTimer -= Time.deltaTime;
        if (_scanTimer > 0f)
            return;
        _scanTimer = ScanInterval;

        ScanEnemies();
    }

    private void ScanEnemies()
    {
        // 후각: 근거리 Idle/Patrol 적 → 약점 마킹
        Collider2D[] smellHits = Physics2D.OverlapCircleAll(
            transform.position,
            _smellRadius,
            _enemyLayer
        );
        foreach (var col in smellHits)
        {
            var enemy = col.GetComponentInParent<EnemyBase>();
            if (enemy == null)
                continue;
            if (
                enemy.CurrentState == EnemyBase.EnemyState.Idle
                || enemy.CurrentState == EnemyBase.EnemyState.Patrol
            )
            {
                enemy.ExposeWeakness(_weaknessDuration);
            }
        }

        // 청각: 원거리 Chase/Attack 적 → 약점 마킹 (소리 있는 상태)
        Collider2D[] hearingHits = Physics2D.OverlapCircleAll(
            transform.position,
            _hearingRadius,
            _enemyLayer
        );
        foreach (var col in hearingHits)
        {
            var enemy = col.GetComponentInParent<EnemyBase>();
            if (enemy == null)
                continue;
            if (
                enemy.CurrentState == EnemyBase.EnemyState.Chase
                || enemy.CurrentState == EnemyBase.EnemyState.Attack
            )
            {
                enemy.ExposeWeakness(_weaknessDuration);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, _smellRadius);
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, _hearingRadius);
    }
}
