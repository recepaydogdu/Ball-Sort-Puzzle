using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.TestTools;
using BallSort;
using BallSort.Core;
using BallSort.Level;

namespace BallSort.Tests.PlayMode
{
    /// <summary>
    /// GameManager entegrasyon testleri (Play Mode).
    /// Gerçek MonoBehaviour/GameObject üzerinde çalışır.
    ///
    /// Singleton temizliği: her test kendi GM'ini oluşturur,
    /// TearDown'da DestroyImmediate ile kaldırır.
    /// Unity'nin == override'ı sayesinde yıkılmış nesne null
    /// gibi davranır; bir sonraki test yeni Singleton'ı set eder.
    /// </summary>
    [TestFixture]
    public class GameManagerTests
    {
        // ─── Test Armatürü ───────────────────────────────────────────
        private GameManager _gm;
        private GameObject  _tubePrefabGO;
        private GameObject  _containerGO;
        private BallColorPalette _palette;

        [SetUp]
        public void SetUp()
        {
            _palette      = ScriptableObject.CreateInstance<BallColorPalette>();
            _containerGO  = new GameObject("TubeContainer");
            _tubePrefabGO = BuildTubePrefab();
            _gm           = BuildGameManager();
        }

        [TearDown]
        public void TearDown()
        {
            if (_gm           != null) Object.DestroyImmediate(_gm.gameObject);
            if (_containerGO  != null) Object.DestroyImmediate(_containerGO);
            if (_tubePrefabGO != null) Object.DestroyImmediate(_tubePrefabGO);
            if (_palette      != null) ScriptableObject.DestroyImmediate(_palette);
        }

        // ─── LoadLevel ───────────────────────────────────────────────

        [Test]
        [Description("Beklenen: LoadLevel geçerli veriyle null fırlatmamalı.")]
        public void LoadLevel_ValidLevel_DoesNotThrow()
        {
            var level = MakeLevel(
                new[] { new[] { BallColor.Red,  BallColor.Blue },
                        new[] { BallColor.Blue, BallColor.Red  } },
                emptyTubes: 2);

            Assert.DoesNotThrow(() => _gm.LoadLevel(level),
                "Hata: LoadLevel geçerli level ile exception fırlattı.");
        }

        [Test]
        [Description("Beklenen: LoadLevel sonrası MoveCount sıfır olmalı.")]
        public void LoadLevel_ResetsMoveCountToZero()
        {
            var level = MakeLevel(
                new[] { new[] { BallColor.Red } },
                emptyTubes: 1);

            _gm.LoadLevel(level);

            Assert.AreEqual(0, _gm.MoveCount,
                "Hata: LoadLevel sonrası MoveCount sıfıra sıfırlanmalı.");
        }

        [Test]
        [Description("Beklenen: LoadLevel OnLevelLoaded event'ini ateşlemeli.")]
        public void LoadLevel_FiresOnLevelLoadedEvent()
        {
            bool fired = false;
            _gm.OnLevelLoaded += () => fired = true;

            _gm.LoadLevel(MakeLevel(
                new[] { new[] { BallColor.Red } },
                emptyTubes: 1));

            Assert.IsTrue(fired,
                "Hata: LoadLevel sonrası OnLevelLoaded ateşlenmedi.");
        }

        // ─── TryMove — Geçerli Hamle ─────────────────────────────────

        [Test]
        [Description("Beklenen: uyumlu renk ve boş yer varken TryMove true döner.")]
        public void TryMove_ValidMove_ReturnsTrue()
        {
            LoadThreeRedPlusOneRed();

            bool result = _gm.TryMove(from: 1, to: 0);

            Assert.IsTrue(result,
                "Hata: Geçerli hamle reddedildi; kabul edilmeliydi.");
        }

        [Test]
        [Description("Beklenen: başarılı hamle MoveCount'u bir artırmalı.")]
        public void TryMove_SuccessfulMove_IncrementsMoveCount()
        {
            LoadThreeRedPlusOneRed();

            _gm.TryMove(from: 1, to: 0);

            Assert.AreEqual(1, _gm.MoveCount,
                "Hata: Hamle sonrası MoveCount 1 olmalı.");
        }

        // ─── TryMove — Engellenen Hamleler ───────────────────────────

        [Test]
        [Description("Beklenen: dolu tüpe TryMove false döner, MoveCount değişmez.")]
        public void TryMove_FullTargetTube_ReturnsFalse()
        {
            // Tube 0: [R,R,R,R] dolu | Tube 1: [Blue] | Tube 2: boş
            var level = MakeLevel(new[]
            {
                new[] { BallColor.Red, BallColor.Red, BallColor.Red, BallColor.Red },
                new[] { BallColor.Blue }
            }, emptyTubes: 1);
            _gm.LoadLevel(level);

            bool result = _gm.TryMove(from: 1, to: 0);

            Assert.IsFalse(result,
                "Hata: Dolu tüpe hamle kabul edildi; reddedilmeliydi.");
            Assert.AreEqual(0, _gm.MoveCount,
                "Hata: Başarısız hamlede MoveCount artmamalı.");
        }

        [Test]
        [Description("Beklenen: tamamlanmış (dolu+tek renk) tüpe hamle yapılamaz → false.")]
        public void TryMove_ToSolvedTube_ReturnsFalse()
        {
            // Tube 0: [R,R,R,R] solved (dolu+tek renk) | Tube 1: [Red] | Tube 2: boş
            var level = MakeLevel(new[]
            {
                new[] { BallColor.Red, BallColor.Red, BallColor.Red, BallColor.Red },
                new[] { BallColor.Red }
            }, emptyTubes: 1);
            _gm.LoadLevel(level);

            bool result = _gm.TryMove(from: 1, to: 0);

            Assert.IsFalse(result,
                "Hata: Çözülmüş (tam dolu) tüpe hamle kabul edildi; reddedilmeliydi.");
        }

        [Test]
        [Description("Beklenen: renk uyumsuzluğunda TryMove false döner.")]
        public void TryMove_IncompatibleColors_ReturnsFalse()
        {
            // Tube 0: [Red] | Tube 1: [Blue] | renk uyumsuz
            var level = MakeLevel(new[]
            {
                new[] { BallColor.Red  },
                new[] { BallColor.Blue }
            }, emptyTubes: 2);
            _gm.LoadLevel(level);

            bool result = _gm.TryMove(from: 1, to: 0);

            Assert.IsFalse(result,
                "Hata: Uyumsuz renkler arasında hamle kabul edildi.");
        }

        // ─── Undo ─────────────────────────────────────────────────────

        [Test]
        [Description("Beklenen: UndoLastMove hamle sayısını azaltmalı.")]
        public void UndoLastMove_DecrementsMoveCount()
        {
            LoadThreeRedPlusOneRed();
            _gm.TryMove(from: 1, to: 0);

            _gm.UndoLastMove();

            Assert.AreEqual(0, _gm.MoveCount,
                "Hata: UndoLastMove MoveCount'u düşürmedi.");
        }

        [Test]
        [Description("Beklenen: hamle geçmişi boşken UndoLastMove exception fırlatmamalı.")]
        public void UndoLastMove_EmptyStack_DoesNotThrow()
        {
            LoadThreeRedPlusOneRed();

            Assert.DoesNotThrow(() => _gm.UndoLastMove(),
                "Hata: Boş undo stack'te UndoLastMove exception fırlattı.");
        }

        [Test]
        [Description("Beklenen: hamle yokken UndoLastMove MoveCount'u değiştirmemeli.")]
        public void UndoLastMove_NoMoves_MoveCountStaysZero()
        {
            LoadThreeRedPlusOneRed();

            _gm.UndoLastMove();

            Assert.AreEqual(0, _gm.MoveCount,
                "Hata: Hamle yokken undo MoveCount'u bozdu.");
        }

        [Test]
        [Description("Beklenen: hamle yapıldıktan sonra CanUndo true olmalı.")]
        public void CanUndo_AfterMove_ReturnsTrue()
        {
            LoadThreeRedPlusOneRed();
            _gm.TryMove(from: 1, to: 0);

            Assert.IsTrue(_gm.CanUndo(),
                "Hata: Hamle yapıldıktan sonra CanUndo false döndü.");
        }

        [Test]
        [Description("Beklenen: hamle olmadan CanUndo false olmalı.")]
        public void CanUndo_NoMoves_ReturnsFalse()
        {
            LoadThreeRedPlusOneRed();

            Assert.IsFalse(_gm.CanUndo(),
                "Hata: Hamle olmadan CanUndo true döndü.");
        }

        // ─── Win Koşulu ───────────────────────────────────────────────

        [Test]
        [Description("Beklenen: tüm tüpler çözülünce OnLevelSolved event'i ateşlenmeli.")]
        public void WinCondition_AllSolved_FiresOnLevelSolvedEvent()
        {
            // Kurulum: Tube0=[R,R,R], Tube1=[R], Tube2-3=boş
            // Hamle: Red tube1→tube0 → Tube0=[R,R,R,R] solved, Tube1=boş → WIN
            LoadThreeRedPlusOneRed();

            bool winFired     = false;
            int  winMoveCount = -1;
            _gm.OnLevelSolved += (moves) => { winFired = true; winMoveCount = moves; };

            _gm.TryMove(from: 1, to: 0);

            Assert.IsTrue(winFired,
                "Hata: Tüm tüpler çözüldü ama OnLevelSolved ateşlenmedi.");
            Assert.AreEqual(1, winMoveCount,
                "Hata: Win event'i yanlış hamle sayısıyla ateşlendi.");
        }

        [Test]
        [Description("Beklenen: win sonrası _levelComplete kilidi CanUndo'yu false yapmalı.")]
        public void WinCondition_AfterWin_CanUndoReturnsFalse()
        {
            LoadThreeRedPlusOneRed();
            _gm.TryMove(from: 1, to: 0); // seviyeyi kazandır

            Assert.IsFalse(_gm.CanUndo(),
                "Hata: Seviye kazanıldıktan sonra CanUndo true döndü; " +
                "_levelComplete kilidi undo'yu engellemiyor.");
        }

        // ─── Kurulum Yardımcıları ─────────────────────────────────────

        /// <summary>
        /// Standart test seviyesi: Tube0=[R,R,R] Tube1=[R] Tube2-3=boş.
        /// Tek hamleyle kazanılabilir senaryodur.
        /// </summary>
        private void LoadThreeRedPlusOneRed()
        {
            _gm.LoadLevel(MakeLevel(
                new[]
                {
                    new[] { BallColor.Red, BallColor.Red, BallColor.Red },
                    new[] { BallColor.Red }
                },
                emptyTubes: 2));
        }

        // ─── Static Yardımcılar ───────────────────────────────────────

        private GameManager BuildGameManager()
        {
            var go = new GameObject("GameManager");
            var gm = go.AddComponent<GameManager>();

            SetField(gm, "_tubePrefab",    _tubePrefabGO);
            SetField(gm, "_tubeContainer", _containerGO.transform);
            SetField(gm, "_colorPalette",  _palette);

            return gm;
        }

        private static GameObject BuildTubePrefab()
        {
            var go = new GameObject("TubePrefab");
            go.AddComponent<BoxCollider2D>();
            go.AddComponent<Tube>();
            go.SetActive(false);  // Sahneye gizli; yalnızca Instantiate template'i
            return go;
        }

        private static LevelData MakeLevel(BallColor[][] tubeContents, int emptyTubes)
        {
            var tubes = new TubeJsonModel[tubeContents.Length];
            for (int i = 0; i < tubeContents.Length; i++)
            {
                tubes[i] = new TubeJsonModel { balls = new int[tubeContents[i].Length] };
                for (int j = 0; j < tubeContents[i].Length; j++)
                    tubes[i].balls[j] = (int)tubeContents[i][j];
            }

            return LevelData.CreateFromJson(new LevelJsonModel
            {
                levelNumber    = 99,
                levelName      = "Test Level",
                emptyTubeCount = emptyTubes,
                tubes          = tubes
            });
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
            throw new Exception($"[TestHelper] '{fieldName}' alanı {target.GetType().Name} sınıfında bulunamadı.");
        }
    }
}
