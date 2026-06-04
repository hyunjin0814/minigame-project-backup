using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemPopupUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup        canvasGroup;
    [SerializeField] private Image              iconImage;
    [SerializeField] private TextMeshProUGUI    nameLabel;
    [SerializeField] private TextMeshProUGUI    hintLabel;
    [SerializeField] private float              displayDuration = 3.5f;
    [SerializeField] private float              fadeDuration    = 0.3f;

    private readonly Queue<ItemData> _queue = new();
    private Coroutine _current;

    private void OnEnable()  => ItemPickup.OnItemPickedUp += Enqueue;
    private void OnDisable() => ItemPickup.OnItemPickedUp -= Enqueue;

    private void Start() => canvasGroup.alpha = 0f;

    private void Enqueue(ItemData item)
    {
        if (string.IsNullOrEmpty(item.hintText)) return;
        _queue.Enqueue(item);
        if (_current == null)
            _current = StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        while (_queue.Count > 0)
            yield return ShowRoutine(_queue.Dequeue());
        _current = null;
    }

    private IEnumerator ShowRoutine(ItemData item)
    {
        iconImage.sprite  = item.icon;
        iconImage.enabled = item.icon != null;
        nameLabel.text    = item.displayName;
        hintLabel.text    = item.hintText;

        yield return Fade(0f, 1f);
        yield return new WaitForSecondsRealtime(displayDuration);
        yield return Fade(1f, 0f);
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
