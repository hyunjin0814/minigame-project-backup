using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteSheetEffect : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float fps = 12f;
    [SerializeField] private bool destroyOnFinish = true;

    private SpriteRenderer _sr;
    private float _elapsed;
    private int _currentFrame;

    private void Awake() => _sr = GetComponent<SpriteRenderer>();

    private void OnEnable()
    {
        _elapsed = 0f;
        _currentFrame = 0;
        if (frames.Length > 0) _sr.sprite = frames[0];
    }

    private void Update()
    {
        _elapsed += Time.unscaledDeltaTime; // timeScale=0(히트스톱) 중에도 재생
        int target = Mathf.FloorToInt(_elapsed * fps);
        if (target == _currentFrame) return;
        _currentFrame = target;
        if (_currentFrame >= frames.Length)
        {
            if (destroyOnFinish) Destroy(gameObject);
            else gameObject.SetActive(false);
            return;
        }
        _sr.sprite = frames[_currentFrame];
    }
}
