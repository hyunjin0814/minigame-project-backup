using UnityEngine;

[CreateAssetMenu(menuName = "Game/Items/Heal Item", fileName = "HealItemData")]
public class HealItemData : InstantItemData
{
    [Header("Effect")]
    public int healAmount = 1;

    public override void ApplyEffect(GameObject player)
    {
        player.GetComponent<Health>()?.Heal(healAmount);
    }
}
