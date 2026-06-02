using System;
using System.Collections.Generic;

/// <summary>
/// JSON으로 직렬화되는 세이브 데이터 구조.
/// MonoBehaviour 없는 순수 C# 클래스 — JsonUtility로 읽고 쓴다.
/// </summary>
[Serializable]
public class SaveData
{
    // ── 능력 해금 ──────────────────────────────────────────────────────────
    public bool dashUnlocked;
    public bool catUnlocked;
    public bool dogUnlocked;

    // ── 월드 상태 (HashSet → List 직렬화) ─────────────────────────────────
    public List<string> openedDoors    = new();   // 열린 문 + 처치된 보스 문 ID
    public List<string> collectedItems = new();   // 한 번 수집한 아이템 ID

    // ── 지도 (탐험한 방) ──────────────────────────────────────────────────
    public List<RoomMapEntry> roomMap = new();          // 방별 크기 + 자동배치 좌표

    // ── 인벤토리 (현재 보유 중인 아이템 ID) ───────────────────────────────
    public List<string> inventoryItemIds = new();

    // ── 체크포인트 ─────────────────────────────────────────────────────────
    public string lastCheckpointScene = "";
    public string lastCheckpointID    = "";
    public float  spawnPositionX;
    public float  spawnPositionY;

    // ── HP ─────────────────────────────────────────────────────────────────
    public int savedHP    = -1;   // -1 = 미설정 (풀 HP 복원)
    public int savedMaxHP = -1;

    // ── 변신 형태 ──────────────────────────────────────────────────────────
    public string savedForm       = "Human";
    public bool   savedFacingLeft = false;

    // ── 플레이 통계 (슬롯 UI 표시용) ──────────────────────────────────────
    public float  playTime  = 0f;   // 누적 플레이 시간 (초)
    public string lastSaved = "";   // 마지막 저장 시각 문자열 (표시용)

    // ── 재화 (추후 상점 시스템에서 사용) ──────────────────────────────────
    public int coins = 0;
}

/// <summary>탐험한 방 1개의 지도 정보 직렬화용. 그리드 칸(열,행).</summary>
[Serializable]
public struct RoomMapEntry
{
    public string scene;
    public int    cx;
    public int    cy;
}
