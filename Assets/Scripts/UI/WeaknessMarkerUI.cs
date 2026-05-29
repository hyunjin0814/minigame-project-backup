using UnityEngine;

/// <summary>
/// 강아지 약점 포착 시 대상 위에 마커 아이콘을 표시.
/// IWeaknessTarget을 구현한 모든 대상(잡몹/보스)에 공용으로 동작.
/// 대상 프리팹 하위에 배치하고, _markerRoot에 아이콘 오브젝트를 연결.
/// </summary>
public class WeaknessMarkerUI : MonoBehaviour
{
    [SerializeField] private GameObject _markerRoot;
    [SerializeField] private Vector3 _offset = new Vector3(0f, 1.5f, 0f);

    private IWeaknessTarget _target;
    private Transform _targetTransform;

    private void Awake()
    {
        _target = GetComponentInParent<IWeaknessTarget>();
        _targetTransform = _target != null ? _target.Transform : null;

        if (_markerRoot != null)
            _markerRoot.SetActive(false);
    }

    private void OnEnable()
    {
        if (_target != null)
            _target.OnWeaknessChanged += OnWeaknessChanged;
    }

    private void OnDisable()
    {
        if (_target != null)
            _target.OnWeaknessChanged -= OnWeaknessChanged;
    }

    private void OnWeaknessChanged(bool exposed)
    {
        if (_markerRoot != null)
            _markerRoot.SetActive(exposed);
    }

    private void LateUpdate()
    {
        if (_markerRoot == null || !_markerRoot.activeSelf || _targetTransform == null) return;
        // 스프라이트 플립과 무관하게 항상 대상 위 고정 위치
        _markerRoot.transform.position = _targetTransform.position + _offset;
        _markerRoot.transform.localScale = Vector3.one;
    }
}
