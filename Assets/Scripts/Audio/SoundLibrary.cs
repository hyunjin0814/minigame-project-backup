using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 내 모든 사운드 클립·설정을 보관하는 ScriptableObject.
/// Resources/ 아래에 두지 않아도 됨 — AudioManager 인스펙터에서 직접 참조.
///
/// [생성] Project 창 우클릭 → Create → Audio → Sound Library
/// </summary>
[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Audio/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    // ── 데이터 타입 ────────────────────────────────────────────────────────

    [Serializable]
    public class SFXEntry
    {
        public SoundType type;
        public AudioClip clip;

        [Range(0f, 1f)]   public float volume   = 1f;
        [Range(0.5f, 2f)] public float pitchMin = 1f;
        [Range(0.5f, 2f)] public float pitchMax = 1f;
    }

    [Serializable]
    public class BGMEntry
    {
        public BGMType  type;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.7f;
    }

    // ── 인스펙터 필드 ──────────────────────────────────────────────────────

    [SerializeField] private SFXEntry[] sfxEntries = Array.Empty<SFXEntry>();
    [SerializeField] private BGMEntry[] bgmEntries = Array.Empty<BGMEntry>();

    // ── 런타임 딕셔너리 ───────────────────────────────────────────────────

    private Dictionary<SoundType, SFXEntry> _sfxMap;
    private Dictionary<BGMType,  BGMEntry>  _bgmMap;

    /// <summary>AudioManager.Awake() 에서 한 번 호출해 딕셔너리를 빌드한다.</summary>
    public void Initialize()
    {
        _sfxMap = new Dictionary<SoundType, SFXEntry>(sfxEntries.Length);
        foreach (var e in sfxEntries)
        {
            if (!_sfxMap.ContainsKey(e.type))
                _sfxMap[e.type] = e;
        }

        _bgmMap = new Dictionary<BGMType, BGMEntry>(bgmEntries.Length);
        foreach (var e in bgmEntries)
        {
            if (!_bgmMap.ContainsKey(e.type))
                _bgmMap[e.type] = e;
        }
    }

    public SFXEntry GetSFX(SoundType type)
    {
        if (_sfxMap == null) return null;
        _sfxMap.TryGetValue(type, out var entry);
        return entry;
    }

    public BGMEntry GetBGM(BGMType type)
    {
        if (_bgmMap == null) return null;
        _bgmMap.TryGetValue(type, out var entry);
        return entry;
    }
}
