using System;
using System.Collections.Generic;
using System.Linq;

namespace BallSort.Core
{
    /// <summary>
    /// Bir tüpün saf veri ve kural katmanı.
    /// MonoBehaviour bağımlılığı yoktur; birim testlerde doğrudan kullanılabilir.
    /// </summary>
    public class TubeData
    {
        // ─── Sabitler ───────────────────────────────────────────────
        public const int DefaultCapacity = 4;

        // ─── Alanlar ────────────────────────────────────────────────
        private readonly int            _capacity;
        private readonly List<BallColor> _balls;   // index 0 = en alt top

        // ─── Properties ─────────────────────────────────────────────
        public int Capacity => _capacity;
        public int Count    => _balls.Count;

        // ─── Konstruktörler ─────────────────────────────────────────

        /// <summary>Boş tüp oluşturur.</summary>
        public TubeData(int capacity = DefaultCapacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Kapasite > 0 olmalı.");
            _capacity = capacity;
            _balls    = new List<BallColor>(capacity);
        }

        /// <summary>Başlangıç toplarıyla tüp oluşturur (alt→üst sıra).</summary>
        public TubeData(IEnumerable<BallColor> initialBalls, int capacity = DefaultCapacity)
            : this(capacity)
        {
            foreach (var ball in initialBalls)
            {
                if (_balls.Count >= _capacity)
                    throw new InvalidOperationException(
                        $"Başlangıç topları kapasiteyi ({_capacity}) aşıyor.");
                if (ball == BallColor.None)
                    throw new ArgumentException("None rengi tüpe eklenemez.");
                _balls.Add(ball);
            }
        }

        // ─── Sorgu Metodları ────────────────────────────────────────

        /// <summary>Tüp boş mu?</summary>
        public bool IsEmpty() => _balls.Count == 0;

        /// <summary>Tüp dolu mu?</summary>
        public bool IsFull()  => _balls.Count == _capacity;

        /// <summary>
        /// Tüp tamamlandı mı?
        /// Koşul: tam dolu VE tüm toplar aynı renkte.
        /// </summary>
        public bool IsSolved()
        {
            if (!IsFull()) return false;
            var first = _balls[0];
            return _balls.All(b => b == first);
        }

        /// <summary>En üstteki topu kaldırmadan döner. Boşsa None.</summary>
        public BallColor PeekTop() => IsEmpty() ? BallColor.None : _balls[_balls.Count - 1];

        /// <summary>
        /// Bu tüp verilen rengi kabul edebilir mi?
        /// Boşsa her rengi alır; doluysa üstteki renk eşleşmeli.
        /// </summary>
        public bool CanAccept(BallColor color)
        {
            if (color == BallColor.None) return false;
            if (IsFull())                return false;
            if (IsEmpty())               return true;
            return PeekTop() == color;
        }

        /// <summary>
        /// Belirli index'teki topu döner (0 = en alt).
        /// Görsel katman tarafından top sprite'larını güncellemek için kullanılır.
        /// </summary>
        public BallColor GetBallAt(int index) => _balls[index];

        // ─── Mutasyon Metodları ─────────────────────────────────────

        /// <summary>
        /// Üste top ekler. Kural ihlalinde false döner, tüp değişmez.
        /// </summary>
        public bool TryPush(BallColor color)
        {
            if (!CanAccept(color)) return false;
            _balls.Add(color);
            return true;
        }

        /// <summary>
        /// Üstten top çıkarır. Tüp boşsa false döner.
        /// </summary>
        public bool TryPop(out BallColor color)
        {
            if (IsEmpty()) { color = BallColor.None; return false; }
            color = _balls[_balls.Count - 1];
            _balls.RemoveAt(_balls.Count - 1);
            return true;
        }

        // ─── Yardımcı Metodlar ──────────────────────────────────────

        /// <summary>
        /// Derin kopya döner. Undo snapshot'ları için kullanılır;
        /// dönen nesne orijinalden bağımsızdır.
        /// </summary>
        public TubeData Clone()
        {
            var clone = new TubeData(_capacity);
            foreach (var b in _balls) clone._balls.Add(b);
            return clone;
        }

        public override string ToString() =>
            $"Tube[{string.Join(",", _balls)}] ({Count}/{_capacity})";
    }
}
