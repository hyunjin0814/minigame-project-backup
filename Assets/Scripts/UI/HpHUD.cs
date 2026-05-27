using UnityEngine;
using TMPro;

public class HpHUD : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private TMP_Text hpText;

    private void Start() => Refresh(); // 모든 Awake 이후 보장 → CurrentHp 채워진 상태로 읽음

    private void OnEnable()
    {
        if (playerHealth == null) return;
        playerHealth.OnChanged += Refresh; // 복원 이벤트 구독 (순서 무관)
    }

    private void OnDisable()
    {
        if (playerHealth == null) return;
        playerHealth.OnChanged -= Refresh;
    }

    private void Refresh()
    {
        if (hpText != null)
            hpText.text = $"HP {playerHealth.CurrentHp} / {playerHealth.MaxHp}";
    }
}
