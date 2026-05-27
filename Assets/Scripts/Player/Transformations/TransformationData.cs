using UnityEngine;

[CreateAssetMenu(menuName = "Game/Transformation Data", fileName = "TransformationData")]
public class TransformationData : ScriptableObject
{
    [Header("Identity")]
    public string formName = "Human";

    [Header("Collider")]
    public Vector2 colliderSize;
    public Vector2 colliderOffset;

    [Header("Movement")]
    public float maxSpeed = 9f;
    public float accelTime = 0.08f;
    public float decelTime = 0.05f;
    public float airControl = 0.9f;

    [Header("Jump")]
    public float jumpSpeed = 15f;
    public float riseGravity = 3f;
    public float fallGravity = 5f;
    public float jumpCutMultiplier = 0.5f;

    [Header("Visual")]
    public RuntimeAnimatorController animatorController;
}
