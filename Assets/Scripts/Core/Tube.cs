using System;
using UnityEngine;
using BallSort.Core;

namespace BallSort.Core
{
    /// <summary>
    /// Tek bir tüpün sahne temsilcisi (View + Input katmanı).
    /// Oyun mantığı <see cref="TubeData"/>'da tutulur;
    /// orkestrasyon GameManager'da yürütülür.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Tube : MonoBehaviour
    {
        // ─── Görsel Sabitler ────────────────────────────────────────
        private static readonly Color DefaultTubeColor   = new Color(0.80f, 0.80f, 0.80f);
        private static readonly Color SelectedTubeColor  = Color.white;
        private static readonly Color SolvedTubeColor    = new Color(0.55f, 0.95f, 0.55f);

        // ─── Inspector Alanları ─────────────────────────────────────
        [Tooltip("Alt'tan üste sıralı top SpriteRenderer'ları (index 0 = en alt)")]
        [SerializeField] private SpriteRenderer[] _ballRenderers;

        [Tooltip("Tüpün kendi SpriteRenderer'ı (seçim/çözüm renklendirmesi için)")]
        [SerializeField] private SpriteRenderer   _tubeRenderer;

        // ─── Runtime ────────────────────────────────────────────────
        private TubeData          _data;
        private BallColorPalette  _palette;
        private bool              _isSelected;

        /// <summary>
        /// Kullanıcı bu tüpe tıkladığında ateşlenir.
        /// GameManager bu event'e abone olur.
        /// </summary>
        public event Action<Tube> OnTubeClicked;

        // ─── Başlatma ───────────────────────────────────────────────

        /// <summary>
        /// Tüpü verilen veri ve palet ile başlatır, görseli günceller.
        /// GameManager tarafından seviye kurulumunda çağrılır.
        /// </summary>
        public void Initialize(TubeData data, BallColorPalette palette)
        {
            _data    = data    ?? throw new ArgumentNullException(nameof(data));
            _palette = palette ?? throw new ArgumentNullException(nameof(palette));
            SetSelected(false);
            RefreshVisuals();
        }

        // ─── Sorgular ───────────────────────────────────────────────

        /// <summary>Bu tüpün TubeData referansını döner.</summary>
        public TubeData GetData() => _data;

        /// <summary>Bu tüp çözüldü mü? TubeData.IsSolved()'a delege eder.</summary>
        public bool IsSolved() => _data?.IsSolved() ?? false;

        // ─── Görsel Güncelleme ──────────────────────────────────────

        /// <summary>
        /// _data ile senkronize olarak tüm top görsellerini günceller.
        /// Hamle ve undo sonrasında GameManager tarafından çağrılır.
        /// </summary>
        public void RefreshVisuals()
        {
            if (_data == null || _ballRenderers == null) return;

            for (int i = 0; i < _ballRenderers.Length; i++)
            {
                if (i < _data.Count)
                {
                    var color = _data.GetBallAt(i);
                    _ballRenderers[i].enabled = true;
                    _ballRenderers[i].color   = _palette.GetColor(color);

                    var sprite = _palette.GetSprite(color);
                    if (sprite != null)
                        _ballRenderers[i].sprite = sprite;
                }
                else
                {
                    _ballRenderers[i].enabled = false;
                }
            }

            RefreshTubeColor();
        }

        /// <summary>
        /// Seçim durumunu ayarlar ve tüp renklendirilmesini günceller.
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            RefreshTubeColor();
        }

        private void RefreshTubeColor()
        {
            if (_tubeRenderer == null) return;

            if (IsSolved())      { _tubeRenderer.color = SolvedTubeColor;  return; }
            if (_isSelected)     { _tubeRenderer.color = SelectedTubeColor; return; }
            _tubeRenderer.color = DefaultTubeColor;
        }

        // ─── Input ──────────────────────────────────────────────────

        private void OnMouseDown() => OnTubeClicked?.Invoke(this);
    }
}
