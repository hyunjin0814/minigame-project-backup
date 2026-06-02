using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 정적 클래스. 슬롯 3개의 JSON 세이브 파일을 읽고 쓴다.
///
/// [저장 경로]
///   Application.persistentDataPath/save_slot_0.json
///   Application.persistentDataPath/save_slot_1.json
///   Application.persistentDataPath/save_slot_2.json
///
/// [사용 흐름]
///   타이틀 → PeekSlot(slot)으로 슬롯 정보 표시
///   슬롯 선택 → Load(slot)으로 GameState 복원 → 씬 전환
///   체크포인트 도달 → Save(currentSaveSlot)으로 자동 저장
/// </summary>
public static class SaveManager
{
    private const string FilePrefix    = "save_slot_";
    private const string FileExtension = ".json";

    // ── 경로 ───────────────────────────────────────────────────────────────

    private static string GetPath(int slot) =>
        Path.Combine(Application.persistentDataPath, $"{FilePrefix}{slot}{FileExtension}");

    // ── 슬롯 상태 확인 ─────────────────────────────────────────────────────

    public static bool HasSave(int slot) => File.Exists(GetPath(slot));

    /// <summary>
    /// 슬롯 파일을 읽어 SaveData를 반환한다.
    /// GameState에는 적용하지 않는다 — 타이틀 슬롯 UI 표시 전용.
    /// </summary>
    public static SaveData PeekSlot(int slot)
    {
        string path = GetPath(slot);
        if (!File.Exists(path)) return null;
        try
        {
            return JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] 슬롯 {slot} 읽기 실패: {e.Message}");
            return null;
        }
    }

    // ── 저장 ───────────────────────────────────────────────────────────────

    /// <summary>
    /// 현재 GameState + InventoryManager 상태를 슬롯에 저장한다.
    /// 체크포인트 도달 시 Checkpoint.cs에서 호출.
    /// </summary>
    public static void Save(int slot)
    {
        if (GameState.Instance == null)
        {
            Debug.LogError("[SaveManager] GameState 없음 — 저장 실패");
            return;
        }

        SaveData data = TakeSnapshot();
        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(GetPath(slot), json);
            Debug.Log($"[SaveManager] 슬롯 {slot} 저장 완료 → {GetPath(slot)}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] 슬롯 {slot} 저장 실패: {e.Message}");
        }
    }

    // ── 불러오기 ───────────────────────────────────────────────────────────

    /// <summary>
    /// 슬롯 데이터를 GameState에 적용한다.
    /// 호출 후 SceneTransitionManager.TransitionTo(data.lastCheckpointScene)로 씬 전환.
    /// </summary>
    public static bool Load(int slot)
    {
        SaveData data = PeekSlot(slot);
        if (data == null)
        {
            Debug.LogError($"[SaveManager] 슬롯 {slot} 불러오기 실패: 파일 없음");
            return false;
        }

        ApplyToGameState(data, slot);
        Debug.Log($"[SaveManager] 슬롯 {slot} 불러오기 완료");
        return true;
    }

    // ── 삭제 ───────────────────────────────────────────────────────────────

    public static void Delete(int slot)
    {
        string path = GetPath(slot);
        if (!File.Exists(path)) return;
        try
        {
            File.Delete(path);
            Debug.Log($"[SaveManager] 슬롯 {slot} 삭제 완료");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] 슬롯 {slot} 삭제 실패: {e.Message}");
        }
    }

    // ── 내부: GameState → SaveData 스냅샷 ─────────────────────────────────

    private static SaveData TakeSnapshot()
    {
        var gs   = GameState.Instance;
        var data = new SaveData
        {
            dashUnlocked = gs.dashUnlocked,
            catUnlocked  = gs.catUnlocked,
            dogUnlocked  = gs.dogUnlocked,

            openedDoors    = new List<string>(gs.openedDoors),
            collectedItems = new List<string>(gs.collectedItems),

            lastCheckpointScene = gs.lastCheckpointScene,
            lastCheckpointID    = gs.lastCheckpointID,
            spawnPositionX      = gs.spawnPosition.x,
            spawnPositionY      = gs.spawnPosition.y,

            savedForm       = gs.savedForm.ToString(),
            savedFacingLeft = gs.savedFacingLeft,

            playTime  = gs.playTime,
            lastSaved = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            coins     = gs.coins,
        };

        // 현재 플레이어 HP 스냅샷 (체크포인트에서 저장 시 풀 HP 상태)
        var player = GameObject.FindWithTag("Player");
        if (player != null && player.TryGetComponent<Health>(out var health))
        {
            data.savedHP    = health.CurrentHp;
            data.savedMaxHP = health.MaxHp;
        }
        else
        {
            data.savedHP    = gs.savedHP;
            data.savedMaxHP = gs.savedMaxHP;
        }

        // 인벤토리 아이템 ID 목록
        if (InventoryManager.Instance != null)
        {
            foreach (var item in InventoryManager.Instance.Items)
                if (!string.IsNullOrEmpty(item.id))
                    data.inventoryItemIds.Add(item.id);
        }

        // 탐험한 방 지도 정보 (그리드 칸) Dictionary → List
        foreach (var kv in gs.roomCells)
            data.roomMap.Add(new RoomMapEntry { scene = kv.Key, cx = kv.Value.x, cy = kv.Value.y });

        return data;
    }

    // ── 내부: SaveData → GameState 적용 ───────────────────────────────────

    private static void ApplyToGameState(SaveData data, int slot)
    {
        var gs = GameState.Instance;
        if (gs == null) return;

        gs.currentSaveSlot = slot;

        gs.dashUnlocked = data.dashUnlocked;
        gs.catUnlocked  = data.catUnlocked;
        gs.dogUnlocked  = data.dogUnlocked;

        gs.openedDoors.Clear();
        foreach (var id in data.openedDoors)    gs.openedDoors.Add(id);

        gs.collectedItems.Clear();
        foreach (var id in data.collectedItems) gs.collectedItems.Add(id);

        gs.roomCells.Clear();
        foreach (var e in data.roomMap)
            gs.roomCells[e.scene] = new Vector2Int(e.cx, e.cy);

        gs.lastCheckpointScene = data.lastCheckpointScene;
        gs.lastCheckpointID    = data.lastCheckpointID;
        gs.spawnPosition       = new Vector2(data.spawnPositionX, data.spawnPositionY);

        gs.savedHP    = data.savedHP;
        gs.savedMaxHP = data.savedMaxHP;

        if (Enum.TryParse<PlayerForm>(data.savedForm, out var form))
            gs.savedForm = form;
        gs.savedFacingLeft = data.savedFacingLeft;

        gs.playTime = data.playTime;
        gs.coins    = data.coins;

        // 체크포인트가 저장된 경우에만 해당 좌표로 스폰 예약
        // 새 게임(checkpointID 없음)은 씬의 기본 스폰 포인트 사용
        if (!string.IsNullOrEmpty(data.lastCheckpointID))
            gs.MarkCheckpointRespawn();

        // 인벤토리 복원
        var database = Resources.Load<ItemDatabase>("ItemDatabase");
        if (database != null)
            InventoryManager.Instance?.RestoreFromIds(data.inventoryItemIds, database);
        else
            Debug.LogWarning("[SaveManager] ItemDatabase 없음 — Assets/Resources/ItemDatabase.asset 확인");
    }
}
