using UnityEngine;
using TMPro;

/// <summary>
/// 데미지 배율(×2, ×3 등)을 적 위에 잠깐 떠오르며 표시하는 텍스트.
/// EffectSpawner.SpawnMultiplierText가 Instantiate 후 Setup을 호출한다.
/// 프리팹 구성: 빈 오브젝트 → TextMeshPro 컴포넌트 부착 (Sorting Layer: 높게 설정).
/// </summary>
public class FloatingMultiplierText : MonoBehaviour
{
    [SerializeField] private TextMeshPro _tmp;
    [SerializeField] private float _floatSpeed = 2.2f;
    [SerializeField] private float _duration = 0.75f;
    [SerializeField] private float _startScale = 1.6f;
    [SerializeField] private float _endScale = 1.1f;

    private float _elapsed;
    private Color _baseColor;

    public void Setup(string text, Color color)
    {
        if (_tmp == null)
            _tmp = GetComponentInChildren<TextMeshPro>();

        _tmp.text = text;
        _tmp.color = color;
        _baseColor = color;
        transform.localScale = Vector3.one * _startScale;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float t = _elapsed / _duration;

        // 위로 떠오름
        transform.position += Vector3.up * _floatSpeed * Time.deltaTime;

        // 스케일: 크게 시작 → 작게
        transform.localScale = Vector3.one * Mathf.Lerp(_startScale, _endScale, t);

        // 후반부에 빠르게 페이드 아웃 (제곱 커브)
        Color c = _baseColor;
        c.a = 1f - t * t;
        _tmp.color = c;

        if (_elapsed >= _duration)
            Destroy(gameObject);
    }
}
