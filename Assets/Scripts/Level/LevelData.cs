using System;
using UnityEngine;
using BallSort.Core;

namespace BallSort.Level
{
    /// <summary>
    /// Tek bir seviyenin tüm başlangıç verilerini tutan ScriptableObject.
    /// Inspector: Create → BallSort → Level Data
    /// Kod: <see cref="CreateFromJson"/> factory ile runtime'da da üretilebilir.
    /// </summary>
    [CreateAssetMenu(fileName = "Level_000", menuName = "BallSort/Level Data")]
    public class LevelData : ScriptableObject
    {
        // ─── İç Tipler ──────────────────────────────────────────────

        [Serializable]
        public class TubeSetup
        {
            [Tooltip("Alt'tan üste doğru topların renkleri (max 4)")]
            public BallColor[] balls;
        }

        // ─── Inspector Alanları ─────────────────────────────────────

        [Header("Seviye Kimliği")]
        [SerializeField] private int    _levelNumber;
        [SerializeField] private string _levelName;

        [Header("Yapılandırma")]
        [SerializeField] private int         _emptyTubeCount = 2;
        [SerializeField] private TubeSetup[] _tubePlacements;

        // ─── Properties ─────────────────────────────────────────────

        /// <summary>Seviye numarası (1 tabanlı).</summary>
        public int LevelNumber => _levelNumber;

        /// <summary>Görüntülenecek seviye adı. Boşsa otomatik üretilir.</summary>
        public string LevelName =>
            string.IsNullOrEmpty(_levelName) ? $"Level {_levelNumber}" : _levelName;

        /// <summary>Başlangıçta boş olan yedek tüp sayısı.</summary>
        public int EmptyTubeCount => _emptyTubeCount;

        /// <summary>Toplam tüp sayısı (dolu + boş).</summary>
        public int TotalTubeCount => (_tubePlacements?.Length ?? 0) + _emptyTubeCount;

        /// <summary>Dolu tüplerin başlangıç top dizilimi (salt okunur).</summary>
        public TubeSetup[] TubePlacements => _tubePlacements;

        // ─── Doğrulama ──────────────────────────────────────────────

        /// <summary>
        /// Seviye verisinin tutarlılığını denetler.
        /// Yükleme öncesinde ve Editor'da çağrılır.
        /// </summary>
        public bool Validate(out string error)
        {
            if (_tubePlacements == null || _tubePlacements.Length == 0)
            { error = "TubePlacements boş."; return false; }

            foreach (var setup in _tubePlacements)
            {
                if (setup.balls == null)
                { error = "Bir tüpte balls dizisi null."; return false; }

                if (setup.balls.Length > TubeData.DefaultCapacity)
                {
                    error = $"Tüp en fazla {TubeData.DefaultCapacity} top alabilir; " +
                            $"girilen: {setup.balls.Length}.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        // ─── Runtime Factory ────────────────────────────────────────

        /// <summary>
        /// JSON modelinden runtime LevelData instance'ı üretir.
        /// ScriptableObject.CreateInstance kullanır; asset dosyası oluşturmaz.
        /// </summary>
        public static LevelData CreateFromJson(LevelJsonModel model)
        {
            var instance             = CreateInstance<LevelData>();
            instance._levelNumber    = model.levelNumber;
            instance._levelName      = model.levelName;
            instance._emptyTubeCount = model.emptyTubeCount;

            if (model.tubes != null)
            {
                instance._tubePlacements = new TubeSetup[model.tubes.Length];
                for (int i = 0; i < model.tubes.Length; i++)
                {
                    var balls = model.tubes[i].balls ?? Array.Empty<int>();
                    var setup = new TubeSetup();
                    setup.balls = new BallColor[balls.Length];
                    for (int j = 0; j < balls.Length; j++)
                        setup.balls[j] = (BallColor)balls[j];
                    instance._tubePlacements[i] = setup;
                }
            }

            return instance;
        }
    }

    // ─── JSON Veri Modelleri ─────────────────────────────────────────
    // JsonUtility için aynı dosyada tutulur; bağımlılık eklenmez.

    [Serializable]
    public class LevelJsonModel
    {
        public int             levelNumber;
        public string          levelName;
        public int             emptyTubeCount;
        public TubeJsonModel[] tubes;
    }

    [Serializable]
    public class TubeJsonModel
    {
        /// <summary>Alt'tan üste BallColor int değerleri.</summary>
        public int[] balls;
    }
}
