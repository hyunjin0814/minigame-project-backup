using UnityEngine;

/// <summary>
/// 보스 그로기 상태일 때 머리 위에 강아지 아이콘을 표시하는 임시 UI.
/// "지금 강아지 폼으로 약점 노출 스킬 사용 가능"이라는 힌트.
/// 보스 자식에 배치하고, _iconRoot에 아이콘 오브젝트(SpriteRenderer 등) 연결.
/// </summary>
public class BossGroggyIndicator : MonoBehaviour
{
    [SerializeField] private GameObject _iconRoot;
    [SerializeField] private Vector3 _offset = new Vector3(0f, 2.5f, 0f);

    private BossBase _boss;
    private Transform _bossTransform;

    private void Awake()
    {
        _boss = GetComponentInParent<BossBase>();
        _bossTransform = _boss != null ? _boss.transform : null;

        if (_iconRoot != null)
            _iconRoot.SetActive(false);
    }

    private void OnEnable()
    {
        if (_boss != null)
        {
            _boss.OnGroggyStarted += HandleGroggyStarted;
            _boss.OnGroggyEnded += HandleGroggyEnded;
        }
    }

    private void OnDisable()
    {
        if (_boss != null)
        {
            _boss.OnGroggyStarted -= HandleGroggyStarted;
            _boss.OnGroggyEnded -= HandleGroggyEnded;
        }
    }

    private void HandleGroggyStarted()
    {
        if (_iconRoot != null) _iconRoot.SetActive(true);
    }

    private void HandleGroggyEnded()
    {
        if (_iconRoot != null) _iconRoot.SetActive(false);
    }

    private void LateUpdate()
    {
        if (_iconRoot == null || !_iconRoot.activeSelf || _bossTransform == null) return;
        // 스프라이트 플립과 무관하게 항상 보스 위 고정
        _iconRoot.transform.position = _bossTransform.position + _offset;
        _iconRoot.transform.localScale = Vector3.one;
    }
}
