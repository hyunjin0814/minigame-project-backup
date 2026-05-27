using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// DontDestroyOnLoad 싱글톤.
/// 씬 간 페이드 아웃 → LoadSceneAsync → 페이드 인 전환을 전담한다.
///
/// [씬 설정]
/// 첫 번째 씬 Hierarchy에 빈 오브젝트(이름 예: "SceneTransitionManager")를 만들고
/// 이 컴포넌트를 붙인다. 하위에 아래 UI Canvas를 구성한다:
///   └ Canvas (Screen Space - Overlay, Sort Order 99)
///       └ FadeImage (Image, 검정, Rect 전체, RaycastTarget OFF)
/// fadeImage 필드에 FadeImage를 연결하면 된다.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.5f;

    public bool IsTransitioning { get; private set; }

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 게임 시작 시 화면이 검정(불투명)에서 페이드 인되도록 준비
        SetAlpha(1f);
        if (fadeImage != null) fadeImage.gameObject.SetActive(true);
    }

    private void Start()
    {
        // 최초 씬 진입 페이드 인
        StartCoroutine(FadeInCoroutine());
    }

    // ── 외부 호출 ──────────────────────────────────────────────────────────

    /// <summary>
    /// 페이드 아웃 → 씬 로드 → 페이드 인.
    /// ZoneTransition, GameOverManager에서 호출한다.
    /// </summary>
    /// <param name="sceneName">로드할 씬 이름 (Build Settings에 등록 필요)</param>
    /// <param name="entryID">대상 씬에서 찾을 SpawnPoint ID. 비우면 GameState의 기존 스폰 설정(체크포인트 복귀 등)을 따른다.</param>
    public void TransitionTo(string sceneName, string entryID = null)
    {
        if (IsTransitioning) return;

        if (!string.IsNullOrEmpty(entryID) && GameState.Instance != null)
            GameState.Instance.SetTransitionEntry(entryID);

        StartCoroutine(TransitionCoroutine(sceneName));
    }

    // ── 코루틴 ────────────────────────────────────────────────────────────

    private IEnumerator TransitionCoroutine(string sceneName)
    {
        IsTransitioning = true;

        // 히트스톱 중 전환 시 timeScale=0이 고착되지 않도록 강제 해제
        HitStopManager.Instance?.ForceResume();

        yield return StartCoroutine(FadeOutCoroutine());
        yield return SceneManager.LoadSceneAsync(sceneName);
        yield return StartCoroutine(FadeInCoroutine());

        IsTransitioning = false;
    }

    public IEnumerator FadeInCoroutine()
    {
        if (fadeImage == null) yield break;
        fadeImage.gameObject.SetActive(true);
        yield return StartCoroutine(Fade(1f, 0f));
        fadeImage.gameObject.SetActive(false);
    }

    public IEnumerator FadeOutCoroutine()
    {
        if (fadeImage == null) yield break;
        fadeImage.gameObject.SetActive(true);
        yield return StartCoroutine(Fade(0f, 1f));
    }

    // ── 내부 유틸 ─────────────────────────────────────────────────────────

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; // 히트스톱(timeScale=0) 중에도 동작
            SetAlpha(Mathf.Lerp(from, to, elapsed / fadeDuration));
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float a)
    {
        if (fadeImage == null) return;
        Color c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
    }
}
