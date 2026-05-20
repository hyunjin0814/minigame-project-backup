using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SightConeRenderer : MonoBehaviour
{
    [SerializeField]
    private EnemyController controller;

    [SerializeField]
    private EnemySight sight;

    [SerializeField]
    private int resolution = 16;

    private static readonly Color32 ColPatrol = new(180, 180, 180, 60);
    private static readonly Color32 ColAlert = new(255, 220, 0, 80);
    private static readonly Color32 ColSearch = new(255, 165, 0, 70);
    private static readonly Color32 ColChase = new(255, 90, 30, 100);
    private static readonly Color32 ColAttack = new(200, 0, 0, 140);

    private Mesh mesh;
    private Material mat;
    private float cachedRadius = -1f;
    private float cachedAngle = -1f;

    private void Awake()
    {
        mesh = new Mesh { name = "SightCone" };
        GetComponent<MeshFilter>().mesh = mesh;

        mat = new Material(Shader.Find("Sprites/Default"));
        var mr = GetComponent<MeshRenderer>();
        mr.material = mat;
        mr.sortingOrder = -1;
    }

    private void LateUpdate()
    {
        TryRebuildMesh();

        mat.color = controller.CurrentState switch
        {
            EnemyController.EnemyState.Alert => ColAlert,
            EnemyController.EnemyState.Search => ColSearch,
            EnemyController.EnemyState.Chase => ColChase,
            EnemyController.EnemyState.Attack => ColAttack,
            _ => ColPatrol,
        };
    }

    // 상태별로 다른 감지 모양 — 시야각(Cone) 또는 원형(Circle)
    // 값이 바뀔 때만 메시 재생성 (캐싱)
    private void TryRebuildMesh()
    {
        (float r, float a) = GetCurrentGeometry();

        if (Mathf.Approximately(r, cachedRadius) && Mathf.Approximately(a, cachedAngle))
            return;

        cachedRadius = r;
        cachedAngle = a;
        BuildMesh(r, a);
    }

    // 상태별 메시 geometry 결정:
    // - Chase/Attack: 원형 (360°) + 각 상태의 전용 반경
    // - 그 외 (Patrol/Alert/Search): 시야각 (DetectionAngle°) + DetectionRadius
    private (float radius, float angle) GetCurrentGeometry()
    {
        return controller.CurrentState switch
        {
            EnemyController.EnemyState.Chase => (controller.ChaseCircleRadius, 360f),
            // Attack은 3c에서 AttackCircleRadius 추가되면 함께 처리
            _ => (sight.DetectionRadius, sight.DetectionAngle),
        };
    }

    // 항상 +X 방향(오른쪽)으로 메시 생성.
    // 부모(적)가 localScale.x = -1로 뒤집힐 때 이 자식도 같이 뒤집혀서 좌우 반전 자동 처리됨.
    private void BuildMesh(float radius, float angle)
    {
        float half = angle * 0.5f * Mathf.Deg2Rad;
        var verts = new Vector3[resolution + 2];
        var tris = new int[resolution * 3];

        verts[0] = Vector3.zero;

        for (int i = 0; i <= resolution; i++)
        {
            float t = (float)i / resolution;
            float rad = Mathf.Lerp(-half, half, t);
            verts[i + 1] = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
        }

        for (int i = 0; i < resolution; i++)
        {
            tris[i * 3] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = i + 2;
        }

        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
    }
}
