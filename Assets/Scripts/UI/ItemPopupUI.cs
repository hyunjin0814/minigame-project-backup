using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemPopupUI : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField]
    private Image iconImage;

    [SerializeField]
    private TextMeshProUGUI nameLabel;

    [SerializeField]
    private TextMeshProUGUI hintLabel;

    [SerializeField]
    private float displayDuration = 3.5f;

    [SerializeField]
    private float fadeDuration = 0.3f;

    private readonly Queue<ItemData> _queue = new();
    private Coroutine _current;

    private void OnEnable()
    {
        ItemPickup.OnItemPickedUp += Enqueue;
        Debug.Log("[ItemPopupUI] OnEnable — 이벤트 구독 완료");
    }

    private void OnDisable()
    {
        ItemPickup.OnItemPickedUp -= Enqueue;
        Debug.Log("[ItemPopupUI] OnDisable — 이벤트 구독 해제");
    }

    private void Start()
    {
        if (canvasGroup == null)
            Debug.LogError("[ItemPopupUI] canvasGroup이 null — Inspector에서 연결 필요");
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    private void Enqueue(ItemData item)
    {
        Debug.Log(
            $"[ItemPopupUI] Enqueue 호출 — item: {item?.displayName}, hintText: '{item?.hintText}'"
        );
        if (string.IsNullOrEmpty(item.hintText))
        {
            Debug.LogWarning(
                $"[ItemPopupUI] hintText가 비어 있어 팝업 생략 — item: {item?.displayName}"
            );
            return;
        }
        _queue.Enqueue(item);
        Debug.Log(
            $"[ItemPopupUI] 큐에 추가, 현재 큐 크기: {_queue.Count}, 코루틴 실행 중: {_current != null}"
        );
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
        Debug.Log($"[ItemPopupUI] ShowRoutine 시작 — {item.displayName}");
        iconImage.sprite = item.icon;
        iconImage.enabled = item.icon != null;
        nameLabel.text = item.displayName;
        hintLabel.text = item.hintText;

        yield return Fade(0f, 1f);
        Debug.Log($"[ItemPopupUI] 팝업 표시 중 — {item.displayName}");
        yield return new WaitForSecondsRealtime(displayDuration);
        yield return Fade(1f, 0f);
        Debug.Log($"[ItemPopupUI] 팝업 종료 — {item.displayName}");
    }

    private IEnumerator Fade(float from, float to)
    {
        if (to > 0f)
            canvasGroup.blocksRaycasts = true;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = to;
        if (to == 0f)
            canvasGroup.blocksRaycasts = false;
    }
}
