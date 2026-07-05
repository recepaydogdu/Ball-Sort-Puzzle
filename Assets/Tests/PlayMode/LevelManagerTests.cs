using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BallSort.Level;
using Object = UnityEngine.Object;

namespace BallSort.Tests.PlayMode
{
    /// <summary>
    /// LevelManager ilerleme kaydı ve seviye geçiş testleri (Play Mode).
    ///
    /// Notlar:
    /// - Her test kendi LevelManager instance'ını oluşturur.
    /// - TearDown'da PlayerPrefs.DeleteAll() ile test verisi temizlenir.
    /// - GameManager olmadan çalışır; LevelManager null-safe referanslarla korunmuştur.
    /// </summary>
    [TestFixture]
    public class LevelManagerTests
    {
        private LevelManager _lm;
        private GameObject   _lmGO;
        private LevelData[]  _testLevels;

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteAll();   // temiz slate

            _testLevels = BuildFiveLevels();

            _lmGO = new GameObject("LevelManager");
            _lm   = _lmGO.AddComponent<LevelManager>();
            SetField(_lm, "_levels", _testLevels);
        }

        [TearDown]
        public void TearDown()
        {
            if (_lm   != null) Object.DestroyImmediate(_lmGO);
            PlayerPrefs.DeleteAll();
        }

        // ─── Kilit / Unlock Durumu ────────────────────────────────────

        [Test]
        [Description("Beklenen: Level 0 (ilk seviye) başlangıçta her zaman açık olmalı.")]
        public void Level0_IsAlwaysUnlocked()
        {
            Assert.IsTrue(_lm.IsUnlocked(0),
                "Hata: Level 0 kilitli görünüyor; her zaman açık olmalı.");
        }

        [Test]
        [Description("Beklenen: Level 1 başlangıçta kilitli olmalı (Level 0 tamamlanmadı).")]
        public void Level1_IsInitiallyLocked()
        {
            Assert.IsFalse(_lm.IsUnlocked(1),
                "Hata: Level 1 başlangıçta açık görünüyor; kilitli olmalı.");
        }

        [Test]
        [Description("Beklenen: geçersiz index için IsUnlocked false döner.")]
        public void IsUnlocked_InvalidIndex_ReturnsFalse()
        {
            Assert.IsFalse(_lm.IsUnlocked(999),
                "Hata: Geçersiz index için IsUnlocked true döndü.");
            Assert.IsFalse(_lm.IsUnlocked(-1),
                "Hata: Negatif index için IsUnlocked true döndü.");
        }

        // ─── LoadLevel ────────────────────────────────────────────────

        [Test]
        [Description("Beklenen: açık seviyeye LoadLevel true döner.")]
        public void LoadLevel_UnlockedLevel_ReturnsTrue()
        {
            // Level 0 her zaman açık
            bool result = _lm.LoadLevel(0);

            Assert.IsTrue(result,
                "Hata: Açık seviyeye LoadLevel false döndü.");
        }

        [Test]
        [Description("Beklenen: kilitli seviyeye LoadLevel false döner.")]
        public void LoadLevel_LockedLevel_ReturnsFalse()
        {
            bool result = _lm.LoadLevel(1); // Level 1 kilitli

            Assert.IsFalse(result,
                "Hata: Kilitli seviyeye LoadLevel true döndü; reddedilmeli.");
        }

        [Test]
        [Description("Beklenen: geçersiz index için LoadLevel false döner.")]
        public void LoadLevel_InvalidIndex_ReturnsFalse()
        {
            bool result = _lm.LoadLevel(999);

            Assert.IsFalse(result,
                "Hata: Geçersiz index ile LoadLevel true döndü.");
        }

        // ─── İlerleme Kaydı (PlayerPrefs) ────────────────────────────

        [Test]
        [Description("Beklenen: tamamlanmamış seviyenin GetBestMoves -1 döner.")]
        public void GetBestMoves_UnplayedLevel_ReturnsMinusOne()
        {
            int best = _lm.GetBestMoves(0);

            Assert.AreEqual(-1, best,
                "Hata: Tamamlanmamış seviye için GetBestMoves -1 döndürmeli.");
        }

        [Test]
        [Description("Beklenen: HandleLevelSolved ile en iyi hamle kaydedilmeli.")]
        public void HandleLevelSolved_SavesBestMoves()
        {
            // HandleLevelSolved private olduğundan reflection ile çağırıyoruz
            InvokeLevelSolved(moveCount: 10);

            int best = _lm.GetBestMoves(0);

            Assert.AreEqual(10, best,
                "Hata: Tamamlama sonrası GetBestMoves doğru değeri saklamadı.");
        }

        [Test]
        [Description("Beklenen: daha iyi (düşük) hamle kaydı öncekinin üzerine yazmalı.")]
        public void HandleLevelSolved_BetterScore_UpdatesBestMoves()
        {
            InvokeLevelSolved(moveCount: 15);
            InvokeLevelSolved(moveCount:  8); // daha iyi

            int best = _lm.GetBestMoves(0);

            Assert.AreEqual(8, best,
                "Hata: Daha iyi hamle kaydı eskisinin üzerine yazmalıydı.");
        }

        [Test]
        [Description("Beklenen: daha kötü hamle kaydı en iyi skoru değiştirmemeli.")]
        public void HandleLevelSolved_WorseScore_DoesNotUpdateBestMoves()
        {
            InvokeLevelSolved(moveCount:  8);
            InvokeLevelSolved(moveCount: 20); // daha kötü

            int best = _lm.GetBestMoves(0);

            Assert.AreEqual(8, best,
                "Hata: Daha kötü hamle kaydı en iyiyi bozdu.");
        }

        [Test]
        [Description("Beklenen: level tamamlanınca bir sonraki seviye açılmalı.")]
        public void HandleLevelSolved_UnlocksNextLevel()
        {
            // Önce Level 0'dayız
            Assert.IsFalse(_lm.IsUnlocked(1), "Test kurulumu: Level 1 kilitli olmalı.");

            InvokeLevelSolved(moveCount: 5);

            Assert.IsTrue(_lm.IsUnlocked(1),
                "Hata: Level 0 tamamlandı ama Level 1 hâlâ kilitli.");
        }

        [Test]
        [Description("Beklenen: son seviye tamamlanınca var olmayan index için unlock hata fırlatmamalı.")]
        public void HandleLevelSolved_LastLevel_DoesNotThrow()
        {
            // Son seviyeye git
            SetField(_lm, "_currentIndex", _testLevels.Length - 1);

            Assert.DoesNotThrow(() => InvokeLevelSolved(moveCount: 5),
                "Hata: Son seviye tamamlanınca exception fırlatıldı.");
        }

        // ─── PlayerPrefs Kalıcılığı ───────────────────────────────────

        [Test]
        [Description("Beklenen: ResetAllProgress PlayerPrefs'teki tüm ilerleme verisini silmeli.")]
        public void ResetAllProgress_ClearsAllProgress()
        {
            InvokeLevelSolved(moveCount: 7); // Level 0 tamamla → Level 1 açıldı

            _lm.ResetAllProgress();

            Assert.IsFalse(_lm.IsUnlocked(1),
                "Hata: ResetAllProgress sonrası Level 1 hâlâ açık görünüyor.");
            Assert.AreEqual(-1, _lm.GetBestMoves(0),
                "Hata: ResetAllProgress sonrası en iyi hamle kaydı sıfırlanmadı.");
        }

        [Test]
        [Description("Beklenen: PlayerPrefs üzerinde kayıt; nesne yeniden oluşturulsa veri korunmalı.")]
        public void PlayerPrefs_DataPersistsAcrossInstances()
        {
            // İlerlemeyi kaydet
            InvokeLevelSolved(moveCount: 12);
            PlayerPrefs.Save();

            // Eski instance'ı yok et, yeni oluştur
            Object.DestroyImmediate(_lmGO);

            var newGO = new GameObject("LevelManager2");
            var newLM = newGO.AddComponent<LevelManager>();
            SetField(newLM, "_levels", _testLevels);

            try
            {
                // Veri PlayerPrefs'ten okunmalı
                Assert.IsTrue(newLM.IsUnlocked(1),
                    "Hata: Yeni LevelManager instance'ı PlayerPrefs'ten Level 1'in açık olduğunu göremedi.");
                Assert.AreEqual(12, newLM.GetBestMoves(0),
                    "Hata: Yeni instance PlayerPrefs'ten en iyi hamle sayısını okuyamadı.");
            }
            finally
            {
                Object.DestroyImmediate(newGO);
            }
        }

        // ─── LevelCount / Katalog ─────────────────────────────────────

        [Test]
        public void LevelCount_ReflectsInjectedLevels()
        {
            Assert.AreEqual(_testLevels.Length, _lm.LevelCount,
                "Hata: LevelCount enjekte edilen seviye sayısıyla uyuşmuyor.");
        }

        // ─── Yardımcılar ─────────────────────────────────────────────

        /// <summary>LevelManager'ın HandleLevelSolved private metodunu reflection ile çağırır.</summary>
        private void InvokeLevelSolved(int moveCount)
        {
            var method = typeof(LevelManager).GetMethod(
                "HandleLevelSolved",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (method == null)
                throw new Exception("[TestHelper] HandleLevelSolved bulunamadı.");

            method.Invoke(_lm, new object[] { moveCount });
        }

        private static LevelData[] BuildFiveLevels()
        {
            var levels = new LevelData[5];
            for (int i = 0; i < 5; i++)
            {
                levels[i] = LevelData.CreateFromJson(new LevelJsonModel
                {
                    levelNumber    = i + 1,
                    levelName      = $"Test Level {i + 1}",
                    emptyTubeCount = 1,
                    tubes = new[]
                    {
                        new TubeJsonModel { balls = new[] { 1, 2 } }
                    }
                });
            }
            return levels;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null) { field.SetValue(target, value); return; }
                type = type.BaseType;
            }
            throw new Exception($"[TestHelper] '{fieldName}' alanı {target.GetType().Name}'da bulunamadı.");
        }
    }
}
