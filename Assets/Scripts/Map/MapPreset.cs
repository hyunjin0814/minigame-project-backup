using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 전체 방 레이아웃을 사전 정의하는 ScriptableObject.
/// MapUI에 연결하면 미방문 방도 지도에 표시된다.
/// </summary>
[CreateAssetMenu(fileName = "MapPreset", menuName = "Map/MapPreset")]
public class MapPreset : ScriptableObject
{
    [Serializable]
    public class RoomEntry
    {
        [Tooltip("씬 이름 (빌드 세팅의 씬 이름과 정확히 일치).")]
        public string sceneName;
        [Tooltip("지도 그리드 칸 (열, 행). 첫 방을 (0,0)으로 둔다.")]
        public Vector2Int cell;
        [Tooltip("이 방에서 나가는 출구 방향. 미방문 통로 stub 그리기에 사용.")]
        public MapDir[] exitDirections;
        [Tooltip("이 방에서 획득 가능한 아이템 목록 (아이콘 표시용).")]
        public ItemData[] items;
    }

    public List<RoomEntry> rooms = new();

    public bool TryGetEntry(string sceneName, out RoomEntry entry)
    {
        foreach (var r in rooms)
        {
            if (r.sceneName == sceneName) { entry = r; return true; }
        }
        entry = null;
        return false;
    }

    public Dictionary<string, RoomEntry> BuildLookup()
    {
        var dict = new Dictionary<string, RoomEntry>(rooms.Count);
        foreach (var r in rooms)
            if (!string.IsNullOrEmpty(r.sceneName))
                dict[r.sceneName] = r;
        return dict;
    }
}
