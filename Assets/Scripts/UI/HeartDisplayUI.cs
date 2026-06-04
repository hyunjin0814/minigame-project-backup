using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeartDisplayUI : MonoBehaviour
{
    [SerializeField] private GameObject heartIconPrefab;
    [SerializeField] private Sprite fullHeartSprite;

    private static readonly Color EmptyColor = Color.black;
    private static readonly Color FullColor  = Color.white;

    private readonly List<Image> _slots = new();

    public void Refresh(int current, int max)
    {
        while (_slots.Count < max)
        {
            var go = Instantiate(heartIconPrefab, transform);
            var img = go.GetComponent<Image>();
            img.sprite = fullHeartSprite;
            _slots.Add(img);
        }
        while (_slots.Count > max)
        {
            Destroy(_slots[^1].gameObject);
            _slots.RemoveAt(_slots.Count - 1);
        }

        for (int i = 0; i < _slots.Count; i++)
            _slots[i].color = i < current ? FullColor : EmptyColor;
    }
}
