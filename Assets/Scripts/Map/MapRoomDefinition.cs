using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// 각 게임플레이 씬에 하나씩 배치한다. 씬 로드 시:
///   1) Ground Tilemap 영역(WorldBounds)을 측정한다 — 플레이어 아이콘 정규화용.
///   2) 씬의 ZoneTransition을 스캔해 이웃 연결(방향 + 그리드 칸 이동)을 기록한다.
///   3) 이미 배치된 이웃의 칸 옆에 자신의 정수 칸(열,행)을 배치한다(첫 방은 (0,0)).
///
/// 픽셀 위치·크기는 MapUI가 칸 정보로 계산한다(모든 방 동일 크기·간격).
/// </summary>
public class MapRoomDefinition : MonoBehaviour
{
    /// <summary>현재 씬의 활성 방 정의. MapUI가 플레이어 아이콘 위치 계산에 사용.</summary>
    public static MapRoomDefinition Active { get; private set; }

    [Tooltip("이 방의 지형 Tilemap. 영역(bounds)을 읽어 플레이어 아이콘 정규화에 사용한다.")]
    [SerializeField] private Tilemap groundTilemap;

    /// <summary>지형 Tilemap의 월드 좌표 AABB. 플레이어 위치 정규화에 사용.</summary>
    public Bounds WorldBounds { get; private set; }

    /// <summary>이 방의 ID(= 씬 이름).</summary>
    public string RoomID { get; private set; }

    private void Awake() => Active = this;

    private void Start()
    {
        RoomID = SceneManager.GetActiveScene().name;

        if (groundTilemap == null)
        {
            Debug.LogWarning($"[MapRoomDefinition] Ground Tilemap이 지정되지 않았습니다: {RoomID}");
            return;
        }
        if (GameState.Instance == null) return;

        // ── 1) bounds 측정 (플레이어 아이콘 정규화용) ──────────────────────
        groundTilemap.CompressBounds();
        Bounds lb   = groundTilemap.localBounds;
        Vector3 wMin = groundTilemap.transform.TransformPoint(lb.min);
        Vector3 wMax = groundTilemap.transform.TransformPoint(lb.max);
        Bounds wb = new Bounds();
        wb.SetMinMax(wMin, wMax);
        WorldBounds = wb;

        var gs = GameState.Instance;
        gs.currentRoomID = RoomID;

        // ── 2) 연결 스캔 (방향 + 그리드 칸 이동) ───────────────────────────
        var conns = new List<MapConnection>();
        var transitions = Object.FindObjectsByType<ZoneTransition>(FindObjectsSortMode.None);
        Vector2 half = new Vector2(Mathf.Max(wb.extents.x, 0.01f),
                                   Mathf.Max(wb.extents.y, 0.01f));
        foreach (var zt in transitions)
        {
            if (string.IsNullOrEmpty(zt.TargetScene)) continue;

            // 카디널 방향: 실제 문 위치(정규화) 기반 — stub 그리기에 사용
            Vector2 delta = (Vector2)zt.transform.position - (Vector2)wb.center;
            Vector2 n = new Vector2(delta.x / half.x, delta.y / half.y);
            MapDir dir = MapDirUtil.FromDelta(n);

            // 칸 이동: override 지정 시 그 값(대각 포함), 아니면 카디널
            Vector2Int step = zt.MapDirection != MapDirOverride.Auto
                ? MapDirUtil.StepFromOverride(zt.MapDirection)
                : MapDirUtil.StepFromDir(dir);

            conns.Add(new MapConnection(zt.TargetScene, dir, step));
        }
        gs.roomConnections[RoomID] = conns;

        // ── 3) 그리드 칸 배치 (이미 있으면 유지) ───────────────────────────
        if (!gs.roomCells.ContainsKey(RoomID))
            gs.roomCells[RoomID] = ResolveCell(gs, conns);

        Debug.Log($"[MapRoomDefinition] 방 등록: {RoomID}, 칸={gs.roomCells[RoomID]}, 연결={conns.Count}");
    }

    /// <summary>
    /// 이미 배치된 이웃의 칸을 기준으로 자신의 칸을 정한다.
    /// 이웃 N이 기록한 "나(T)로 가는 칸 이동"을 우선 사용(2층 분기 override 반영).
    /// 없으면 내 연결의 반대 방향을 사용. 배치된 이웃이 없으면 원점.
    /// </summary>
    private Vector2Int ResolveCell(GameState gs, List<MapConnection> conns)
    {
        foreach (var c in conns)
        {
            if (!gs.roomCells.TryGetValue(c.target, out var nCell)) continue;

            // 이웃 N의 "T(나)로 가는" 연결을 찾으면 그 step 사용
            if (gs.roomConnections.TryGetValue(c.target, out var nConns))
            {
                foreach (var nc in nConns)
                    if (nc.target == RoomID)
                        return nCell + nc.step;
            }

            // 폴백: 내 step의 반대 (N = T + c.step → T = N - c.step)
            return nCell - c.step;
        }
        return Vector2Int.zero; // 첫 방(루트)
    }

    private void OnDestroy()
    {
        if (Active == this) Active = null;
    }
}
