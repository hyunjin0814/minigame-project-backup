using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class DebugCheats : MonoBehaviour
{
    public static DebugCheats Instance { get; private set; }

    [Header("Keys — 전투")]
    [SerializeField] private Key invincibleKey  = Key.F1;
    [SerializeField] private Key damageDownKey  = Key.F2;
    [SerializeField] private Key damageUpKey    = Key.F3;
    [SerializeField] private Key damageResetKey = Key.F4;

    [Header("Keys — 능력 해금")]
    [SerializeField] private Key dashUnlockKey = Key.F5;
    [SerializeField] private Key catUnlockKey  = Key.F6;
    [SerializeField] private Key dogUnlockKey  = Key.F7;
    [SerializeField] private Key unlockAllKey  = Key.F8;

    [Header("HUD")]
    [SerializeField] private bool showHud = true;
    [SerializeField] private int  hudFontSize = 22;

    // 씬 전환 사이에 유지되는 치트 상태
    private bool _isInvincible;
    private int  _damageOffset;

    // 씬 로컬 참조 (씬마다 재탐색)
    private Health         _playerHealth;
    private AttackHitbox[] _playerHitboxes;
    private int[]          _originalDamages;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()                                  => FindAndApply();
    private void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
    private void OnSceneLoaded(Scene s, LoadSceneMode m)  => FindAndApply();

    private void FindAndApply()
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null) return;

        _playerHealth    = player.GetComponent<Health>();
        _playerHitboxes  = player.GetComponentsInChildren<AttackHitbox>(true);
        _originalDamages = new int[_playerHitboxes.Length];
        for (int i = 0; i < _playerHitboxes.Length; i++)
            _originalDamages[i] = _playerHitboxes[i].Damage;

        if (_playerHealth != null)
            _playerHealth.IsInvincible = _isInvincible;
        for (int i = 0; i < _playerHitboxes.Length; i++)
            _playerHitboxes[i].Damage = Mathf.Max(0, _originalDamages[i] + _damageOffset);
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (WasPressed(kb, invincibleKey) && _playerHealth != null)
        {
            _isInvincible = !_isInvincible;
            _playerHealth.IsInvincible = _isInvincible;
            Debug.Log($"[DebugCheats] 무적 {(_isInvincible ? "ON" : "OFF")}");
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
        if (key <= Key.None || (int)key >= (int)Key.OEM5) return false;
        try { var c = kb[key]; return c != null && c.wasPressedThisFrame; }
        catch (System.ArgumentOutOfRangeException) { return false; }
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
        _damageOffset += delta;
        if (_playerHitboxes == null) return;
        for (int i = 0; i < _playerHitboxes.Length; i++)
            _playerHitboxes[i].Damage = Mathf.Max(0, _originalDamages[i] + _damageOffset);
        int cur = _playerHitboxes.Length > 0 ? _playerHitboxes[0].Damage : 0;
        Debug.Log($"[DebugCheats] 공격력 {delta:+#;-#;0} → {cur}");
    }

    private void ResetDamage()
    {
        _damageOffset = 0;
        if (_playerHitboxes == null) return;
        for (int i = 0; i < _playerHitboxes.Length; i++)
            _playerHitboxes[i].Damage = _originalDamages[i];
        Debug.Log("[DebugCheats] 공격력 원복");
    }

    private void OnGUI()
    {
        if (!showHud) return;
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize  = hudFontSize,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = Color.yellow },
        };
        int lineH  = (int)(hudFontSize * 1.6f);
        int startY = Screen.height - lineH * 3 - 10;
        int width  = Screen.width - 20;
        int dmg    = _playerHitboxes != null && _playerHitboxes.Length > 0 ? _playerHitboxes[0].Damage : 0;
        var gs     = GameState.Instance;
        string dash = gs != null && gs.dashUnlocked ? "O" : "X";
        string cat  = gs != null && gs.catUnlocked  ? "O" : "X";
        string dog  = gs != null && gs.dogUnlocked  ? "O" : "X";

        GUI.Label(new Rect(10, startY,           width, lineH), $"[Debug] 무적: {(_isInvincible ? "ON" : "OFF")} (F1)", style);
        GUI.Label(new Rect(10, startY + lineH,   width, lineH), $"[Debug] 공격력: {dmg} (F2/F3 ±, F4 복원)", style);
        GUI.Label(new Rect(10, startY + lineH*2, width, lineH), $"[Debug] 해금 — 대시:{dash}(F5) 고양이:{cat}(F6) 강아지:{dog}(F7) / 전부(F8)", style);
    }
}
