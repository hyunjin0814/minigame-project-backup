using UnityEngine;

/// <summary>
/// SentryEnemy의 시야 콘을 반투명 부채꼴로 렌더링.
/// AI 로직에 영향 없음 — 순수 시각 표현 전용.
/// SentryEnemy 자식 오브젝트에 붙여서 사용.
/// 메쉬는 Start에서 1회 생성, 이후 localScale 반전으로 좌우 전환.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class ViewConeVisualizer : MonoBehaviour
{
    [Header("Mesh")]
    [Tooltip("부채꼴 호를 몇 개 삼각형으로 나눌지. 높을수록 부드럽고 비용 증가.")]
    [SerializeField] private int _meshSegments = 24;

    [Header("Material")]
    [Tooltip("비워두면 Sprites/Default 반투명 노랑으로 자동 생성.")]
    [SerializeField] private Material _material;
    [SerializeField] private Color _coneColor = new Color(1f, 1f, 0f, 0.25f);

    [Header("Sorting")]
    [SerializeField] private string _sortingLayerName = "Default";
    [SerializeField] private int _sortingOrder = -1;

    private SentryEnemy _sentry;
    private MeshRenderer _meshRenderer;

    // ── 라이프사이클 ─────────────────────────────────────────
    private void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
    }

    private void Start()
    {
        _sentry = GetComponentInParent<SentryEnemy>();
        if (_sentry == null)
        {
            Debug.LogWarning("[ViewConeVisualizer] 부모에서 SentryEnemy를 찾을 수 없음. 비활성화.");
            enabled = false;
            return;
        }

        // localScale은 (1,1,1) 고정 — 방향 전환은 SentryEnemy의 부모 localScale이 처리함.
        // SentryEnemy.UpdateFacing()이 transform.localScale.x를 반전시키면
        // 자식인 ViewCone이 자동으로 함께 뒤집힘.
        transform.localScale = Vector3.one;

        SetupMaterial();
        GetComponent<MeshFilter>().mesh = BuildFanMesh(_sentry.ViewAngle, _sentry.ViewDistance);
    }

    // ── 내부 메서드 ──────────────────────────────────────────
    private void SetupMaterial()
    {
        if (_material == null)
        {
            // Inspector에 머티리얼이 없으면 Sprites/Default로 자동 생성
            // URP 프로젝트에서 경고가 뜨면 Inspector에서 직접 머티리얼 할당할 것
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");

            _material = new Material(shader);
        }

        _material.color = _coneColor;
        _meshRenderer.material = _material;
        _meshRenderer.sortingLayerName = _sortingLayerName;
        _meshRenderer.sortingOrder = _sortingOrder;
    }

    /// <summary>
    /// 오른쪽을 향하는 부채꼴 메쉬를 1회 생성.
    /// 중심(0,0) + 호 위의 꼭짓점으로 삼각형 팬 구성.
    /// </summary>
    private Mesh BuildFanMesh(float angleDegrees, float radius)
    {
        Mesh mesh = new Mesh();
        mesh.name = "ViewConeMesh";

        // 꼭짓점: center(1) + 호 위 점(segments+1)
        int vertCount = _meshSegments + 2;
        Vector3[] vertices = new Vector3[vertCount];
        int[] triangles = new int[_meshSegments * 3];

        vertices[0] = Vector3.zero; // 부채꼴 꼭짓점 (적의 위치)

        float halfAngle = angleDegrees * 0.5f * Mathf.Deg2Rad;
        for (int i = 0; i <= _meshSegments; i++)
        {
            float t = (float)i / _meshSegments;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
            vertices[i + 1] = new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f
            );
        }

        for (int i = 0; i < _meshSegments; i++)
        {
            triangles[i * 3]     = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
