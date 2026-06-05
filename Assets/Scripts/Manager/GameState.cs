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

    // ── 지도 (탐험한 방) ──────────────────────────────────────────────────
    /// <summary>현재 플레이어가 위치한 방(씬) ID. MapRoomDefinition이 씬 로드 시 갱신.</summary>
    public string currentRoomID = string.Empty;
    /// <summary>방(씬)별 그리드 칸(열,행). 루트 방을 (0,0)으로 두고 연결로 자동 배치. 키 존재 = 방문함.</summary>
    public Dictionary<string, Vector2Int> roomCells = new();
    /// <summary>방(씬)별 이웃 연결 목록. 출구 통로 그리기에 사용. 런타임 전용(저장 안 함).</summary>
    [System.NonSerialized] public Dictionary<string, List<MapConnection>> roomConnections = new();

    /// <summary>해당 방을 한 번이라도 방문했는지.</summary>
    public bool HasVisitedRoom(string scene) => roomCells.ContainsKey(scene);

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

    // ── 시각 상태 유지 (씬 전환용) ──────────────────────────────────────────
    /// <summary>마지막 스프라이트 방향. true = 왼쪽(flipX). 씬 전환 후 PlayerAnimator가 복원.</summary>
    public bool savedFacingLeft = false;

    // ── 세이브 슬롯 / 통계 ────────────────────────────────────────────────
    /// <summary>현재 활성 저장 슬롯 번호. -1이면 미선택(타이틀에서 슬롯 고르기 전).</summary>
    public int currentSaveSlot = -1;
    /// <summary>누적 플레이 시간 (초). 슬롯 선택 후부터 증가.</summary>
    public float playTime = 0f;
    /// <summary>보유 재화. 추후 상점 시스템에서 사용.</summary>
    public int coins = 0;

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

    private void Update()
    {
        // 슬롯이 선택된 경우에만 플레이 시간 누적
        if (currentSaveSlot >= 0)
            playTime += Time.deltaTime;
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

    public void Reset()
    {
        dashUnlocked     = false;
        catUnlocked      = false;
        dogUnlocked      = false;
        openedDoors.Clear();
        collectedItems.Clear();
        currentRoomID = string.Empty;
        roomCells.Clear();
        roomConnections.Clear();
        lastCheckpointID    = string.Empty;
        lastCheckpointScene = string.Empty;
        spawnPosition       = Vector2.zero;
        pendingEntryID      = string.Empty;
        hasSpawnOverride    = false;
        savedHP             = -1;
        savedMaxHP          = -1;
        savedForm           = PlayerForm.Human;
        savedFacingLeft     = false;
        currentSaveSlot     = -1;
        playTime            = 0f;
        coins               = 0;
        InventoryManager.Instance?.Clear();
        Debug.Log("[GameState] 초기화");
    }
}
