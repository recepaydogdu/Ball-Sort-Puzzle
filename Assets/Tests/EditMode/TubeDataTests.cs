using NUnit.Framework;
using BallSort.Core;

namespace BallSort.Tests.EditMode
{
    /// <summary>
    /// TubeData saf mantık katmanı için EditMode testleri.
    /// MonoBehaviour gerektirmez; Unity Play modu olmadan çalışır.
    /// </summary>
    [TestFixture]
    public class TubeDataTests
    {
        private TubeData _tube;

        [SetUp]
        public void SetUp() => _tube = new TubeData();

        // ─── Başlangıç Durumu ────────────────────────────────────────

        [Test]
        public void NewTube_IsEmpty()
        {
            Assert.IsTrue(_tube.IsEmpty(),
                "Hata: Yeni oluşturulan tüp boş görünmüyor.");
        }

        [Test]
        public void NewTube_IsNotFull()
        {
            Assert.IsFalse(_tube.IsFull(),
                "Hata: Yeni tüp dolu olarak işaretlenmiş.");
        }

        [Test]
        public void NewTube_CountIsZero()
        {
            Assert.AreEqual(0, _tube.Count,
                "Hata: Yeni tüpün Count değeri 0 olmalı.");
        }

        [Test]
        public void NewTube_PeekTopReturnsNone()
        {
            Assert.AreEqual(BallColor.None, _tube.PeekTop(),
                "Hata: Boş tüpte PeekTop None dönmeli.");
        }

        // ─── TryPush — Başarılı Durumlar ────────────────────────────

        [Test]
        [Description("Beklenen: boş tüpe geçerli renk eklenmeli → true.")]
        public void TryPush_EmptyTube_ReturnsTrue()
        {
            bool result = _tube.TryPush(BallColor.Red);

            Assert.IsTrue(result,
                "Hata: Boş tüpe TryPush false döndü; geçerli renk eklenebilmeli.");
        }

        [Test]
        public void TryPush_EmptyTube_IncreasesCount()
        {
            _tube.TryPush(BallColor.Red);

            Assert.AreEqual(1, _tube.Count,
                "Hata: Push sonrası Count 1 olmalıydı.");
        }

        [Test]
        [Description("Beklenen: üstteki renkle aynı renk eklenebilmeli → true.")]
        public void TryPush_SameColorOnNonEmpty_ReturnsTrue()
        {
            _tube.TryPush(BallColor.Blue);
            bool result = _tube.TryPush(BallColor.Blue);

            Assert.IsTrue(result,
                "Hata: Aynı renkli top üstüne eklenemiyor.");
        }

        // ─── TryPush — Başarısız Durumlar ────────────────────────────

        [Test]
        [Description("Beklenen: dolu tüpe TryPush false dönmeli, tüp değişmemeli.")]
        public void TryPush_FullTube_ReturnsFalse()
        {
            FillWith(BallColor.Red);
            bool result = _tube.TryPush(BallColor.Red);

            Assert.IsFalse(result,
                "Hata: Dolu tüpe top eklendi; eklenmemeli.");
        }

        [Test]
        public void TryPush_FullTube_CountUnchanged()
        {
            FillWith(BallColor.Red);
            _tube.TryPush(BallColor.Red);

            Assert.AreEqual(TubeData.DefaultCapacity, _tube.Count,
                "Hata: Dolu tüpe push sonrası Count artmamalıydı.");
        }

        [Test]
        [Description("Beklenen: üstteki renkten farklı renk reddedilmeli → false.")]
        public void TryPush_DifferentColorOnNonEmpty_ReturnsFalse()
        {
            _tube.TryPush(BallColor.Red);
            bool result = _tube.TryPush(BallColor.Blue);

            Assert.IsFalse(result,
                "Hata: Uyumsuz renk eklendi; eklenmemeli.");
        }

        [Test]
        [Description("Beklenen: BallColor.None hiçbir zaman eklenemez → false.")]
        public void TryPush_NoneColor_ReturnsFalse()
        {
            bool result = _tube.TryPush(BallColor.None);

            Assert.IsFalse(result,
                "Hata: None rengi tüpe eklendi; eklenmemeli.");
        }

        // ─── TryPop ─────────────────────────────────────────────────

        [Test]
        [Description("Beklenen: boş tüpten TryPop false dönmeli, output None olmalı.")]
        public void TryPop_EmptyTube_ReturnsFalse()
        {
            bool result = _tube.TryPop(out _);

            Assert.IsFalse(result,
                "Hata: Boş tüpten pop yapıldı; yapılmamalı.");
        }

        [Test]
        public void TryPop_EmptyTube_OutputIsNone()
        {
            _tube.TryPop(out BallColor color);

            Assert.AreEqual(BallColor.None, color,
                "Hata: Boş tüpten pop output'u None olmalı.");
        }

        [Test]
        [Description("Beklenen: TryPop en üstteki rengi çıkarmalı (LIFO).")]
        public void TryPop_ReturnsTopColor()
        {
            _tube.TryPush(BallColor.Green);
            _tube.TryPush(BallColor.Red);
            _tube.TryPop(out BallColor color);

            Assert.AreEqual(BallColor.Red, color,
                "Hata: Pop son eklenen rengi (üst) döndürmeli.");
        }

        [Test]
        public void TryPop_DecreasesCount()
        {
            _tube.TryPush(BallColor.Red);
            _tube.TryPop(out _);

            Assert.AreEqual(0, _tube.Count,
                "Hata: Pop sonrası Count azalmalı.");
        }

        // ─── PeekTop ────────────────────────────────────────────────

        [Test]
        [Description("Beklenen: PeekTop topu tüpten çıkarmamalı; Count değişmemeli.")]
        public void PeekTop_DoesNotRemoveBall()
        {
            _tube.TryPush(BallColor.Red);
            _tube.PeekTop();

            Assert.AreEqual(1, _tube.Count,
                "Hata: PeekTop Count'u değiştirdi; sadece bakmalı.");
        }

        [Test]
        public void PeekTop_ReturnsCorrectColor()
        {
            _tube.TryPush(BallColor.Yellow);
            _tube.TryPush(BallColor.Purple);

            Assert.AreEqual(BallColor.Purple, _tube.PeekTop(),
                "Hata: PeekTop üstteki rengi dönmeli.");
        }

        // ─── CanAccept ───────────────────────────────────────────────

        [Test]
        public void CanAccept_EmptyTube_AcceptsAnyValidColor()
        {
            Assert.IsTrue(_tube.CanAccept(BallColor.Teal),
                "Hata: Boş tüp her geçerli rengi kabul etmeli.");
        }

        [Test]
        [Description("Beklenen: dolu tüp hiçbir rengi kabul etmemeli → false.")]
        public void CanAccept_FullTube_ReturnsFalse()
        {
            FillWith(BallColor.Red);

            Assert.IsFalse(_tube.CanAccept(BallColor.Red),
                "Hata: Dolu tüp top kabul etti; etmemeli.");
        }

        [Test]
        [Description("Beklenen: tamamlanmış (dolu+tek renk) tüpe hamle yapılamaz → false.")]
        public void CanAccept_SolvedTube_ReturnsFalse()
        {
            FillWith(BallColor.Yellow);

            // IsSolved == true (tam dolu + tek renk) ama CanAccept false olmalı (IsFull engeli)
            Assert.IsTrue(_tube.IsSolved(), "Test kurulumu hatalı: tüp çözülmüş olmalı.");
            Assert.IsFalse(_tube.CanAccept(BallColor.Yellow),
                "Hata: Çözülmüş tüpe top eklendi; IsFull bunu engellemeli.");
        }

        [Test]
        public void CanAccept_NoneColor_ReturnsFalse()
        {
            Assert.IsFalse(_tube.CanAccept(BallColor.None),
                "Hata: None rengi kabul edildi; edilmemeli.");
        }

        // ─── IsSolved ────────────────────────────────────────────────

        [Test]
        [Description("Beklenen: tam dolu + tüm toplar aynı renk → IsSolved true.")]
        public void IsSolved_FullSameColor_ReturnsTrue()
        {
            FillWith(BallColor.Green);

            Assert.IsTrue(_tube.IsSolved(),
                "Hata: Dolu tek-renkli tüp IsSolved=false döndü.");
        }

        [Test]
        [Description("Beklenen: yarım dolu tüp çözülmüş sayılmaz → false.")]
        public void IsSolved_NotFull_ReturnsFalse()
        {
            _tube.TryPush(BallColor.Green);
            _tube.TryPush(BallColor.Green);

            Assert.IsFalse(_tube.IsSolved(),
                "Hata: Yarım dolu tüp çözülmüş sayıldı.");
        }

        [Test]
        [Description("Beklenen: dolu ama karışık renkli tüp çözülmüş sayılmaz → false.")]
        public void IsSolved_FullMixedColors_ReturnsFalse()
        {
            _tube.TryPush(BallColor.Red);
            _tube.TryPush(BallColor.Red);
            _tube.TryPush(BallColor.Blue);   // farklı renk
            _tube.TryPush(BallColor.Red);

            Assert.IsFalse(_tube.IsSolved(),
                "Hata: Karışık renkli dolu tüp çözülmüş sayıldı.");
        }

        [Test]
        public void IsSolved_EmptyTube_ReturnsFalse()
        {
            Assert.IsFalse(_tube.IsSolved(),
                "Hata: Boş tüp çözülmüş sayıldı.");
        }

        // ─── Clone ───────────────────────────────────────────────────

        [Test]
        [Description("Beklenen: Clone aynı içeriği taşımalı.")]
        public void Clone_ContainsSameCountAndTopColor()
        {
            _tube.TryPush(BallColor.Orange);
            _tube.TryPush(BallColor.Pink);
            var clone = _tube.Clone();

            Assert.AreEqual(_tube.Count,    clone.Count,
                "Hata: Clone farklı sayıda top içeriyor.");
            Assert.AreEqual(_tube.PeekTop(), clone.PeekTop(),
                "Hata: Clone'un üst rengi orijinalden farklı.");
        }

        [Test]
        [Description("Beklenen: orijinalde değişiklik clone'u etkilemez → bağımsız kopya.")]
        public void Clone_IsIndependentFromOriginal()
        {
            _tube.TryPush(BallColor.Red);
            var clone = _tube.Clone();
            _tube.TryPop(out _);  // orijinalden çıkar

            Assert.AreEqual(1, clone.Count,
                "Hata: Orijinalden pop yapıldı ama clone etkilendi; bağımsız olmalı.");
        }

        [Test]
        [Description("Beklenen: clone'da değişiklik orijinali etkilemez → bağımsız kopya.")]
        public void Clone_OriginalUnaffectedByCloneChange()
        {
            _tube.TryPush(BallColor.Blue);
            var clone = _tube.Clone();
            clone.TryPop(out _);  // clone'dan çıkar

            Assert.AreEqual(1, _tube.Count,
                "Hata: Clone'dan pop yapıldı ama orijinal etkilendi; bağımsız olmalı.");
        }

        // ─── IsFull / Kapasite ───────────────────────────────────────

        [Test]
        public void IsFull_AfterFillingToCapacity_ReturnsTrue()
        {
            FillWith(BallColor.Teal);

            Assert.IsTrue(_tube.IsFull(),
                "Hata: Kapasitesi dolana kadar doldurulmuş tüp IsFull=false döndü.");
        }

        [Test]
        public void GetBallAt_ReturnsCorrectColorAtIndex()
        {
            _tube.TryPush(BallColor.Red);    // index 0
            _tube.TryPush(BallColor.Blue);   // index 1

            Assert.AreEqual(BallColor.Red,  _tube.GetBallAt(0),
                "Hata: index 0 beklenen rengi döndürmedi.");
            Assert.AreEqual(BallColor.Blue, _tube.GetBallAt(1),
                "Hata: index 1 beklenen rengi döndürmedi.");
        }

        // ─── Helpers ─────────────────────────────────────────────────

        private void FillWith(BallColor color)
        {
            for (int i = 0; i < TubeData.DefaultCapacity; i++)
                _tube.TryPush(color);
        }
    }
}
