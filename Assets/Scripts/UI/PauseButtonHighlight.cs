using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 일시정지 버튼에 부착.
/// 마우스 호버 시 좌우 날개 장식을 표시한다.
/// </summary>
public class PauseButtonHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject leftWing;
    [SerializeField] private GameObject rightWing;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (leftWing  != null) leftWing.SetActive(true);
        if (rightWing != null) rightWing.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (leftWing  != null) leftWing.SetActive(false);
        if (rightWing != null) rightWing.SetActive(false);
    }
}
