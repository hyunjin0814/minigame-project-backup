using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 버튼에 클릭음을 자동으로 붙여주는 컴포넌트.
/// Button 컴포넌트와 같은 오브젝트에 추가하면 OnClick 시 자동으로 소리가 난다.
///
/// [사용법] 버튼 오브젝트에 이 컴포넌트 추가. 끝.
/// 특별한 소리가 필요하면 soundType 을 인스펙터에서 변경.
/// </summary>
[RequireComponent(typeof(Button))]
public class SoundButton : MonoBehaviour
{
    [SerializeField] private SoundType soundType = SoundType.UIButtonClick;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(PlaySound);
    }

    private void PlaySound()
    {
        AudioManager.Instance?.PlayUISFX(soundType);
    }
}
