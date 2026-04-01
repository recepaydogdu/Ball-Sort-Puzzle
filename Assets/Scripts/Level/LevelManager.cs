using System;
using UnityEngine;

namespace BallSort.Level
{
    /// <summary>
    /// Seviye kataloğunu, oyuncu ilerleme kaydını ve seviyeler arası geçişi yönetir.
    /// İlerleme PlayerPrefs üzerinde saklanır; sahne geçişinde veri kaybolmaz.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        // ─── PlayerPrefs Anahtar Sabitleri ──────────────────────────
        private const string KeyProgress  = "bs_unlocked_";   // + index
        private const string KeyBestMoves = "bs_best_";        // + index
        private const string KeyLastLevel = "bs_last_level";

        // ─── Singleton ──────────────────────────────────────────────
        public static LevelManager Instance { get; private set; }

        // ─── Inspector Alanları ─────────────────────────────────────
        [Header("Seviye Kaynağı")]
        [Tooltip("Inspector'dan ScriptableObject asset'leri atayın. " +
                 "Boş bırakılırsa Resources/Levels/*.json otomatik yüklenir.")]
        [SerializeField] private LevelData[] _levels;

        // ─── Runtime ────────────────────────────────────────────────
        private int _currentIndex;

        // ─── Properties ─────────────────────────────────────────────

        /// <summary>Kataloğdaki toplam seviye sayısı.</summary>
        public int LevelCount => _levels?.Length ?? 0;

        /// <summary>Aktif seviyenin index'i (0 tabanlı).</summary>
        public int CurrentIndex => _currentIndex;

        /// <summary>Aktif LevelData. Katalog boşsa null.</summary>
        public LevelData CurrentLevel =>
            IsValidIndex(_currentIndex) ? _levels[_currentIndex] : null;

        // ─── Events ─────────────────────────────────────────────────

        /// <summary>Yeni bir seviye başlamadan önce ateşlenir.</summary>
        public event Action<int> OnLevelStarted;  // index

        // ─── Unity Lifecycle ────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeCatalog();
            _currentIndex = Mathf.Clamp(
                PlayerPrefs.GetInt(KeyLastLevel, 0), 0, Mathf.Max(0, LevelCount - 1));
        }

        private void OnEnable()
        {
            if (BallSort.GameManager.Instance != null)
                BallSort.GameManager.Instance.OnLevelSolved += HandleLevelSolved;
        }

        private void OnDisable()
        {
            if (BallSort.GameManager.Instance != null)
                BallSort.GameManager.Instance.OnLevelSolved -= HandleLevelSolved;
        }

        // ─── Katalog Başlatma ────────────────────────────────────────

        private void InitializeCatalog()
        {
            if (_levels != null && _levels.Length > 0) return;   // Inspector'dan atandı

            // Fallback: JSON'dan yükle
            _levels = LevelJsonLoader.LoadAllLevels();
            if (_levels.Length == 0)
                Debug.LogWarning("[LevelManager] Hiç level bulunamadı. " +
                                 "Resources/Levels/ klasörünü veya Inspector'daki diziyi kontrol edin.");
        }

        // ─── Yükleme ────────────────────────────────────────────────

        /// <summary>
        /// Belirtilen index'teki seviyeyi yükler.
        /// Seviye kilitliyse veya index geçersizse false döner.
        /// </summary>
        public bool LoadLevel(int index)
        {
            if (!IsValidIndex(index))   return false;
            if (!IsUnlocked(index))     return false;

            _currentIndex = index;
            PlayerPrefs.SetInt(KeyLastLevel, _currentIndex);

            OnLevelStarted?.Invoke(_currentIndex);
            BallSort.GameManager.Instance?.LoadLevel(_levels[_currentIndex]);
            return true;
        }

        /// <summary>
        /// Bir sonraki seviyeye geçer.
        /// Son seviyedeyse veya sonraki kilitliyse false döner.
        /// </summary>
        public bool LoadNextLevel() => LoadLevel(_currentIndex + 1);

        /// <summary>Mevcut seviyeyi sıfırdan başlatır.</summary>
        public void RestartCurrentLevel()
        {
            BallSort.GameManager.Instance?.LoadLevel(_levels[_currentIndex]);
        }

        // ─── İlerleme Sorgulama ──────────────────────────────────────

        /// <summary>Belirtilen seviyenin açık olup olmadığını döner.</summary>
        public bool IsUnlocked(int index)
        {
            if (index == 0) return true;
            return PlayerPrefs.GetInt(KeyProgress + index, 0) == 1;
        }

        /// <summary>
        /// En iyi hamle sayısını döner.
        /// Seviye henüz tamamlanmamışsa -1 döner.
        /// </summary>
        public int GetBestMoves(int index)
        {
            if (!IsValidIndex(index)) return -1;
            return PlayerPrefs.GetInt(KeyBestMoves + index, -1);
        }

        /// <summary>
        /// Tüm ilerleme kaydını siler. Yalnızca debug/test için kullanın.
        /// </summary>
        public void ResetAllProgress()
        {
            for (int i = 0; i < LevelCount; i++)
            {
                PlayerPrefs.DeleteKey(KeyProgress  + i);
                PlayerPrefs.DeleteKey(KeyBestMoves + i);
            }
            PlayerPrefs.DeleteKey(KeyLastLevel);
            PlayerPrefs.Save();
            _currentIndex = 0;
        }

        // ─── Olay İşleme ────────────────────────────────────────────

        private void HandleLevelSolved(int moveCount)
        {
            // En iyi hamle kaydını güncelle
            int best = GetBestMoves(_currentIndex);
            if (best < 0 || moveCount < best)
            {
                PlayerPrefs.SetInt(KeyBestMoves + _currentIndex, moveCount);
            }

            // Sonraki seviyeyi aç
            int next = _currentIndex + 1;
            if (IsValidIndex(next))
                PlayerPrefs.SetInt(KeyProgress + next, 1);

            PlayerPrefs.Save();
        }

        // ─── Yardımcılar ────────────────────────────────────────────

        private bool IsValidIndex(int i) => _levels != null && i >= 0 && i < _levels.Length;
    }
}
