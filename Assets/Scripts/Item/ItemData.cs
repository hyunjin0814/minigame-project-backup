using UnityEngine;

public abstract class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    public Sprite icon;
}
