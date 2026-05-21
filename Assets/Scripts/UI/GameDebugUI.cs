using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// [임시] 빌드 테스트용 디버그 UI - 출시 전 제거
public class GameDebugUI : MonoBehaviour
{
    private void Awake()
    {
        var canvas = CreateCanvas();
        CreateButton(canvas.transform, "재시작",   new Vector2(-20, 70), OnRestart);
        CreateButton(canvas.transform, "게임 종료", new Vector2(-20, 20), OnQuit);
    }

    private Canvas CreateCanvas()
    {
        var go = new GameObject("DebugCanvas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private void CreateButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label);
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot     = new Vector2(1f, 0f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(120f, 40f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f, 1f);
        colors.pressedColor     = new Color(0.55f, 0.55f, 0.55f, 1f);
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);

        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;

        var text = textGo.AddComponent<Text>();
        text.text      = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.color     = Color.white;
        text.fontSize  = 15;
        text.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private void OnRestart() =>
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
