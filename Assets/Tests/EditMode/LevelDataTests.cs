using NUnit.Framework;
using BallSort.Core;
using BallSort.Level;

namespace BallSort.Tests.EditMode
{
    /// <summary>
    /// LevelData doğrulama ve JSON factory testleri.
    /// ScriptableObject.CreateInstance kullandığı için PlayMode gerektirmez;
    /// EditMode'da çalışır.
    /// </summary>
    [TestFixture]
    public class LevelDataTests
    {
        // ─── Validate ────────────────────────────────────────────────

        [Test]
        [Description("Beklenen: geçerli level verisi Validate'i geçmeli → true, error null.")]
        public void Validate_ValidLevel_ReturnsTrue()
        {
            var level = BuildLevel(new[]
            {
                new[] { BallColor.Red,  BallColor.Blue,  BallColor.Red,  BallColor.Blue },
                new[] { BallColor.Blue, BallColor.Red,   BallColor.Blue, BallColor.Red  }
            }, emptyTubes: 2);

            bool isValid = level.Validate(out string error);

            Assert.IsTrue(isValid,
                $"Hata: Geçerli level Validate'i geçemedi. Mesaj: {error}");
            Assert.IsNull(error,
                "Hata: Geçerli level için error mesajı dolu olmamalı.");
        }

        [Test]
        [Description("Beklenen: tubePlacements null → Validate false, hata mesajı dolu.")]
        public void Validate_NullTubePlacements_ReturnsFalse()
        {
            var model = new LevelJsonModel
            {
                levelNumber    = 1,
                emptyTubeCount = 2,
                tubes          = null
            };
            var level = LevelData.CreateFromJson(model);

            bool isValid = level.Validate(out string error);

            Assert.IsFalse(isValid,
                "Hata: Null tubes ile Validate geçti; geçmemeli.");
            Assert.IsNotEmpty(error,
                "Hata: Validate başarısızken hata mesajı boş olamaz.");
        }

        [Test]
        [Description("Beklenen: kapasite aşan tüp (5 top) → Validate false.")]
        public void Validate_TubeExceedsCapacity_ReturnsFalse()
        {
            var model = new LevelJsonModel
            {
                levelNumber    = 1,
                emptyTubeCount = 1,
                tubes = new[]
                {
                    new TubeJsonModel { balls = new[] { 1, 2, 3, 4, 5 } } // 5 > DefaultCapacity(4)
                }
            };
            var level = LevelData.CreateFromJson(model);

            bool isValid = level.Validate(out string error);

            Assert.IsFalse(isValid,
                "Hata: Kapasite aşımı olan tüple Validate geçti.");
            StringAssert.Contains(TubeData.DefaultCapacity.ToString(), error,
                "Hata: Kapasite bilgisi hata mesajında yer almalı.");
        }

        // ─── CreateFromJson — Veri Doğruluğu ────────────────────────

        [Test]
        [Description("Beklenen: JSON'dan üretilen LevelData doğru levelNumber'ı taşımalı.")]
        public void CreateFromJson_SetsLevelNumber()
        {
            var level = CreateFromModel(levelNumber: 42);

            Assert.AreEqual(42, level.LevelNumber,
                "Hata: LevelNumber JSON'dan yanlış aktarıldı.");
        }

        [Test]
        public void CreateFromJson_SetsLevelName()
        {
            var level = CreateFromModel(levelName: "Özel Seviye");

            Assert.AreEqual("Özel Seviye", level.LevelName,
                "Hata: LevelName JSON'dan yanlış aktarıldı.");
        }

        [Test]
        [Description("Beklenen: levelName boşsa oto-üretilen ad level numarasını içermeli.")]
        public void CreateFromJson_EmptyName_AutoGeneratesName()
        {
            var level = CreateFromModel(levelNumber: 7, levelName: "");

            StringAssert.Contains("7", level.LevelName,
                "Hata: Oto-üretilen isim level numarasını içermeli.");
        }

        [Test]
        public void CreateFromJson_CorrectFilledTubeCount()
        {
            var model = BuildModel(filledCount: 3, emptyTubes: 2);
            var level = LevelData.CreateFromJson(model);

            Assert.AreEqual(3, level.TubePlacements.Length,
                "Hata: Dolu tüp sayısı JSON ile uyuşmuyor.");
        }

        [Test]
        [Description("Beklenen: TotalTubeCount = dolu + boş tüp sayısı.")]
        public void CreateFromJson_TotalTubeCountIncludesEmptyTubes()
        {
            var model = BuildModel(filledCount: 4, emptyTubes: 3);
            var level = LevelData.CreateFromJson(model);

            Assert.AreEqual(7, level.TotalTubeCount,
                "Hata: TotalTubeCount = dolu(4) + boş(3) = 7 olmalı.");
        }

        [Test]
        [Description("Beklenen: JSON'daki int değerleri doğru BallColor enum'a dönüşmeli.")]
        public void CreateFromJson_BallColorsConvertedCorrectly()
        {
            var model = new LevelJsonModel
            {
                levelNumber    = 1,
                emptyTubeCount = 1,
                tubes = new[]
                {
                    new TubeJsonModel
                    {
                        balls = new[] { (int)BallColor.Red, (int)BallColor.Blue, (int)BallColor.Green }
                    }
                }
            };
            var level = LevelData.CreateFromJson(model);

            var balls = level.TubePlacements[0].balls;
            Assert.AreEqual(BallColor.Red,   balls[0], "Hata: index 0 Red olmalı.");
            Assert.AreEqual(BallColor.Blue,  balls[1], "Hata: index 1 Blue olmalı.");
            Assert.AreEqual(BallColor.Green, balls[2], "Hata: index 2 Green olmalı.");
        }

        // ─── Helpers ─────────────────────────────────────────────────

        private static LevelData BuildLevel(BallColor[][] contents, int emptyTubes)
        {
            var tubes = new TubeJsonModel[contents.Length];
            for (int i = 0; i < contents.Length; i++)
            {
                tubes[i] = new TubeJsonModel { balls = new int[contents[i].Length] };
                for (int j = 0; j < contents[i].Length; j++)
                    tubes[i].balls[j] = (int)contents[i][j];
            }

            return LevelData.CreateFromJson(new LevelJsonModel
            {
                levelNumber    = 1,
                levelName      = "Test",
                emptyTubeCount = emptyTubes,
                tubes          = tubes
            });
        }

        private static LevelData CreateFromModel(int levelNumber = 1, string levelName = "Test")
        {
            return LevelData.CreateFromJson(new LevelJsonModel
            {
                levelNumber    = levelNumber,
                levelName      = levelName,
                emptyTubeCount = 1,
                tubes = new[] { new TubeJsonModel { balls = new[] { 1, 2 } } }
            });
        }

        private static LevelJsonModel BuildModel(int filledCount, int emptyTubes)
        {
            var tubes = new TubeJsonModel[filledCount];
            for (int i = 0; i < filledCount; i++)
                tubes[i] = new TubeJsonModel { balls = new[] { 1, 2 } };

            return new LevelJsonModel
            {
                levelNumber    = 1,
                levelName      = "Test",
                emptyTubeCount = emptyTubes,
                tubes          = tubes
            };
        }
    }
}
