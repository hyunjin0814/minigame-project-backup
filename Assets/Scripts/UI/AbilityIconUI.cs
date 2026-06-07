using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 능력 해금 시 아이콘을 표시하는 UI.
/// AbilityType을 Inspector에서 선택해 강아지/고양이 모두 사용 가능.
/// _iconRoot        : 아이콘 전체 GameObject — 해금 전엔 숨김.
/// _cooldownOverlay : Filled/Vertical Image — Dog 전용, Cat은 비워둠.
/// _skillIcon       : 아이콘 이미지 — Cat 전용 스프라이트 교체에 사용.
/// _primarySprite   : Cat — 인간 폼일 때 표시 (고양이 아이콘).
/// _secondarySprite : Cat — 고양이 폼일 때 표시 (인간 아이콘).
/// </summary>
public class AbilityIconUI : MonoBehaviour
{
    public enum AbilityType { Dog, Cat }

    [SerializeField] private AbilityType _abilityType;
    [SerializeField] private GameObject _iconRoot;
    [SerializeField] private Image _cooldownOverlay;  // Dog만 사용, Cat은 비워둠
    [SerializeField] private Image _skillIcon;        // Cat 스프라이트 교체 대상
    [SerializeField] private Sprite _primarySprite;   // Cat: 인간 폼일 때 (고양이 아이콘)
    [SerializeField] private Sprite _secondarySprite; // Cat: 고양이 폼일 때 (인간 아이콘)

    private DogDashAttack _dogDashAttack;
    private PlayerTransformController _playerController;

    private void Start()
    {
        if (_abilityType == AbilityType.Dog)
            _dogDashAttack = FindObjectOfType<DogDashAttack>();
        else if (_abilityType == AbilityType.Cat)
            _playerController = FindObjectOfType<PlayerTransformController>();
    }

    private void Update()
    {
        if (_iconRoot == null || GameState.Instance == null) return;

        bool unlocked = _abilityType == AbilityType.Dog
            ? GameState.Instance.dogUnlocked
            : GameState.Instance.catUnlocked;

        _iconRoot.SetActive(unlocked);

        if (!unlocked) return;

        // Cat: 현재 폼에 따라 스프라이트 교체
        if (_abilityType == AbilityType.Cat
            && _skillIcon != null
            && _playerController != null)
        {
            _skillIcon.sprite = _playerController.CurrentForm == PlayerForm.Cat
                ? _secondarySprite  // 고양이 폼 → 인간 아이콘 (복귀 암시)
                : _primarySprite;   // 인간 폼 → 고양이 아이콘 (변신 암시)
        }

        // Dog: 쿨타임 오버레이 갱신
        if (_abilityType == AbilityType.Dog
            && _cooldownOverlay != null
            && _dogDashAttack != null)
        {
            float fill = _dogDashAttack.TotalCooldown > 0f
                ? _dogDashAttack.CooldownRemaining / _dogDashAttack.TotalCooldown
                : 0f;
            _cooldownOverlay.fillAmount = fill;
        }
    }
}
