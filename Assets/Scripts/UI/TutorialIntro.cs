using System.Collections;
using UnityEngine;

public class TutorialIntro : MonoBehaviour
{
    [SerializeField] private float  delay      = 2f;
    [SerializeField] private string keyText    = "← →";
    [SerializeField] private string actionText = "이동";

    private IEnumerator Start()
    {
        if (GameState.Instance != null && !string.IsNullOrEmpty(GameState.Instance.lastCheckpointID))
            yield break;

        yield return new WaitForSeconds(delay);
        TutorialManager.Instance?.ShowSubtitle(keyText, actionText);
    }
}
