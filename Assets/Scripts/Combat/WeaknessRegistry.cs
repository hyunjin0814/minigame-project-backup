using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 현재 약점 노출 상태인 IWeaknessTarget을 추적하는 정적 레지스트리.
/// 강아지 돌진 공격 발동 조건 체크 등에 활용 (Phase 5).
/// EnemyBase/BossBase가 ExposeWeakness/ClearWeakness 안에서 Notify 호출.
/// </summary>
public static class WeaknessRegistry
{
    private static readonly HashSet<IWeaknessTarget> _exposed = new HashSet<IWeaknessTarget>();

    public static int Count => _exposed.Count;
    public static bool HasAny => _exposed.Count > 0;
    public static IEnumerable<IWeaknessTarget> Exposed => _exposed;

    public static void NotifyExposed(IWeaknessTarget target)
    {
        if (target == null) return;
        _exposed.Add(target);
    }

    public static void NotifyCleared(IWeaknessTarget target)
    {
        if (target == null) return;
        _exposed.Remove(target);
    }

    // 씬 전환/도메인 리로드 시 자동 정리 (정적 상태가 씬 간에 남지 않도록)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnLoad()
    {
        _exposed.Clear();
    }
}
