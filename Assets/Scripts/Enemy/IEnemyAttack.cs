using UnityEngine;

public interface IEnemyAttack
{
    bool IsInRange(Transform target);
    void DoAttack(Transform target);
}
