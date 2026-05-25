using UnityEngine;

/// <summary>
/// 강아지 약점 포착 시 적 위에 마커 아이콘을 표시하는 임시 UI.
/// Enemy 프리팹 하위에 배치하고, _markerRoot에 아이콘 오브젝트를 연결.
/// </summary>
public class WeaknessMarkerUI : MonoBehaviour
{
    [SerializeField] private GameObject _markerRoot;
    [SerializeField] private Vector3 _offset = new Vector3(0f, 1.5f, 0f);

    private EnemyBase _enemy;
    private Transform _enemyTransform;

    private void Awake()
    {
        _enemy = GetComponentInParent<EnemyBase>();
        _enemyTransform = _enemy != null ? _enemy.transform : null;

        if (_markerRoot != null)
            _markerRoot.SetActive(false);
    }

    private void OnEnable()
    {
        if (_enemy != null)
            _enemy.OnWeaknessChanged += OnWeaknessChanged;
    }

    private void OnDisable()
    {
        if (_enemy != null)
            _enemy.OnWeaknessChanged -= OnWeaknessChanged;
    }

    private void OnWeaknessChanged(bool exposed)
    {
        if (_markerRoot != null)
            _markerRoot.SetActive(exposed);
    }

    private void LateUpdate()
    {
        if (_markerRoot == null || !_markerRoot.activeSelf || _enemyTransform == null) return;
        // 스프라이트 플립과 무관하게 항상 적 위 고정 위치
        _markerRoot.transform.position = _enemyTransform.position + _offset;
        _markerRoot.transform.localScale = Vector3.one;
    }
}
