using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>히트스톱 강도 등급.</summary>
public enum HitStopType
{
    Light,    // 잡몹
    Heavy,    // 보스/대형 적
    Critical, // 백스탭·마무리 일격
}

/// <summary>
/// DontDestroyOnLoad 싱글톤. 히트스톱(Time.timeScale=0)의 단일 소유자.
///
/// CLAUDE.md 원칙대로 전역 timeScale=0 방식("전체 정지")을 유지하되,
/// 정지 주체를 일시적 오브젝트(Player)가 아닌 영속 매니저로 옮겨
/// 정지 중 Player가 파괴돼도 timeScale이 고착되지 않도록 한다.
///
/// 재진입은 누적이 아니라 "더 긴 쪽으로 연장"한다.
///
/// [씬 설정] 첫 씬의 [Managers] 오브젝트에 이 컴포넌트를 추가.
/// [주의] 정지 중에도 계속 움직여야 하는 요소는 각자 timeScale을 무시하도록 설정:
///   - VFX/UI Animator → Update Mode = Unscaled Time
///   - ParticleSystem  → Main 모듈 → Use Unscaled Time
///   - 코루틴 연출      → unscaledDeltaTime / WaitForSecondsRealtime
/// (사운드는 timeScale 영향을 받지 않으므로 별도 조치 불필요)
/// </summary>
public class HitStopManager : MonoBehaviour
{
    public static HitStopManager Instance { get; private set; }

    [Header("타입별 지속시간 (초)")]
    [SerializeField] private float lightDuration    = 0.025f; // 잡몹
    [SerializeField] private float heavyDuration    = 0.09f;  // 보스/대형
    [SerializeField] private float criticalDuration = 0.17f;  // 백스탭·마무리 일격

    private float _remaining;

    public bool IsActive => _remaining > 0f;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 어떤 경로로 씬이 로드되든(직접 LoadScene 포함) 새 씬은 timeScale=1로 시작
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ForceResume();

    /// <summary>히트스톱 발동. 적용된 지속시간(초)을 반환 → 호출부가 넉백 타이밍에 사용.</summary>
    public float Freeze(HitStopType type)
    {
        float d = DurationOf(type);
        _remaining = Mathf.Max(_remaining, d); // 누적이 아니라 더 긴 쪽으로 연장
        Time.timeScale = 0f;
        Debug.Log($"[HitStopManager] Freeze {type} ({d}s), remaining={_remaining}");
        return d;
    }

    /// <summary>씬 전환 등에서 즉시 해제. timeScale 고착 방지용 안전망.</summary>
    public void ForceResume()
    {
        if (_remaining <= 0f && Mathf.Approximately(Time.timeScale, 1f)) return;
        _remaining = 0f;
        Time.timeScale = 1f;
        Debug.Log("[HitStopManager] ForceResume");
    }

    private void Update()
    {
        if (_remaining <= 0f) return;

        _remaining -= Time.unscaledDeltaTime; // timeScale=0이어도 Update는 돈다
        if (_remaining <= 0f)
            Time.timeScale = 1f;
    }

    private float DurationOf(HitStopType type) => type switch
    {
        HitStopType.Heavy    => heavyDuration,
        HitStopType.Critical => criticalDuration,
        _                    => lightDuration,
    };
}
