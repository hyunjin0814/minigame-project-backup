using UnityEngine;

public abstract class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    public Sprite icon;

    [Header("Tutorial")]
    [TextArea] public string hintText;

    [Header("Sound")]
    public bool      useCustomPickupSound = false;
    public SoundType pickupSound          = SoundType.ItemPickup;
}
