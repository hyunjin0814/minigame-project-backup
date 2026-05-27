using UnityEngine;

/// <summary>
/// 씬 로드 후 GameState에 저장된 spawnPosition으로 플레이어를 이동시키고,
/// savedHP가 있으면 HP를 복원한다.
///
/// Player 루트 GameObject에 추가한다.
/// Health 컴포넌트의 Awake(maxHp 초기화)가 먼저 실행된 뒤
/// Start에서 HP를 덮어쓰기 때문에 순서 문제가 없다.
/// </summary>
[RequireComponent(typeof(Health))]
public class PlayerSpawner : MonoBehaviour
{
    private void Start()
    {
        if (GameState.Instance == null) return;
        if (!GameState.Instance.hasSpawnOverride) return;

        // ── 스폰 위치 복원: 진입점 ID 우선, 없으면 체크포인트 직접 좌표 ──────
        string entryID = GameState.Instance.pendingEntryID;
        if (!string.IsNullOrEmpty(entryID))
        {
            Vector2? p = SpawnPoint.GetPosition(entryID);
            if (p.HasValue)
                transform.position = p.Value;
            else
                Debug.LogWarning($"[PlayerSpawner] 진입점 '{entryID}'을(를) 찾지 못했습니다. 위치 유지.");
        }
        else
        {
            transform.position = GameState.Instance.spawnPosition;
        }
        Debug.Log($"[PlayerSpawner] 스폰 위치: {transform.position}");

        GameState.Instance.ConsumeSpawnOverride();

        // ── 최대 HP / 현재 HP 복원 ─────────────────────────────────────────
        var health = GetComponent<Health>();
        if (health != null)
        {
            // 최대 HP 복원 (업그레이드 유지). 반드시 현재 HP보다 먼저.
            if (GameState.Instance.savedMaxHP > 0)
            {
                health.SetMaxHp(GameState.Instance.savedMaxHP);
                GameState.Instance.savedMaxHP = -1;
            }

            // 현재 HP: savedHP가 있으면 그 값(씬 전환), 없으면 풀HP(체크포인트 복귀)
            if (GameState.Instance.savedHP > 0)
                health.ForceSetHp(Mathf.Clamp(GameState.Instance.savedHP, 1, health.MaxHp));
            else
                health.ForceSetHp(health.MaxHp);

            GameState.Instance.savedHP = -1;
            Debug.Log($"[PlayerSpawner] HP 복원: {health.CurrentHp}/{health.MaxHp}");
        }
    }
}
