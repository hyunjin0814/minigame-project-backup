using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField]
    private Transform target; // 플레이어 Transform

    [SerializeField]
    private float smoothTime = 0.15f; // 추적 지연 시간 (낮을수록 칼같이 따라옴)

    [SerializeField]
    private Vector2 offset; // 플레이어 기준 카메라 중심 위치 조정

    private Vector3 currentVelocity = Vector3.zero;

    private void LateUpdate()
    {
        if (target == null)
            return;

        // 목표 위치 계산 (Z축은 카메라 고유의 값 유지)
        Vector3 targetPosition = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            transform.position.z
        );

        // SmoothDamp를 사용해 부드럽게 이동 (Pixel Perfect Camera가 내부적으로 픽셀 스냅 처리함)
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref currentVelocity,
            smoothTime
        );
    }
}
