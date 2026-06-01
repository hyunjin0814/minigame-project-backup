using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 씬 전체를 제어한다.
///   - 버튼 클릭 → 슬롯 패널 / 설정 패널 전환
///   - 슬롯 패널 열릴 때 SaveManager.PeekSlot으로 슬롯 데이터 주입
/// </summary>
public class TitleManager : MonoBehaviour
{
    [Header("타이틀 버튼")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("화면 그룹")]
    [SerializeField] private GameObject buttonContainer;  // 버튼 묶음 (게임 시작·설정·종료)
    [SerializeField] private GameObject slotPanel;        // 슬롯 선택 패널
    [SerializeField] private GameObject settingsPanel;    // 설정 패널 (추후 구현)

    [Header("슬롯 (3개)")]
    [SerializeField] private SaveSlotUI[] slots;          // SaveSlot_0 ~ SaveSlot_2 연결

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        startButton.onClick.AddListener(OnStartClicked);
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);
        quitButton.onClick.AddListener(OnQuitClicked);

        // 시작 시 패널 모두 비활성
        slotPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    // ── 버튼 핸들러 ───────────────────────────────────────────────────────

    private void OnStartClicked()
    {
        // 각 슬롯에 저장 데이터 주입 (없으면 null → 빈 슬롯 표시)
        for (int i = 0; i < slots.Length; i++)
            slots[i].Setup(i, SaveManager.PeekSlot(i));

        buttonContainer.SetActive(false);
        slotPanel.SetActive(true);
    }

    private void OnSettingsClicked()
    {
        // TODO: 설정 패널 구현 후 활성화
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── 슬롯 패널 Back 버튼에서 호출 ─────────────────────────────────────

    public void CloseSlotPanel()
    {
        slotPanel.SetActive(false);
        buttonContainer.SetActive(true);
    }
}
