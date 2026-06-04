using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 슬롯 패널의 개별 슬롯 UI를 제어한다.
/// TitleManager가 Setup()을 호출해 슬롯 데이터를 주입한다.
/// </summary>
public class SaveSlotUI : MonoBehaviour
{
    [Header("공통")]
    [SerializeField] private Button          slotButton;
    [SerializeField] private TextMeshProUGUI slotNumberText;

    [Header("빈 슬롯")]
    [SerializeField] private GameObject emptyLabel;

    [Header("저장 데이터")]
    [SerializeField] private GameObject      saveInfoGroup;
    [SerializeField] private HeartDisplayUI  heartDisplay;
    [SerializeField] private TextMeshProUGUI locationText;
    [SerializeField] private TextMeshProUGUI playTimeText;

    [Header("삭제 버튼")]
    [SerializeField] private Button deleteButton;

    private int  _slotIndex;
    private bool _hasSave;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        slotButton.onClick.AddListener(OnSlotClicked);
        if (deleteButton != null)
            deleteButton.onClick.AddListener(OnDeleteClicked);
    }

    // ── 외부 호출 ──────────────────────────────────────────────────────────

    /// <summary>
    /// TitleManager에서 호출. data가 null이면 빈 슬롯으로 표시.
    /// </summary>
    public void Setup(int slotIndex, SaveData data)
    {
        _slotIndex = slotIndex;
        _hasSave   = data != null;

        slotNumberText.text = $"{slotIndex + 1}.";

        if (_hasSave)
            ShowSaveData(data);
        else
            ShowEmpty();
    }

    // ── 슬롯 클릭 ─────────────────────────────────────────────────────────

    private void OnSlotClicked()
    {
        if (_hasSave)
            LoadGame();
        else
            StartNewGame();
    }

    private void LoadGame()
    {
        if (!SaveManager.Load(_slotIndex)) return;

        string scene = GameState.Instance.lastCheckpointScene;
        if (string.IsNullOrEmpty(scene)) scene = "Map1";

        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionTo(scene);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
    }

    private void StartNewGame()
    {
        // 튜토리얼 씬 추가 시 "Map1" → 튜토리얼 씬 이름으로 변경
        const string startScene = "Map1";

        GameState.Instance.Reset();
        GameState.Instance.currentSaveSlot    = _slotIndex;
        GameState.Instance.lastCheckpointScene = startScene;

        // 슬롯 점유 표시를 위해 초기 세이브 파일 즉시 생성
        SaveManager.Save(_slotIndex);

        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionTo(startScene);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(startScene);
    }

    // ── 삭제 클릭 ─────────────────────────────────────────────────────────

    private void OnDeleteClicked()
    {
        SaveManager.Delete(_slotIndex);
        ShowEmpty();
        _hasSave = false;
    }

    // ── 슬롯 표시 ─────────────────────────────────────────────────────────

    private void ShowSaveData(SaveData data)
    {
        emptyLabel.SetActive(false);
        saveInfoGroup.SetActive(true);
        if (deleteButton != null) deleteButton.gameObject.SetActive(true);

        // 체력 아이콘 (저장 당시 현재/최대 체력)
        int maxHp = data.savedMaxHP > 0 ? data.savedMaxHP : 3;
        int curHp = data.savedHP    > 0 ? data.savedHP    : maxHp;
        heartDisplay.Refresh(curHp, maxHp);

        // 위치 (씬 이름)
        locationText.text = string.IsNullOrEmpty(data.lastCheckpointScene)
            ? "Map1"
            : data.lastCheckpointScene;

        // 플레이 시간
        playTimeText.text = FormatPlayTime(data.playTime);
    }

    private void ShowEmpty()
    {
        emptyLabel.SetActive(true);
        saveInfoGroup.SetActive(false);
        if (deleteButton != null) deleteButton.gameObject.SetActive(false);
    }

    // ── 유틸 ──────────────────────────────────────────────────────────────

    private static string FormatPlayTime(float seconds)
    {
        int totalMinutes = Mathf.FloorToInt(seconds / 60f);
        int hours   = totalMinutes / 60;
        int minutes = totalMinutes % 60;
        return hours > 0 ? $"{hours}h {minutes:D2}m" : $"{minutes}m";
    }
}
