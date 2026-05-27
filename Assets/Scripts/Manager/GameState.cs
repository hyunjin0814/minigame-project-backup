using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DontDestroyOnLoad 싱글톤.
/// 씬 전환 사이에서 게임 진행 상태(능력 해금·월드·체크포인트·HP)를 유지한다.
/// </summary>
public class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }

    // ── 능력 해금 ──────────────────────────────────────────────────────────
    public bool dashUnlocked;
    public bool catUnlocked;
    public bool dogUnlocked;

    // ── 월드 상태 ──────────────────────────────────────────────────────────
    /// <summary>열린 잠금 구역 ID 집합 (BossDoor 등)</summary>
    public HashSet<string> openedDoors    = new();
    /// <summary>수집 완료 아이템 ID 집합 (중복 방지용)</summary>
    public HashSet<string> collectedItems = new();

    // ── 체크포인트 ─────────────────────────────────────────────────────────
    public string  lastCheckpointID    = string.Empty;
    public string  lastCheckpointScene = string.Empty;
    /// <summary>체크포인트 복귀용 직접 좌표 (게임 오버 시 사용).</summary>
    public Vector2 spawnPosition;

    /// <summary>
    /// 씬 전환 시 다음 씬에서 찾을 SpawnPoint ID.
    /// 비어 있으면 spawnPosition 직접 좌표를 사용한다(체크포인트 복귀).
    /// </summary>
    public string pendingEntryID = string.Empty;

    /// <summary>씬 로드 후 PlayerSpawner가 위치를 복원해야 하는지 여부.</summary>
    public bool hasSpawnOverride;

    // ── HP 유지 (씬 전환용) ────────────────────────────────────────────────
    /// <summary>-1 이면 미설정 → PlayerSpawner가 풀HP로 복원(체크포인트 복귀).</summary>
    public int savedHP = -1;

    /// <summary>최대 HP 업그레이드 유지용. -1 이면 미설정 → Health.maxHp 기본값 사용.</summary>
    public int savedMaxHP = -1;

    // ── 변신 상태 유지 (씬 전환용) ──────────────────────────────────────────
    /// <summary>마지막 변신 형태. 씬 전환 후 PlayerTransformController가 복원한다.</summary>
    public PlayerForm savedForm = PlayerForm.Human;

    // ── Lifecycle ──────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── 체크포인트 ─────────────────────────────────────────────────────────

    /// <summary>
    /// Checkpoint.cs에서 접촉 시 호출. 데이터만 저장한다.
    /// (플레이어가 같은 씬에 있으므로 즉시 이동은 일으키지 않는다 → hasSpawnOverride 건드리지 않음)
    /// </summary>
    public void SaveCheckpoint(string id, string sceneName, Vector2 pos)
    {
        lastCheckpointID    = id;
        lastCheckpointScene = sceneName;
        spawnPosition       = pos;
        Debug.Log($"[GameState] 체크포인트 저장: id={id}, scene={sceneName}, pos={pos}");
    }

    /// <summary>ZoneTransition이 씬 전환 전 목적지 진입점 ID를 지정.</summary>
    public void SetTransitionEntry(string entryID)
    {
        pendingEntryID   = entryID;
        hasSpawnOverride = true;
    }

    /// <summary>게임 오버 후 체크포인트(직접 좌표)로 복귀하도록 예약.</summary>
    public void MarkCheckpointRespawn()
    {
        pendingEntryID   = string.Empty; // 진입점 ID 대신 spawnPosition 직접 사용
        hasSpawnOverride = true;
    }

    /// <summary>PlayerSpawner가 위치 복원을 마친 뒤 호출. 플래그 리셋.</summary>
    public void ConsumeSpawnOverride()
    {
        hasSpawnOverride = false;
        pendingEntryID   = string.Empty;
    }

    // ── 문 / 아이템 ────────────────────────────────────────────────────────

    public void OpenDoor(string doorID)
    {
        openedDoors.Add(doorID);
        Debug.Log($"[GameState] 문 열림: {doorID}");
    }

    public bool IsDoorOpen(string doorID) => openedDoors.Contains(doorID);

    public void CollectItem(string itemID) => collectedItems.Add(itemID);
    public bool HasCollected(string itemID) => collectedItems.Contains(itemID);

    // ── 능력 해금 ──────────────────────────────────────────────────────────

    public void UnlockDash() { dashUnlocked = true; Debug.Log("[GameState] 대시 해금"); }
    public void UnlockCat()  { catUnlocked  = true; Debug.Log("[GameState] 고양이 해금"); }
    public void UnlockDog()  { dogUnlocked  = true; Debug.Log("[GameState] 강아지 해금"); }
}
