using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 테스트용 디버그 치트. 플레이어 오브젝트에 붙여 사용.
/// F1: 무적 토글 / F2-F3: 공격력 -+ / F4: 공격력 복원
/// F5: 대시 해금 / F6: 고양이 해금 / F7: 강아지 해금 / F8: 전부 해금
/// 새 Input System(Keyboard.current) 기반.
/// </summary>
public class DebugCheats : MonoBehaviour
{
    [Header("Refs (자동 탐색 가능)")]
    [SerializeField] private Health playerHealth;
    [SerializeField] private AttackHitbox[] playerHitboxes;

    [Header("Keys — 전투")]
    [SerializeField] private Key invincibleKey  = Key.F1;
    [SerializeField] private Key damageDownKey  = Key.F2;
    [SerializeField] private Key damageUpKey    = Key.F3;
    [SerializeField] private Key damageResetKey = Key.F4;

    [Header("Keys — 능력 해금 (단방향)")]
    [SerializeField] private Key dashUnlockKey = Key.F5;
    [SerializeField] private Key catUnlockKey  = Key.F6;
    [SerializeField] private Key dogUnlockKey  = Key.F7;
    [SerializeField] private Key unlockAllKey  = Key.F8;

    [Header("HUD")]
    [SerializeField] private bool showHud = true;

    private int[] originalDamages;

    private void Awake()
    {
        if (playerHealth == null) playerHealth = GetComponent<Health>();
        if (playerHitboxes == null || playerHitboxes.Length == 0)
            playerHitboxes = GetComponentsInChildren<AttackHitbox>(true);

        originalDamages = new int[playerHitboxes.Length];
        for (int i = 0; i < playerHitboxes.Length; i++)
            originalDamages[i] = playerHitboxes[i].Damage;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return; // 키보드 미연결/포커스 없음

        if (WasPressed(kb, invincibleKey) && playerHealth != null)
        {
            playerHealth.IsInvincible = !playerHealth.IsInvincible;
            Debug.Log($"[DebugCheats] 무적 {(playerHealth.IsInvincible ? "ON" : "OFF")}");
        }

        if (WasPressed(kb, damageUpKey))    AdjustDamage(+1);
        if (WasPressed(kb, damageDownKey))  AdjustDamage(-1);
        if (WasPressed(kb, damageResetKey)) ResetDamage();

        if (WasPressed(kb, dashUnlockKey))  UnlockDash();
        if (WasPressed(kb, catUnlockKey))   UnlockCat();
        if (WasPressed(kb, dogUnlockKey))   UnlockDog();
        if (WasPressed(kb, unlockAllKey))   UnlockAll();
    }

    private static bool WasPressed(Keyboard kb, Key key)
    {
        // Key enum 범위를 벗어난 값(이전 KeyCode 잔재 등)에서 ArgumentOutOfRangeException 방지
        if (key <= Key.None || (int)key >= (int)Key.OEM5) return false;
        try
        {
            var ctrl = kb[key];
            return ctrl != null && ctrl.wasPressedThisFrame;
        }
        catch (System.ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private void UnlockDash()
    {
        if (GameState.Instance == null) { Debug.LogWarning("[DebugCheats] GameState 없음"); return; }
        GameState.Instance.UnlockDash();
    }

    private void UnlockCat()
    {
        if (GameState.Instance == null) { Debug.LogWarning("[DebugCheats] GameState 없음"); return; }
        GameState.Instance.UnlockCat();
    }

    private void UnlockDog()
    {
        if (GameState.Instance == null) { Debug.LogWarning("[DebugCheats] GameState 없음"); return; }
        GameState.Instance.UnlockDog();
    }

    private void UnlockAll()
    {
        if (GameState.Instance == null) { Debug.LogWarning("[DebugCheats] GameState 없음"); return; }
        GameState.Instance.UnlockDash();
        GameState.Instance.UnlockCat();
        GameState.Instance.UnlockDog();
        Debug.Log("[DebugCheats] 전부 해금");
    }

    private void AdjustDamage(int delta)
    {
        foreach (var hb in playerHitboxes)
            hb.Damage = Mathf.Max(0, hb.Damage + delta);
        int current = playerHitboxes.Length > 0 ? playerHitboxes[0].Damage : 0;
        Debug.Log($"[DebugCheats] 공격력 조정: {delta:+#;-#;0} → 현재 {current}");
    }

    private void ResetDamage()
    {
        for (int i = 0; i < playerHitboxes.Length; i++)
            playerHitboxes[i].Damage = originalDamages[i];
        Debug.Log("[DebugCheats] 공격력 원복");
    }

    [Header("HUD Style")]
    [SerializeField] private int hudFontSize = 22;

    private void OnGUI()
    {
        if (!showHud) return;
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = hudFontSize,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.yellow },
        };
        int lineHeight = hudFontSize + 8;
        int y = 10;

        bool inv = playerHealth != null && playerHealth.IsInvincible;
        int dmg = playerHitboxes.Length > 0 ? playerHitboxes[0].Damage : 0;
        GUI.Label(new Rect(10, y, 800, lineHeight), $"[Debug] 무적: {(inv ? "ON" : "OFF")} (F1)", style);
        y += lineHeight;
        GUI.Label(new Rect(10, y, 800, lineHeight), $"[Debug] 공격력: {dmg} (F2/F3 ±, F4 복원)", style);
        y += lineHeight;

        var gs = GameState.Instance;
        string dash = gs != null && gs.dashUnlocked ? "O" : "X";
        string cat  = gs != null && gs.catUnlocked  ? "O" : "X";
        string dog  = gs != null && gs.dogUnlocked  ? "O" : "X";
        GUI.Label(new Rect(10, y, 800, lineHeight),
            $"[Debug] 해금 — 대시:{dash}(F5) 고양이:{cat}(F6) 강아지:{dog}(F7) / 전부(F8)", style);
    }
}
