using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정 패널(PauseManager.settingsPanel)에 붙이는 볼륨 슬라이더 컨트롤러.
/// 타이틀과 인게임 일시정지 양쪽에서 동일한 패널을 재사용하므로
/// 스크립트도 하나로 공유된다.
///
/// [씬 설정]
///   1. settingsPanel 오브젝트에 이 컴포넌트 추가
///   2. 인스펙터에서 Slider 3개 연결 (sfxSlider / bgmSlider / masterSlider)
///   3. 각 Slider: Min=0, Max=1, WholeNumbers=false
/// </summary>
public class VolumeSettingsUI : MonoBehaviour
{
    [Header("슬라이더")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider bgmSlider;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void OnEnable()
    {
        // 패널이 열릴 때마다 현재 AudioManager 값으로 슬라이더 초기화
        RefreshSliders();

        // 슬라이더 이벤트 등록 (중복 방지를 위해 먼저 제거 후 추가)
        if (masterSlider != null)
        {
            masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
            masterSlider.onValueChanged.AddListener(OnMasterChanged);
        }
        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(OnSFXChanged);
            sfxSlider.onValueChanged.AddListener(OnSFXChanged);
        }
        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveListener(OnBGMChanged);
            bgmSlider.onValueChanged.AddListener(OnBGMChanged);
        }
    }

    private void OnDisable()
    {
        masterSlider?.onValueChanged.RemoveListener(OnMasterChanged);
        sfxSlider?.onValueChanged.RemoveListener(OnSFXChanged);
        bgmSlider?.onValueChanged.RemoveListener(OnBGMChanged);
    }

    // ── 슬라이더 → AudioManager ───────────────────────────────────────────

    private void OnMasterChanged(float value)
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.MasterVolume = value;
        AudioManager.Instance.PlayUISFX(SoundType.UIButtonClick);
    }

    private void OnSFXChanged(float value)
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.SFXVolume = value;
        // SFX 슬라이더를 움직일 때 미리 듣기 (UI 채널로 재생)
        AudioManager.Instance.PlayUISFX(SoundType.UIButtonClick);
    }

    private void OnBGMChanged(float value)
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.BGMVolume = value;
    }

    // ── AudioManager → 슬라이더 ───────────────────────────────────────────

    private void RefreshSliders()
    {
        if (AudioManager.Instance == null) return;

        // SetValueWithoutNotify: 슬라이더 값 세팅 시 onValueChanged 이벤트 미발생
        masterSlider?.SetValueWithoutNotify(AudioManager.Instance.MasterVolume);
        sfxSlider?.SetValueWithoutNotify(AudioManager.Instance.SFXVolume);
        bgmSlider?.SetValueWithoutNotify(AudioManager.Instance.BGMVolume);
    }
}
