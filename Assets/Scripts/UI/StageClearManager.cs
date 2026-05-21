using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StageClearManager : MonoBehaviour
{
    public static StageClearManager Instance { get; private set; }

    [SerializeField]
    private PlayerInputHandler playerInput;

    [SerializeField]
    private Image fadeImage;

    [SerializeField]
    private GameObject stageClearText;

    [SerializeField]
    private float fadeDuration = 1f;

    [SerializeField]
    private float displayDuration = 2f;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void TriggerClear() => StartCoroutine(ClearSequence());

    private IEnumerator ClearSequence()
    {
        if (playerInput != null)
            playerInput.enabled = false;

        Color c = fadeImage.color;
        c.a = 0f;
        fadeImage.color = c;
        fadeImage.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Clamp01(elapsed / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }

        if (stageClearText != null)
            stageClearText.SetActive(true);
    }
}
