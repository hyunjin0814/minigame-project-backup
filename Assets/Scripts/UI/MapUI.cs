using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 지도 UI. 두 가지 모드로 동작한다.
///   • QuickMap(Tab 홀드): 오버레이로 펼침. 게임은 계속 진행, 아이콘 실시간 갱신.
///   • FullMap(M 토글):   Time.timeScale=0으로 정지 후 펼침. ESC 차단(PauseManager 충돌 방지).
///
/// mapPreset이 지정된 경우 미방문 방까지 모두 표시한다.
/// 방은 정수 그리드 칸으로 GameState에 저장되며, 칸 → 픽셀 변환해 그린다.
/// </summary>
public class MapUI : MonoBehaviour
{
    /// <summary>전체 지도(M)가 열려 있는지. PauseManager가 ESC 차단 판단에 사용.</summary>
    public static bool IsFullMapOpen { get; private set; }

    [Header("UI 참조")]
    [Tooltip("켜고 끌 지도 패널 루트. MapUI 컴포넌트 자신이 아닌 자식 오브젝트로 둔다.")]
    [SerializeField] private GameObject panelRoot;
    [Tooltip("방/통로 사각형이 생성될 부모. 앵커·피벗 중앙 권장.")]
    [SerializeField] private RectTransform roomContainer;
    [Tooltip("방 하나를 나타내는 Image 프리팹(흰 사각형). 피벗 중앙.")]
    [SerializeField] private Image roomPrefab;
    [Tooltip("플레이어 현재 위치 아이콘. roomContainer의 자식으로 둔다.")]
    [SerializeField] private RectTransform playerIcon;

    [Header("크기 / 간격 (월드 단위, scale로 픽셀 변환)")]
    [Tooltip("모든 방 동일 크기.")]
    [SerializeField] private Vector2 roomSize = new Vector2(40f, 26f);
    [Tooltip("칸 사이 빈 간격(통로 영역).")]
    [SerializeField] private Vector2 cellGap = new Vector2(14f, 14f);
    [Tooltip("월드 1유닛 = 캔버스 몇 픽셀. 지도 전체 확대/축소.")]
    [SerializeField] private float worldToMapScale = 2f;
    [Tooltip("출구 통로 stub의 길이. cellGap과 비슷하게.")]
    [SerializeField] private float stubLength = 14f;
    [Tooltip("출구 통로 stub의 두께.")]
    [SerializeField] private float stubThickness = 3f;

    [Header("색상")]
    [SerializeField] private Color currentColor        = new Color(0.36f, 0.78f, 0.91f);
    [SerializeField] private Color currentBorderColor  = new Color(0.63f, 0.89f, 0.97f);
    [SerializeField] private Color visitedColor        = new Color(0.18f, 0.29f, 0.37f);
    [SerializeField] private Color visitedBorderColor  = new Color(0.30f, 0.48f, 0.62f);
    [SerializeField] private Color corridorColor       = new Color(0.18f, 0.29f, 0.37f);
    [SerializeField] private Color undiscoveredColor       = new Color(0.08f, 0.13f, 0.17f);
    [SerializeField] private Color undiscoveredBorderColor = new Color(0.13f, 0.21f, 0.28f);
    [Tooltip("방 테두리 두께 (픽셀, worldToMapScale 이전 기준).")]
    [SerializeField] private float borderThickness = 2f;

    [Header("맵 프리셋 (전체 맵 공개용)")]
    [Tooltip("모든 방의 그리드 칸·출구·아이템 정보. 지정 시 미탐색 방도 표시된다.")]
    [SerializeField] private MapPreset mapPreset;

    [Header("아이템 아이콘")]
    [Tooltip("아이템 아이콘으로 쓸 Image 프리팹. Sprite가 비어 있는 흰 이미지 권장.")]
    [SerializeField] private Image itemIconPrefab;
    [Tooltip("아이콘 한 개 크기 (픽셀, worldToMapScale 이전).")]
    [SerializeField] private float itemIconSize = 8f;
    [Tooltip("아이콘 사이 간격 (픽셀, worldToMapScale 이전).")]
    [SerializeField] private float itemIconGap  = 2f;
    [Tooltip("이미 수집한 아이템 아이콘의 투명도.")]
    [SerializeField][Range(0f, 1f)] private float collectedIconAlpha = 0.3f;

    private enum Mode { Closed, Quick, Full }
    private Mode _mode = Mode.Closed;

    private PlayerInputActions _input;
    private readonly List<Image> _pool = new();
    private int _used;
    private readonly List<Image> _iconPool = new();
    private int _iconsUsed;
    private Transform _player;
    private string _lastBuiltRoom;

    private Vector2 Pitch => roomSize + cellGap;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        _input = new PlayerInputActions();
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        _input.Player.Enable();
        _input.Player.QuickMap.started  += OnQuickStarted;
        _input.Player.QuickMap.canceled += OnQuickCanceled;
        _input.Player.FullMap.performed += OnFullPerformed;
    }

    private void OnDisable()
    {
        _input.Player.QuickMap.started  -= OnQuickStarted;
        _input.Player.QuickMap.canceled -= OnQuickCanceled;
        _input.Player.FullMap.performed -= OnFullPerformed;
        _input.Player.Disable();

        if (_mode == Mode.Full) Time.timeScale = 1f;
        _mode = Mode.Closed;
        IsFullMapOpen = false;
    }

    private void Update()
    {
        if (_mode != Mode.Quick) return;
        if (GameState.Instance != null && GameState.Instance.currentRoomID != _lastBuiltRoom)
            RebuildMap();
        UpdatePlayerIcon();
    }

    // ── 입력 콜백 ──────────────────────────────────────────────────────────

    private void OnQuickStarted(InputAction.CallbackContext _)
    {
        if (_mode == Mode.Closed) Open(Mode.Quick);
    }

    private void OnQuickCanceled(InputAction.CallbackContext _)
    {
        if (_mode == Mode.Quick) Close();
    }

    private void OnFullPerformed(InputAction.CallbackContext _)
    {
        if (_mode == Mode.Full)        Close();
        else if (_mode == Mode.Closed) Open(Mode.Full);
    }

    // ── 열기 / 닫기 ────────────────────────────────────────────────────────

    private void Open(Mode mode)
    {
        _mode = mode;
        if (panelRoot != null) panelRoot.SetActive(true);
        RebuildMap();
        UpdatePlayerIcon();

        if (mode == Mode.Full)
        {
            Time.timeScale = 0f;
            IsFullMapOpen  = true;
        }
    }

    private void Close()
    {
        if (_mode == Mode.Full)
        {
            Time.timeScale = 1f;
            IsFullMapOpen  = false;
        }
        _mode = Mode.Closed;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    // ── 렌더링 ─────────────────────────────────────────────────────────────

    /// <summary>현재 방을 패널 중앙에 두고 모든 방·출구 통로를 다시 그린다.</summary>
    private void RebuildMap()
    {
        var gs = GameState.Instance;
        if (gs == null || roomContainer == null || roomPrefab == null) return;

        string current = gs.currentRoomID;
        _lastBuiltRoom = current;

        // curCell: 프리셋 우선, 없으면 gs.roomCells
        Dictionary<string, MapPreset.RoomEntry> presetLookup = mapPreset != null ? mapPreset.BuildLookup() : null;
        Vector2Int curCell;
        if (presetLookup != null && presetLookup.TryGetValue(current, out var curPreset))
            curCell = curPreset.cell;
        else
            curCell = gs.roomCells.TryGetValue(current, out var gc) ? gc : Vector2Int.zero;

        BeginPool();
        BeginIconPool();

        float b = borderThickness * worldToMapScale;

        if (presetLookup != null)
        {
            // ── 프리셋 모드: 미탐색 방 포함 전체 표시 ──────────────────────
            foreach (var entry in mapPreset.rooms)
            {
                bool isCurrent = entry.sceneName == current;
                bool isVisited = gs.roomCells.ContainsKey(entry.sceneName);
                Vector2 center   = CellCenter(entry.cell, curCell);
                Vector2 fillSz   = roomSize * worldToMapScale;
                Vector2 borderSz = fillSz + new Vector2(b * 2f, b * 2f);

                Color fillColor   = isCurrent ? currentColor      : (isVisited ? visitedColor      : undiscoveredColor);
                Color borderColor = isCurrent ? currentBorderColor : (isVisited ? visitedBorderColor : undiscoveredBorderColor);

                Place(GetPooled(), center, borderSz, borderColor);
                Place(GetPooled(), center, fillSz,   fillColor);

                if (entry.items != null && entry.items.Length > 0)
                    DrawItemIcons(entry.items, center, fillSz, gs);
            }

            // 방문한 방의 통로 stub (ZoneTransition 기반 정확한 방향)
            foreach (var kv in gs.roomConnections)
            {
                Vector2Int cell = presetLookup.TryGetValue(kv.Key, out var pe) ? pe.cell
                                : gs.roomCells.TryGetValue(kv.Key, out var gc)  ? gc : Vector2Int.zero;
                Vector2 rc = CellCenter(cell, curCell);

                foreach (var c in kv.Value)
                {
                    Vector2 dir = MapDirUtil.ToVector(c.dir);
                    bool hz     = (c.dir == MapDir.Left || c.dir == MapDir.Right);
                    Vector2 edge = new Vector2(dir.x * roomSize.x * 0.5f, dir.y * roomSize.y * 0.5f) * worldToMapScale;
                    Vector2 sc   = rc + edge + dir * (stubLength * 0.5f * worldToMapScale);
                    Vector2 ss   = (hz ? new Vector2(stubLength, stubThickness) : new Vector2(stubThickness, stubLength)) * worldToMapScale;
                    Place(GetPooled(), sc, ss, corridorColor);
                }
            }

            // 미방문 방의 통로 stub (프리셋 exitDirections 기반)
            foreach (var entry in mapPreset.rooms)
            {
                if (gs.roomCells.ContainsKey(entry.sceneName)) continue;
                if (entry.exitDirections == null || entry.exitDirections.Length == 0) continue;

                Vector2 rc = CellCenter(entry.cell, curCell);
                foreach (var dir in entry.exitDirections)
                {
                    Vector2 dv   = MapDirUtil.ToVector(dir);
                    bool hz      = (dir == MapDir.Left || dir == MapDir.Right);
                    Vector2 edge = new Vector2(dv.x * roomSize.x * 0.5f, dv.y * roomSize.y * 0.5f) * worldToMapScale;
                    Vector2 sc   = rc + edge + dv * (stubLength * 0.5f * worldToMapScale);
                    Vector2 ss   = (hz ? new Vector2(stubLength, stubThickness) : new Vector2(stubThickness, stubLength)) * worldToMapScale;
                    Place(GetPooled(), sc, ss, undiscoveredColor);
                }
            }
        }
        else
        {
            // ── 폴백: 방문한 방만 표시 (기존 동작) ─────────────────────────
            foreach (var kv in gs.roomCells)
            {
                bool isCurrent = kv.Key == current;
                Vector2 center   = CellCenter(kv.Value, curCell);
                Vector2 fillSz   = roomSize * worldToMapScale;
                Vector2 borderSz = fillSz + new Vector2(b * 2f, b * 2f);
                Place(GetPooled(), center, borderSz, isCurrent ? currentBorderColor : visitedBorderColor);
                Place(GetPooled(), center, fillSz,   isCurrent ? currentColor       : visitedColor);
            }

            foreach (var kv in gs.roomConnections)
            {
                if (!gs.roomCells.TryGetValue(kv.Key, out var cell)) continue;
                Vector2 roomCenter = CellCenter(cell, curCell);
                foreach (var c in kv.Value)
                {
                    Vector2 dir = MapDirUtil.ToVector(c.dir);
                    bool horizontal = (c.dir == MapDir.Left || c.dir == MapDir.Right);
                    Vector2 edge = new Vector2(dir.x * roomSize.x * 0.5f, dir.y * roomSize.y * 0.5f) * worldToMapScale;
                    Vector2 stubCenter = roomCenter + edge + dir * (stubLength * 0.5f * worldToMapScale);
                    Vector2 stubSize   = (horizontal
                        ? new Vector2(stubLength, stubThickness)
                        : new Vector2(stubThickness, stubLength)) * worldToMapScale;
                    Place(GetPooled(), stubCenter, stubSize, corridorColor);
                }
            }
        }

        EndPool();
        EndIconPool();

        if (playerIcon != null) playerIcon.SetAsLastSibling();
    }

    /// <summary>그리드 칸 → 현재 방 중심 기준 캔버스 좌표.</summary>
    private Vector2 CellCenter(Vector2Int cell, Vector2Int curCell)
    {
        return new Vector2((cell.x - curCell.x) * Pitch.x,
                           (cell.y - curCell.y) * Pitch.y) * worldToMapScale;
    }

    /// <summary>플레이어 월드 좌표를 현재 방 bounds 안에서 정규화해 아이콘 위치를 잡는다.</summary>
    private void UpdatePlayerIcon()
    {
        if (playerIcon == null) return;

        var def = MapRoomDefinition.Active;
        var gs  = GameState.Instance;
        if (def == null || gs == null || !gs.roomCells.ContainsKey(def.RoomID))
        {
            playerIcon.gameObject.SetActive(false);
            return;
        }

        if (_player == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) _player = p.transform;
        }
        if (_player == null)
        {
            playerIcon.gameObject.SetActive(false);
            return;
        }

        Bounds bounds = def.WorldBounds;
        float nx = Mathf.InverseLerp(bounds.min.x, bounds.max.x, _player.position.x);
        float ny = Mathf.InverseLerp(bounds.min.y, bounds.max.y, _player.position.y);

        Vector2 offset = new Vector2((nx - 0.5f) * roomSize.x, (ny - 0.5f) * roomSize.y) * worldToMapScale;
        playerIcon.gameObject.SetActive(true);
        playerIcon.anchoredPosition = offset;
    }

    // ── 아이템 아이콘 ──────────────────────────────────────────────────────

    private void DrawItemIcons(ItemData[] items, Vector2 roomCenter, Vector2 fillSz, GameState gs)
    {
        if (itemIconPrefab == null) return;

        float iconSz  = itemIconSize * worldToMapScale;
        float iconGap = itemIconGap  * worldToMapScale;

        // 유효한 아이템 수 계산
        int count = 0;
        for (int i = 0; i < items.Length; i++)
            if (items[i] != null && items[i].icon != null) count++;
        if (count == 0) return;

        float totalW = count * iconSz + (count - 1) * iconGap;
        float x0 = roomCenter.x - totalW * 0.5f + iconSz * 0.5f;
        float y  = roomCenter.y - fillSz.y * 0.5f + iconSz * 0.5f + 2f;

        int idx = 0;
        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (item == null || item.icon == null) continue;

            var img = GetIconPooled();
            img.sprite         = item.icon;
            img.preserveAspect = true;
            bool collected     = !string.IsNullOrEmpty(item.id) && gs.HasCollected(item.id);
            img.color          = collected ? new Color(1f, 1f, 1f, collectedIconAlpha) : Color.white;

            var rt = img.rectTransform;
            rt.anchoredPosition = new Vector2(x0 + idx * (iconSz + iconGap), y);
            rt.sizeDelta        = new Vector2(iconSz, iconSz);
            idx++;
        }
    }

    // ── 이미지 풀 (방 타일) ────────────────────────────────────────────────

    private void BeginPool() => _used = 0;

    private Image GetPooled()
    {
        Image img;
        if (_used < _pool.Count) img = _pool[_used];
        else { img = Instantiate(roomPrefab, roomContainer); _pool.Add(img); }
        _used++;
        img.gameObject.SetActive(true);
        return img;
    }

    private void EndPool()
    {
        for (int i = _used; i < _pool.Count; i++)
            _pool[i].gameObject.SetActive(false);
    }

    // ── 이미지 풀 (아이템 아이콘) ─────────────────────────────────────────

    private void BeginIconPool() => _iconsUsed = 0;

    private Image GetIconPooled()
    {
        Image img;
        if (_iconsUsed < _iconPool.Count) img = _iconPool[_iconsUsed];
        else { img = Instantiate(itemIconPrefab, roomContainer); _iconPool.Add(img); }
        _iconsUsed++;
        img.gameObject.SetActive(true);
        return img;
    }

    private void EndIconPool()
    {
        for (int i = _iconsUsed; i < _iconPool.Count; i++)
            _iconPool[i].gameObject.SetActive(false);
    }

    // ── 유틸 ──────────────────────────────────────────────────────────────

    private static void Place(Image img, Vector2 anchoredPos, Vector2 sizePx, Color color)
    {
        var rt = img.rectTransform;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = sizePx;
        img.color           = color;
    }
}
