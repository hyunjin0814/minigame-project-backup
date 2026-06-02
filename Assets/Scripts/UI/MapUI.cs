using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 지도 UI. 두 가지 모드로 동작한다.
///   • QuickMap(Tab 홀드): 오버레이로 펼침. 게임은 계속 진행, 아이콘 실시간 갱신.
///   • FullMap(M 토글):   Time.timeScale=0으로 정지 후 펼침. ESC 차단(PauseManager 충돌 방지).
///
/// 방은 정수 그리드 칸으로 GameState에 저장된다. 여기서 칸 → 픽셀로 변환하며
/// 모든 방을 동일 크기·간격으로 그린다. 항상 현재 방을 패널 중앙에 둔다.
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
    [SerializeField] private Color currentColor  = new Color(0.30f, 0.70f, 0.40f);
    [SerializeField] private Color visitedColor  = new Color(0.18f, 0.37f, 0.22f);
    [SerializeField] private Color corridorColor = new Color(0.14f, 0.29f, 0.17f);

    private enum Mode { Closed, Quick, Full }
    private Mode _mode = Mode.Closed;

    private PlayerInputActions _input;
    private readonly List<Image> _pool = new();
    private int _used;
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

        if (_mode == Mode.Full) Time.timeScale = 1f; // 안전장치
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
        Vector2Int curCell = gs.roomCells.TryGetValue(current, out var cc) ? cc : Vector2Int.zero;
        _lastBuiltRoom = current;

        BeginPool();

        // 방 사각형
        foreach (var kv in gs.roomCells)
        {
            Vector2 center = CellCenter(kv.Value, curCell);
            Place(GetPooled(), center, roomSize * worldToMapScale,
                  kv.Key == current ? currentColor : visitedColor);
        }

        // 출구 통로 stub (방문한 방의 연결마다)
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
                Vector2 stubSize = (horizontal
                    ? new Vector2(stubLength, stubThickness)
                    : new Vector2(stubThickness, stubLength)) * worldToMapScale;

                Place(GetPooled(), stubCenter, stubSize, corridorColor);
            }
        }

        EndPool();

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

        Bounds b = def.WorldBounds;
        float nx = Mathf.InverseLerp(b.min.x, b.max.x, _player.position.x);
        float ny = Mathf.InverseLerp(b.min.y, b.max.y, _player.position.y);

        // 현재 방은 패널 중앙(0,0)에 그려지므로 중심 기준 오프셋만 적용
        Vector2 offset = new Vector2((nx - 0.5f) * roomSize.x, (ny - 0.5f) * roomSize.y) * worldToMapScale;
        playerIcon.gameObject.SetActive(true);
        playerIcon.anchoredPosition = offset;
    }

    // ── 이미지 풀 ──────────────────────────────────────────────────────────

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

    private static void Place(Image img, Vector2 anchoredPos, Vector2 sizePx, Color color)
    {
        var rt = img.rectTransform;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = sizePx;
        img.color           = color;
    }
}
