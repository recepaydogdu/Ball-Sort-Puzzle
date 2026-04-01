using System;
using System.Collections.Generic;
using UnityEngine;

namespace BallSort.Core
{
    /// <summary>
    /// BallColor → görsel materyal/renk/sprite eşlemesini tutan ScriptableObject.
    /// Oyundaki tüm renk görsel kaynaklarının tek merkezi; magic value yoktur.
    /// </summary>
    [CreateAssetMenu(fileName = "BallColorPalette", menuName = "BallSort/Color Palette")]
    public class BallColorPalette : ScriptableObject
    {
        [Serializable]
        public class ColorEntry
        {
            public BallColor colorId;
            [Tooltip("Inspector'da önizleme ve UI metin rengi için")]
            public Color     unityColor = Color.white;
            public Material  material;
            public Sprite    sprite;
        }

        [SerializeField] private ColorEntry[] _entries;

        private Dictionary<BallColor, ColorEntry> _lookup;

        private void OnEnable() => BuildLookup();

        private void BuildLookup()
        {
            _lookup = new Dictionary<BallColor, ColorEntry>();
            if (_entries == null) return;
            foreach (var entry in _entries)
                _lookup[entry.colorId] = entry;
        }

        private ColorEntry Find(BallColor color)
        {
            if (_lookup == null) BuildLookup();
            return _lookup.TryGetValue(color, out var e) ? e : null;
        }

        /// <summary>Verilen renge ait Material'ı döner. Bulunamazsa null.</summary>
        public Material GetMaterial(BallColor color) => Find(color)?.material;

        /// <summary>Verilen renge ait Unity Color'ı döner. Bulunamazsa magenta.</summary>
        public Color GetColor(BallColor color) => Find(color)?.unityColor ?? Color.magenta;

        /// <summary>Verilen renge ait Sprite'ı döner. Bulunamazsa null.</summary>
        public Sprite GetSprite(BallColor color) => Find(color)?.sprite;
    }
}
