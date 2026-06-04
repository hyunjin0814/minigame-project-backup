using System.Collections;
using UnityEngine;

/// <summary>
/// DontDestroyOnLoad 싱글톤 오디오 매니저.
///
/// ● SFX  : PlaySFX(SoundType)   — 피치 랜덤화 지원, 8채널 풀
/// ● UI   : PlayUISFX(SoundType) — 게임 일시정지 중에도 재생
/// ● BGM  : PlayBGM(BGMType)     — 크로스페이드, 일시정지 중 유지
/// ● 볼륨 : MasterVolume / SFXVolume / BGMVolume (PlayerPrefs 저장)
///
/// [씬 설정] 첫 씬의 [Managers] 오브젝트에 이 컴포넌트를 추가하고
///           인스펙터에서 SoundLibrary 에셋을 연결한다.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // ── 인스펙터 ───────────────────────────────────────────────────────────

    [Header("라이브러리")]
    [SerializeField] private SoundLibrary soundLibrary;

    [Header("기본 볼륨")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume    = 1f;
    [SerializeField, Range(0f, 1f)] private float bgmVolume    = 0.7f;

    [Header("SFX 풀 크기")]
    [SerializeField] private int sfxPoolSize = 8;

    // ── PlayerPrefs 키 ─────────────────────────────────────────────────────

    private const string KEY_MASTER = "Audio_Master";
    private const string KEY_SFX    = "Audio_SFX";
    private const string KEY_BGM    = "Audio_BGM";

    // ── 런타임 상태 ────────────────────────────────────────────────────────

    private AudioSource   _bgmSource;   // BGM 전용 (ignoreListenerPause = true)
    private AudioSource   _uiSource;    // UI SFX 전용 (ignoreListenerPause = true)
    private AudioSource[] _sfxPool;     // 게임 SFX 풀 (일시정지 중 멈춤)
    private int           _sfxPoolIdx;

    private BGMType  _currentBGM  = BGMType.None;
    private Coroutine _bgmRoutine;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadVolumeSettings();
        BuildAudioSources();
        soundLibrary.Initialize();
    }

    private void BuildAudioSources()
    {
        // BGM — 일시정지(AudioListener.pause) 무시, 항상 재생 유지
        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop               = true;
        _bgmSource.playOnAwake        = false;
        _bgmSource.ignoreListenerPause = true;
        _bgmSource.volume             = bgmVolume * masterVolume;

        // UI SFX — 일시정지 중에도 메뉴 버튼 소리 재생
        _uiSource = gameObject.AddComponent<AudioSource>();
        _uiSource.playOnAwake         = false;
        _uiSource.ignoreListenerPause  = true;

        // Game SFX Pool — 일시정지 시 AudioListener.pause 에 의해 중단됨
        _sfxPool = new AudioSource[sfxPoolSize];
        for (int i = 0; i < sfxPoolSize; i++)
        {
            _sfxPool[i]            = gameObject.AddComponent<AudioSource>();
            _sfxPool[i].playOnAwake = false;
        }
    }

    // ── SFX ───────────────────────────────────────────────────────────────

    /// <summary>게임 SFX 재생. 일시정지 중에는 소리가 나지 않는다.</summary>
    public void PlaySFX(SoundType type)
    {
        var e = soundLibrary.GetSFX(type);
        if (e?.clip == null) return;

        AudioSource src = NextSFXSource();
        src.clip   = e.clip;
        src.volume = e.volume * sfxVolume * masterVolume;
        src.pitch  = Random.Range(e.pitchMin, e.pitchMax);
        src.Play();
    }

    /// <summary>UI / 시스템 SFX 재생. 일시정지 중에도 재생된다.</summary>
    public void PlayUISFX(SoundType type)
    {
        var e = soundLibrary.GetSFX(type);
        if (e?.clip == null) return;

        // PlayOneShot 로 중첩 재생 허용
        _uiSource.volume = e.volume * sfxVolume * masterVolume;
        _uiSource.pitch  = Random.Range(e.pitchMin, e.pitchMax);
        _uiSource.PlayOneShot(e.clip);
    }

    private AudioSource NextSFXSource()
    {
        // 라운드로빈 — 가장 오래 사용된 채널을 재사용
        var src = _sfxPool[_sfxPoolIdx];
        _sfxPoolIdx = (_sfxPoolIdx + 1) % sfxPoolSize;
        return src;
    }

    // ── BGM ───────────────────────────────────────────────────────────────

    /// <summary>
    /// BGM 전환. 같은 타입을 다시 호출하면 무시된다.
    /// fade=true 이면 0.5초 크로스페이드, false 이면 즉시 전환.
    /// </summary>
    public void PlayBGM(BGMType type, bool fade = true)
    {
        if (_currentBGM == type) return;
        _currentBGM = type;

        if (_bgmRoutine != null) StopCoroutine(_bgmRoutine);

        if (type == BGMType.None)
        {
            _bgmRoutine = StartCoroutine(fade ? FadeOutBGM(0.5f) : StopBGMImmediate());
            return;
        }

        var e = soundLibrary.GetBGM(type);
        if (e?.clip == null) return;

        float targetVol = e.volume * bgmVolume * masterVolume;

        if (fade && _bgmSource.isPlaying)
            _bgmRoutine = StartCoroutine(CrossFade(e.clip, targetVol, 0.5f));
        else
        {
            _bgmSource.clip   = e.clip;
            _bgmSource.volume = targetVol;
            _bgmSource.Play();
        }
    }

    /// <summary>BGM 정지. fade=true 이면 0.5초 페이드 아웃.</summary>
    public void StopBGM(bool fade = true) => PlayBGM(BGMType.None, fade);

    private IEnumerator StopBGMImmediate()
    {
        _bgmSource.Stop();
        yield break;
    }

    private IEnumerator FadeOutBGM(float duration)
    {
        float start = _bgmSource.volume;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            _bgmSource.volume = Mathf.Lerp(start, 0f, t / duration);
            yield return null;
        }
        _bgmSource.Stop();
        _bgmSource.volume = 0f;
    }

    private IEnumerator CrossFade(AudioClip newClip, float targetVol, float duration)
    {
        float half     = duration * 0.5f;
        float startVol = _bgmSource.volume;

        // 페이드 아웃
        for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
        {
            _bgmSource.volume = Mathf.Lerp(startVol, 0f, t / half);
            yield return null;
        }

        // 클립 교체 & 재생
        _bgmSource.clip   = newClip;
        _bgmSource.volume = 0f;
        _bgmSource.Play();

        // 페이드 인
        for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
        {
            _bgmSource.volume = Mathf.Lerp(0f, targetVol, t / half);
            yield return null;
        }
        _bgmSource.volume = targetVol;
    }

    // ── 일시정지 연동 ─────────────────────────────────────────────────────

    /// <summary>
    /// PauseManager.Pause() / Resume() 에서 호출.
    /// paused=true → 게임 SFX 정지 (BGM·UI SFX 는 계속 재생).
    /// </summary>
    public void SetGamePaused(bool paused)
    {
        AudioListener.pause = paused;
    }

    // ── 볼륨 프로퍼티 ─────────────────────────────────────────────────────

    public float MasterVolume
    {
        get => masterVolume;
        set
        {
            masterVolume = Mathf.Clamp01(value);
            RefreshBGMVolume();
            PlayerPrefs.SetFloat(KEY_MASTER, masterVolume);
        }
    }

    public float SFXVolume
    {
        get => sfxVolume;
        set
        {
            sfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KEY_SFX, sfxVolume);
        }
    }

    public float BGMVolume
    {
        get => bgmVolume;
        set
        {
            bgmVolume = Mathf.Clamp01(value);
            RefreshBGMVolume();
            PlayerPrefs.SetFloat(KEY_BGM, bgmVolume);
        }
    }

    private void RefreshBGMVolume()
    {
        if (!_bgmSource.isPlaying || _currentBGM == BGMType.None) return;
        var e = soundLibrary.GetBGM(_currentBGM);
        _bgmSource.volume = (e?.volume ?? 1f) * bgmVolume * masterVolume;
    }

    private void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(KEY_MASTER, masterVolume);
        sfxVolume    = PlayerPrefs.GetFloat(KEY_SFX,    sfxVolume);
        bgmVolume    = PlayerPrefs.GetFloat(KEY_BGM,    bgmVolume);
    }
}
