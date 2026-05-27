using UnityEngine;

public enum AbilityType { Dash, Cat, Dog }

[CreateAssetMenu(fileName = "AbilityUnlockItem", menuName = "Items/Ability Unlock Item")]
public class AbilityUnlockItemData : InstantItemData
{
    [Header("해금 능력")]
    public AbilityType abilityType;

    public override void ApplyEffect(GameObject player)
    {
        var gs = GameState.Instance;
        if (gs == null) return;

        // 이미 수집한 경우 중복 적용 방지
        if (!string.IsNullOrEmpty(id) && gs.HasCollected(id))
        {
            Debug.Log($"[AbilityUnlockItem] '{displayName}'은 이미 해금됨.");
            return;
        }

        switch (abilityType)
        {
            case AbilityType.Dash: gs.UnlockDash(); break;
            case AbilityType.Cat:  gs.UnlockCat();  break;
            case AbilityType.Dog:  gs.UnlockDog();  break;
        }

        if (!string.IsNullOrEmpty(id))
            gs.CollectItem(id);
    }
}
