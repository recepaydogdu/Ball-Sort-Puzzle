using NUnit.Framework;
using BallSort.Core;

namespace BallSort.Tests.EditMode
{
    /// <summary>
    /// MoveRecord struct'ının davranış testleri.
    /// Undo mekanizmasının doğru çalıştığını garanti eder.
    /// </summary>
    [TestFixture]
    public class MoveRecordTests
    {
        [Test]
        public void Constructor_SetsAllFieldsCorrectly()
        {
            var record = new MoveRecord(from: 2, to: 5, color: BallColor.Blue);

            Assert.AreEqual(2,              record.FromTubeIndex, "Hata: FromTubeIndex yanlış.");
            Assert.AreEqual(5,              record.ToTubeIndex,   "Hata: ToTubeIndex yanlış.");
            Assert.AreEqual(BallColor.Blue, record.MovedColor,    "Hata: MovedColor yanlış.");
        }

        [Test]
        [Description("Beklenen: Reversed() From↔To index'lerini yer değiştirmeli.")]
        public void Reversed_SwapsFromAndToIndices()
        {
            var original = new MoveRecord(from: 1, to: 4, color: BallColor.Red);
            var reversed = original.Reversed();

            Assert.AreEqual(4, reversed.FromTubeIndex,
                "Hata: Reversed().From orijinal To olmalı.");
            Assert.AreEqual(1, reversed.ToTubeIndex,
                "Hata: Reversed().To orijinal From olmalı.");
        }

        [Test]
        [Description("Beklenen: Reversed() MovedColor'ı değiştirmemeli.")]
        public void Reversed_PreservesMovedColor()
        {
            var original = new MoveRecord(from: 0, to: 3, color: BallColor.Green);
            var reversed = original.Reversed();

            Assert.AreEqual(BallColor.Green, reversed.MovedColor,
                "Hata: Reversed() rengi değiştirdi; değiştirmemeli.");
        }

        [Test]
        [Description("Beklenen: iki kez Reversed() uygulamak orijinali geri vermeli.")]
        public void DoubleReversed_EqualsOriginal()
        {
            var original     = new MoveRecord(from: 2, to: 4, color: BallColor.Purple);
            var doubleReversed = original.Reversed().Reversed();

            Assert.AreEqual(original.FromTubeIndex, doubleReversed.FromTubeIndex,
                "Hata: Çift Reversed() From'u bozdu.");
            Assert.AreEqual(original.ToTubeIndex,   doubleReversed.ToTubeIndex,
                "Hata: Çift Reversed() To'yu bozdu.");
            Assert.AreEqual(original.MovedColor,    doubleReversed.MovedColor,
                "Hata: Çift Reversed() rengi bozdu.");
        }

        [Test]
        [Description("Beklenen: ToString() rengi ve tube index'lerini içermeli.")]
        public void ToString_ContainsColorAndIndices()
        {
            var record = new MoveRecord(from: 1, to: 2, color: BallColor.Orange);
            var str    = record.ToString();

            StringAssert.Contains("Orange", str, "Hata: ToString rengi içermiyor.");
            StringAssert.Contains("1",      str, "Hata: ToString From index'ini içermiyor.");
            StringAssert.Contains("2",      str, "Hata: ToString To index'ini içermiyor.");
        }
    }
}
