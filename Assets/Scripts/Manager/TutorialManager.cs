using System;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }
    public static event Action<string, string> OnShowSubtitle;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void ShowSubtitle(string keyText, string actionText)
    {
        OnShowSubtitle?.Invoke(keyText, actionText);
    }
}
