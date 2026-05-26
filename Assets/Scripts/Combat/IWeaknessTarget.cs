using System;
using UnityEngine;

/// <summary>
/// 강아지 약점 시스템의 마킹 대상 (잡몹/보스 공통).
/// 잡몹: 항상 외부 마킹 가능 (CanBeSensedExternally=true).
/// 보스: 그로기 상태에서만 외부 마킹 가능 (Phase 2에서 구현).
/// </summary>
public interface IWeaknessTarget
{
    /// <summary>현재 약점 노출 상태인지.</summary>
    bool IsWeaknessExposed { get; }

    /// <summary>마커 UI/이펙트 위치 기준이 되는 Transform.</summary>
    Transform Transform { get; }

    /// <summary>약점 노출 상태 변경 이벤트. true=노출 시작, false=종료.</summary>
    event Action<bool> OnWeaknessChanged;

    /// <summary>약점 노출 진입 (또는 타이머 갱신).</summary>
    void ExposeWeakness(float duration);

    /// <summary>약점 노출 즉시 종료.</summary>
    void ClearWeakness();

    /// <summary>
    /// 강아지 외부 스캔으로 마킹 가능한지.
    /// 잡몹: 기본 true. 보스: IsGroggy일 때만 true (Phase 2).
    /// </summary>
    bool CanBeSensedExternally { get; }
}
