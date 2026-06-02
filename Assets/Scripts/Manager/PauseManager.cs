using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// DontDestroyOnLoad 싱글톤.
/// ESC 입력으로 일시정지/재개를 처리하고 설정 패널을 관리한다.
/// 설정 패널은 타이틀(TitleManager)과 게임 중(PausePanel) 양쪽에서 공유된다.
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("패널")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;

    public bool IsPaused { get; private set; }

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // 타이틀 씬에서는 ESC 일시정지 비활성
        if (SceneManager.GetActiveScene().name == "Title") return;

        // 씬 전환 중(페이드 아웃/인)에는 ESC 차단
        if (SceneTransitionManager.Instance != null && SceneTransitionManager.Instance.IsTransitioning) return;

        // 전체 지도(M)가 열려 있으면 ESC가 일시정지 패널을 열지 않도록 차단
        if (MapUI.IsFullMapOpen) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            TogglePause();
    }

    // ── 일시정지 / 재개 ───────────────────────────────────────────────────

    public void TogglePause()
    {
        if (IsPaused) Resume();
        else          Pause();
    }

    public void Pause()
    {
        // 히트스톱 중 일시정지 시 timeScale 고착 방지
        HitStopManager.Instance?.ForceResume();

        IsPaused = true;
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    // ── 설정 패널 (타이틀 / 게임 중 공유) ─────────────────────────────────

    /// <summary>
    /// 타이틀의 설정 버튼, 일시정지 패널의 설정 버튼 모두 여기서 호출.
    /// </summary>
    public void OpenSettings()
    {
        pausePanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    /// <summary>
    /// 설정 패널 닫기 버튼에서 호출.
    /// 게임 중이면 일시정지 패널로 복귀, 타이틀이면 그냥 닫힘.
    /// </summary>
    public void CloseSettings()
    {
        settingsPanel.SetActive(false);

        if (IsPaused)
            pausePanel.SetActive(true);
    }

    // ── 종료하고 나가기 ───────────────────────────────────────────────────

    public void SaveAndQuit()
    {
        // 현재 HP·플레이타임 등 즉시 저장
        if (GameState.Instance != null && GameState.Instance.currentSaveSlot >= 0)
            SaveManager.Save(GameState.Instance.currentSaveSlot);

        // 일시정지 해제 (timeScale 복구)
        Resume();

        // 타이틀로 전환
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionTo("Title");
        else
            SceneManager.LoadScene("Title");
    }
}
