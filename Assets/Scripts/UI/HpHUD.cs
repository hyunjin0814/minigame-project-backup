using UnityEngine;

public class HpHUD : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private HeartDisplayUI heartDisplay;

    private void Start() => Refresh();

    private void OnEnable()
    {
        if (playerHealth == null) return;
        playerHealth.OnChanged += Refresh;
    }

    private void OnDisable()
    {
        if (playerHealth == null) return;
        playerHealth.OnChanged -= Refresh;
    }

    private void Refresh()
    {
        heartDisplay?.Refresh(playerHealth.CurrentHp, playerHealth.MaxHp);
    }
}
