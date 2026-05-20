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
    private static readonly Color32 ColChase = new(255, 50, 50, 100);

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
            EnemyController.EnemyState.Chase => ColChase,
            EnemyController.EnemyState.Attack => ColChase,
            _ => ColPatrol,
        };
    }

    // Inspector에서 detectionRadius/Angle을 바꾸지 않는 한 매 프레임 재생성 안 함
    private void TryRebuildMesh()
    {
        float r = sight.DetectionRadius;
        float a = sight.DetectionAngle;

        if (Mathf.Approximately(r, cachedRadius) && Mathf.Approximately(a, cachedAngle))
            return;

        cachedRadius = r;
        cachedAngle = a;
        BuildMesh(r, a);
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
