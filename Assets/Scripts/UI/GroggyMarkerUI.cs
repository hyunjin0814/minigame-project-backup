using UnityEngine;

/// <summary>
/// 보스 그로기 상태 표시 마커. IsGroggy && !IsWeaknessExposed일 때 ON.
/// 약점 노출 중에는 OFF — 그땐 WeaknessMarkerUI가 대신 표시한다 (옵션 A).
/// 대상 프리팹 하위에 배치하고, _markerRoot에 아이콘 오브젝트를 연결.
/// </summary>
public class GroggyMarkerUI : MonoBehaviour
{
    [SerializeField] private GameObject _markerRoot;
    [SerializeField] private Vector3 _offset = new Vector3(0f, 1.5f, 0f);

    private BossBase _boss;
    private Transform _bossTransform;

    private void Awake()
    {
        _boss = GetComponentInParent<BossBase>();
        _bossTransform = _boss != null ? _boss.transform : null;

        if (_markerRoot != null)
            _markerRoot.SetActive(false);
    }

    private void OnEnable()
    {
        if (_boss == null) return;
        _boss.OnGroggyStarted += HandleStateChanged;
        _boss.OnGroggyEnded += HandleStateChanged;
        _boss.OnWeaknessChanged += HandleWeaknessChanged;
    }

    private void OnDisable()
    {
        if (_boss == null) return;
        _boss.OnGroggyStarted -= HandleStateChanged;
        _boss.OnGroggyEnded -= HandleStateChanged;
        _boss.OnWeaknessChanged -= HandleWeaknessChanged;
    }

    private void HandleStateChanged() => Refresh();
    private void HandleWeaknessChanged(bool _) => Refresh();

    private void Refresh()
    {
        if (_markerRoot == null || _boss == null) return;
        bool show = _boss.IsGroggy && !_boss.IsWeaknessExposed;
        _markerRoot.SetActive(show);
    }

    private void LateUpdate()
    {
        if (_markerRoot == null || !_markerRoot.activeSelf || _bossTransform == null) return;
        // 스프라이트 플립과 무관하게 항상 대상 위 고정 위치
        _markerRoot.transform.position = _bossTransform.position + _offset;
        _markerRoot.transform.localScale = Vector3.one;
    }
}
