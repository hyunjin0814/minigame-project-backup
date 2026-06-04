using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HintSubtitleUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup          canvasGroup;
    [SerializeField] private GameObject           keyBackgroundPrefab;
    [SerializeField] private Transform            keysContainer;
    [SerializeField] private TextMeshProUGUI      actionLabel;
    [SerializeField] private float                displayDuration = 3f;
    [SerializeField] private float                fadeDuration    = 0.3f;

    private Coroutine                _current;
    private readonly List<GameObject> _keyBoxes = new();

    private void OnEnable()  => TutorialManager.OnShowSubtitle += Show;
    private void OnDisable() => TutorialManager.OnShowSubtitle -= Show;
    private void Start()     => canvasGroup.alpha = 0f;

    private void Show(string keyText, string actionText)
    {
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(ShowRoutine(keyText, actionText));
    }

    private IEnumerator ShowRoutine(string keyText, string actionText)
    {
        foreach (var box in _keyBoxes) Destroy(box);
        _keyBoxes.Clear();

        foreach (var key in keyText.Split('|'))
        {
            var box = Instantiate(keyBackgroundPrefab, keysContainer);
            box.GetComponentInChildren<TextMeshProUGUI>().text = key.Trim();
            _keyBoxes.Add(box);
        }

        actionLabel.text = actionText;

        yield return Fade(0f, 1f);
        yield return new WaitForSecondsRealtime(displayDuration);
        yield return Fade(1f, 0f);
        _current = null;
    }

    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}
