using System;

[Serializable]
public class WeaknessDebuff
{
    public float Duration;
    public float DamageMultiplier;
    public float RemainingTime;

    public WeaknessDebuff(float duration, float damageMultiplier)
    {
        Duration = duration;
        DamageMultiplier = damageMultiplier;
        RemainingTime = duration;
    }
}
