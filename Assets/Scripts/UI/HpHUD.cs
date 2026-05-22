using UnityEngine;
using TMPro;

public class HpHUD : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private TMP_Text hpText;

    private void Start() => Refresh();

    private void OnEnable()
    {
        if (playerHealth == null) return;
        playerHealth.OnHit += OnHit;
        playerHealth.OnDeath += Refresh;
        playerHealth.OnHeal += OnHeal;
    }

    private void OnDisable()
    {
        if (playerHealth == null) return;
        playerHealth.OnHit -= OnHit;
        playerHealth.OnDeath -= Refresh;
        playerHealth.OnHeal -= OnHeal;
    }

    private void OnHit(int amount, Vector2 source) => Refresh();
    private void OnHeal(int amount) => Refresh();

    private void Refresh()
    {
        if (hpText != null)
            hpText.text = $"HP {playerHealth.CurrentHp} / {playerHealth.MaxHp}";
    }
}
